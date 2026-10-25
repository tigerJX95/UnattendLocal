using System.Xml;
using System.Text;
using System.Text.Json;
using Schneegans.Unattend;
namespace UnattendLocal.Core;
public static class ConfigurationSetSettings
{
    public static readonly string Description = global::UnattendLocal.Localization.UiText.Source("text.03c469a47964");
    public record State(bool Enabled, bool Ambiguous);
    private static XmlElement[] Nodes(XmlDocument doc) { var ns = new XmlNamespaceManager(doc.NameTable); ns.AddNamespace("u", XmlMerge.Namespace); return doc.SelectNodes("//u:settings[@pass='windowsPE']/u:component[@name='Microsoft-Windows-Setup']/u:UseConfigurationSet", ns)!.Cast<XmlElement>().ToArray(); }
    public static State Inspect(XmlDocument doc)
    {
        var nodes = Nodes(doc);
        bool scripted = doc.InnerText.Contains("ROBOCOPY_ARGS", StringComparison.OrdinalIgnoreCase) || doc.InnerText.Contains("Copying contents of %OEM_FOLDER%", StringComparison.OrdinalIgnoreCase);
        bool invalid = nodes.Length > 1 || nodes.Any(n => n.InnerText is not ("true" or "false" or "1" or "0"));
        return new(!scripted && !invalid && nodes.SingleOrDefault()?.InnerText is "true" or "1", scripted || invalid);
    }
    public static string Summary(InstallPlan p) => global::UnattendLocal.Localization.UiText.Concat(new object?[] { global::UnattendLocal.Localization.UiText.Source("text.8363127218d7"), (p.UseConfigurationSet ? global::UnattendLocal.Localization.UiText.Source("text.7bc6ecd70064") : global::UnattendLocal.Localization.UiText.Source("text.8093f86a4b0f")) },new bool[]{false,false});
    public static void ReadIntoPlan(XmlDocument doc, InstallPlan p, List<string> notes) { var state = Inspect(doc); p.UseConfigurationSet = state.Enabled; if (state.Ambiguous) notes.Add(global::UnattendLocal.Localization.UiText.Source("text.3dd4f1c36bc9")); }
    public static void ValidateImported(InstallPlan p, XmlDocument doc) { if (p.UseConfigurationSet && Inspect(doc).Ambiguous) throw new ArgumentException(global::UnattendLocal.Localization.UiText.Source("text.d714cb58f773")); }
    public static void NormalizeLoaded(InstallPlan p, JsonElement json)
    {
        if (json.TryGetProperty(nameof(InstallPlan.UseConfigurationSet), out _) || p.ImportedXml is not { OriginalOmitted: false } data || data.SourceXml.Length == 0 || data.BaselineXml.Length == 0 || !Inspect(XmlMerge.Read(data.SourceXml)).Enabled) return;
        var before = JsonSerializer.Deserialize<InstallPlan>(JsonSerializer.Serialize(p))!; before.ImportedXml = null; before.ConfirmWipeDisk = before.ConfirmFormatPartition = before.ConfirmDiskScript = true;
        var after = JsonSerializer.Deserialize<InstallPlan>(JsonSerializer.Serialize(before))!; p.UseConfigurationSet = after.UseConfigurationSet = true;
        try { var g = new GeneratorService(); data.BaselineXml = XmlMerge.Apply(data.BaselineXml, Encoding.UTF8.GetString(g.GenerateImportBaseline(before)), XmlMerge.Read(Encoding.UTF8.GetString(g.GenerateImportBaseline(after)))).OuterXml; }
        catch (Exception e) when (e is ArgumentException or ConfigurationException or XmlException) { data.Blockers = data.Blockers.Append(global::UnattendLocal.Localization.UiText.Source("text.1c5a1fee2a36")).Distinct().ToArray(); }
    }
}
