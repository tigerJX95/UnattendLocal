using System.Text;
using System.Text.Json;
using System.Xml;
using Schneegans.Unattend;
namespace UnattendLocal.Core;

// Recognize complete fixed-generator groups without executing any target commands.
public static class SecurityOptions
{
    public record Item(string Property, string Label, string HelpId, string[] Markers);
    public static IReadOnlyList<Item> Items { get; } = new Item[]
    {
        new(nameof(InstallPlan.DisableSac), "智能应用控制", "sac", ["VerifiedAndReputablePolicyState"]),
        new(nameof(InstallPlan.DisableSmartScreen), "SmartScreen", "smartscreen", ["SmartScreen", @"\WTDS\Components", "EnableWebContentEvaluation", "PreventOverride", "HideSystray"]),
        new(nameof(InstallPlan.DisableCoreIsolation), "核心隔离", "coreisolation", [@"\DeviceGuard", "EnableVirtualizationBasedSecurity", "HypervisorEnforcedCodeIntegrity"]),
        new(nameof(InstallPlan.HardenSystemDriveAcl), "C 盘根目录权限", "rootacl", ["S-1-5-11", @"icacls.exe C:\"])
    };
    public static string Description(string property) => property switch
    {
        nameof(InstallPlan.DisableSac) => "默认不勾选，遵循 Windows 默认行为。启用此开关会在目标安装的 specialize 阶段设置 VerifiedAndReputablePolicyState=0，关闭 Windows 11 智能应用控制（Smart App Control，SAC），降低对不受信任或潜在危险应用的保护；不等于关闭 Defender 防病毒。按微软当前说明，关闭后重新开启通常需要重置或重新安装 Windows，不能当作临时暂停。实际适用性受系统版本和组织策略影响。取消只影响新配置，不恢复已安装系统；本工具不修改当前电脑。",
        nameof(InstallPlan.DisableSmartScreen) => "默认不勾选。启用后在 specialize 阶段设置 Windows SmartScreen、WTDS 钓鱼与密码风险提示相关项，并隐藏安全中心托盘图标；默认用户阶段配置 Edge SmartScreen、潜在不受欢迎应用及 Windows 应用网页内容评估相关项。会降低网站、下载和应用信誉检查保护，隐藏图标也减少状态可见性；不关闭 Defender 防病毒引擎，不保证所有应用或浏览器停止检查。组织策略和系统更新可能覆盖设置。取消只影响新配置，不恢复已安装系统；本工具不修改当前电脑。",
        nameof(InstallPlan.DisableCoreIsolation) => "默认不勾选，表示不添加覆盖，并非强制启用。启用此开关会在 specialize 阶段设置 VBS 及内存完整性（HVCI）相关注册表项为关闭，降低基于虚拟化的内核保护，也可能影响依赖 VBS 的安全能力。某些驱动或应用有兼容要求，应优先检查兼容版本；不能保证性能提升。不会卸载 Hyper-V、WSL2、Docker 或虚拟机工具，也不修改 BIOS/UEFI 虚拟化设置。组织策略及固件锁定可能影响结果。取消只影响新配置，不重新启用已安装系统保护；本工具不修改当前电脑。",
        nameof(InstallPlan.HardenSystemDriveAcl) => "默认不勾选。启用后在 specialize 阶段执行 icacls.exe C:\\ /remove:g \"*S-1-5-11\"，移除 Authenticated Users（已验证用户）组在 C 盘根目录的显式允许权限，减少普通用户直接在根目录创建目录等操作。沿用固定命令，不递归重设整个磁盘权限，不改动其他盘；其他组的权限仍可能允许写入。可能影响依赖普通用户在根目录写入的旧软件或工作流程。取消只影响新生成配置，不还原已安装系统权限；本工具不修改当前电脑。",
        _ => throw new ArgumentException("未知安全设置。")
    };
    public static bool Selected(InstallPlan p, Item item) => (bool)typeof(InstallPlan).GetProperty(item.Property)!.GetValue(p)!;
    public static string Summary(InstallPlan p) => string.Join('\n', Items.Select(i => i.Label + "：" + (!Selected(p, i) ? "遵循 Windows 默认行为" : i.Property switch
    {
        nameof(InstallPlan.DisableSac) => "关闭（恢复可能需重置或重装）",
        nameof(InstallPlan.DisableSmartScreen) => "关闭相关保护并隐藏安全托盘图标",
        nameof(InstallPlan.DisableCoreIsolation) => "关闭 VBS / 内存完整性（保护降低）",
        _ => "移除已验证用户组的根目录显式允许权限"
    })));
    public static string HelpState(InstallPlan p, string id)
    {
        var item = Items.Single(i => i.HelpId == id);
        return "当前：" + (!Selected(p, item) ? "不添加覆盖，遵循 Windows 默认行为。" : item.Property == nameof(InstallPlan.HardenSystemDriveAcl) ? "收紧 C 盘根目录权限，可能影响根目录写入。" : "关闭此项相关保护；请核对影响说明。");
    }
    private static string Normal(string value) => string.Join('\n', value.Replace("\r\n", "\n").Split('\n').Select(l => l.Trim()).Where(l => l.Length > 0));
    private record Template(Dictionary<string, (string Stage, string Block)[]> Groups);
    private static readonly Lazy<Template> Expected = new(() =>
    {
        var doc = new UnattendGenerator().GenerateXml(Configuration.Default with { DisableSac = true, DisableSmartScreen = true, DisableCoreIsolation = true, HardenSystemDriveAcl = true });
        return new(Items.ToDictionary(i => i.Property, i => new[] { "Specialize.ps1", "DefaultUser.ps1" }.SelectMany(stage => AdditionalSetupSettings.KnownBlocks(doc, stage).Where(b => Related(i, b)).Select(b => (stage, b))).ToArray()));
    });
    private static bool Related(Item i, string line) => i.Markers.Any(m => line.Contains(m, StringComparison.OrdinalIgnoreCase));
    public record State(bool Enabled, bool Ambiguous);
    public static Dictionary<string, State> Inspect(XmlDocument doc)
    {
        var ns = new XmlNamespaceManager(doc.NameTable); ns.AddNamespace("u", XmlMerge.Namespace);
        var lines = doc.SelectNodes("//*[local-name()='File']")!.Cast<XmlElement>().SelectMany(f => Normal(f.InnerText).Split('\n')).Concat(doc.SelectNodes("//u:Path|//u:CommandLine", ns)!.Cast<XmlElement>().Select(n => n.InnerText)).ToArray();
        var actual = new[] { "Specialize.ps1", "DefaultUser.ps1" }.ToDictionary(n => n, n => AdditionalSetupSettings.KnownBlocks(doc, n));
        return Items.ToDictionary(i => i.Property, i =>
        {
            var needed = Expected.Value.Groups[i.Property];
            var relevant = lines.Where(l => Related(i, l)).ToArray();
            var expectedLines = needed.SelectMany(b => b.Block.Split('\n')).Where(l => Related(i, l)).ToArray();
            bool enabled = needed.Length > 0 && needed.All(b => actual[b.Stage].Count(v => v == b.Block) == 1) && relevant.Length == expectedLines.Length && expectedLines.All(l => relevant.Count(v => v == l) == expectedLines.Count(v => v == l));
            return new State(enabled, relevant.Length > 0 && !enabled);
        });
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
            if (Selected(p, item) && states[item.Property].Ambiguous) throw new ArgumentException(item.Label + "与原 XML 中未完整识别的内容重叠，请用原工具调整后重新导入。");
    }
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
        { data.Blockers = data.Blockers.Append("旧方案的应用与内核保护设置编辑基准无法可靠迁移，请重新导入原 XML。").Distinct().ToArray(); }
    }
}
