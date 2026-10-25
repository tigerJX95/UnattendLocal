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
        new(nameof(InstallPlan.DeleteJunctions), global::UnattendLocal.Localization.UiText.Source("text.13b8559bcaa2"), "junctions", ["ReparsePoint"])
    };
    public static readonly string Description = global::UnattendLocal.Localization.UiText.Source("text.ef008782a728");
    public static bool Selected(InstallPlan p, Item item) => p.DeleteJunctions;
    public static string Summary(InstallPlan p) => global::UnattendLocal.Localization.UiText.Concat(new object?[] { global::UnattendLocal.Localization.UiText.Source("text.5ba4c9a18268"), (p.DeleteJunctions ? global::UnattendLocal.Localization.UiText.Source("text.d3c562d7d573") : global::UnattendLocal.Localization.UiText.Source("text.1acf429c45df")) },new bool[]{false,false});
    private static string Normal(string value) => global::UnattendLocal.Localization.UiText.Join('\n', value.Replace("\r\n", "\n").Split('\n').Select(l => l.Trim()).Where(l => l.Length > 0), true);
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
            if (states[item.Property].Ambiguous) notes.Add(global::UnattendLocal.Localization.UiText.Concat(new object?[] { item.Label, global::UnattendLocal.Localization.UiText.Source("text.2286b6e20992") },new bool[]{false,false}));
        }
    }
    public static void ValidateImported(InstallPlan p, XmlDocument doc)
    {
        var states = Inspect(doc);
        foreach (var item in Items)
            if (Selected(p, item) && states[item.Property].Ambiguous) throw new ArgumentException(global::UnattendLocal.Localization.UiText.Concat(new object?[] { item.Label, global::UnattendLocal.Localization.UiText.Source("text.3f1e7bc717a9") },new bool[]{false,false}));
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
            if (states[item.Property].Ambiguous) data.Notices = data.Notices.Append(global::UnattendLocal.Localization.UiText.Concat(new object?[] { item.Label, global::UnattendLocal.Localization.UiText.Source("text.e3dccaf58f64") },new bool[]{false,false})).Distinct().ToArray();
            else if (states[item.Property].Enabled) data.Notices = data.Notices.Where(n => !n.Contains(global::UnattendLocal.Localization.UiText.Concat(new object?[] { global::UnattendLocal.Localization.UiText.Source("text.3c0a3997fdd8"), item.Property, "；" },new bool[]{false,false,false}), StringComparison.Ordinal)).ToArray();
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
        { data.Blockers = data.Blockers.Append(global::UnattendLocal.Localization.UiText.Source("text.5be3c5b343b3")).Distinct().ToArray(); }
    }
}
