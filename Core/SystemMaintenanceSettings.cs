using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Xml;
using Schneegans.Unattend;
namespace UnattendLocal.Core;

// Use the fixed upstream generator for both payloads and inspect actual XML before editing.
public static class SystemMaintenanceSettings
{
    private const string Root = @"C:\Windows\Setup\Scripts\";
    private const string StagePath = Root + "Specialize.ps1";
    private const string TaskPath = Root + "MoveActiveHours.xml";
    private const string BlockPattern = @"(?m)^\t\{\n(?<body>(?:\t\t[^\n]*\n)*)\t\};\n";
    public record Item(string Property, string Label, string[] Commands, string[] Markers, string? AuxiliaryPath = null);
    public static IReadOnlyList<Item> Items { get; } = new Item[]
    {
        new(nameof(InstallPlan.DisableLastAccess), "NTFS 最后访问时间", [
            "fsutil.exe behavior set disableLastAccess 1;"
        ], ["disableLastAccess", "NtfsDisableLastAccessUpdate"]),
        new(nameof(InstallPlan.PreventAutomaticReboot), "Windows 更新自动重启", [
            "reg.exe add \"HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows\\WindowsUpdate\\AU\" /v AUOptions /t REG_DWORD /d 4 /f;",
            "reg.exe add \"HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows\\WindowsUpdate\\AU\" /v NoAutoRebootWithLoggedOnUsers /t REG_DWORD /d 1 /f;",
            "Register-ScheduledTask -TaskName 'MoveActiveHours' -Xml $( Get-Content -LiteralPath 'C:\\Windows\\Setup\\Scripts\\MoveActiveHours.xml' -Raw );"
        ], ["AUOptions", "NoAutoRebootWithLoggedOnUsers", "MoveActiveHours", "ActiveHoursStart", "ActiveHoursEnd", "SmartActiveHoursState"], TaskPath)
    };
    public const string RestartDescription = "在目标 Windows 安装阶段沿用原生成器的设置：配置 AUOptions=4、NoAutoRebootWithLoggedOnUsers=1，并注册 MoveActiveHours 计划任务，定期调整 Windows 更新活动时间。此项不停止更新下载与安装，与循环暂停更新独立。Windows 版本、组织策略、更新截止期限或用户安排的重启可能覆盖，不保证阻止所有重启；请主动安排重启完成更新。未勾选时遵循 Windows 默认行为。取消只影响新生成配置，不删除已安装系统的任务或策略，也不修改当前电脑。";
    public const string LastAccessDescription = "在目标 Windows 安装阶段沿用原生成器的 fsutil.exe behavior set disableLastAccess 1，禁用 NTFS 最后访问时间更新。创建时间、修改时间及文件内容不变，不删除文件；依赖最后访问时间的归档、备份或清理规则可能受影响。未勾选时不写入覆盖，遵循 Windows 默认行为。取消只影响新生成配置，不恢复已安装系统的设置；本工具不在当前电脑执行该命令。";
    private static string Normal(string body) => string.Join('\n', body.Replace("\r\n", "\n").Split('\n').Select(l => l.Trim()).Where(l => l.Length > 0));
    private static string WindowStyle(string command) => Regex.Replace(command, "-WindowStyle \"(?:Hidden|Normal)\"", "-WindowStyle \"Hidden\"", RegexOptions.None, TimeSpan.FromSeconds(1));
    private static string Skeleton(string body) => Normal(Regex.Replace(body.Replace("\r\n", "\n"), BlockPattern, "", RegexOptions.None, TimeSpan.FromSeconds(1)));
    private record Template(string Wrapper, string Task, string Extraction, string ExtractionCommand);
    private static readonly Lazy<Template> Expected = new(() =>
    {
        var doc = new UnattendGenerator().GenerateXml(Configuration.Default with { DisableLastAccess = true, PreventAutomaticReboot = true });
        var files = doc.SelectNodes("//*[local-name()='File']")!.Cast<XmlElement>();
        string extraction = doc.SelectSingleNode("//*[local-name()='ExtractScript']")!.InnerText;
        string extractionCommand = doc.SelectNodes("//*[local-name()='Path']")!.Cast<XmlElement>().Single(p => p.InnerText.Contains("Extensions.ExtractScript", StringComparison.Ordinal)).InnerText;
        return new(Skeleton(files.Single(f => f.GetAttribute("path") == StagePath).InnerText),
            Normal(files.Single(f => f.GetAttribute("path") == TaskPath).InnerText), Normal(extraction), extractionCommand);
    });
    // Shared by the audit reader; rejects altered, duplicated, or inactive native execution pipelines.
    internal static string[] KnownSpecializeBlocks(XmlDocument doc)
    {
        var expected = Expected.Value;
        var files = doc.SelectNodes("//*[local-name()='File']")!.Cast<XmlElement>().ToArray();
        var stage = files.Where(f => f.GetAttribute("path").Equals(StagePath, StringComparison.OrdinalIgnoreCase)).ToArray();
        var ns = new XmlNamespaceManager(doc.NameTable); ns.AddNamespace("u", XmlMerge.Namespace);
        var paths = doc.SelectNodes("//u:settings[@pass='specialize']//u:Path", ns)!.Cast<XmlElement>().ToArray();
        var extraction = doc.SelectNodes("//*[local-name()='ExtractScript']")!.Cast<XmlElement>().ToArray();
        bool extracted = extraction.Length == 1 && Normal(extraction[0].InnerText) == expected.Extraction && paths.Count(p => WindowStyle(p.InnerText) == WindowStyle(expected.ExtractionCommand)) == 1;
        bool active = paths.Count(p => Regex.IsMatch(p.InnerText, "^powershell\\.exe -WindowStyle \"(?:Hidden|Normal)\" -ExecutionPolicy \"Unrestricted\" -NoProfile -File \"" + Regex.Escape(StagePath) + "\"$", RegexOptions.IgnoreCase, TimeSpan.FromSeconds(1))) == 1;
        bool knownStage = NativeScriptPipeline.StageActive(doc, "Specialize.ps1") && extracted && stage.Length == 1 && active && Skeleton(stage[0].InnerText) == expected.Wrapper;
        return knownStage ? Regex.Matches(stage[0].InnerText.Replace("\r\n", "\n"), BlockPattern, RegexOptions.None, TimeSpan.FromSeconds(1)).Select(m => Normal(m.Groups["body"].Value)).ToArray() : [];
    }
    public record State(bool Enabled, bool Ambiguous);
    public static Dictionary<string, State> Inspect(XmlDocument doc)
    {
        var expected = Expected.Value;
        var files = doc.SelectNodes("//*[local-name()='File']")!.Cast<XmlElement>().ToArray();
        var ns = new XmlNamespaceManager(doc.NameTable); ns.AddNamespace("u", XmlMerge.Namespace);
        var paths = doc.SelectNodes("//u:settings[@pass='specialize']//u:Path", ns)!.Cast<XmlElement>().ToArray();
        var blocks = KnownSpecializeBlocks(doc);
        var result = new Dictionary<string, State>();
        foreach (var item in Items)
        {
            var auxiliary = item.AuxiliaryPath == null ? [] : files.Where(f => f.GetAttribute("path").Equals(item.AuxiliaryPath, StringComparison.OrdinalIgnoreCase)).ToArray();
            bool validAuxiliary = item.AuxiliaryPath == null || (auxiliary.Length == 1 && Normal(auxiliary[0].InnerText) == expected.Task);
            var relevant = files.Where(f => !auxiliary.Contains(f)).SelectMany(f => Normal(f.InnerText).Split('\n')).Concat(paths.Select(p => p.InnerText)).Where(line => item.Markers.Any(marker => line.Contains(marker, StringComparison.OrdinalIgnoreCase))).ToArray();
            bool mentioned = relevant.Length > 0 || auxiliary.Length > 0;
            // Upstream appends the two restart policies as one block, then task registration as another.
            var commandGroups = item.AuxiliaryPath == null ? new[] { item.Commands } : new[] { item.Commands[..2], item.Commands[2..] };
            bool enabled = validAuxiliary && commandGroups.All(group => blocks.Count(b => b == Normal(string.Join('\n', group))) == 1) && relevant.Length == item.Commands.Length && item.Commands.All(c => relevant.Count(line => line == Normal(c)) == 1);
            result[item.Property] = new(enabled, mentioned && !enabled);
        }
        return result;
    }
    public static void ReadIntoPlan(XmlDocument doc, InstallPlan p, List<string> notes)
    {
        var states = Inspect(doc);
        foreach (var item in Items)
        {
            typeof(InstallPlan).GetProperty(item.Property)!.SetValue(p, states[item.Property].Enabled);
            if (states[item.Property].Ambiguous) notes.Add(item.Label + "命令、任务或执行管线未完整识别，原文保留；不能用此开关覆盖。");
        }
    }
    public static void ValidateImported(InstallPlan p, XmlDocument doc)
    {
        var states = Inspect(doc);
        foreach (var item in Items)
            if (states[item.Property].Ambiguous && (bool)typeof(InstallPlan).GetProperty(item.Property)!.GetValue(p)!)
                throw new ArgumentException(item.Label + "与原 XML 中未完整识别的内容重叠，请用原工具调整后重新导入。");
    }
    public static string Summary(InstallPlan p) => "Windows 更新自动重启：" + (p.PreventAutomaticReboot ? "减少自动重启（MoveActiveHours 任务及策略；不保证阻止全部重启）" : "遵循 Windows 默认行为") + "\nNTFS 最后访问时间：" + (p.DisableLastAccess ? "禁用更新；创建与修改时间不变" : "遵循 Windows 默认行为");
    public static void NormalizeLoaded(InstallPlan p, JsonElement json)
    {
        if (p.ImportedXml is not { OriginalOmitted: false } data || data.SourceXml.Length == 0 || data.BaselineXml.Length == 0) return;
        var missing = Items.Where(i => !json.TryGetProperty(i.Property, out _)).ToArray(); if (missing.Length == 0) return;
        var states = Inspect(XmlMerge.Read(data.SourceXml));
        var before = JsonSerializer.Deserialize<InstallPlan>(JsonSerializer.Serialize(p))!;
        before.ImportedXml = null; before.ConfirmWipeDisk = before.ConfirmFormatPartition = before.ConfirmDiskScript = true;
        foreach (var item in missing)
        {
            typeof(InstallPlan).GetProperty(item.Property)!.SetValue(p, states[item.Property].Enabled);
            if (states[item.Property].Ambiguous) data.Notices = data.Notices.Append(item.Label + "未完整识别，原文保留，不能用此开关覆盖。").Distinct().ToArray();
        }
        if (!missing.Any(i => states[i.Property].Enabled)) return;
        var after = JsonSerializer.Deserialize<InstallPlan>(JsonSerializer.Serialize(before))!;
        foreach (var item in missing) typeof(InstallPlan).GetProperty(item.Property)!.SetValue(after, states[item.Property].Enabled);
        try
        {
            var g = new GeneratorService();
            data.BaselineXml = XmlMerge.Apply(data.BaselineXml, Encoding.UTF8.GetString(g.GenerateImportBaseline(before)), XmlMerge.Read(Encoding.UTF8.GetString(g.GenerateImportBaseline(after)))).OuterXml;
        }
        catch (Exception e) when (e is ArgumentException or ConfigurationException or XmlException)
        {
            data.Blockers = data.Blockers.Append("旧方案的更新重启/NTFS 设置编辑基准无法可靠迁移，请重新导入原 XML 后再导出。").Distinct().ToArray();
        }
    }
}
