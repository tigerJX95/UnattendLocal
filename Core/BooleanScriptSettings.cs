using System.Text.RegularExpressions;
using System.Xml;
using Schneegans.Unattend;

namespace UnattendLocal.Core;

// Match complete native blocks and their execution stage, never bare value
// names from comments, opposite registry values, or arbitrary custom scripts.
internal static class BooleanScriptSettings
{
    private static readonly Dictionary<string, string> Markers = new()
    {
        [nameof(InstallPlan.DisableFastStartup)]="HiberbootEnabled", [nameof(InstallPlan.EnableLongPaths)]="LongPathsEnabled",
        [nameof(InstallPlan.DisableWindowsUpdate)]="PauseWindowsUpdate", [nameof(InstallPlan.DisableAppSuggestions)]="DisableWindowsConsumerFeatures",
        [nameof(InstallPlan.DisablePointerPrecision)]="MouseThreshold1", [nameof(InstallPlan.HideEdgeFre)]="HideFirstRunExperience",
        [nameof(InstallPlan.DisableEdgeStartupBoost)]="StartupBoostEnabled", [nameof(InstallPlan.ClassicContextMenu)]="{86ca1aa0-34aa-4e8b-a509-50c905bae2a2}",
        [nameof(InstallPlan.ShowFileExtensions)]="HideFileExt", [nameof(InstallPlan.HideInfoTip)]="ShowInfoTip",
        [nameof(InstallPlan.LaunchToThisPC)]="LaunchTo", [nameof(InstallPlan.ShowEndTask)]="TaskbarEndTask",
        [nameof(InstallPlan.DisableWidgets)]="AllowNewsAndInterests", [nameof(InstallPlan.LeftTaskbar)]="TaskbarAl",
        [nameof(InstallPlan.HideTaskViewButton)]="ShowTaskViewButton", [nameof(InstallPlan.ShowAllTrayIcons)]="ShowAllTrayIcons.xml",
        [nameof(InstallPlan.DisableBingResults)]="DisableSearchBoxSuggestions", [nameof(InstallPlan.DeleteEdgeDesktopIcon)]="Microsoft Edge.lnk",
        [nameof(InstallPlan.TurnOffSystemSounds)]="AppEvents\\Schemes", [nameof(InstallPlan.PreventDeviceEncryption)]="PreventDeviceEncryption"
    };
    private static readonly string[] Stages = ["Specialize.ps1", "DefaultUser.ps1", "FirstLogon.ps1", "UserOnce.ps1"];
    private const string Root = @"C:\Windows\Setup\Scripts\";
    private const string Pattern = @"(?m)^\t\{\n(?<body>(?:\t\t[^\n]*\n)*)\t\};\n";
    private static string Normal(string text) => global::UnattendLocal.Localization.UiText.Join('\n', text.Replace("-WindowStyle \"Hidden\"", "-WindowStyle \"Normal\"", StringComparison.OrdinalIgnoreCase).Replace("-WindowStyle \\\"\"Hidden\\\"\"", "-WindowStyle \\\"\"Normal\\\"\"", StringComparison.OrdinalIgnoreCase).Replace("\r\n", "\n").Split('\n').Select(l => l.Trim()).Where(l => l.Length > 0), false);
    private static Dictionary<string,string> Files(XmlDocument doc) => doc.SelectNodes("//*[local-name()='File']")!.Cast<XmlElement>().ToDictionary(f => f.GetAttribute("path"), f => f.InnerText, StringComparer.OrdinalIgnoreCase);
    private record Template(Dictionary<string,string[]> Blocks, Dictionary<string,string> Helpers);
    private static readonly Lazy<Dictionary<string, Template>> Templates = new(() =>
    {
        var generator = new UnattendGenerator();
        var result = new Dictionary<string,Template>();
        foreach (string property in Markers.Keys)
        {
            var off = Configuration.Default with { };
            typeof(Configuration).GetProperty(property)!.SetValue(off, false);
            var on = off with { };
            typeof(Configuration).GetProperty(property)!.SetValue(on, true);
            var before = Files(XmlImportPaths.Inspection(generator.GenerateXml(off))); var after = Files(XmlImportPaths.Inspection(generator.GenerateXml(on)));
            string[] Blocks(string text) => Regex.Matches(text.Replace("\r\n", "\n"), Pattern, RegexOptions.None, TimeSpan.FromSeconds(1)).Select(m => Normal(m.Groups["body"].Value)).ToArray();
            var blocks = Stages.ToDictionary(n => n, n => Blocks(after.GetValueOrDefault(Root+n, "")).Except(Blocks(before.GetValueOrDefault(Root+n,""))).ToArray());
            var helpers = after.Where(f => !Stages.Any(n => f.Key.Equals(Root+n, StringComparison.OrdinalIgnoreCase)) && (!before.TryGetValue(f.Key, out string? previous) || Normal(previous) != Normal(f.Value))).ToDictionary(f => f.Key, f => Normal(f.Value));
            result[property] = new(blocks, helpers);
        }
        return result;
    });
    internal static void ReadIntoPlan(XmlDocument original, InstallPlan plan, List<string> notes)
    {
        var doc = XmlImportPaths.Inspection(original);
        var files = doc.SelectNodes("//*[local-name()='File']")!.Cast<XmlElement>().ToArray();
        var known = Stages.ToDictionary(n => n, n => AdditionalSetupSettings.KnownBlocks(doc, n, allowTrailingContent: true));
        var text = files.Select(f => f.InnerText).Concat(doc.SelectNodes("//*[local-name()='Path' or local-name()='CommandLine']")!.Cast<XmlElement>().Select(n => n.InnerText)).ToArray();
        foreach (var (property, marker) in Markers)
        {
            var template = Templates.Value[property];
            bool enabled = template.Blocks.Any(b => b.Value.Length > 0) && template.Blocks.All(b => b.Value.All(block => known[b.Key].Count(actual => Normal(actual) == block) == 1))
                && template.Helpers.All(h => files.Count(f => f.GetAttribute("path").Equals(h.Key, StringComparison.OrdinalIgnoreCase) && Normal(f.InnerText) == h.Value) == 1);
            // Contradictory or duplicated marker-bearing commands are ambiguous.
            var expectedLines = template.Blocks.SelectMany(b => b.Value).Concat(template.Helpers.Values).SelectMany(b => b.Split('\n')).Where(l => l.Contains(marker, StringComparison.OrdinalIgnoreCase)).ToArray();
            var actualLines = text.SelectMany(t => Normal(t).Split('\n')).Where(l => l.Contains(marker, StringComparison.OrdinalIgnoreCase)).ToArray();
            enabled &= actualLines.Length == expectedLines.Length && expectedLines.All(l => actualLines.Count(a => a == l) == expectedLines.Count(a => a == l));
            typeof(InstallPlan).GetProperty(property)!.SetValue(plan, enabled);
            if (!enabled && actualLines.Length > 0) notes.Add(global::UnattendLocal.Localization.UiText.Concat(new object?[] { Recommendations.SettingLabel(property), global::UnattendLocal.Localization.UiText.Source("text.2fc0c1cc1a56") },new bool[]{false,false}));
        }
    }
}
