using System.Text;
using System.Text.Json;
using System.Xml;
using System.Xml.Schema;
using Schneegans.Unattend;
namespace UnattendLocal.Core;

public static class AppControlSettings
{
    public const int MaxLength = 2 * 1024 * 1024;
    private const string PolicyPath = @"C:\Windows\Setup\Scripts\AppLockerPolicy.xml";
    public const string Example = """
        <AppLockerPolicy Version="1">
          <RuleCollection Type="Exe" EnforcementMode="AuditOnly">
            <FilePathRule Id="d3b66501-7907-4ad5-9ad6-165630844817" Name="Windows directory audit example" Description="Audit example only; adapt and test before enforcing." UserOrGroupSid="S-1-1-0" Action="Allow">
              <Conditions><FilePathCondition Path="%WINDIR%\*" /></Conditions>
            </FilePathRule>
          </RuleCollection>
        </AppLockerPolicy>
        """;
    public static readonly string Description = global::UnattendLocal.Localization.UiText.Source("text.71c3cf397ce0");
    public static XmlDocument ReadPolicy(string text)
    {
        if (string.IsNullOrWhiteSpace(text) || text.Length > MaxLength) throw new ArgumentException(global::UnattendLocal.Localization.UiText.Source("text.2fa48221ea40"));
        var doc = new XmlDocument { XmlResolver = null };
        try
        {
            using var reader = XmlReader.Create(new StringReader(text), new XmlReaderSettings { DtdProcessing = DtdProcessing.Prohibit, XmlResolver = null, MaxCharactersInDocument = MaxLength, MaxCharactersFromEntities = 0 });
            doc.Load(reader);
            if (doc.DocumentElement?.Name != "AppLockerPolicy" || doc.DocumentElement.NamespaceURI.Length != 0 || doc.SelectNodes("//processing-instruction()")!.Count > 0) throw new ArgumentException(global::UnattendLocal.Localization.UiText.Source("text.d58093f0bd94"));
            var pending = new Stack<(XmlElement Element, int Depth)>(); pending.Push((doc.DocumentElement, 1)); int count = 0;
            while (pending.Count > 0) { var (element, depth) = pending.Pop(); if (depth > 64 || ++count > 50000) throw new ArgumentException(global::UnattendLocal.Localization.UiText.Source("text.ae6a77a76098")); foreach (var child in element.ChildNodes.OfType<XmlElement>()) pending.Push((child, depth + 1)); }
            using var schema = typeof(UnattendGenerator).Assembly.GetManifestResourceStream("Schneegans.Unattend.resource.AppLocker.xsd")!;
            doc.Schemas.XmlResolver = null; doc.Schemas.Add(null, XmlReader.Create(schema, new XmlReaderSettings { DtdProcessing = DtdProcessing.Prohibit, XmlResolver = null }));
            doc.Validate((_, e) => throw new ArgumentException(global::UnattendLocal.Localization.UiText.Concat(new object?[] { global::UnattendLocal.Localization.UiText.Source("text.097aa8e71d60"), e.Message },new bool[]{false,false})));
            return doc;
        }
        catch (XmlException e) { throw new ArgumentException(global::UnattendLocal.Localization.UiText.Concat(new object?[] { global::UnattendLocal.Localization.UiText.Source("text.4a8494695212"), e.Message },new bool[]{false,false}), e); }
    }
    public static IAppLockerSettings Build(InstallPlan p) => p.UseAppLocker ? new ConfigureAppLockerSettings(ReadPolicy(p.AppLockerPolicyXml).OuterXml) : new SkipAppLockerSettings();
    public static string Summary(InstallPlan p)
    {
        if (!p.UseAppLocker) return global::UnattendLocal.Localization.UiText.Source("text.2dd0ae2a6155");
        try { var doc = ReadPolicy(p.AppLockerPolicyXml); var collections = doc.DocumentElement!.ChildNodes.OfType<XmlElement>().Where(e => e.Name == "RuleCollection").ToArray(); return global::UnattendLocal.Localization.UiText.Concat(new object?[] { "AppLocker：", collections.Length, global::UnattendLocal.Localization.UiText.Source("text.affe372e11cd"), global::UnattendLocal.Localization.UiText.Join("、", collections.Select(e => e.GetAttribute("Type") + "=" + e.GetAttribute("EnforcementMode")), false), global::UnattendLocal.Localization.UiText.Source("text.ad934e56761c") },new bool[]{false,false,false,false,false}); }
        catch (ArgumentException) { return global::UnattendLocal.Localization.UiText.Source("text.f70ae0f7b32a"); }
    }
    private static string Normal(string text) => global::UnattendLocal.Localization.UiText.Join('\n', text.Replace("\r\n", "\n").Split('\n').Select(l => l.Trim()).Where(l => l.Length > 0), false);
    private static bool Related(string line) => new[] { "AppLockerPolicy.xml", "AppIDSvc", "Set-AppLockerPolicy" }.Any(m => line.Contains(m, StringComparison.OrdinalIgnoreCase));
    private static readonly Lazy<string> Expected = new(() => AdditionalSetupSettings.KnownBlocks(new UnattendGenerator().GenerateXml(Configuration.Default with { AppLockerSettings = new ConfigureAppLockerSettings(Example) }), "Specialize.ps1").Single(Related));
    public record State(bool Enabled, bool Ambiguous, string Xml);
    public static State Inspect(XmlDocument doc)
    {
        var files = doc.SelectNodes("//*[local-name()='File']")!.Cast<XmlElement>().ToArray();
        var policies = files.Where(f => f.GetAttribute("path").Equals(PolicyPath, StringComparison.OrdinalIgnoreCase)).ToArray();
        var ns = new XmlNamespaceManager(doc.NameTable); ns.AddNamespace("u", XmlMerge.Namespace);
        var refs = files.Except(policies).SelectMany(f => Normal(f.InnerText).Split('\n')).Concat(doc.SelectNodes("//u:Path|//u:CommandLine", ns)!.Cast<XmlElement>().Select(n => n.InnerText)).Where(Related).ToArray();
        var expected = Expected.Value; var needed = expected.Split('\n').Where(Related).ToArray();
        bool valid = false;
        if (policies.Length == 1) { try { _ = ReadPolicy(policies[0].InnerText); valid = true; } catch (ArgumentException) { } }
        bool enabled = valid && AdditionalSetupSettings.KnownBlocks(doc, "Specialize.ps1").Count(b => b == expected) == 1 && refs.Length == needed.Length && needed.All(l => refs.Count(v => v == l) == needed.Count(v => v == l));
        return new(enabled, (policies.Length > 0 || refs.Length > 0) && !enabled, enabled ? policies[0].InnerText : "");
    }
    public static void ReadIntoPlan(XmlDocument doc, InstallPlan p, List<string> notes)
    {
        var state = Inspect(doc); p.UseAppLocker = state.Enabled; p.AppLockerPolicyXml = state.Xml;
        if (state.Ambiguous) notes.Add(global::UnattendLocal.Localization.UiText.Source("text.84ab037587e6"));
    }
    public static void ValidateImported(InstallPlan p, XmlDocument doc)
    {
        if (p.UseAppLocker && Inspect(doc).Ambiguous) throw new ArgumentException(global::UnattendLocal.Localization.UiText.Source("text.87cf12598e80"));
    }
    public static void NormalizeLoaded(InstallPlan p, JsonElement json)
    {
        if (json.TryGetProperty(nameof(InstallPlan.UseAppLocker), out _) || p.ImportedXml is not { OriginalOmitted: false } data || data.SourceXml.Length == 0 || data.BaselineXml.Length == 0) return;
        var source = XmlMerge.Read(data.SourceXml); var state = Inspect(source); if (!state.Enabled) return;
        var before = JsonSerializer.Deserialize<InstallPlan>(JsonSerializer.Serialize(p))!; before.ImportedXml = null; before.ConfirmWipeDisk = before.ConfirmFormatPartition = before.ConfirmDiskScript = true;
        var after = JsonSerializer.Deserialize<InstallPlan>(JsonSerializer.Serialize(before))!; ReadIntoPlan(source, after, new()); p.UseAppLocker = after.UseAppLocker; p.AppLockerPolicyXml = after.AppLockerPolicyXml;
        try { var g = new GeneratorService(); data.BaselineXml = XmlMerge.Apply(data.BaselineXml, Encoding.UTF8.GetString(g.GenerateImportBaseline(before)), XmlMerge.Read(Encoding.UTF8.GetString(g.GenerateImportBaseline(after)))).OuterXml; }
        catch (Exception e) when (e is ArgumentException or ConfigurationException or XmlException) { data.Blockers = data.Blockers.Append(global::UnattendLocal.Localization.UiText.Source("text.3f7b99e7ece0")).Distinct().ToArray(); }
    }
}
