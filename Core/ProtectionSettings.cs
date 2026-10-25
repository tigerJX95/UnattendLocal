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
        new(nameof(InstallPlan.DisableUac), global::UnattendLocal.Localization.UiText.Source("text.b2771e6e82de"), "reg.exe add \"HKLM\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Policies\\System\" /v EnableLUA /t REG_DWORD /d 0 /f", "EnableLUA"),
        new(nameof(InstallPlan.DisableWpbt), global::UnattendLocal.Localization.UiText.Source("text.83cd4c6e5228"), "reg.exe add \"HKLM\\System\\CurrentControlSet\\Control\\Session Manager\" /v \"DisableWpbtExecution\" /t REG_DWORD /d 1 /f;", "DisableWpbtExecution")
    };
    public static readonly string UacDescription = global::UnattendLocal.Localization.UiText.Source("text.3402500bca0e");
    public static readonly string WpbtDescription = global::UnattendLocal.Localization.UiText.Source("text.d3893a765a65");
    private static string Normal(string body) => global::UnattendLocal.Localization.UiText.Join('\n', body.Replace("\r\n", "\n").Split('\n').Select(l => l.Trim()).Where(l => l.Length > 0), false);
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
            if (states[item.Property].Ambiguous) notes.Add(global::UnattendLocal.Localization.UiText.Concat(new object?[] { item.Label, global::UnattendLocal.Localization.UiText.Source("text.2286b6e20992") },new bool[]{false,false}));
        }
    }
    public static void ValidateImported(InstallPlan p, XmlDocument doc)
    {
        var states = Inspect(doc);
        foreach (var item in Items)
            if (states[item.Property].Ambiguous && (bool)typeof(InstallPlan).GetProperty(item.Property)!.GetValue(p)!)
                throw new ArgumentException(global::UnattendLocal.Localization.UiText.Concat(new object?[] { item.Label, global::UnattendLocal.Localization.UiText.Source("text.3f1e7bc717a9") },new bool[]{false,false}));
    }
    public static string Summary(InstallPlan p) => global::UnattendLocal.Localization.UiText.Concat(new object?[] { global::UnattendLocal.Localization.UiText.Source("text.45b430124b7e"), (p.DisableUac ? global::UnattendLocal.Localization.UiText.Source("text.007cf5896cb6") : global::UnattendLocal.Localization.UiText.Source("text.76e08c70fb95")), global::UnattendLocal.Localization.UiText.Source("text.2af64899969d"), (p.DisableWpbt ? global::UnattendLocal.Localization.UiText.Source("text.c2024b7f5fc4") : global::UnattendLocal.Localization.UiText.Source("text.4e9901953ce4")) },new bool[]{false,false,false,false});
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
            else if (states[item.Property].Enabled)
                data.Notices = data.Notices.Where(n => !n.Contains(global::UnattendLocal.Localization.UiText.Concat(new object?[] { global::UnattendLocal.Localization.UiText.Source("text.3c0a3997fdd8"), item.Property, "；" },new bool[]{false,false,false}), StringComparison.Ordinal)).ToArray();
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
            data.Blockers = data.Blockers.Append(global::UnattendLocal.Localization.UiText.Source("text.b51d24194bbd")).Distinct().ToArray();
        }
    }
}
