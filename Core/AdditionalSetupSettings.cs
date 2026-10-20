using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Xml;
using Schneegans.Unattend;
namespace UnattendLocal.Core;

// Payloads and execution stages come from the fixed generator. Import only complete native groups.
public static class AdditionalSetupSettings
{
    private const string Root = @"C:\Windows\Setup\Scripts\";
    private const string BlockPattern = @"(?m)^\t\{\n(?<body>(?:\t\t[^\n]*\n)*)\t\};\n";
    public record Item(string Property, string Label, string HelpId, string[] Markers);
    public static IReadOnlyList<Item> Items { get; } = new Item[]
    {
        new(nameof(InstallPlan.UseNarrator), "讲述人辅助安装", "narrator", ["Narrator.exe", "/d narrator"]),
        new(nameof(InstallPlan.MakeEdgeUninstallable), "允许卸载 Edge", "edgeuninstall", ["MakeEdgeUninstallable.ps1", "1bca278a-5d11-4acf-ad2f-f9ab6d7f93a6"]),
        new(nameof(InstallPlan.DisableSystemRestore), "C 盘系统还原", "systemrestore", ["Disable-ComputerRestore"])
    };
    public static string Description(string property) => property switch
    {
        nameof(InstallPlan.UseNarrator) => "默认不勾选。启用后在 Windows PE 安装环境启动讲述人，在系统安装阶段和每位用户首次登录时启动，并配置相应的辅助功能注册表项。适合需要屏幕朗读辅助的安装；需要安装介质包含讲述人及可用语音、音频支持，不保证定制 PE 可用。不会自动关闭朗读，也不强制开启首次自动登录；安装后可在辅助功能设置中调整。取消只影响新生成配置，不关闭已安装系统的讲述人；本工具不启动当前电脑的讲述人。",
        nameof(InstallPlan.MakeEdgeUninstallable) => "默认不勾选。启用后在目标系统安装阶段尝试修改 IntegratedServicesRegionPolicySet.json 中 Edge 对应策略的 defaultState，使支持此机制的 Windows 版本提供卸载入口。不实际卸载 Edge，不移除 WebView2，也不更改默认浏览器。系统版本、策略文件权限及后续更新可能使修改失败或恢复原值，不能保证所有版本可卸载；脚本失败时记录错误并继续。取消只影响新配置，不还原已安装系统的策略文件；本工具不修改当前电脑。",
        nameof(InstallPlan.DisableSystemRestore) => "默认不勾选。启用后在目标 Windows 的全机首次登录阶段执行 Disable-ComputerRestore -Drive 'C:\\'，关闭 C 盘系统还原。关闭系统还原可能删除该盘已有还原点，并失去通过还原点回退系统设置的能力；请按需要保留恢复手段。这不是仅暂停创建还原点，不关闭其他盘，也不等于关闭 Windows 恢复环境、文件历史记录或所有备份。不强制开启首次自动登录；需实际执行首次登录脚本。取消只影响新生成配置，不重新启用已安装系统的系统还原；本工具不修改当前电脑。",
        _ => throw new ArgumentException("未知安装选项。")
    };
    public static bool Selected(InstallPlan p, Item item) => (bool)typeof(InstallPlan).GetProperty(item.Property)!.GetValue(p)!;
    public static string Summary(InstallPlan p) => "讲述人辅助安装：" + (p.UseNarrator ? "启用（PE、安装阶段及每位用户首次登录）" : "不添加启动命令") +
        "\nEdge 卸载入口：" + (p.MakeEdgeUninstallable ? "尝试允许卸载；不实际卸载" : "遵循 Windows 默认行为") +
        "\nC 盘系统还原：" + (p.DisableSystemRestore ? "首次登录时关闭；可能删除已有还原点" : "遵循 Windows 默认行为");
    public static string HelpState(InstallPlan p, string id)
    {
        var item = Items.Single(i => i.HelpId == id);
        return "当前：" + (Selected(p, item) ? item.Property switch
        {
            nameof(InstallPlan.UseNarrator) => "启用讲述人辅助安装。",
            nameof(InstallPlan.MakeEdgeUninstallable) => "尝试提供 Edge 卸载入口，不实际卸载。",
            _ => "首次登录时关闭 C 盘系统还原，可能删除已有还原点。"
        } : "不添加此项配置，遵循 Windows 默认行为。");
    }
    private static string Normal(string value) => string.Join('\n', value.Replace("\r\n", "\n").Split('\n').Select(l => l.Trim()).Where(l => l.Length > 0));
    private static string Style(string value) => value.Replace("Hidden", "Normal", StringComparison.Ordinal);
    private static string Skeleton(string body) => Normal(Regex.Replace(body.Replace("\r\n", "\n"), BlockPattern, "", RegexOptions.None, TimeSpan.FromSeconds(1)));
    private static string[] Blocks(string body) => Regex.Matches(body.Replace("\r\n", "\n"), BlockPattern, RegexOptions.None, TimeSpan.FromSeconds(1)).Select(m => Normal(m.Groups["body"].Value)).ToArray();
    private static XmlElement[] Files(XmlDocument doc) => doc.SelectNodes("//*[local-name()='File']")!.Cast<XmlElement>().ToArray();
    private static XmlNamespaceManager Ns(XmlDocument doc) { var ns = new XmlNamespaceManager(doc.NameTable); ns.AddNamespace("u", XmlMerge.Namespace); return ns; }
    private static string[] Commands(XmlDocument doc, string pass) => doc.SelectNodes("//u:settings[@pass='" + pass + "']//u:Path|//u:settings[@pass='" + pass + "']//u:CommandLine", Ns(doc))!.Cast<XmlElement>().Select(n => n.InnerText).ToArray();
    private record Template(XmlDocument Document, Dictionary<string, string[]> FeatureBlocks, string[] PeCommands);
    private static readonly Lazy<Template> Expected = new(() =>
    {
        var generator = new UnattendGenerator();
        var doc = generator.GenerateXml(Configuration.Default with { UseNarrator = true, MakeEdgeUninstallable = true, DisableSystemRestore = true });
        var blocks = new Dictionary<string, string[]>();
        foreach (var file in Files(doc).Where(f => f.GetAttribute("path").EndsWith(".ps1", StringComparison.Ordinal)))
            blocks[file.GetAttribute("path")] = Blocks(file.InnerText);
        return new(doc, blocks, Commands(doc, "windowsPE").Where(c => c.Contains("Narrator.exe", StringComparison.OrdinalIgnoreCase)).ToArray());
    });
    private static bool ExtractionActive(XmlDocument doc)
    {
        var actual = doc.SelectNodes("//*[local-name()='ExtractScript']")!.Cast<XmlElement>().ToArray();
        var expected = Expected.Value.Document;
        string call = Commands(expected, "specialize").Single(c => c.Contains("Extensions.ExtractScript", StringComparison.Ordinal));
        var mentions = Commands(doc, "specialize").Where(c => c.Contains("Extensions.ExtractScript", StringComparison.OrdinalIgnoreCase)).ToArray();
        return actual.Length == 1 && Normal(actual[0].InnerText) == Normal(expected.SelectSingleNode("//*[local-name()='ExtractScript']")!.InnerText) && mentions.Length == 1 && Style(mentions[0]) == Style(call);
    }
    internal static string[] KnownBlocks(XmlDocument doc, string name)
    {
        doc=XmlImportPaths.Inspection(doc);
        string path = Root + name;
        var stage = Files(doc).Where(f => f.GetAttribute("path").Equals(path, StringComparison.OrdinalIgnoreCase)).ToArray();
        var reference = Files(Expected.Value.Document).Single(f => f.GetAttribute("path") == path);
        if (stage.Length != 1 || !ExtractionActive(doc) || Skeleton(stage[0].InnerText) != Skeleton(reference.InnerText)) return [];
        if (name == "UserOnce.ps1")
        {
            var defaultBlocks = KnownBlocks(doc, "DefaultUser.ps1");
            string registration = Expected.Value.FeatureBlocks[Root + "DefaultUser.ps1"].Single(b => b.Contains("UserOnce.ps1", StringComparison.Ordinal));
            var refs = Files(doc).SelectMany(f => Normal(f.InnerText).Split('\n')).Concat(Commands(doc, "specialize")).Where(l => l.Contains("UserOnce.ps1", StringComparison.OrdinalIgnoreCase)).ToArray();
            if (defaultBlocks.Count(b => Style(b) == Style(registration)) != 1 || refs.Length != 1 || Style(refs[0]) != Style(registration)) return [];
        }
        else
        {
            string pass = name == "FirstLogon.ps1" ? "oobeSystem" : "specialize";
            string call = Commands(Expected.Value.Document, pass).Single(c => c.Contains(path, StringComparison.Ordinal));
            var commands = Commands(doc, pass);
            var mentions = commands.Where(c => c.Contains(name, StringComparison.OrdinalIgnoreCase)).ToArray();
            if (mentions.Length != 1 || Style(mentions[0]) != Style(call)) return [];
            if (name == "DefaultUser.ps1")
            {
                var chain = Commands(Expected.Value.Document, "specialize").Where(c => c.Contains("HKU\\DefaultUser", StringComparison.Ordinal) || c.Contains("DefaultUser.ps1", StringComparison.Ordinal)).Select(Style).ToArray();
                var actualChain = commands.Where(c => c.Contains("HKU\\DefaultUser", StringComparison.OrdinalIgnoreCase) || c.Contains("DefaultUser.ps1", StringComparison.OrdinalIgnoreCase)).Select(Style).ToArray();
                if (!actualChain.SequenceEqual(chain)) return [];
            }
        }
        return Blocks(stage[0].InnerText);
    }
    internal static bool IsNarratorPeCommand(string command) => Expected.Value.PeCommands.Contains(command, StringComparer.Ordinal);
    public record State(bool Enabled, bool Ambiguous);
    public static Dictionary<string, State> Inspect(XmlDocument doc)
    {
        var expected = Expected.Value;
        var files = Files(doc);
        var lines = files.SelectMany(f => Normal(f.InnerText).Split('\n')).Concat(Commands(doc, "windowsPE")).Concat(Commands(doc, "specialize")).Concat(Commands(doc, "oobeSystem")).ToArray();
        var knownBlocks = new[] { "Specialize.ps1", "FirstLogon.ps1", "UserOnce.ps1" }.ToDictionary(n => Root + n, n => KnownBlocks(doc, n));
        var result = new Dictionary<string, State>();
        foreach (var item in Items)
        {
            bool Related(string text) => item.Markers.Any(m => text.Contains(m, StringComparison.OrdinalIgnoreCase));
            var needed = expected.FeatureBlocks.SelectMany(kv => kv.Value.Where(Related).Select(b => (Path: kv.Key, Block: b))).Where(b => b.Path != Root + "MakeEdgeUninstallable.ps1").ToArray();
            var relevant = lines.Where(Related).ToArray();
            var expectedLines = needed.SelectMany(b => b.Block.Split('\n')).Where(Related).ToList();
            bool enabled = needed.All(b => knownBlocks[b.Path].Count(v => v == b.Block) == 1);
            bool mentioned = relevant.Length > 0;
            if (item.Property == nameof(InstallPlan.UseNarrator))
            {
                var pe = Commands(doc, "windowsPE").Where(Related).ToArray();
                enabled &= pe.SequenceEqual(expected.PeCommands);
                expectedLines.AddRange(expected.PeCommands);
            }
            if (item.Property == nameof(InstallPlan.MakeEdgeUninstallable))
            {
                const string helper = Root + "MakeEdgeUninstallable.ps1";
                var actual = files.Where(f => f.GetAttribute("path").Equals(helper, StringComparison.OrdinalIgnoreCase)).ToArray();
                string body = Files(expected.Document).Single(f => f.GetAttribute("path") == helper).InnerText;
                enabled &= actual.Length == 1 && Normal(actual[0].InnerText) == Normal(body);
                mentioned |= files.Any(f => f.GetAttribute("path").EndsWith("MakeEdgeUninstallable.ps1", StringComparison.OrdinalIgnoreCase));
                expectedLines.AddRange(Normal(body).Split('\n').Where(Related));
            }
            enabled &= relevant.Length == expectedLines.Count && expectedLines.All(l => relevant.Count(v => v == l) == expectedLines.Count(v => v == l));
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
            if (states[item.Property].Ambiguous) notes.Add(item.Label + "脚本或执行管线未完整识别，原文保留；不能用此开关覆盖。");
        }
    }
    public static void ValidateImported(InstallPlan p, XmlDocument doc)
    {
        var states = Inspect(doc);
        foreach (var item in Items)
            if (Selected(p, item) && states[item.Property].Ambiguous)
                throw new ArgumentException(item.Label + "与原 XML 中未完整识别的内容重叠，请用原工具调整后重新导入。");
    }
    public static void NormalizeLoaded(InstallPlan p, JsonElement json)
    {
        if (p.ImportedXml is not { OriginalOmitted: false } data || data.SourceXml.Length == 0 || data.BaselineXml.Length == 0) return;
        var missing = Items.Where(i => !json.TryGetProperty(i.Property, out _)).ToArray();
        if (missing.Length == 0) return;
        var states = Inspect(XmlMerge.Read(data.SourceXml));
        var before = JsonSerializer.Deserialize<InstallPlan>(JsonSerializer.Serialize(p))!;
        before.ImportedXml = null; before.ConfirmWipeDisk = before.ConfirmFormatPartition = before.ConfirmDiskScript = true;
        foreach (var item in missing)
        {
            typeof(InstallPlan).GetProperty(item.Property)!.SetValue(p, states[item.Property].Enabled);
            if (states[item.Property].Ambiguous) data.Notices = data.Notices.Append(item.Label + "未完整识别，原文保留，不能覆盖。").Distinct().ToArray();
            else if (states[item.Property].Enabled) data.Notices = data.Notices.Where(n => !n.Contains("生成参数：" + item.Property + "；", StringComparison.Ordinal)).ToArray();
        }
        if (!missing.Any(i => states[i.Property].Enabled)) return;
        var after = JsonSerializer.Deserialize<InstallPlan>(JsonSerializer.Serialize(before))!;
        foreach (var item in missing) typeof(InstallPlan).GetProperty(item.Property)!.SetValue(after, states[item.Property].Enabled);
        try
        {
            var service = new GeneratorService();
            data.BaselineXml = XmlMerge.Apply(data.BaselineXml, Encoding.UTF8.GetString(service.GenerateImportBaseline(before)), XmlMerge.Read(Encoding.UTF8.GetString(service.GenerateImportBaseline(after)))).OuterXml;
        }
        catch (Exception e) when (e is ArgumentException or ConfigurationException or XmlException)
        {
            data.Blockers = data.Blockers.Append("旧方案的新增安装选项编辑基准无法可靠迁移，请重新导入原 XML 后再导出。").Distinct().ToArray();
        }
    }
}
