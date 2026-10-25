using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Xml;

namespace UnattendLocal.Core;

public static class CommonSystemSettings
{
    public record Item(string Property, string Label, string File, string[] Commands);
    public static IReadOnlyList<Item> Items { get; } = new Item[]
    {
        new(nameof(InstallPlan.DisableAutomaticRestartSignOn), global::UnattendLocal.Localization.UiText.Source("text.9328734717b0"), "Specialize.ps1", [
            "reg.exe add \"HKLM\\Software\\Microsoft\\Windows\\CurrentVersion\\Policies\\System\" /v \"DisableAutomaticRestartSignOn\" /t REG_DWORD /d 1 /f;"
        ]),
        new(nameof(InstallPlan.PreventDeviceApps), global::UnattendLocal.Localization.UiText.Source("text.e107f7886578"), "Specialize.ps1", [
            "reg.exe add \"HKLM\\Software\\Microsoft\\Windows\\CurrentVersion\\Device Metadata\" /v \"PreventDeviceMetadataFromNetwork\" /t REG_DWORD /d 1 /f;",
            "reg.exe add \"HKLM\\Software\\Microsoft\\Windows\\CurrentVersion\\Device Installer\" /v \"DisableCoInstallers\" /t REG_DWORD /d 1 /f;"
        ]),
        new(nameof(InstallPlan.DeleteWindowsOld), global::UnattendLocal.Localization.UiText.Source("text.1fa60cde8f71"), "FirstLogon.ps1", [
            "cmd.exe /c \"rmdir C:\\Windows.old\";"
        ])
    };
    private static string Normalize(string line) => Regex.Replace(line.Trim(), @"\s+", " ");
    private static Dictionary<string, (bool Enabled, bool Ambiguous)> Read(XmlDocument doc, List<string> notes)
    {
        var files = doc.SelectNodes("//*[local-name()='File']")!.Cast<XmlElement>().ToArray();
        var result = new Dictionary<string, (bool Enabled, bool Ambiguous)>();
        foreach (var item in Items)
        {
            var matches = files.Where(f => f.GetAttribute("path").Equals("C:\\Windows\\Setup\\Scripts\\" + item.File, StringComparison.OrdinalIgnoreCase)).ToArray();
            var lines = matches.SelectMany(f => f.InnerText.Replace("\r\n", "\n").Split('\n')).Select(Normalize).ToArray();
            var markers = item.Property switch
            {
                nameof(InstallPlan.DisableAutomaticRestartSignOn) => new[] { "DisableAutomaticRestartSignOn" },
                nameof(InstallPlan.PreventDeviceApps) => new[] { "PreventDeviceMetadataFromNetwork", "DisableCoInstallers" },
                _ => new[] { "Windows.old" }
            };
            var relevant = lines.Where(l =>
                (l.StartsWith("reg.exe ", StringComparison.OrdinalIgnoreCase) || l.StartsWith("cmd.exe /c ", StringComparison.OrdinalIgnoreCase)) &&
                markers.Any(m => l.Contains(m, StringComparison.OrdinalIgnoreCase))).ToArray();
            // Require the known complete command group. Partial or altered payloads remain untouched.
            bool enabled = matches.Length == 1 && relevant.Length == item.Commands.Length &&
                item.Commands.All(c => relevant.Count(l => l.Equals(Normalize(c), StringComparison.OrdinalIgnoreCase)) == 1);
            bool ambiguous = relevant.Length > 0 && !enabled;
            result[item.Property] = (enabled, ambiguous);
            if (ambiguous)
                notes.Add(global::UnattendLocal.Localization.UiText.Concat(new object?[] { item.Label, global::UnattendLocal.Localization.UiText.Source("text.eac899d61077") },new bool[]{false,false}));
        }
        return result;
    }
    public static Dictionary<string, bool> Inspect(XmlDocument doc, List<string> notes) =>
        Read(doc, notes).ToDictionary(pair => pair.Key, pair => pair.Value.Enabled);
    public static void ValidateImported(InstallPlan plan, XmlDocument source)
    {
        var states = Read(source, new());
        foreach (var item in Items)
            if (states[item.Property].Ambiguous && (bool)typeof(InstallPlan).GetProperty(item.Property)!.GetValue(plan)!)
                throw new ArgumentException(global::UnattendLocal.Localization.UiText.Concat(new object?[] { item.Label, global::UnattendLocal.Localization.UiText.Source("text.53e322edd701") },new bool[]{false,false}));
    }
    public static void ReadIntoPlan(XmlDocument doc, InstallPlan plan, List<string> notes)
    {
        foreach (var pair in Inspect(doc, notes))
            typeof(InstallPlan).GetProperty(pair.Key)!.SetValue(plan, pair.Value);
    }
    public static string Summary(InstallPlan p) =>
        global::UnattendLocal.Localization.UiText.Concat(new object?[] { global::UnattendLocal.Localization.UiText.Format("message.28dada809aea", new string[] { global::UnattendLocal.Localization.UiText.Arg(((p.DisableAutomaticRestartSignOn ? global::UnattendLocal.Localization.UiText.Source("text.c957b98168df") : global::UnattendLocal.Localization.UiText.Source("text.4e9901953ce4"))), 0, null) }), global::UnattendLocal.Localization.UiText.Format("message.c4a29ec48706", new string[] { global::UnattendLocal.Localization.UiText.Arg(((p.PreventDeviceApps ? global::UnattendLocal.Localization.UiText.Source("text.f7bba1f477ca") : global::UnattendLocal.Localization.UiText.Source("text.4e9901953ce4"))), 0, null) }), global::UnattendLocal.Localization.UiText.Format("message.b0b34ceecbaa", new string[] { global::UnattendLocal.Localization.UiText.Arg(((p.DeleteWindowsOld ? global::UnattendLocal.Localization.UiText.Source("text.e3c3a8f4b07c") : global::UnattendLocal.Localization.UiText.Source("text.44d62c15f116"))), 0, null) }) },new bool[]{false,false,false});

