using System.Xml;
namespace UnattendLocal.Core;

// Generator URL values describe the original form, not the installed image or actual payload.
public static class XmlImportParameters
{
    private static readonly HashSet<string> AppIds = AppRemovalSettings.Items.Select(i => i.Id).ToHashSet();
    public static Dictionary<string, string> ReadValues(XmlDocument doc, List<string>? notes = null)
    {
        var result = new Dictionary<string, string>();
        var url = doc.SelectNodes("//comment()")!.Cast<XmlComment>().Select(c => c.Value?.Trim()).FirstOrDefault(text =>
            Uri.TryCreate(text, UriKind.Absolute, out var uri) && uri.Scheme == "https" && uri.Host.Equals("schneegans.de", StringComparison.OrdinalIgnoreCase) && uri.AbsolutePath.TrimEnd('/') == "/windows/unattend-generator" && uri.Query.Length > 1);
        if (url == null) return result;
        foreach (var entry in new Uri(url).Query[1..].Split('&'))
        {
            var pair = entry.Split('=', 2);
            static string Decode(string value) => Uri.UnescapeDataString(value.Replace('+', ' '));
            string key = Decode(pair[0]);
            if (!result.TryAdd(key, pair.Length == 2 ? Decode(pair[1]) : "")) notes?.Add(global::UnattendLocal.Localization.UiText.Concat(new object?[] { global::UnattendLocal.Localization.UiText.Source("text.dc8e51e38acd"), key },new bool[]{false,true}));
        }
        return result;
    }
    public static bool IsHandled(string key, string value) => AppIds.Contains(key) || (key, value) switch
    {
        ("LockKeysMode", "Skip" or "Default" or "Custom") => true,
        ("CompactOsMode", "Default" or "Always" or "Never") => true,
        ("PartitionMode", "Interactive") => true,
        ("DiskAssertionMode", "Skip") => true,
        ("InstallFromMode", "Automatic") => true,
        ("WdacMode", "Skip") => true,
        _ => false
    };
    private static bool StaleNotice(string notice, IReadOnlyDictionary<string, string> values)
    {
        foreach (string prefix in new[] { global::UnattendLocal.Localization.UiText.Source("text.14828b99e96e"), global::UnattendLocal.Localization.UiText.Source("text.990030e78bf8"), global::UnattendLocal.Localization.UiText.Source("text.4e00ee262381") })
            if (notice.StartsWith(prefix, StringComparison.Ordinal))
            {
                string key = notice[prefix.Length..].Split('；')[0];
                return values.TryGetValue(key, out var value) && IsHandled(key, value);
            }
        return false;
    }
    public static void Reconcile(XmlDocument doc, InstallPlan plan, AppRemovalSettings.Source appState, List<string> notes)
    {
        var values = ReadValues(doc);
        notes.RemoveAll(n => StaleNotice(n, values));
        var declarations = values.Where(v => AppIds.Contains(v.Key)).ToArray();
        int selected = declarations.Count(v => v.Value == "true");
        if (appState.Ambiguous && selected > 0)
            notes.Add(global::UnattendLocal.Localization.UiText.Format("message.7dd6cddf5703", new string[] { global::UnattendLocal.Localization.UiText.Arg((selected), 0, null) }));
        else if (!appState.Ambiguous)
        {
            var differences = declarations.Where(v => v.Value is "true" or "false" && (v.Value == "true") != appState.Selected.Contains(v.Key)).Select(v => AppRemovalSettings.Items.Single(i => i.Id == v.Key).Label).ToArray();
            if (differences.Length > 0) notes.Add(global::UnattendLocal.Localization.UiText.Concat(new object?[] { global::UnattendLocal.Localization.UiText.Format("message.810897f5f711", new string[] { global::UnattendLocal.Localization.UiText.Arg((differences.Length), 0, null) }), global::UnattendLocal.Localization.UiText.Join("、", differences, false), global::UnattendLocal.Localization.UiText.Source("text.50c0feafcaaa") },new bool[]{false,false,false}));
        }
        if (declarations.Any(v => v.Value is not ("true" or "false" or ""))) notes.Add(global::UnattendLocal.Localization.UiText.Source("text.96bea43344c9"));
        if (values.TryGetValue("CompactOsMode", out var compact) && compact is "Always" or "Never")
        {
            var state = CompactOsSettings.Inspect(doc);
            if (state.Known && state.Mode != (compact == "Always" ? CompactOsSettings.Enabled : CompactOsSettings.Disabled))
                notes.Add(global::UnattendLocal.Localization.UiText.Source("text.aad6b6b3c7af"));
        }
    }
    public static void RefreshLoaded(InstallPlan plan)
    {
        if (plan.ImportedXml is not { OriginalOmitted: false } data || data.SourceXml.Length == 0) return;
        var doc = XmlMerge.Read(data.SourceXml);
        var state = AppRemovalSettings.Inspect(doc);
        data.AppRemovalReadOnly = state.Ambiguous;
        var notes = data.Notices.ToList();
        var legacyHelpers = AppRemovalSettings.KnownLegacyHelpers(doc).ToHashSet();
        notes.RemoveAll(n => n == global::UnattendLocal.Localization.UiText.Source("text.157be5561045") || n == AppRemovalSettings.ReadOnlyDescription || n == AppRemovalSettings.LegacyDescription || n.StartsWith(global::UnattendLocal.Localization.UiText.Source("text.3e87bc6aa5b0"),StringComparison.Ordinal) || n.StartsWith(global::UnattendLocal.Localization.UiText.Source("text.4139b51569ad"),StringComparison.Ordinal) || n.StartsWith(global::UnattendLocal.Localization.UiText.Source("text.dfe7674760c8"),StringComparison.Ordinal) || n.StartsWith(global::UnattendLocal.Localization.UiText.Source("text.df67d84033fe"),StringComparison.Ordinal) || legacyHelpers.Any(name=>n==global::UnattendLocal.Localization.UiText.Concat(new object?[] { global::UnattendLocal.Localization.UiText.Source("text.9eee6c2b8190"), name },new bool[]{false,true})));
        if (state.Ambiguous) notes.Add(AppRemovalSettings.ReadOnlyDescription);
        else if (AppRemovalSettings.IsEditableLegacy(doc)) notes.Add(AppRemovalSettings.LegacyDescription);
        Reconcile(doc, plan, state, notes);
        data.Notices = notes.Distinct().ToArray();
    }
}
