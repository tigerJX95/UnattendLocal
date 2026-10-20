using System.Text;
using System.Text.Json;
using System.Xml;
using Schneegans.Unattend;
namespace UnattendLocal.Core;

public static class LanguagePreferences
{
    public const string Description = "第二、第三组按顺序追加语言偏好与键盘/输入法，可分别启用。第一组继续决定初始区域格式，显示语言仍必须与安装镜像匹配。本设置不下载安装语言包，也不改变本工具的界面语言；输入法能否使用取决于目标系统及已安装组件。切换为安装时手动选择后，保留已填内容但不生成语言覆盖。关闭某组仅影响新配置，不卸载已安装系统的输入法。";
    public static ILanguageSettings Build(UnattendGenerator g, InstallPlan p)
    {
        if (p.InteractiveLanguage) return new InteractiveLanguageSettings();
        LocaleAndKeyboard Pair(string locale, string keyboard, string label)
        {
            if (!g.UserLocales.ContainsKey(locale ?? "") || !g.KeyboardIdentifiers.ContainsKey(keyboard ?? "")) throw new ArgumentException("请填写有效的" + label + "语言和键盘 / 输入法。");
            return new(g.Lookup<UserLocale>(locale!), g.Lookup<KeyboardIdentifier>(keyboard!));
        }
        return new UnattendedLanguageSettings(g.Lookup<ImageLanguage>(p.ImageLanguage), Pair(p.Locale, p.Keyboard, "第一组"),
            p.UseSecondLanguage ? Pair(p.SecondLocale, p.SecondKeyboard, "第二组") : null,
            p.UseThirdLanguage ? Pair(p.ThirdLocale, p.ThirdKeyboard, "第三组") : null, g.Lookup<GeoLocation>(p.GeoId));
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
        if (!state.Supported) { notes.Add("额外语言与键盘组合未完整识别或超过三组，原文保留；不能用新增组覆盖。"); return; }
        for (int i = 0; i < state.Locales.Length; i++)
        {
            if (i == 0 && !(thirdOnly && state.Locales.Length == 1)) { p.UseSecondLanguage = true; p.SecondLocale = state.Locales[i]; p.SecondKeyboard = state.Keyboards[i]; }
            else { p.UseThirdLanguage = true; p.ThirdLocale = state.Locales[i]; p.ThirdKeyboard = state.Keyboards[i]; }
        }
    }
    public static void ValidateImported(InstallPlan p, XmlDocument doc)
    {
        if (!p.InteractiveLanguage && (p.UseSecondLanguage || p.UseThirdLanguage) && !Inspect(doc, p).Supported) throw new ArgumentException("原 XML 的额外语言与键盘未完整识别，不能覆盖；请用原工具调整后重新导入。");
    }
    public static string Summary(InstallPlan p)
    {
        string Label(string type, string id) => ChoiceLocalization.Get(type, id, id);
        var parts = new List<string>();
        if (p.UseSecondLanguage) parts.Add("第二组：" + Label("UserLocale", p.SecondLocale) + " / " + Label("KeyboardIdentifier", p.SecondKeyboard));
        if (p.UseThirdLanguage) parts.Add("第三组：" + Label("UserLocale", p.ThirdLocale) + " / " + Label("KeyboardIdentifier", p.ThirdKeyboard));
        return "额外语言与键盘：" + (p.InteractiveLanguage ? "安装时选择（已填内容不参与生成）" : parts.Count == 0 ? "未启用" : string.Join("；", parts));
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
            data.Notices = data.Notices.Where(n => !n.StartsWith("检测到多个键盘/区域组合", StringComparison.Ordinal)).ToArray();
        }
        catch (Exception e) when (e is ArgumentException or ConfigurationException or XmlException) { data.Blockers = data.Blockers.Append("旧方案的额外语言与键盘基准无法可靠迁移，请重新导入原 XML。").Distinct().ToArray(); }
    }
}