    public static void NormalizeLoaded(InstallPlan plan, JsonElement json)
    {
        if (plan.ImportedXml is not { OriginalOmitted: false } data || data.SourceXml.Length == 0 || data.BaselineXml.Length == 0)
            return;
        var missing = Items.Where(i => !json.TryGetProperty(i.Property, out _)).ToArray();
        if (missing.Length == 0) return;
        var notes = new List<string>();
        var actual = Inspect(XmlMerge.Read(data.SourceXml), notes);
        var before = JsonSerializer.Deserialize<InstallPlan>(JsonSerializer.Serialize(plan))!;
        before.ImportedXml = null;
        before.ConfirmWipeDisk = before.ConfirmFormatPartition = before.ConfirmDiskScript = true;
        foreach (var item in missing)
            typeof(InstallPlan).GetProperty(item.Property)!.SetValue(plan, actual[item.Property]);
        data.Notices = data.Notices.Concat(notes).Distinct().ToArray();
        if (!missing.Any(i => actual[i.Property])) return;
        var after = JsonSerializer.Deserialize<InstallPlan>(JsonSerializer.Serialize(before))!;
        foreach (var item in missing)
            typeof(InstallPlan).GetProperty(item.Property)!.SetValue(after, actual[item.Property]);
        try
        {
            var service = new GeneratorService();
            var oldXml = Encoding.UTF8.GetString(service.GenerateImportBaseline(before));
            var newXml = XmlMerge.Read(Encoding.UTF8.GetString(service.GenerateImportBaseline(after)));
            data.BaselineXml = XmlMerge.Apply(data.BaselineXml, oldXml, newXml).OuterXml;
        }
        catch (Exception e) when (e is ArgumentException or Schneegans.Unattend.ConfigurationException or XmlException)
        {
            data.Blockers = data.Blockers.Append(global::UnattendLocal.Localization.UiText.Source("text.ff178253c885")).Distinct().ToArray();
        }
    }
}
