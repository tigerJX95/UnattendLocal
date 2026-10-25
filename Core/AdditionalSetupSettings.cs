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
        new(nameof(InstallPlan.UseNarrator), global::UnattendLocal.Localization.UiText.Source("text.d45604280001"), "narrator", ["Narrator.exe", "/d narrator"]),
        new(nameof(InstallPlan.MakeEdgeUninstallable), global::UnattendLocal.Localization.UiText.Source("text.3c2010a271a7"), "edgeuninstall", ["MakeEdgeUninstallable.ps1", "1bca278a-5d11-4acf-ad2f-f9ab6d7f93a6"]),
        new(nameof(InstallPlan.DisableSystemRestore), global::UnattendLocal.Localization.UiText.Source("text.63e5e50b2b64"), "systemrestore", ["Disable-ComputerRestore"])
    };
    public static string Description(string property) => property switch
    {
        nameof(InstallPlan.UseNarrator) => global::UnattendLocal.Localization.UiText.Source("text.12c81b0789cc"),
        nameof(InstallPlan.MakeEdgeUninstallable) => global::UnattendLocal.Localization.UiText.Source("text.e84ab79c8f74"),
        nameof(InstallPlan.DisableSystemRestore) => global::UnattendLocal.Localization.UiText.Source("text.24d571acf7df"),
        _ => throw new ArgumentException(global::UnattendLocal.Localization.UiText.Source("text.4b4d3df7dcfd"))
    };
    public static bool Selected(InstallPlan p, Item item) => (bool)typeof(InstallPlan).GetProperty(item.Property)!.GetValue(p)!;
    public static string Summary(InstallPlan p) => global::UnattendLocal.Localization.UiText.Concat(new object?[] { global::UnattendLocal.Localization.UiText.Source("text.c70aa51c1697"), (p.UseNarrator ? global::UnattendLocal.Localization.UiText.Source("text.65e9471512b3") : global::UnattendLocal.Localization.UiText.Source("text.506d351b1729")), global::UnattendLocal.Localization.UiText.Source("text.da6baf3f6e56"), (p.MakeEdgeUninstallable ? global::UnattendLocal.Localization.UiText.Source("text.079e8f650d39") : global::UnattendLocal.Localization.UiText.Source("text.4e9901953ce4")), global::UnattendLocal.Localization.UiText.Source("text.63f3d2f5b91c"), (p.DisableSystemRestore ? global::UnattendLocal.Localization.UiText.Source("text.84f84a76095f") : global::UnattendLocal.Localization.UiText.Source("text.4e9901953ce4")) },new bool[]{false,false,false,false,false,false});
    public static string HelpState(InstallPlan p, string id)
    {
        var item = Items.Single(i => i.HelpId == id);
        return global::UnattendLocal.Localization.UiText.Concat(new object?[] { global::UnattendLocal.Localization.UiText.Source("text.660648805666"), (Selected(p, item) ? item.Property switch
        {
            nameof(InstallPlan.UseNarrator) => global::UnattendLocal.Localization.UiText.Source("text.dc863196075b"),
            nameof(InstallPlan.MakeEdgeUninstallable) => global::UnattendLocal.Localization.UiText.Source("text.c2bfde530874"),
            _ => global::UnattendLocal.Localization.UiText.Source("text.e2ef501fcfeb")
        } : global::UnattendLocal.Localization.UiText.Source("text.e1de76978e36")) },new bool[]{false,false});
    }
    private static string Normal(string value) => global::UnattendLocal.Localization.UiText.Join('\n', value.Replace("\r\n", "\n").Split('\n').Select(l => l.Trim()).Where(l => l.Length > 0), true);
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
    internal static string[] KnownBlocks(XmlDocument doc, string name, bool allowTrailingContent = false)
    {
        doc=XmlImportPaths.Inspection(doc);
        string path = Root + name;
        var stage = Files(doc).Where(f => f.GetAttribute("path").Equals(path, StringComparison.OrdinalIgnoreCase)).ToArray();
        var reference = Files(Expected.Value.Document).Single(f => f.GetAttribute("path") == path);
        if (stage.Length != 1 || !NativeScriptPipeline.StageActive(doc, name) || !ExtractionActive(doc) || (Skeleton(stage[0].InnerText) != Skeleton(reference.InnerText) && !(allowTrailingContent && Skeleton(stage[0].InnerText).StartsWith(Skeleton(reference.InnerText) + "\n", StringComparison.Ordinal)))) return [];
        if (name == "UserOnce.ps1")
        {
            var defaultBlocks = KnownBlocks(doc, "DefaultUser.ps1", allowTrailingContent);
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
                var actualChain = NativeScriptPipeline.OrderedCommands(doc, "specialize").Select(NativeScriptPipeline.Text).Where(c => c.Contains("HKU\\DefaultUser", StringComparison.OrdinalIgnoreCase) || c.Contains("DefaultUser.ps1", StringComparison.OrdinalIgnoreCase)).Select(Style).ToArray();
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
            if (states[item.Property].Ambiguous) notes.Add(global::UnattendLocal.Localization.UiText.Concat(new object?[] { item.Label, global::UnattendLocal.Localization.UiText.Source("text.66e029170dda") },new bool[]{false,false}));
        }
    }
    public static void ValidateImported(InstallPlan p, XmlDocument doc)
    {
        var states = Inspect(doc);
        foreach (var item in Items)
            if (Selected(p, item) && states[item.Property].Ambiguous)
                throw new ArgumentException(global::UnattendLocal.Localization.UiText.Concat(new object?[] { item.Label, global::UnattendLocal.Localization.UiText.Source("text.3f1e7bc717a9") },new bool[]{false,false}));
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
        {
            data.Blockers = data.Blockers.Append(global::UnattendLocal.Localization.UiText.Source("text.94df6ea1bda7")).Distinct().ToArray();
        }
    }
}
