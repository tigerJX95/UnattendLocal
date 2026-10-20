using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Xml;

namespace UnattendLocal.Core;

public static class CommonSystemSettings
{
    public record Item(string Property, string Label, string File, string[] Commands);
    public static IReadOnlyList<Item> Items { get; } = new Item[]
    {
        new(nameof(InstallPlan.DisableAutomaticRestartSignOn), "重启后自动登录", "Specialize.ps1", [
            "reg.exe add \"HKLM\\Software\\Microsoft\\Windows\\CurrentVersion\\Policies\\System\" /v \"DisableAutomaticRestartSignOn\" /t REG_DWORD /d 1 /f;"
        ]),
        new(nameof(InstallPlan.PreventDeviceApps), "硬件关联应用", "Specialize.ps1", [
            "reg.exe add \"HKLM\\Software\\Microsoft\\Windows\\CurrentVersion\\Device Metadata\" /v \"PreventDeviceMetadataFromNetwork\" /t REG_DWORD /d 1 /f;",
            "reg.exe add \"HKLM\\Software\\Microsoft\\Windows\\CurrentVersion\\Device Installer\" /v \"DisableCoInstallers\" /t REG_DWORD /d 1 /f;"
        ]),
        new(nameof(InstallPlan.DeleteWindowsOld), "空 Windows.old 目录", "FirstLogon.ps1", [
            "cmd.exe /c \"rmdir C:\\Windows.old\";"
        ])
    };
    private static string Normalize(string line) => Regex.Replace(line.Trim(), @"\s+", " ");
    private static Dictionary<string, (bool Enabled, bool Ambiguous)> Read(XmlDocument doc, List<string> notes)
    {
        var files = doc.SelectNodes("//*[local-name()='File']")!.Cast<XmlElement>().ToArray();
        var result = new Dictionary<string, (bool Enabled, bool Ambiguous)>();
        foreach (var item in Items)
        {
            var matches = files.Where(f => f.GetAttribute("path").Equals("C:\\Windows\\Setup\\Scripts\\" + item.File, StringComparison.OrdinalIgnoreCase)).ToArray();
            var lines = matches.SelectMany(f => f.InnerText.Replace("\r\n", "\n").Split('\n')).Select(Normalize).ToArray();
            var markers = item.Property switch
            {
                nameof(InstallPlan.DisableAutomaticRestartSignOn) => new[] { "DisableAutomaticRestartSignOn" },
                nameof(InstallPlan.PreventDeviceApps) => new[] { "PreventDeviceMetadataFromNetwork", "DisableCoInstallers" },
                _ => new[] { "Windows.old" }
            };
            var relevant = lines.Where(l =>
                (l.StartsWith("reg.exe ", StringComparison.OrdinalIgnoreCase) || l.StartsWith("cmd.exe /c ", StringComparison.OrdinalIgnoreCase)) &&
                markers.Any(m => l.Contains(m, StringComparison.OrdinalIgnoreCase))).ToArray();
            // Require the known complete command group. Partial or altered payloads remain untouched.
            bool enabled = matches.Length == 1 && relevant.Length == item.Commands.Length &&
                item.Commands.All(c => relevant.Count(l => l.Equals(Normalize(c), StringComparison.OrdinalIgnoreCase)) == 1);
            bool ambiguous = relevant.Length > 0 && !enabled;
            result[item.Property] = (enabled, ambiguous);
            if (ambiguous)
                notes.Add(item.Label + "含部分、修改过或重复的脚本，未作为完整开关还原；原文保留，暂不能用该开关覆盖。");
        }
        return result;
    }
    public static Dictionary<string, bool> Inspect(XmlDocument doc, List<string> notes) =>
        Read(doc, notes).ToDictionary(pair => pair.Key, pair => pair.Value.Enabled);
    public static void ValidateImported(InstallPlan plan, XmlDocument source)
    {
        var states = Read(source, new());
        foreach (var item in Items)
            if (states[item.Property].Ambiguous && (bool)typeof(InstallPlan).GetProperty(item.Property)!.GetValue(plan)!)
                throw new ArgumentException(item.Label + "与原 XML 中未完整识别的脚本重叠，不能可靠覆盖；请用原工具调整后重新导入。");
    }
    public static void ReadIntoPlan(XmlDocument doc, InstallPlan plan, List<string> notes)
    {
        foreach (var pair in Inspect(doc, notes))
            typeof(InstallPlan).GetProperty(pair.Key)!.SetValue(plan, pair.Value);
    }
    public static string Summary(InstallPlan p) =>
        $"重启后自动登录：{(p.DisableAutomaticRestartSignOn ? "禁用最后用户自动登录（首次安装登录另行配置）" : "遵循 Windows 默认行为")}\n" +
        $"硬件关联应用：{(p.PreventDeviceApps ? "阻止自动获取设备元数据及协同安装程序" : "遵循 Windows 默认行为")}\n" +
        $"空 Windows.old 目录：{(p.DeleteWindowsOld ? "首次登录时尝试删除；非空目录保留" : "不添加清理命令")}";

    public static void NormalizeLoaded(InstallPlan plan, JsonElement json)
    {
        if (plan.ImportedXml is not { OriginalOmitted: false } data || data.SourceXml.Length == 0 || data.BaselineXml.Length == 0)
            return;
        var missing = Items.Where(i => !json.TryGetProperty(i.Property, out _)).ToArray();
        if (missing.Length == 0) return;
        var notes = new List<string>();
        var actual = Inspect(XmlMerge.Read(data.SourceXml), notes);
        var before = JsonSerializer.Deserialize<InstallPlan>(JsonSerializer.Serialize(plan))!;
        before.ImportedXml = null;
        before.ConfirmWipeDisk = before.ConfirmFormatPartition = before.ConfirmDiskScript = true;
        foreach (var item in missing)
            typeof(InstallPlan).GetProperty(item.Property)!.SetValue(plan, actual[item.Property]);
        data.Notices = data.Notices.Concat(notes).Distinct().ToArray();
        if (!missing.Any(i => actual[i.Property])) return;
        var after = JsonSerializer.Deserialize<InstallPlan>(JsonSerializer.Serialize(before))!;
        foreach (var item in missing)
            typeof(InstallPlan).GetProperty(item.Property)!.SetValue(after, actual[item.Property]);
        try
        {
            var service = new GeneratorService();
            var oldXml = Encoding.UTF8.GetString(service.GenerateImportBaseline(before));
            var newXml = XmlMerge.Read(Encoding.UTF8.GetString(service.GenerateImportBaseline(after)));
            data.BaselineXml = XmlMerge.Apply(data.BaselineXml, oldXml, newXml).OuterXml;
        }
        catch (Exception e) when (e is ArgumentException or Schneegans.Unattend.ConfigurationException or XmlException)
        {
            data.Blockers = data.Blockers.Append("旧方案的新增系统设置无法可靠迁移编辑基准，请重新导入原 XML 后再导出。").Distinct().ToArray();
        }
    }
}
