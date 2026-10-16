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
            if (!result.TryAdd(key, pair.Length == 2 ? Decode(pair[1]) : "")) notes?.Add("生成参数有重复项，优先读取实际 XML；重复项：" + key);
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
        foreach (string prefix in new[] { "界面尚未实现或无法完整还原的生成参数：", "生成参数暂不能完全还原：", "暂未识别的生成参数：" })
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
            notes.Add($"原文件的生成参数声明选择了 {selected} 项应用移除；这是原文件的选择记录，不代表目标系统实际安装了这些应用。原脚本保留，清单暂不可编辑。");
        else if (!appState.Ambiguous)
        {
            var differences = declarations.Where(v => v.Value is "true" or "false" && (v.Value == "true") != appState.Selected.Contains(v.Key)).Select(v => AppRemovalSettings.Items.Single(i => i.Id == v.Key).Label).ToArray();
            if (differences.Length > 0) notes.Add($"应用移除生成参数与实际脚本不一致（{differences.Length} 项）：" + string.Join("、", differences) + "；界面以实际脚本为准，原 XML 保留。");
        }
        if (declarations.Any(v => v.Value is not ("true" or "false" or ""))) notes.Add("部分应用移除生成参数的取值无法识别；未依据参数自动勾选，以实际脚本为准。");
        if (values.TryGetValue("CompactOsMode", out var compact) && compact is "Always" or "Never")
        {
            var state = CompactOsSettings.Inspect(doc);
            if (state.Known && state.Mode != (compact == "Always" ? CompactOsSettings.Enabled : CompactOsSettings.Disabled))
                notes.Add("CompactOS 生成参数与实际安装配置不一致；界面以实际 XML 或已识别的安装脚本为准，原文保留。");
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
        notes.RemoveAll(n => n == "应用移除脚本含未知、修改过或不完整的内容，原文保留；本版不能通过应用移除列表覆盖这些脚本。" || n == AppRemovalSettings.ReadOnlyDescription || n == AppRemovalSettings.LegacyDescription || n.StartsWith("原文件的生成参数声明选择了 ",StringComparison.Ordinal) || n.StartsWith("应用移除生成参数与实际脚本不一致",StringComparison.Ordinal) || n.StartsWith("部分应用移除生成参数的取值无法识别",StringComparison.Ordinal) || n.StartsWith("CompactOS 生成参数与实际安装配置不一致",StringComparison.Ordinal) || legacyHelpers.Any(name=>n=="原文保留，未完全还原到界面："+name));
        if (state.Ambiguous) notes.Add(AppRemovalSettings.ReadOnlyDescription);
        else if (AppRemovalSettings.IsEditableLegacy(doc)) notes.Add(AppRemovalSettings.LegacyDescription);
        Reconcile(doc, plan, state, notes);
        data.Notices = notes.Distinct().ToArray();
    }
}
