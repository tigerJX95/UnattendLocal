using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Xml;
using Schneegans.Unattend;
namespace UnattendLocal.Core;

// Use the fixed upstream generator for both payloads and inspect actual XML before editing.
public static class SystemMaintenanceSettings
{
    private const string Root = @"C:\Windows\Setup\Scripts\";
    private const string StagePath = Root + "Specialize.ps1";
    private const string TaskPath = Root + "MoveActiveHours.xml";
    private const string BlockPattern = @"(?m)^\t\{\n(?<body>(?:\t\t[^\n]*\n)*)\t\};\n";
    public record Item(string Property, string Label, string[] Commands, string[] Markers, string? AuxiliaryPath = null);
    public static IReadOnlyList<Item> Items { get; } = new Item[]
    {
        new(nameof(InstallPlan.DisableLastAccess), global::UnattendLocal.Localization.UiText.Source("text.ad9c574533ea"), [
            "fsutil.exe behavior set disableLastAccess 1;"
        ], ["disableLastAccess", "NtfsDisableLastAccessUpdate"]),
        new(nameof(InstallPlan.PreventAutomaticReboot), global::UnattendLocal.Localization.UiText.Source("text.dbf530112040"), [
            "reg.exe add \"HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows\\WindowsUpdate\\AU\" /v AUOptions /t REG_DWORD /d 4 /f;",
            "reg.exe add \"HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows\\WindowsUpdate\\AU\" /v NoAutoRebootWithLoggedOnUsers /t REG_DWORD /d 1 /f;",
            "Register-ScheduledTask -TaskName 'MoveActiveHours' -Xml $( Get-Content -LiteralPath 'C:\\Windows\\Setup\\Scripts\\MoveActiveHours.xml' -Raw );"
        ], ["AUOptions", "NoAutoRebootWithLoggedOnUsers", "MoveActiveHours", "ActiveHoursStart", "ActiveHoursEnd", "SmartActiveHoursState"], TaskPath)
    };
    public static readonly string RestartDescription = global::UnattendLocal.Localization.UiText.Source("text.95b4206761d4");
    public static readonly string LastAccessDescription = global::UnattendLocal.Localization.UiText.Source("text.b908aed410d5");
    private static string Normal(string body) => global::UnattendLocal.Localization.UiText.Join('\n', body.Replace("\r\n", "\n").Split('\n').Select(l => l.Trim()).Where(l => l.Length > 0), false);
    private static string WindowStyle(string command) => Regex.Replace(command, "-WindowStyle \"(?:Hidden|Normal)\"", "-WindowStyle \"Hidden\"", RegexOptions.None, TimeSpan.FromSeconds(1));
    private static string Skeleton(string body) => Normal(Regex.Replace(body.Replace("\r\n", "\n"), BlockPattern, "", RegexOptions.None, TimeSpan.FromSeconds(1)));
    private record Template(string Wrapper, string Task, string Extraction, string ExtractionCommand);
    private static readonly Lazy<Template> Expected = new(() =>
    {
        var doc = new UnattendGenerator().GenerateXml(Configuration.Default with { DisableLastAccess = true, PreventAutomaticReboot = true });
        var files = doc.SelectNodes("//*[local-name()='File']")!.Cast<XmlElement>();
        string extraction = doc.SelectSingleNode("//*[local-name()='ExtractScript']")!.InnerText;
        string extractionCommand = doc.SelectNodes("//*[local-name()='Path']")!.Cast<XmlElement>().Single(p => p.InnerText.Contains("Extensions.ExtractScript", StringComparison.Ordinal)).InnerText;
        return new(Skeleton(files.Single(f => f.GetAttribute("path") == StagePath).InnerText),
            Normal(files.Single(f => f.GetAttribute("path") == TaskPath).InnerText), Normal(extraction), extractionCommand);
    });
    // Shared by the audit reader; rejects altered, duplicated, or inactive native execution pipelines.
    internal static string[] KnownSpecializeBlocks(XmlDocument doc)
    {
        var expected = Expected.Value;
        var files = doc.SelectNodes("//*[local-name()='File']")!.Cast<XmlElement>().ToArray();
        var stage = files.Where(f => f.GetAttribute("path").Equals(StagePath, StringComparison.OrdinalIgnoreCase)).ToArray();
        var ns = new XmlNamespaceManager(doc.NameTable); ns.AddNamespace("u", XmlMerge.Namespace);
        var paths = doc.SelectNodes("//u:settings[@pass='specialize']//u:Path", ns)!.Cast<XmlElement>().ToArray();
        var extraction = doc.SelectNodes("//*[local-name()='ExtractScript']")!.Cast<XmlElement>().ToArray();
        bool extracted = extraction.Length == 1 && Normal(extraction[0].InnerText) == expected.Extraction && paths.Count(p => WindowStyle(p.InnerText) == WindowStyle(expected.ExtractionCommand)) == 1;
        bool active = paths.Count(p => Regex.IsMatch(p.InnerText, "^powershell\\.exe -WindowStyle \"(?:Hidden|Normal)\" -ExecutionPolicy \"Unrestricted\" -NoProfile -File \"" + Regex.Escape(StagePath) + "\"$", RegexOptions.IgnoreCase, TimeSpan.FromSeconds(1))) == 1;
        bool knownStage = NativeScriptPipeline.StageActive(doc, "Specialize.ps1") && extracted && stage.Length == 1 && active && Skeleton(stage[0].InnerText) == expected.Wrapper;
        return knownStage ? Regex.Matches(stage[0].InnerText.Replace("\r\n", "\n"), BlockPattern, RegexOptions.None, TimeSpan.FromSeconds(1)).Select(m => Normal(m.Groups["body"].Value)).ToArray() : [];
    }
    public record State(bool Enabled, bool Ambiguous);
    public static Dictionary<string, State> Inspect(XmlDocument doc)
    {
        var expected = Expected.Value;
        var files = doc.SelectNodes("//*[local-name()='File']")!.Cast<XmlElement>().ToArray();
        var ns = new XmlNamespaceManager(doc.NameTable); ns.AddNamespace("u", XmlMerge.Namespace);
        var paths = doc.SelectNodes("//u:settings[@pass='specialize']//u:Path", ns)!.Cast<XmlElement>().ToArray();
        var blocks = KnownSpecializeBlocks(doc);
        var result = new Dictionary<string, State>();
        foreach (var item in Items)
        {
            var auxiliary = item.AuxiliaryPath == null ? [] : files.Where(f => f.GetAttribute("path").Equals(item.AuxiliaryPath, StringComparison.OrdinalIgnoreCase)).ToArray();
            bool validAuxiliary = item.AuxiliaryPath == null || (auxiliary.Length == 1 && Normal(auxiliary[0].InnerText) == expected.Task);
            var relevant = files.Where(f => !auxiliary.Contains(f)).SelectMany(f => Normal(f.InnerText).Split('\n')).Concat(paths.Select(p => p.InnerText)).Where(line => item.Markers.Any(marker => line.Contains(marker, StringComparison.OrdinalIgnoreCase))).ToArray();
            bool mentioned = relevant.Length > 0 || auxiliary.Length > 0;
            // Upstream appends the two restart policies as one block, then task registration as another.
            var commandGroups = item.AuxiliaryPath == null ? new[] { item.Commands } : new[] { item.Commands[..2], item.Commands[2..] };
            bool enabled = validAuxiliary && commandGroups.All(group => blocks.Count(b => b == Normal(global::UnattendLocal.Localization.UiText.Join('\n', group, false))) == 1) && relevant.Length == item.Commands.Length && item.Commands.All(c => relevant.Count(line => line == Normal(c)) == 1);
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
            if (states[item.Property].Ambiguous) notes.Add(global::UnattendLocal.Localization.UiText.Concat(new object?[] { item.Label, global::UnattendLocal.Localization.UiText.Source("text.6f82f20e70c1") },new bool[]{false,false}));
        }
    }
    public static void ValidateImported(InstallPlan p, XmlDocument doc)
    {
        var states = Inspect(doc);
        foreach (var item in Items)
            if (states[item.Property].Ambiguous && (bool)typeof(InstallPlan).GetProperty(item.Property)!.GetValue(p)!)
                throw new ArgumentException(global::UnattendLocal.Localization.UiText.Concat(new object?[] { item.Label, global::UnattendLocal.Localization.UiText.Source("text.3f1e7bc717a9") },new bool[]{false,false}));
    }
    public static string Summary(InstallPlan p) => global::UnattendLocal.Localization.UiText.Concat(new object?[] { global::UnattendLocal.Localization.UiText.Source("text.79038a934b6d"), (p.PreventAutomaticReboot ? global::UnattendLocal.Localization.UiText.Source("text.b397d0495fe4") : global::UnattendLocal.Localization.UiText.Source("text.4e9901953ce4")), global::UnattendLocal.Localization.UiText.Source("text.e359bd7012df"), (p.DisableLastAccess ? global::UnattendLocal.Localization.UiText.Source("text.dcbfd72d1427") : global::UnattendLocal.Localization.UiText.Source("text.4e9901953ce4")) },new bool[]{false,false,false,false});
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
            if (states[item.Property].Ambiguous) data.Notices = data.Notices.Append(global::UnattendLocal.Localization.UiText.Concat(new object?[] { item.Label, global::UnattendLocal.Localization.UiText.Source("text.aaf57dc5718a") },new bool[]{false,false})).Distinct().ToArray();
        }
        if (!missing.Any(i => states[i.Property].Enabled)) return;
        var after = JsonSerializer.Deserialize<InstallPlan>(JsonSerializer.Serialize(before))!;
        foreach (var item in missing) typeof(InstallPlan).GetProperty(item.Property)!.SetValue(after, states[item.Property].Enabled);
        try
        {
            var g = new GeneratorService();
            data.BaselineXml = XmlMerge.Apply(data.BaselineXml, Encoding.UTF8.GetString(g.GenerateImportBaseline(before)), XmlMerge.Read(Encoding.UTF8.GetString(g.GenerateImportBaseline(after)))).OuterXml;
        }
        catch (Exception e) when (e is ArgumentException or ConfigurationException or XmlException)
        {
            data.Blockers = data.Blockers.Append(global::UnattendLocal.Localization.UiText.Source("text.145657a4454c")).Distinct().ToArray();
        }
    }
}
