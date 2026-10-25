using System.Text;
using System.Text.Json;
using System.Xml;
using Schneegans.Unattend;
namespace UnattendLocal.Core;

public static class LanguagePreferences
{
    public static readonly string Description = global::UnattendLocal.Localization.UiText.Source("text.6cbe94253268");
    public static ILanguageSettings Build(UnattendGenerator g, InstallPlan p)
    {
        if (p.InteractiveLanguage) return new InteractiveLanguageSettings();
        LocaleAndKeyboard Pair(string locale, string keyboard, string label)
        {
            if (!g.UserLocales.ContainsKey(locale ?? "") || !g.KeyboardIdentifiers.ContainsKey(keyboard ?? "")) throw new ArgumentException(global::UnattendLocal.Localization.UiText.Concat(new object?[] { global::UnattendLocal.Localization.UiText.Source("text.c3448bc65e54"), label, global::UnattendLocal.Localization.UiText.Source("text.d10bc8df9c4c") },new bool[]{false,false,false}));
            return new(g.Lookup<UserLocale>(locale!), g.Lookup<KeyboardIdentifier>(keyboard!));
        }
        return new UnattendedLanguageSettings(g.Lookup<ImageLanguage>(p.ImageLanguage), Pair(p.Locale, p.Keyboard, global::UnattendLocal.Localization.UiText.Source("text.ed8e490189b7")),
            p.UseSecondLanguage ? Pair(p.SecondLocale, p.SecondKeyboard, global::UnattendLocal.Localization.UiText.Source("text.a89d1ab08f71")) : null,
            p.UseThirdLanguage ? Pair(p.ThirdLocale, p.ThirdKeyboard, global::UnattendLocal.Localization.UiText.Source("text.237c0a355fb4")) : null, g.Lookup<GeoLocation>(p.GeoId));
    }
    private static string Token(UnattendGenerator g, string image, string locale, string keyboard)
    {
        var k = g.Lookup<KeyboardIdentifier>(keyboard); var l = g.Lookup<UserLocale>(locale);
        if (k.Type == InputType.IME) return k.Id;
        if (l.LCID == "1000")
        {
            if (g.UserLocales.TryGetValue(image, out var replacement)) l = replacement;
            else if (image == "zh-CN") l = g.UserLocales["zh"];
            else if (image == "zh-TW") l = g.UserLocales["zh-Hant"];
        }
        return l.LCID + ":" + k.Id;
    }
    public record State(bool Supported, string[] Locales, string[] Keyboards);
    public static State Inspect(XmlDocument doc, InstallPlan p)
    {
        var ns = new XmlNamespaceManager(doc.NameTable); ns.AddNamespace("u", XmlMerge.Namespace);
        var inputs = doc.SelectNodes("//u:component[@name='Microsoft-Windows-International-Core']/u:InputLocale", ns)!.Cast<XmlElement>().ToArray();
        if (inputs.Length == 0) return new(true, [], []);
        var tokens = inputs[0].InnerText.Split(';');
        if (inputs.Length != 1 || tokens.Length > 3 || tokens.Any(string.IsNullOrWhiteSpace)) return new(false, [], []);
        var g = new UnattendGenerator(); var locales = new List<string>(); var keyboards = new List<string>();
        string image = doc.SelectSingleNode("//u:component[@name='Microsoft-Windows-International-Core']/u:UILanguage", ns)?.InnerText ?? p.ImageLanguage;
        foreach (var token in tokens.Skip(1))
        {
            int slot = locales.Count + 2;
            string candidateLocale = slot == 2 ? p.SecondLocale : p.ThirdLocale, candidateKeyboard = slot == 2 ? p.SecondKeyboard : p.ThirdKeyboard;
            if (tokens.Length == 2 && !p.UseSecondLanguage && p.UseThirdLanguage) { candidateLocale = p.ThirdLocale; candidateKeyboard = p.ThirdKeyboard; }
            if (g.UserLocales.ContainsKey(candidateLocale) && g.KeyboardIdentifiers.ContainsKey(candidateKeyboard) && Token(g, image, candidateLocale, candidateKeyboard) == token) { locales.Add(candidateLocale); keyboards.Add(candidateKeyboard); continue; }
            string key = g.KeyboardIdentifiers.ContainsKey(token) ? token : token.Contains(':') ? token[(token.IndexOf(':') + 1)..] : token;
            string lcid = token.Contains(':') ? token[..token.IndexOf(':')] : "";
            var locale = g.UserLocales.Values.FirstOrDefault(l => l.LCID.Equals(lcid, StringComparison.OrdinalIgnoreCase));
            if (locale == null || !g.KeyboardIdentifiers.ContainsKey(key) || Token(g, image, locale.Id, key) != token) return new(false, [], []);
            locales.Add(locale.Id); keyboards.Add(key);
        }
        return new(true, locales.ToArray(), keyboards.ToArray());
    }
    public static void ReadIntoPlan(XmlDocument doc, InstallPlan p, List<string> notes)
    {
        var state = Inspect(doc, p); bool thirdOnly = !p.UseSecondLanguage && p.UseThirdLanguage;
        p.UseSecondLanguage = p.UseThirdLanguage = false;
        if (!state.Supported) { notes.Add(global::UnattendLocal.Localization.UiText.Source("text.16e680a7e18d")); return; }
        for (int i = 0; i < state.Locales.Length; i++)
        {
            if (i == 0 && !(thirdOnly && state.Locales.Length == 1)) { p.UseSecondLanguage = true; p.SecondLocale = state.Locales[i]; p.SecondKeyboard = state.Keyboards[i]; }
            else { p.UseThirdLanguage = true; p.ThirdLocale = state.Locales[i]; p.ThirdKeyboard = state.Keyboards[i]; }
        }
    }
    public static void ValidateImported(InstallPlan p, XmlDocument doc)
    {
        if (!p.InteractiveLanguage && (p.UseSecondLanguage || p.UseThirdLanguage) && !Inspect(doc, p).Supported) throw new ArgumentException(global::UnattendLocal.Localization.UiText.Source("text.0de30d705e45"));
    }
    public static string Summary(InstallPlan p)
    {
        string Label(string type, string id) => ChoiceLocalization.Get(type, id, id);
        var parts = new List<string>();
        if (p.UseSecondLanguage) parts.Add(global::UnattendLocal.Localization.UiText.Concat(new object?[] { global::UnattendLocal.Localization.UiText.Source("text.0ef5ea5aacec"), Label("UserLocale", p.SecondLocale), " / ", Label("KeyboardIdentifier", p.SecondKeyboard) },new bool[]{false,false,false,false}));
        if (p.UseThirdLanguage) parts.Add(global::UnattendLocal.Localization.UiText.Concat(new object?[] { global::UnattendLocal.Localization.UiText.Source("text.405459afe318"), Label("UserLocale", p.ThirdLocale), " / ", Label("KeyboardIdentifier", p.ThirdKeyboard) },new bool[]{false,false,false,false}));
        return global::UnattendLocal.Localization.UiText.Concat(new object?[] { global::UnattendLocal.Localization.UiText.Source("text.2e329968c6e6"), (p.InteractiveLanguage ? global::UnattendLocal.Localization.UiText.Source("text.4ad0eaf99025") : parts.Count == 0 ? global::UnattendLocal.Localization.UiText.Source("text.f95ea7f4c063") : global::UnattendLocal.Localization.UiText.Join("；", parts, false)) },new bool[]{false,false});
    }
    public static void NormalizeLoaded(InstallPlan p, JsonElement json)
    {
        if (json.TryGetProperty(nameof(InstallPlan.UseSecondLanguage), out _) || p.ImportedXml is not { OriginalOmitted: false } data || data.SourceXml.Length == 0 || data.BaselineXml.Length == 0) return;
        var source = XmlMerge.Read(data.SourceXml); var state = Inspect(source, p); if (!state.Supported || state.Locales.Length == 0) return;
        var before = JsonSerializer.Deserialize<InstallPlan>(JsonSerializer.Serialize(p))!; before.ImportedXml = null; before.ConfirmWipeDisk = before.ConfirmFormatPartition = before.ConfirmDiskScript = true;
        ReadIntoPlan(source, p, new()); var after = JsonSerializer.Deserialize<InstallPlan>(JsonSerializer.Serialize(before))!; ReadIntoPlan(source, after, new());
        try
        {
            var g = new GeneratorService(); data.BaselineXml = XmlMerge.Apply(data.BaselineXml, Encoding.UTF8.GetString(g.GenerateImportBaseline(before)), XmlMerge.Read(Encoding.UTF8.GetString(g.GenerateImportBaseline(after)))).OuterXml;
            data.Notices = data.Notices.Where(n => !n.StartsWith(global::UnattendLocal.Localization.UiText.Source("text.371622eefcab"), StringComparison.Ordinal)).ToArray();
        }
        catch (Exception e) when (e is ArgumentException or ConfigurationException or XmlException) { data.Blockers = data.Blockers.Append(global::UnattendLocal.Localization.UiText.Source("text.48900aec0e73")).Distinct().ToArray(); }
    }
}
