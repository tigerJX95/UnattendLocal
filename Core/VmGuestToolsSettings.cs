using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Xml;
using Schneegans.Unattend;
namespace UnattendLocal.Core;

public static class VmGuestToolsSettings
{
    private const string Root = @"C:\Windows\Setup\Scripts\";
    private const string Stage = "FirstLogon.ps1";
    private const string BlockPattern = @"(?m)^\t\{\n(?<body>(?:\t\t[^\n]*\n)*)\t\};\n";
    public record Item(string Property, string Label, string Resource, string HelpId, string Media);
    public static IReadOnlyList<Item> Items { get; } = new Item[]
    {
        new(nameof(InstallPlan.VMwareTools), "VMware Tools", "VMwareTools.ps1", "vmwaretools", global::UnattendLocal.Localization.UiText.Source("text.c55258f63bdc")),
        new(nameof(InstallPlan.VBoxGuestAdditions), global::UnattendLocal.Localization.UiText.Source("text.791aeb981543"), "VBoxGuestAdditions.ps1", "vboxtools", global::UnattendLocal.Localization.UiText.Source("text.1207ee5ad3fc")),
        new(nameof(InstallPlan.VirtIoGuestTools), "VirtIO Guest Tools", "VirtIoGuestTools.ps1", "virtiotools", global::UnattendLocal.Localization.UiText.Source("text.c1dc0ede3f9b")),
        new(nameof(InstallPlan.ParallelsTools), "Parallels Tools", "ParallelsTools.ps1", "parallelstools", global::UnattendLocal.Localization.UiText.Source("text.311265b9f2ef"))
    };
    public static string Description(string property)
    {
        var item = Items.Single(i => i.Property == property);
        string detail = property switch
        {
            nameof(InstallPlan.VBoxGuestAdditions) => global::UnattendLocal.Localization.UiText.Source("text.707933c3fd45"),
            nameof(InstallPlan.VirtIoGuestTools) => global::UnattendLocal.Localization.UiText.Source("text.74cf5bd917c2"),
            nameof(InstallPlan.ParallelsTools) => global::UnattendLocal.Localization.UiText.Source("text.5a6001247697"),
            _ => global::UnattendLocal.Localization.UiText.Source("text.08aaedc79209")
        };
        return global::UnattendLocal.Localization.UiText.Concat(new object?[] { global::UnattendLocal.Localization.UiText.Source("text.e5329f048a34"), item.Media, global::UnattendLocal.Localization.UiText.Source("text.3b9d7544aa0f"), detail, global::UnattendLocal.Localization.UiText.Source("text.7d798054d7e5") },new bool[]{false,false,false,false,false});
    }
    public static bool Selected(InstallPlan p, Item item) => (bool)typeof(InstallPlan).GetProperty(item.Property)!.GetValue(p)!;
    public static string Summary(InstallPlan p)
    {
        var names = Items.Where(i => Selected(p, i)).Select(i => i.Label).ToArray();
        return global::UnattendLocal.Localization.UiText.Concat(new object?[] { global::UnattendLocal.Localization.UiText.Source("text.cc083d0efd95"), (names.Length == 0 ? global::UnattendLocal.Localization.UiText.Source("text.6f21443725ab") : global::UnattendLocal.Localization.UiText.Concat(new object?[] { global::UnattendLocal.Localization.UiText.Join("、", names, false), global::UnattendLocal.Localization.UiText.Source("text.5af8f22d4d86") },new bool[]{false,false})) },new bool[]{false,false});
    }
    public static string HelpState(InstallPlan p, string helpId)
    {
        var item = Items.Single(i => i.HelpId == helpId);
        return Selected(p, item) ? global::UnattendLocal.Localization.UiText.Concat(new object?[] { global::UnattendLocal.Localization.UiText.Source("text.8c040533f073"), item.Label, global::UnattendLocal.Localization.UiText.Source("text.793408fc5eba") },new bool[]{false,false,false}) : global::UnattendLocal.Localization.UiText.Concat(new object?[] { global::UnattendLocal.Localization.UiText.Source("text.f5f2bd91ccd5"), item.Label, global::UnattendLocal.Localization.UiText.Source("text.758eba4d3af6") },new bool[]{false,false,false});
    }
    private static string Normal(string body) => global::UnattendLocal.Localization.UiText.Join('\n', body.Replace("\r\n", "\n").Split('\n').Select(l => l.Trim()).Where(l => l.Length > 0), false);
    private static string WindowStyle(string command) => Regex.Replace(command, "-WindowStyle \"(?:Hidden|Normal)\"", "-WindowStyle \"Hidden\"", RegexOptions.None, TimeSpan.FromSeconds(1));
    private static string Skeleton(string body) => Normal(Regex.Replace(body.Replace("\r\n", "\n"), BlockPattern, "", RegexOptions.None, TimeSpan.FromSeconds(1)));
    private static XmlElement[] Files(XmlDocument doc, string name) => doc.SelectNodes("//*[local-name()='File']")!.Cast<XmlElement>().Where(f => f.GetAttribute("path").Equals(Root + name, StringComparison.OrdinalIgnoreCase)).ToArray();
    private record Template(string StageWrapper, string Extraction, string ExtractCall, string StageCall, Dictionary<string,string> Helpers);
    private static readonly Lazy<Template> Expected = new(() =>
    {
        var doc = new UnattendGenerator().GenerateXml(Configuration.Default with { VMwareTools = true, VBoxGuestAdditions = true, VirtIoGuestTools = true, ParallelsTools = true });
        var ns = Namespace(doc);
        var extractCall = doc.SelectNodes("//u:settings[@pass='specialize']//u:Path", ns)!.Cast<XmlElement>().Single(n => n.InnerText.Contains("Extensions.ExtractScript", StringComparison.Ordinal)).InnerText;
        var stageCall = doc.SelectNodes("//u:settings[@pass='oobeSystem']//u:FirstLogonCommands/u:SynchronousCommand/u:CommandLine", ns)!.Cast<XmlElement>().Single(n => n.InnerText.Contains(Root + Stage, StringComparison.Ordinal)).InnerText;
        return new(Skeleton(Files(doc, Stage).Single().InnerText), Normal(doc.SelectSingleNode("//*[local-name()='ExtractScript']")!.InnerText), extractCall, stageCall,
            Items.ToDictionary(i => i.Property, i => Normal(Files(doc, i.Resource).Single().InnerText)));
    });
    private static XmlNamespaceManager Namespace(XmlDocument doc) { var ns = new XmlNamespaceManager(doc.NameTable); ns.AddNamespace("u", XmlMerge.Namespace); return ns; }
    public record State(bool Enabled, bool Ambiguous);
    public static Dictionary<string, State> Inspect(XmlDocument doc)
    {
        var expected = Expected.Value; var ns = Namespace(doc);
        var files = doc.SelectNodes("//*[local-name()='File']")!.Cast<XmlElement>().ToArray();
        var stage = Files(doc, Stage);
        var extraction = doc.SelectNodes("//*[local-name()='ExtractScript']")!.Cast<XmlElement>().ToArray();
        var specializePaths = doc.SelectNodes("//u:settings[@pass='specialize']//u:Path", ns)!.Cast<XmlElement>().ToArray();
        var firstCalls = doc.SelectNodes("//u:settings[@pass='oobeSystem']//u:FirstLogonCommands/u:SynchronousCommand/u:CommandLine", ns)!.Cast<XmlElement>().ToArray();
        bool extracted = extraction.Length == 1 && Normal(extraction[0].InnerText) == expected.Extraction && specializePaths.Count(p => WindowStyle(p.InnerText) == WindowStyle(expected.ExtractCall)) == 1;
        bool active = firstCalls.Count(p => WindowStyle(p.InnerText) == WindowStyle(expected.StageCall)) == 1;
        bool nativeStage = extracted && active && stage.Length == 1 && Skeleton(stage[0].InnerText) == expected.StageWrapper;
        var blocks = nativeStage ? Regex.Matches(stage[0].InnerText.Replace("\r\n", "\n"), BlockPattern, RegexOptions.None, TimeSpan.FromSeconds(1)).Select(m => Normal(m.Groups["body"].Value)).ToArray() : [];
        var xmlCommands = doc.SelectNodes("//u:Path|//u:CommandLine", ns)!.Cast<XmlElement>().Select(n => n.InnerText).ToArray();
        var states = new Dictionary<string, State>();
        foreach (var item in Items)
        {
            var helper = Files(doc, item.Resource);
            var references = files.Where(f => !helper.Contains(f)).SelectMany(f => Normal(f.InnerText).Split('\n')).Concat(xmlCommands).Where(l => l.Contains(item.Resource, StringComparison.OrdinalIgnoreCase)).ToArray();
            string call = "& '" + Root + item.Resource + "';";
            bool mentioned = references.Length > 0 || files.Any(f => f.GetAttribute("path").EndsWith(item.Resource, StringComparison.OrdinalIgnoreCase));
            bool enabled = nativeStage && helper.Length == 1 && Normal(helper[0].InnerText) == expected.Helpers[item.Property] && blocks.Count(b => b == call) == 1 && references.Length == 1 && references[0] == call;
            states[item.Property] = new(enabled, mentioned && !enabled);
        }
        return states;
    }
    public static void ReadIntoPlan(XmlDocument doc, InstallPlan p, List<string> notes)
    {
        var states = Inspect(doc);
        if (doc.InnerText.Contains("VIRTIO_DRIVE", StringComparison.OrdinalIgnoreCase))
            notes.Add(global::UnattendLocal.Localization.UiText.Source("text.105e9e960878"));
        foreach (var item in Items)
        {
            typeof(InstallPlan).GetProperty(item.Property)!.SetValue(p, states[item.Property].Enabled);
            if (states[item.Property].Ambiguous) notes.Add(global::UnattendLocal.Localization.UiText.Concat(new object?[] { item.Label, global::UnattendLocal.Localization.UiText.Source("text.350d0999f701") },new bool[]{false,false}));
        }
    }
    public static void ValidateImported(InstallPlan p, XmlDocument doc)
    {
        var states = Inspect(doc);
        foreach (var item in Items)
            if (Selected(p, item) && states[item.Property].Ambiguous)
                throw new ArgumentException(global::UnattendLocal.Localization.UiText.Concat(new object?[] { item.Label, global::UnattendLocal.Localization.UiText.Source("text.e8f1b99121fb") },new bool[]{false,false}));
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
            if (states[item.Property].Ambiguous) data.Notices = data.Notices.Append(global::UnattendLocal.Localization.UiText.Concat(new object?[] { item.Label, global::UnattendLocal.Localization.UiText.Source("text.7c51ab5dba61") },new bool[]{false,false})).Distinct().ToArray();
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
            data.Blockers = data.Blockers.Append(global::UnattendLocal.Localization.UiText.Source("text.7cb36903d99f")).Distinct().ToArray();
        }
    }
}
