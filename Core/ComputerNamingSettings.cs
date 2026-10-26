using System.Text;
using System.Text.Json;
using System.Xml;
using Schneegans.Unattend;
namespace UnattendLocal.Core;

public static class ComputerNamingSettings
{
    public const string Existing = "Existing", Prompt = "Prompt", Script = "Script", Mac = "Mac";
    public const string Example = "return 'DESKTOP-{0:D3}' -f ( Get-Random -Minimum 0 -Maximum 999 );";
    private static readonly Lazy<string> PromptText = new(() => { using var stream = typeof(ComputerNamingSettings).Assembly.GetManifestResourceStream("UnattendLocal.Core.Resources.ComputerNamePrompt.ps1")!; using var reader = new StreamReader(stream, Encoding.UTF8, true); return reader.ReadToEnd(); });
    public static string PromptScript => PromptText.Value;
    private static readonly Lazy<string> MacText = new(() => { using var stream = typeof(ComputerNamingSettings).Assembly.GetManifestResourceStream("UnattendLocal.Core.Resources.ComputerNameFromMac.ps1")!; using var reader = new StreamReader(stream, Encoding.UTF8, true); return reader.ReadToEnd(); });
    public static string MacScript => MacText.Value;
    private const string Root = @"C:\Windows\Setup\Scripts\";
    public static IComputerNameSettings Build(InstallPlan p)
    {
        if (p.ComputerNameMode == Existing) return string.IsNullOrEmpty(p.ComputerName) ? new RandomComputerNameSettings() : new CustomComputerNameSettings(p.ComputerName);
        if (p.ComputerNameMode == Prompt)
        {
            return new ScriptComputerNameSettings(PromptScript);
        }
        if (p.ComputerNameMode == Mac) return new ScriptComputerNameSettings(MacScript);
        if (p.ComputerNameMode != Script) throw new ArgumentException(global::UnattendLocal.Localization.UiText.Source("text.117da4559ecf"));
        if (string.IsNullOrWhiteSpace(p.ComputerNameScript) || p.ComputerNameScript.Length > 65536) throw new ArgumentException(global::UnattendLocal.Localization.UiText.Source("text.7ff84f51cb52"));
        return new ScriptComputerNameSettings(p.ComputerNameScript);
    }
    public static readonly string Description = global::UnattendLocal.Localization.UiText.Source("text.69801855c5ca");
    public static string Summary(InstallPlan p) => global::UnattendLocal.Localization.UiText.Concat(new object?[] { global::UnattendLocal.Localization.UiText.Source("text.1fe4b5bbce78"), (p.ComputerNameMode switch
    {
        Existing => string.IsNullOrEmpty(p.ComputerName) ? global::UnattendLocal.Localization.UiText.Source("text.209256be6095") : p.ComputerName,
        Prompt => global::UnattendLocal.Localization.UiText.Source("text.eadd98418a65"),
        Script => global::UnattendLocal.Localization.UiText.Source("text.7981adc16f36"),
        Mac => global::UnattendLocal.Localization.UiText.Source("v124.mac.name"),
        _ => global::UnattendLocal.Localization.UiText.Source("text.6c98d3e90840")
    }) },new bool[]{false,false});
    private static string Normal(string text) => global::UnattendLocal.Localization.UiText.Join('\n', text.Replace("\r\n", "\n").Split('\n').Select(l => l.Trim()).Where(l => l.Length > 0), false);
    private record Template(string Setter, string Block);
    private static readonly Lazy<Template> Expected = new(() =>
    {
        var doc = new UnattendGenerator().GenerateXml(Configuration.Default with { ComputerNameSettings = new ScriptComputerNameSettings(Example) });
        return new(Normal(Files(doc, "SetComputerName.ps1").Single().InnerText), SystemMaintenanceSettings.KnownSpecializeBlocks(doc).Single(b => b.Contains("GetComputerName.ps1", StringComparison.Ordinal)));
    });
    private static XmlElement[] Files(XmlDocument doc, string name) => doc.SelectNodes("//*[local-name()='File']")!.Cast<XmlElement>().Where(f => f.GetAttribute("path").Equals(Root + name, StringComparison.OrdinalIgnoreCase)).ToArray();
    private static string Name(XmlDocument doc) { var ns = new XmlNamespaceManager(doc.NameTable); ns.AddNamespace("u", XmlMerge.Namespace); return doc.SelectSingleNode("//u:settings[@pass='specialize']/u:component[@name='Microsoft-Windows-Shell-Setup']/u:ComputerName", ns)?.InnerText ?? ""; }
    public record State(bool Enabled, bool Ambiguous, string Getter);
    public static State Inspect(XmlDocument doc)
    {
        var getter = Files(doc, "GetComputerName.ps1"); var setter = Files(doc, "SetComputerName.ps1");
        var files = doc.SelectNodes("//*[local-name()='File']")!.Cast<XmlElement>().ToArray();
        var ns = new XmlNamespaceManager(doc.NameTable); ns.AddNamespace("u", XmlMerge.Namespace);
        var lines = files.Except(getter).Except(setter).SelectMany(f => Normal(f.InnerText).Split('\n')).Concat(doc.SelectNodes("//u:Path|//u:CommandLine", ns)!.Cast<XmlElement>().Select(n => n.InnerText));
        bool Related(string l) => new[] { "GetComputerName.ps1", "SetComputerName.ps1", "ComputerName.txt" }.Any(m => l.Contains(m, StringComparison.OrdinalIgnoreCase));
        var refs = lines.Where(Related).ToArray(); var expected = Expected.Value; var expectedRefs = expected.Block.Split('\n').Where(Related).ToArray();
        bool mentioned = getter.Length + setter.Length > 0 || refs.Length > 0;
        bool enabled = getter.Length == 1 && !string.IsNullOrWhiteSpace(getter[0].InnerText) && getter[0].InnerText.Length <= 65536 && setter.Length == 1 && Normal(setter[0].InnerText) == expected.Setter && Name(doc) == "TEMPNAME" && SystemMaintenanceSettings.KnownSpecializeBlocks(doc).Count(b => b == expected.Block) == 1 && refs.Length == expectedRefs.Length && expectedRefs.All(l => refs.Count(v => v == l) == 1);
        return new(enabled, mentioned && !enabled, enabled ? getter[0].InnerText : "");
    }
    public static void ReadIntoPlan(XmlDocument doc, InstallPlan p, List<string> notes)
    {
        var state = Inspect(doc); p.ComputerNameMode = Existing; p.ComputerNameScript = "";
        if (state.Enabled)
        {
            p.ComputerNameMode = Normal(state.Getter) == Normal(PromptScript) ? Prompt : Normal(state.Getter) == Normal(MacScript) ? Mac : Script;
            if (p.ComputerNameMode == Script) p.ComputerNameScript = state.Getter;
        }
        else if (state.Ambiguous) notes.Add(global::UnattendLocal.Localization.UiText.Source("text.a74e8bff7308"));
    }
    public static void ValidateImported(InstallPlan p, XmlDocument source)
    {
        if (Inspect(source).Ambiguous && (p.ComputerNameMode != Existing || p.ComputerName != (Name(source) == "*" ? "" : Name(source)))) throw new ArgumentException(global::UnattendLocal.Localization.UiText.Source("text.5ec9482951e8"));
    }
    public static void NormalizeLoaded(InstallPlan p, JsonElement json)
    {
        if (json.TryGetProperty(nameof(InstallPlan.ComputerNameMode), out _) || p.ImportedXml is not { OriginalOmitted: false } data || data.SourceXml.Length == 0 || data.BaselineXml.Length == 0) return;
        var source = XmlMerge.Read(data.SourceXml); if (!Inspect(source).Enabled) return;
        var before = JsonSerializer.Deserialize<InstallPlan>(JsonSerializer.Serialize(p))!; before.ImportedXml = null; before.ConfirmWipeDisk = before.ConfirmFormatPartition = before.ConfirmDiskScript = true;
        before.ComputerName = Name(XmlMerge.Read(data.BaselineXml)); if (before.ComputerName == "*") before.ComputerName = "";
        var after = JsonSerializer.Deserialize<InstallPlan>(JsonSerializer.Serialize(before))!; ReadIntoPlan(source, after, new());
        if (p.ComputerName == before.ComputerName) { p.ComputerNameMode = after.ComputerNameMode; p.ComputerNameScript = after.ComputerNameScript; }
        try { var g = new GeneratorService(); data.BaselineXml = XmlMerge.Apply(data.BaselineXml, Encoding.UTF8.GetString(g.GenerateImportBaseline(before)), XmlMerge.Read(Encoding.UTF8.GetString(g.GenerateImportBaseline(after)))).OuterXml; }
        catch (Exception e) when (e is ArgumentException or ConfigurationException or XmlException) { data.Blockers = data.Blockers.Append(global::UnattendLocal.Localization.UiText.Source("text.575ef3c5fe8f")).Distinct().ToArray(); }
    }
}
