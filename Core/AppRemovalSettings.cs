using System.Collections.Immutable;
using System.Collections.ObjectModel;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Xml;
using Schneegans.Unattend;
namespace UnattendLocal.Core;

public static partial class AppRemovalSettings
{
    public record Item(string Id, string Label, string Group, string Description);
    public static IReadOnlyList<Item> Items { get; } = new Item[]
    {
        new("RemoveInternetExplorer","Internet Explorer",global::UnattendLocal.Localization.UiText.Source("text.f74218924583"),global::UnattendLocal.Localization.UiText.Source("text.f3da28e082b7")),
        new("RemoveWindowsMediaPlayer",global::UnattendLocal.Localization.UiText.Source("text.52c03973d50b"),global::UnattendLocal.Localization.UiText.Source("text.f74218924583"),global::UnattendLocal.Localization.UiText.Source("text.3da81872b282")),
        new("RemoveZuneMusic",global::UnattendLocal.Localization.UiText.Source("text.21c49d63c4b2"),global::UnattendLocal.Localization.UiText.Source("text.b41719c11e9d"),global::UnattendLocal.Localization.UiText.Source("text.edb0886aab3b")),
        new("RemoveMediaFeatures",global::UnattendLocal.Localization.UiText.Source("text.1ce92d74fb62"),global::UnattendLocal.Localization.UiText.Source("text.f74218924583"),global::UnattendLocal.Localization.UiText.Source("text.190c3a15e23f")),
        new("RemoveWordPad",global::UnattendLocal.Localization.UiText.Source("text.7b54a2369698"),global::UnattendLocal.Localization.UiText.Source("text.f74218924583"),global::UnattendLocal.Localization.UiText.Source("text.939aca9887e9")),
        new("RemoveNotepadClassic",global::UnattendLocal.Localization.UiText.Source("text.dc35d7ca90b0"),global::UnattendLocal.Localization.UiText.Source("text.f74218924583"),global::UnattendLocal.Localization.UiText.Source("text.63ef88bdb2e0")),
        new("RemoveNotepad",global::UnattendLocal.Localization.UiText.Source("text.7249df4ebce8"),global::UnattendLocal.Localization.UiText.Source("text.b41719c11e9d"),global::UnattendLocal.Localization.UiText.Source("text.e081baea3dc6")),
        new("RemoveStepsRecorder",global::UnattendLocal.Localization.UiText.Source("text.bfbd036af535"),global::UnattendLocal.Localization.UiText.Source("text.f74218924583"),global::UnattendLocal.Localization.UiText.Source("text.6b6b7fd94812")),
        new("RemovePaint",global::UnattendLocal.Localization.UiText.Source("text.8ecb440e9860"),global::UnattendLocal.Localization.UiText.Source("text.f74218924583"),global::UnattendLocal.Localization.UiText.Source("text.ecdf64b721af")),
        new("RemovePaint3D",global::UnattendLocal.Localization.UiText.Source("text.de27f5429fa9"),global::UnattendLocal.Localization.UiText.Source("text.b41719c11e9d"),global::UnattendLocal.Localization.UiText.Source("text.e9ed9289671b")),
        new("RemoveSkype","Skype",global::UnattendLocal.Localization.UiText.Source("text.b41719c11e9d"),global::UnattendLocal.Localization.UiText.Source("text.219c1e87d4fb")),
        new("RemoveSnippingTool",global::UnattendLocal.Localization.UiText.Source("text.1efc0df4bb17"),global::UnattendLocal.Localization.UiText.Source("text.f74218924583"),global::UnattendLocal.Localization.UiText.Source("text.50febb545d67")),
        new("RemoveZuneVideo",global::UnattendLocal.Localization.UiText.Source("text.59aee36df2cf"),global::UnattendLocal.Localization.UiText.Source("text.b41719c11e9d"),global::UnattendLocal.Localization.UiText.Source("text.53a1c563f41e")),
        new("RemoveXboxApps",global::UnattendLocal.Localization.UiText.Source("text.89110256f71c"),global::UnattendLocal.Localization.UiText.Source("text.da1ce496d4f7"),global::UnattendLocal.Localization.UiText.Source("text.5a4af8c46435")),
        new("RemovePhotos",global::UnattendLocal.Localization.UiText.Source("text.7b50017ae47e"),global::UnattendLocal.Localization.UiText.Source("text.b41719c11e9d"),global::UnattendLocal.Localization.UiText.Source("text.88b4eb7c088e")),
        new("RemoveNews",global::UnattendLocal.Localization.UiText.Source("text.71d9a93075be"),global::UnattendLocal.Localization.UiText.Source("text.b41719c11e9d"),global::UnattendLocal.Localization.UiText.Source("text.7dbfdc0a7553")),
        new("RemoveWeather",global::UnattendLocal.Localization.UiText.Source("text.4d947325473f"),global::UnattendLocal.Localization.UiText.Source("text.b41719c11e9d"),global::UnattendLocal.Localization.UiText.Source("text.a5f29c8e919a")),
        new("RemovePeople",global::UnattendLocal.Localization.UiText.Source("text.2500666084b2"),global::UnattendLocal.Localization.UiText.Source("text.b41719c11e9d"),global::UnattendLocal.Localization.UiText.Source("text.2eb475afe395")),
        new("RemoveToDo","Microsoft To Do",global::UnattendLocal.Localization.UiText.Source("text.b41719c11e9d"),global::UnattendLocal.Localization.UiText.Source("text.37b50050840a")),
        new("RemoveSolitaire",global::UnattendLocal.Localization.UiText.Source("text.b322d54672c5"),global::UnattendLocal.Localization.UiText.Source("text.b41719c11e9d"),global::UnattendLocal.Localization.UiText.Source("text.0e64cd00071b")),
        new("RemoveStickyNotes",global::UnattendLocal.Localization.UiText.Source("text.6374c187518d"),global::UnattendLocal.Localization.UiText.Source("text.b41719c11e9d"),global::UnattendLocal.Localization.UiText.Source("text.699599ccb1df")),
        new("Remove3DViewer",global::UnattendLocal.Localization.UiText.Source("text.c1df15a09619"),global::UnattendLocal.Localization.UiText.Source("text.b41719c11e9d"),global::UnattendLocal.Localization.UiText.Source("text.eabaec3b207b")),
        new("RemoveMaps",global::UnattendLocal.Localization.UiText.Source("text.41ab2e0ecbec"),global::UnattendLocal.Localization.UiText.Source("text.b41719c11e9d"),global::UnattendLocal.Localization.UiText.Source("text.f68a5f70e78b")),
        new("RemoveVoiceRecorder",global::UnattendLocal.Localization.UiText.Source("text.bdba42901714"),global::UnattendLocal.Localization.UiText.Source("text.b41719c11e9d"),global::UnattendLocal.Localization.UiText.Source("text.11de607037c3")),
        new("RemoveClipchamp",global::UnattendLocal.Localization.UiText.Source("text.bd8de4274059"),global::UnattendLocal.Localization.UiText.Source("text.b41719c11e9d"),global::UnattendLocal.Localization.UiText.Source("text.5274493c0377")),
        new("RemovePowerAutomate",global::UnattendLocal.Localization.UiText.Source("text.fd7f0e88368f"),global::UnattendLocal.Localization.UiText.Source("text.b41719c11e9d"),global::UnattendLocal.Localization.UiText.Source("text.d69de47cc146")),
        new("RemoveWindowsTerminal",global::UnattendLocal.Localization.UiText.Source("text.05c2e339d73a"),global::UnattendLocal.Localization.UiText.Source("text.b41719c11e9d"),global::UnattendLocal.Localization.UiText.Source("text.ea09b6209c7d")),
        new("RemoveOneNote","OneNote",global::UnattendLocal.Localization.UiText.Source("text.b41719c11e9d"),global::UnattendLocal.Localization.UiText.Source("text.d79756697da8")),
        new("RemoveClock",global::UnattendLocal.Localization.UiText.Source("text.0f58fd2a2e0f"),global::UnattendLocal.Localization.UiText.Source("text.b41719c11e9d"),global::UnattendLocal.Localization.UiText.Source("text.797b9c0645b6")),
        new("RemoveCalculator",global::UnattendLocal.Localization.UiText.Source("text.1283ddbf6956"),global::UnattendLocal.Localization.UiText.Source("text.b41719c11e9d"),global::UnattendLocal.Localization.UiText.Source("text.f7e558e4f02e")),
        new("RemoveCamera",global::UnattendLocal.Localization.UiText.Source("text.dacf07026026"),global::UnattendLocal.Localization.UiText.Source("text.b41719c11e9d"),global::UnattendLocal.Localization.UiText.Source("text.8ddded301c05")),
        new("RemoveYourPhone",global::UnattendLocal.Localization.UiText.Source("text.a1d8189bdf46"),global::UnattendLocal.Localization.UiText.Source("text.b41719c11e9d"),global::UnattendLocal.Localization.UiText.Source("text.1004d69b336e")),
        new("RemoveGetHelp",global::UnattendLocal.Localization.UiText.Source("text.93997c622a9d"),global::UnattendLocal.Localization.UiText.Source("text.b41719c11e9d"),global::UnattendLocal.Localization.UiText.Source("text.ddf7de93683c")),
        new("RemoveFeedbackHub",global::UnattendLocal.Localization.UiText.Source("text.2f2fee6399a4"),global::UnattendLocal.Localization.UiText.Source("text.b41719c11e9d"),global::UnattendLocal.Localization.UiText.Source("text.d1a40c9ff649")),
        new("RemoveOffice365",global::UnattendLocal.Localization.UiText.Source("text.8ea945831ba5"),global::UnattendLocal.Localization.UiText.Source("text.b41719c11e9d"),global::UnattendLocal.Localization.UiText.Source("text.5b5095aa6bee")),
        new("RemoveGetStarted",global::UnattendLocal.Localization.UiText.Source("text.f56c6c82203b"),global::UnattendLocal.Localization.UiText.Source("text.b41719c11e9d"),global::UnattendLocal.Localization.UiText.Source("text.56bc0300f2cc")),
        new("RemoveCortana","Cortana",global::UnattendLocal.Localization.UiText.Source("text.b41719c11e9d"),global::UnattendLocal.Localization.UiText.Source("text.2514277b47a2")),
        new("RemoveMathInputPanel",global::UnattendLocal.Localization.UiText.Source("text.8f7e918e681c"),global::UnattendLocal.Localization.UiText.Source("text.f74218924583"),global::UnattendLocal.Localization.UiText.Source("text.ff948b1f950f")),
        new("RemovePowerShellISE","PowerShell ISE",global::UnattendLocal.Localization.UiText.Source("text.f74218924583"),global::UnattendLocal.Localization.UiText.Source("text.817fb97c022e")),
        new("RemoveOpenSSHClient",global::UnattendLocal.Localization.UiText.Source("text.df28c378930e"),global::UnattendLocal.Localization.UiText.Source("text.f74218924583"),global::UnattendLocal.Localization.UiText.Source("text.f51b2cf47fca")),
        new("RemoveOneDrive","OneDrive",global::UnattendLocal.Localization.UiText.Source("text.da1ce496d4f7"),global::UnattendLocal.Localization.UiText.Source("text.487185f6d4de")),
        new("RemoveTeams","Teams",global::UnattendLocal.Localization.UiText.Source("text.da1ce496d4f7"),global::UnattendLocal.Localization.UiText.Source("text.64f0296b7aee")),
        new("RemoveQuickAssist",global::UnattendLocal.Localization.UiText.Source("text.6776ea45bbca"),global::UnattendLocal.Localization.UiText.Source("text.f74218924583"),global::UnattendLocal.Localization.UiText.Source("text.50628c45f733")),
        new("RemoveFamily",global::UnattendLocal.Localization.UiText.Source("text.a70a77c75b1d"),global::UnattendLocal.Localization.UiText.Source("text.b41719c11e9d"),global::UnattendLocal.Localization.UiText.Source("text.0d8716d80a11")),
        new("RemoveMailCalendar",global::UnattendLocal.Localization.UiText.Source("text.6ee39db2d003"),global::UnattendLocal.Localization.UiText.Source("text.b41719c11e9d"),global::UnattendLocal.Localization.UiText.Source("text.dfa97585689b")),
        new("RemoveOutlook","Outlook（Windows）",global::UnattendLocal.Localization.UiText.Source("text.da1ce496d4f7"),global::UnattendLocal.Localization.UiText.Source("text.259ff3b56055")),
        new("RemoveDevHome",global::UnattendLocal.Localization.UiText.Source("text.6f873be85da4"),global::UnattendLocal.Localization.UiText.Source("text.da1ce496d4f7"),global::UnattendLocal.Localization.UiText.Source("text.0ea35575c206")),
        new("RemoveCopilot","Copilot",global::UnattendLocal.Localization.UiText.Source("text.da1ce496d4f7"),global::UnattendLocal.Localization.UiText.Source("text.a77bd1a1eef0")),
        new("RemoveBingSearch",global::UnattendLocal.Localization.UiText.Source("text.c6cd3a3b6eb2"),global::UnattendLocal.Localization.UiText.Source("text.b41719c11e9d"),global::UnattendLocal.Localization.UiText.Source("text.f1c31a4a4e9b")),
        new("RemoveMixedReality",global::UnattendLocal.Localization.UiText.Source("text.b477795edd91"),global::UnattendLocal.Localization.UiText.Source("text.b41719c11e9d"),global::UnattendLocal.Localization.UiText.Source("text.3e96e76752d8")),
        new("RemoveRecall","Recall",global::UnattendLocal.Localization.UiText.Source("text.f74218924583"),global::UnattendLocal.Localization.UiText.Source("text.b0b5cc8ff56a")),
        new("RemoveRdpClient",global::UnattendLocal.Localization.UiText.Source("text.849bed5a1d23"),global::UnattendLocal.Localization.UiText.Source("text.f74218924583"),global::UnattendLocal.Localization.UiText.Source("text.c8b025360c05")),
        new("RemoveWallet",global::UnattendLocal.Localization.UiText.Source("text.268002cb016d"),global::UnattendLocal.Localization.UiText.Source("text.b41719c11e9d"),global::UnattendLocal.Localization.UiText.Source("text.c85bb4c56fe7")),
        new("RemoveWindowsHello",global::UnattendLocal.Localization.UiText.Source("text.9fbfb283a153"),global::UnattendLocal.Localization.UiText.Source("text.f74218924583"),global::UnattendLocal.Localization.UiText.Source("text.ffd79ede8d57")),
        new("RemovePowerShell2","PowerShell 2.0",global::UnattendLocal.Localization.UiText.Source("text.f74218924583"),global::UnattendLocal.Localization.UiText.Source("text.bec24e9cb32f")),
        new("RemoveHandwriting",global::UnattendLocal.Localization.UiText.Source("text.0ee47923bdd9"),global::UnattendLocal.Localization.UiText.Source("text.f74218924583"),global::UnattendLocal.Localization.UiText.Source("text.2c66f90b220d")),
        new("RemoveSpeech",global::UnattendLocal.Localization.UiText.Source("text.75fa31f0c265"),global::UnattendLocal.Localization.UiText.Source("text.f74218924583"),global::UnattendLocal.Localization.UiText.Source("text.958f90531406")),
        new("RemoveOneSync",global::UnattendLocal.Localization.UiText.Source("text.f8acd91f5b29"),global::UnattendLocal.Localization.UiText.Source("text.f74218924583"),global::UnattendLocal.Localization.UiText.Source("text.4cc7e58b34b0")),
        new("RemoveGameAssist",global::UnattendLocal.Localization.UiText.Source("text.fe1e79485a8a"),global::UnattendLocal.Localization.UiText.Source("text.b41719c11e9d"),global::UnattendLocal.Localization.UiText.Source("text.a1e7c5e981b4")),
        new("RemoveStore","Microsoft Store",global::UnattendLocal.Localization.UiText.Source("text.da1ce496d4f7"),global::UnattendLocal.Localization.UiText.Source("text.40086dce285e")),
    };
    public static void Validate(InstallPlan p)
    {
        if (p.RemovedApps == null || p.RemovedApps.Count > Items.Count || p.RemovedApps.Any(id => id == null || !Items.Any(i => i.Id == id)) || p.RemovedApps.Distinct().Count() != p.RemovedApps.Count)
            throw new ArgumentException(global::UnattendLocal.Localization.UiText.Source("text.4af37c17bfaf"));
    }
    public static ImmutableList<Bloatware> Build(UnattendGenerator g, InstallPlan p)
    {
        Validate(p);
        return p.RemovedApps.Select(id => g.Lookup<Bloatware>(id)).ToImmutableList();
    }
    public static string Selection(InstallPlan p) => p.ImportedXml?.AppRemovalReadOnly==true ? global::UnattendLocal.Localization.UiText.Format("message.e89795aa39a0", new string[] { global::UnattendLocal.Localization.UiText.Arg((p.RemovedApps.Count), 0, null) }) : p.RemovedApps.Count == 0 ? global::UnattendLocal.Localization.UiText.Source("text.8f8abd294f17") : global::UnattendLocal.Localization.UiText.Concat(new object?[] { global::UnattendLocal.Localization.UiText.Format("message.d915fbd299e8", new string[] { global::UnattendLocal.Localization.UiText.Arg((p.RemovedApps.Count), 0, null) }), global::UnattendLocal.Localization.UiText.Join("、", Items.Where(i => p.RemovedApps.Contains(i.Id)).Select(i => i.Label), false) },new bool[]{false,false});
    public static string Summary(InstallPlan p) => global::UnattendLocal.Localization.UiText.Concat(new object?[] { global::UnattendLocal.Localization.UiText.Source("text.1ecc9cbc5019"), Selection(p) },new bool[]{false,false});
    private const string Root = @"C:\Windows\Setup\Scripts\";
    private static readonly string[] Stages = ["Specialize.ps1", "DefaultUser.ps1", "UserOnce.ps1"];
    private static string Normal(string s) => global::UnattendLocal.Localization.UiText.Join('\n', s.Replace("\r\n", "\n").Split('\n').Select(l => l.Trim()).Where(l => l.Length > 0), false);
    private static XmlElement[] Files(XmlDocument d, string name) => d.SelectNodes("//*[local-name()='File']")!.Cast<XmlElement>().Where(f => f.GetAttribute("path").Equals(Root + name, StringComparison.OrdinalIgnoreCase)).ToArray();
    private static List<string> Blocks(string body) => Regex.Matches(body.Replace("\r\n", "\n"), @"(?m)^\t\{\n(?<body>(?:\t\t[^\n]*\n)*)\t\};\n", RegexOptions.None, TimeSpan.FromSeconds(1)).Select(m => Normal(m.Groups["body"].Value)).ToList();
    private static Dictionary<string,string> wrappers = new();
    private static readonly Dictionary<string,string> helperTails = new();
    private static string oneDriveBlock = "";
    private static HashSet<string> runOnceCommands = new();
    private static string Skeleton(string body) => Normal(Regex.Replace(body.Replace("\r\n","\n"), @"(?m)^\t\{\n(?:\t\t[^\n]*\n)*\t\};\n", "", RegexOptions.None, TimeSpan.FromSeconds(1)));
    private record Template(Dictionary<string,string> Helpers, Dictionary<string,string[]> Custom, Dictionary<string,string[]> Selectors);
    private static readonly Lazy<Dictionary<string,Template>> Templates = new(CreateTemplates);
    private static Dictionary<string,Template> CreateTemplates()
    {
        var g = new UnattendGenerator();
        XmlDocument Make(ImmutableList<Bloatware> apps) => g.GenerateXml(Configuration.Default with { Bloatwares = apps });
        var baseline = Make([]);
        var all = Make(g.Bloatwares.Values.ToImmutableList());
        var alternate=g.GenerateXml(Configuration.Default with {Bloatwares=g.Bloatwares.Values.ToImmutableList(),HidePowerShellWindows=!Configuration.Default.HidePowerShellWindows});
        runOnceCommands=new[]{all,alternate}.SelectMany(d=>Files(d,"DefaultUser.ps1")).SelectMany(f=>Blocks(f.InnerText)).Where(b=>b.Contains("RunOnce")&&b.Contains("UnattendedSetup")&&b.Contains(Root+"UserOnce.ps1")).ToHashSet();
        wrappers=Stages.ToDictionary(stage=>stage,stage=>Skeleton(Files(all,stage).Single().InnerText));
        var empty = Stages.ToDictionary(s => s, s => Files(baseline,s).SelectMany(f => Blocks(f.InnerText)).ToHashSet());
        var result = new Dictionary<string,Template>();
        foreach (var item in Items)
        {
            var bw = g.Lookup<Bloatware>(item.Id); var doc = Make([bw]);
            var helpers = new Dictionary<string,string>(); var selectors = new Dictionary<string,string[]>();
            foreach (string type in new[]{"Package", "Capability", "Feature"})
            {
                string name = "Remove" + type + ".ps1"; var f = Files(doc,name);
                if (f.Length == 0) continue;
                var match = Regex.Match(f[0].InnerText.Replace("\r\n","\n"), @"\A\s*\$selectors = @\(\n(?<values>(?:\t'[^'\n]+';\n)+)\);\n(?<tail>[\s\S]+)\z", RegexOptions.None, TimeSpan.FromSeconds(1));
                if (!match.Success) throw new InvalidOperationException(global::UnattendLocal.Localization.UiText.Source("text.0145b94ada40"));
                helpers[name] = Normal(match.Groups["tail"].Value);
                helperTails.TryAdd(name, match.Groups["tail"].Value);
                selectors[name] = Regex.Matches(match.Groups["values"].Value,@"'([^']+)';").Select(m => m.Groups[1].Value).ToArray();
            }
            var custom = Stages.ToDictionary(s => s, s => Files(doc,s).SelectMany(f=>Blocks(f.InnerText)).Where(b => !empty[s].Contains(b) && !(b.Contains("RunOnce") && b.Contains(Root+"UserOnce.ps1")) && !b.StartsWith("& '" + Root + "Remove", StringComparison.Ordinal)).ToArray());
            if (item.Id == "RemoveOneDrive") oneDriveBlock = RawBlocks(Files(doc,"Specialize.ps1").Single().InnerText).Single(m => Normal(m.Groups["body"].Value) == custom["Specialize.ps1"].Single()).Value;
            result[item.Id] = new(helpers, custom, selectors);
        }
        return result;
    }
    public record Source(string[] Selected, bool Ambiguous);
    public static Source Inspect(XmlDocument doc)
    {
        doc=XmlImportPaths.Inspection(doc);
        var normalized = NormalizeLegacy(doc);
        if (normalized != null) return InspectCurrent(normalized);
        var state = InspectCurrent(doc);
        return HasLegacyRemoval(doc) ? state with { Ambiguous = true } : state;
    }
    private static Source InspectCurrent(XmlDocument doc)
    {
        var templates = Templates.Value; var selectors = new Dictionary<string,HashSet<string>>(); bool ambiguous = false;
        var blocks = Stages.ToDictionary(s=>s,s=>AdditionalSetupSettings.KnownBlocks(doc,s));
        foreach(string stage in Stages)
        {
            var files=Files(doc,stage);
            if(files.Length!=1 || !NativeScriptPipeline.StageActive(doc,stage) || Skeleton(files[0].InnerText)!=wrappers[stage])blocks[stage]=[];
            if(stage!="UserOnce.ps1")
            {
                var ns=new XmlNamespaceManager(doc.NameTable);ns.AddNamespace("u",XmlMerge.Namespace);
                var paths=doc.SelectNodes("//u:settings[@pass='specialize']//u:Path",ns)!.Cast<XmlElement>();
                bool active=paths.Any(p=>Regex.IsMatch(p.InnerText, "^powershell\\.exe -WindowStyle \"(?:Hidden|Normal)\" -ExecutionPolicy \"Unrestricted\" -NoProfile -File \""+Regex.Escape(Root+stage)+"\"$",RegexOptions.IgnoreCase,TimeSpan.FromSeconds(1)));
                if(!active)blocks[stage]=[];
            }
            else if(blocks["DefaultUser.ps1"].Count(runOnceCommands.Contains)!=1)blocks[stage]=[];
        }
        foreach (string name in new[]{"RemovePackage.ps1","RemoveCapability.ps1","RemoveFeature.ps1"})
        {
            var files = Files(doc,name); string call = "& '" + Root + name + "';";
            bool mentioned = files.Length > 0 || Files(doc,"Specialize.ps1").Any(f=>f.InnerText.Contains(Root+name,StringComparison.OrdinalIgnoreCase));
            if (!mentioned) continue;
            var match = files.Length==1 ? Regex.Match(files[0].InnerText.Replace("\r\n","\n"), @"\A\s*\$selectors = @\(\n(?<values>(?:\t'[^'\n]+';\n)+)\);\n(?<tail>[\s\S]+)\z", RegexOptions.None, TimeSpan.FromSeconds(1)) : Match.Empty;
            var expected = templates.Values.First(t=>t.Helpers.ContainsKey(name));
            bool valid = match.Success && Normal(match.Groups["tail"].Value)==expected.Helpers[name] && blocks["Specialize.ps1"].Count(b=>b==call)==1 && Files(doc,"Specialize.ps1").Length==1;
            if (!valid) { ambiguous=true; continue; }
            var values = Regex.Matches(match.Groups["values"].Value,@"'([^']+)';").Select(m=>m.Groups[1].Value).ToArray();
            selectors[name] = values.ToHashSet();
            if(values.Distinct().Count()!=values.Length)ambiguous=true;
        }
        var chosen = new List<string>();
        foreach(var pair in templates)
        {
            var t=pair.Value;
            bool selected = t.Selectors.All(p=>selectors.TryGetValue(p.Key,out var values)&&p.Value.All(values.Contains)) && t.Custom.All(p=>p.Value.All(b=>blocks[p.Key].Count(v=>v==b)==1));
            if(selected)chosen.Add(pair.Key);
        }
        foreach(var pair in selectors)
        {
            var accounted=chosen.SelectMany(id=>templates[id].Selectors.GetValueOrDefault(pair.Key,[])).ToHashSet();
            if(pair.Value.Any(v=>!accounted.Contains(v)))ambiguous=true;
        }
        // A partial or altered custom side effect must never be replaced by a new checkbox setting.
        foreach(var pair in templates.Where(p=>p.Value.Custom.Any(c=>c.Value.Length>0)))
        {
            if(chosen.Contains(pair.Key))continue;
            if(pair.Value.Custom.Any(p=>p.Value.Any(b=>blocks[p.Key].Contains(b))))ambiguous=true;
        }
        // Detect recognizable custom markers even if their command/block was edited.
        foreach(string marker in new[]{"OneDrive.lnk", "txtfilelegacy", "notepad.exe,-470", "Microsoft\\Notepad", "Communications", "GameDVR", "UScheduler_Oobe", "OneDriveSetup", "ConfigureChatAutoInstall", "ShowStoreBanner", "OutlookUpdate", "DevHomeUpdate", "TurnOffWindowsCopilot", "AppCaptureEnabled", "Microsoft.Windows.Ai.Copilot.Provider", "Internet Explorer\\LowRegistry"})
        {
            var owner=templates.Where(p=>p.Value.Custom.Any(c=>c.Value.Any(b=>b.Contains(marker,StringComparison.OrdinalIgnoreCase)))).Select(p=>p.Key).ToArray();
            if(owner.Length>0 && Stages.Any(stage=>Files(doc,stage).Any(f=>{
                string rest=Normal(f.InnerText);
                foreach(var id in chosen)foreach(var command in templates[id].Custom[stage])rest=rest.Replace(command,"",StringComparison.Ordinal);
                return rest.Contains(marker,StringComparison.OrdinalIgnoreCase);
            })))ambiguous=true;
        }
        return new(chosen.ToArray(),ambiguous);
    }
    public static Source ReadIntoPlan(XmlDocument doc,InstallPlan p,List<string> notes)
    {
        var state=Inspect(doc); p.RemovedApps=new(state.Selected);
        if (!state.Ambiguous && HasLegacyRemoval(doc)) notes.Add(LegacyDescription);
        if(state.Ambiguous)notes.Add(ReadOnlyDescription);
        return state;
    }
    public static readonly string ReadOnlyDescription = global::UnattendLocal.Localization.UiText.Source("text.738bd90f56b6");
    public static void ValidateImported(InstallPlan p,XmlDocument doc)
    {
        var state=Inspect(doc);
        if(state.Ambiguous&&!state.Selected.Order().SequenceEqual(p.RemovedApps.Order()))throw new ArgumentException(global::UnattendLocal.Localization.UiText.Source("text.dc0d24ca577f"));
    }
    public static void NormalizeLoaded(InstallPlan p,JsonElement json)
    {
        UpgradeLegacyPlan(p);
        if(json.TryGetProperty(nameof(InstallPlan.RemovedApps),out _) || p.ImportedXml is not {OriginalOmitted:false} data || data.SourceXml.Length==0 || data.BaselineXml.Length==0)return;
        var before=JsonSerializer.Deserialize<InstallPlan>(JsonSerializer.Serialize(p))!;before.ImportedXml=null;before.ConfirmWipeDisk=before.ConfirmFormatPartition=before.ConfirmDiskScript=true;
        var notes=new List<string>();ReadIntoPlan(XmlMerge.Read(data.SourceXml),p,notes);data.Notices=data.Notices.Concat(notes).Distinct().ToArray();
        if(p.RemovedApps.Count==0)return;
        var after=JsonSerializer.Deserialize<InstallPlan>(JsonSerializer.Serialize(before))!;after.RemovedApps=new(p.RemovedApps);
        try {var g=new GeneratorService();data.BaselineXml=XmlMerge.Apply(data.BaselineXml,Encoding.UTF8.GetString(g.GenerateImportBaseline(before)),XmlMerge.Read(Encoding.UTF8.GetString(g.GenerateImportBaseline(after)))).OuterXml;}
        catch(Exception e)when(e is ArgumentException or ConfigurationException or XmlException){data.Blockers=data.Blockers.Append(global::UnattendLocal.Localization.UiText.Source("text.4879ed8ab741")).Distinct().ToArray();}
    }
}
