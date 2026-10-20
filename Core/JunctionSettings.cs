using System.Text;
using System.Text.Json;
using System.Xml;
using Schneegans.Unattend;
namespace UnattendLocal.Core;

// Recognize complete fixed-generator groups without executing any target commands.
public static class JunctionSettings
{
    public record Item(string Property, string Label, string HelpId, string[] Markers);
    public static IReadOnlyList<Item> Items { get; } = new Item[]
    {
        new(nameof(InstallPlan.DeleteJunctions), "兼容联接点", "junctions", ["ReparsePoint"])
    };
    public const string Description = "默认关闭。沿用生成器原有命令，在全机首次登录及每位用户首次登录时，枚举 C 盘根目录、Users、Default、Public、ProgramData 和当前用户目录的指定范围，删除其中带 ReparsePoint 属性的项目（包含联接点，也可能包含其他重解析点）。并非只按隐藏属性筛选，也不是扫描全盘；原生命令使用 Remove-Item -Force -Recurse。可能影响依赖旧路径的程序、备份工具或自行建立的链接，建议先在虚拟机验证。取消只从新配置移除命令，不恢复已删除的链接；不修改当前电脑。";
    public static bool Selected(InstallPlan p, Item item) => p.DeleteJunctions;
    public static string Summary(InstallPlan p) => "兼容联接点：" + (p.DeleteJunctions ? "首次登录时删除指定范围重解析点（可能影响旧软件或自建链接）" : "不添加删除命令");
    private static string Normal(string value) => string.Join('\n', value.Replace("\r\n", "\n").Split('\n').Select(l => l.Trim()).Where(l => l.Length > 0));
    private record Template(Dictionary<string, (string Stage, string Block)[]> Groups);
    private static readonly Lazy<Template> Expected = new(() =>
    {
        var doc = new UnattendGenerator().GenerateXml(Configuration.Default with { DeleteJunctions = true });
        return new(Items.ToDictionary(i => i.Property, i => new[] { "FirstLogon.ps1", "UserOnce.ps1" }.SelectMany(stage => AdditionalSetupSettings.KnownBlocks(doc, stage).Where(b => Related(i, b)).Select(b => (stage, b))).ToArray()));
    });
    private static bool Related(Item i, string line) => i.Markers.Any(m => line.Contains(m, StringComparison.OrdinalIgnoreCase));
    public record State(bool Enabled, bool Ambiguous);
    public static Dictionary<string, State> Inspect(XmlDocument doc)
    {
        var ns = new XmlNamespaceManager(doc.NameTable); ns.AddNamespace("u", XmlMerge.Namespace);
        var lines = doc.SelectNodes("//*[local-name()='File']")!.Cast<XmlElement>().SelectMany(f => Normal(f.InnerText).Split('\n')).Concat(doc.SelectNodes("//u:Path|//u:CommandLine", ns)!.Cast<XmlElement>().Select(n => n.InnerText)).ToArray();
        var actual = new[] { "FirstLogon.ps1", "UserOnce.ps1" }.ToDictionary(n => n, n => AdditionalSetupSettings.KnownBlocks(doc, n));
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
        { data.Blockers = data.Blockers.Append("旧方案的兼容联接点设置编辑基准无法可靠迁移，请重新导入原 XML。").Distinct().ToArray(); }
    }
}
