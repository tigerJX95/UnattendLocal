using System.Text;
using System.Text.Json;
using System.Xml;
using Schneegans.Unattend;
namespace UnattendLocal.Core;

public static class ProtectionSettings
{
    public record Item(string Property, string Label, string Command, string Marker);
    public static IReadOnlyList<Item> Items { get; } = new Item[]
    {
        new(nameof(InstallPlan.DisableUac), "用户账户控制（UAC）", "reg.exe add \"HKLM\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Policies\\System\" /v EnableLUA /t REG_DWORD /d 0 /f", "EnableLUA"),
        new(nameof(InstallPlan.DisableWpbt), "WPBT 固件程序", "reg.exe add \"HKLM\\System\\CurrentControlSet\\Control\\Session Manager\" /v \"DisableWpbtExecution\" /t REG_DWORD /d 1 /f;", "DisableWpbtExecution")
    };
    public const string UacDescription = "沿用原生成器，在目标 Windows 的 specialize 阶段将 EnableLUA 设为 0，关闭用户账户控制（UAC）。这不是只隐藏权限提升提示，会降低管理员审批/权限提升保护，可能影响依赖 UAC 的应用。默认不勾选，不写入覆盖，遵循 Windows 默认设置。取消只影响新生成配置，不重新启用已安装系统的 UAC；本工具不修改当前电脑。";
    public const string WpbtDescription = "沿用原生成器，在目标 Windows 的 specialize 阶段设置 DisableWpbtExecution=1，阻止执行通过 Windows Platform Binary Table（WPBT）提供的固件程序。可能影响厂商通过该机制提供的软件；不卸载已有软件，不修改 BIOS/UEFI 固件，不等于全面禁止厂商应用或驱动安装。默认不勾选，遵循 Windows 默认行为；取消只影响新配置，不撤销已安装系统的策略。本工具不修改当前电脑。";
    private static string Normal(string body) => string.Join('\n', body.Replace("\r\n", "\n").Split('\n').Select(l => l.Trim()).Where(l => l.Length > 0));
    public record State(bool Enabled, bool Ambiguous);
    public static Dictionary<string, State> Inspect(XmlDocument doc)
    {
        var blocks = SystemMaintenanceSettings.KnownSpecializeBlocks(doc);
        var files = doc.SelectNodes("//*[local-name()='File']")!.Cast<XmlElement>();
        var ns = new XmlNamespaceManager(doc.NameTable); ns.AddNamespace("u", XmlMerge.Namespace);
        var paths = doc.SelectNodes("//u:settings[@pass='specialize']//u:Path", ns)!.Cast<XmlElement>();
        var lines = files.SelectMany(f => Normal(f.InnerText).Split('\n')).Concat(paths.Select(p => p.InnerText)).ToArray();
        var states = new Dictionary<string, State>();
        foreach (var item in Items)
        {
            var relevant = lines.Where(l => l.Contains(item.Marker, StringComparison.OrdinalIgnoreCase)).ToArray();
            bool enabled = blocks.Count(b => b == item.Command) == 1 && relevant.Length == 1 && relevant[0] == item.Command;
            states[item.Property] = new(enabled, relevant.Length > 0 && !enabled);
        }
        return states;
    }
    public static void ReadIntoPlan(XmlDocument doc, InstallPlan p, List<string> notes)
    {
        var states = Inspect(doc);
        foreach (var item in Items)
        {
            typeof(InstallPlan).GetProperty(item.Property)!.SetValue(p, states[item.Property].Enabled);
            if (states[item.Property].Ambiguous) notes.Add(item.Label + "命令或执行管线未完整识别，原文保留；不能用此开关覆盖。");
        }
    }
    public static void ValidateImported(InstallPlan p, XmlDocument doc)
    {
        var states = Inspect(doc);
        foreach (var item in Items)
            if (states[item.Property].Ambiguous && (bool)typeof(InstallPlan).GetProperty(item.Property)!.GetValue(p)!)
                throw new ArgumentException(item.Label + "与原 XML 中未完整识别的内容重叠，请用原工具调整后重新导入。");
    }
    public static string Summary(InstallPlan p) => "用户账户控制（UAC）：" + (p.DisableUac ? "关闭（权限提升保护降低）" : "遵循 Windows 默认设置") + "\nWPBT 固件程序：" + (p.DisableWpbt ? "阻止执行（不修改固件、不卸载已有软件）" : "遵循 Windows 默认行为");
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
            if (states[item.Property].Ambiguous) data.Notices = data.Notices.Append(item.Label + "未完整识别，原文保留，不能覆盖。").Distinct().ToArray();
            else if (states[item.Property].Enabled)
                data.Notices = data.Notices.Where(n => !n.Contains("生成参数：" + item.Property + "；", StringComparison.Ordinal)).ToArray();
        }
        if (!missing.Any(i => states[i.Property].Enabled)) return;
        var after = JsonSerializer.Deserialize<InstallPlan>(JsonSerializer.Serialize(before))!;
        foreach (var item in missing) typeof(InstallPlan).GetProperty(item.Property)!.SetValue(after, states[item.Property].Enabled);
        try
        {
            var service = new GeneratorService();
            data.BaselineXml = XmlMerge.Apply(data.BaselineXml, Encoding.UTF8.GetString(service.GenerateStandalone(before)), XmlMerge.Read(Encoding.UTF8.GetString(service.GenerateStandalone(after)))).OuterXml;
        }
        catch (Exception e) when (e is ArgumentException or ConfigurationException or XmlException)
        {
            data.Blockers = data.Blockers.Append("旧方案的 UAC/WPBT 编辑基准无法可靠迁移，请重新导入原 XML 后再导出。").Distinct().ToArray();
        }
    }
}
