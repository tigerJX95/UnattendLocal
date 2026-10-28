using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using UnattendLocal.Core;
using UnattendLocal.Desktop;
using UnattendLocal.Localization;

static class LocalizationTests
{
    public static int Run(GeneratorService generator)
    {
        int count = 0; var missing = new List<string>();
        void Check(bool value, string name) { if (!value) { if(name.StartsWith("English ")) { missing.Add(name); return; } throw new Exception("i18n: " + name); } count++; }
        string Key(string text) => UiText.SourceResources.First(p => UiText.Source(p.Key) == text).Key;
        string Hash(string value) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value))).ToLowerInvariant();
        var previousLanguage = UiText.Language;
        var directory = Path.Combine(Path.GetTempPath(), "UnattendLocal-i18n-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        string Pack(string name, object values) { var path = Path.Combine(directory, name); File.WriteAllText(path, JsonSerializer.Serialize(values), new UTF8Encoding(false)); return path; }
        var assembly = typeof(UiText).Assembly;
        using var catalogStream = assembly.GetManifestResourceStream("UnattendLocal.Localization.catalog.json")!;
        using var catalog = JsonDocument.Parse(catalogStream);
        using var stateStream = assembly.GetManifestResourceStream("UnattendLocal.Localization.translation-state.json")!;
        using var state = JsonDocument.Parse(stateStream);
        try
        {
            Check(UiText.SourceResources.Count > 3500 && UiText.SourceResources.Keys.ToHashSet().SetEquals(UiText.EnglishResources.Keys), "complete bilingual key sets");
            foreach (var item in UiText.SourceResources)
            {
                var entry = catalog.RootElement.GetProperty(item.Key);
                var english = UiText.EnglishResources[item.Key];
                Check(UiText.IsValidEntry(item.Key, item.Value) && UiText.IsValidEntry(item.Key, english), "valid placeholders/tokens: " + item.Key);
                Check(entry.GetProperty("SourceHash").GetString() == Hash(UiText.Source(item.Key)), "canonical hash: " + item.Key);
                Check(state.RootElement.GetProperty("en-US").GetProperty(item.Key).GetString() == Hash(item.Value), "translation review hash: " + item.Key);
                Check(entry.TryGetProperty("Opaque", out var opaque) && opaque.GetBoolean() || english == "简体中文" || !Regex.IsMatch(english, "[\u4e00-\u9fff]"), "English coverage: " + item.Key);
            }
            string cancel = Key("取消"), canonical = UiText.Source(cancel);
            var culture = CultureInfo.CurrentCulture;
            int events = 0;
            void Changed(object? sender, EventArgs e) => events++;
            UiText.LanguageChanged += Changed;
            try { UiText.SetLanguage("zh-CN"); UiText.SetLanguage("en-US"); Check(UiText.Get(cancel) == "Cancel" && UiText.Source(cancel) == canonical, "display differs from invariant source"); Check(ReferenceEquals(culture, CultureInfo.CurrentCulture), "UI language leaves parsing culture unchanged"); Check(events >= 1, "language change notifies bindings"); }
            finally { UiText.LanguageChanged -= Changed; }
            foreach (string architecture in new[] { "amd64", "arm64", "x86" })
            foreach (int mode in new[] { 0, 1, 2 })
            {
                UiText.SetLanguage("zh-CN");
                var plan = InstallPlan.New(); plan.Architecture = architecture; plan.Users[0].Password = "I18nTest123!"; plan.Users[0].DisplayName = "取消";
                plan.RegisteredOwner = "默认";
                if (mode == 1) { plan.WifiMode = "预设 WPA2 Wi-Fi"; plan.Ssid = "取消"; plan.WifiPassword = "I18nWifi123!"; }
                if (mode == 2) { plan.DiskMode = InstallationSettings.Wipe; plan.TargetDiskId = 1; plan.DiskLayout = "UEFI / GPT"; plan.ConfirmWipeDisk = true; }
                var chineseXml = generator.Generate(plan);
                var chineseJson = PlanStore.Serialize(plan, true);
                UiText.SetLanguage("en-US");
                Check(generator.Generate(plan).SequenceEqual(chineseXml), "identical generated XML: " + architecture + ":" + mode);
                Check(PlanStore.Serialize(plan, true) == chineseJson, "identical plan JSON: " + architecture + ":" + mode);
                Check(PlanStore.Deserialize(chineseJson).Users[0].DisplayName == "取消", "user value round-trip");
            }
            Check(UiText.Translate(UiText.Format("message.fe34f7d7e8a1", "取消")).Contains("取消"), "raw name equal to a UI label is preserved");
            Check(UiText.Translate(UiText.Format("message.281ae7fd2c2b", "取消")).Contains("取消"), "account name in diagnostics is preserved");
            Check(UiText.Translate(UiText.Concat([UiText.Source(Key("移除：")), "取消"], [false, true])).Contains("取消"), "raw concatenation argument is preserved");
            string formatted = UiText.Arg(12.345, 8, "F2");
            Check(formatted == string.Format(CultureInfo.CurrentCulture, "{0,8:F2}", 12.345), "formatting and alignment preserved");
            string errorSource = UiText.Source("text.f3b91a2a704a");
            string displayedError = UiText.Translate(errorSource);
            Check(UiText.Original(displayedError) == errorSource && DiagnosticText.IsErrorSummary(UiText.Original(displayedError)), "error classification survives display translation");
            Check(SettingsSearchCatalog.Search("workgroup").Count > 0 && SettingsSearchCatalog.Search("工作组").Count > 0, "bilingual setting search");
            Check(SearchTextMatcher.Matches(SearchTextMatcher.SearchText("中国", "zh-CN"), "China"), "English choice search");
            // Exercise long-lived help after bounded diagnostic metadata eviction.
            for (int page = 0; page < HelpCatalog.PageCount; page++) _ = HelpCatalog.ForPage(page);
            for (int index = 0; index < 12005; index++) UiText.Concat(["localization cache test ", index], [false, true]);
            for (int page = 0; page < HelpCatalog.PageCount; page++)
            foreach (var topic in HelpCatalog.ForPage(page))
            foreach (var section in topic.Sections)
                Check(!Regex.IsMatch(UiText.Translate(section.Text), "[\\u4e00-\\u9fff]"), "English help: " + topic.Id + " = " + UiText.Translate(section.Text));
            foreach (var item in AppRemovalSettings.Items)
                Check(!Regex.IsMatch(UiText.Translate(HelpText.RemovalDescription(item.Description)), "[\\u4e00-\\u9fff]"), "English removal help: " + item.Id);
            var summaryView = SummaryPresentation.FromText(Recommendations.Summary(InstallPlan.New()));
            foreach (var section in summaryView.Sections)
            foreach (var row in section.Rows)
                Check(!Regex.IsMatch(UiText.Translate(row.Value), "[\\u4e00-\\u9fff]"), "English summary: " + row.SourceLabel + " = " + UiText.Translate(row.Value));
            // Dynamic destinations must retain translation metadata through every composition.
            foreach (var property in typeof(InstallPlan).GetProperties())
            {
                var location = ConfigurationErrorLocation.FromField(property.Name);
                if (location is not null)
                    Check(!Regex.IsMatch(UiText.Translate(location.Path), "[\u4e00-\u9fff]"), "English error destination: " + property.Name);
            }
            foreach (var field in new[] { "Users.Name", "Users.Password", nameof(InstallPlan.Users) })
            foreach (int index in new[] { 0, 1, 12 })
            {
                var location = ConfigurationErrorLocation.FromField(field, index)!;
                string path = UiText.Translate(location.Path);
                Check(!Regex.IsMatch(path, "[\u4e00-\u9fff]") && path.Contains($"account {index + 1}"), "English indexed account destination: " + field + index);
                Check(UiText.Translate(location.ActionText) == "Go to", "English compact navigation button");
                string tip = UiText.Translate(UiText.Concat([UiText.Source("text.d8da6d237352"), location.Path], [false, false]));
                Check(!Regex.IsMatch(tip, "[\u4e00-\u9fff]") && tip.Contains(path), "English navigation tooltip includes destination");
            }
            Check(UiText.Get("text.0871f0abbd53") == "Location: ", "English location label describes a destination");
            var helpModel = new MainViewModel();
            foreach (var topic in helpModel.HelpTips.Values)
            foreach (var section in topic.Sections)
                Check(!Regex.IsMatch(UiText.Translate(section.Text), "[\u4e00-\u9fff]"), "English dynamic tooltip: " + topic.Id + " = " + UiText.Translate(section.Text));
            var named = InstallPlan.New(); named.UseRegisteredOwner = true; named.RegisteredOwner = "取消";
            named.UseRegisteredOrganization = true; named.RegisteredOrganization = "默认";
            string namedSummary = UiText.Translate(RegisteredInformationSettings.Summary(named));
            Check(namedSummary.Contains("取消") && namedSummary.Contains("默认") && namedSummary.Contains("Owner"), "translated summary preserves raw registered names");
            UiText.SetLanguage("zh-CN");
            Check(UiText.Translate(WorkgroupSettings.Description).Contains("\n\n"), "Chinese long explanations remain paragraphed");
            string proseSource = UiText.Source("text.3cb88008e742");
            string prose = UiText.Get("text.3cb88008e742");
            Check(prose.Split("\n\n").Length > 1 && prose.Split("\n\n").All(p => p.StartsWith("　　")), "Chinese narrative retains indentation on every automatic paragraph");
            Check(UiText.Get("v125.pe.name").StartsWith("使用 PE") && !UiText.Get("v125.pe.summary").StartsWith("　"), "option labels and summaries remain left aligned");
            Check(UiText.Source("text.3cb88008e742") == proseSource, "paragraph display does not modify canonical prose");
            UiText.SetLanguage("en-US");
            Check(UiText.Get("text.f430459281cd") == "{0}" && !UiText.Get("text.3cb88008e742").StartsWith("　"), "English prose and prose format do not use Chinese indentation");
            UiText.SetLanguage("en-US");
            var preferences = Path.Combine(directory, "preferences.json");
            File.WriteAllText(preferences, "{\"theme\":\"Dark\"}");
            Check(UiPreferences.LoadLanguage(preferences) == "zh-CN" && UiPreferences.Load(preferences) == "Dark", "legacy preferences");
            Check(UiPreferences.SaveLanguage("en-US", preferences) && UiPreferences.Load(preferences) == "Dark", "language preserves theme");
            Check(UiPreferences.Save("Light", preferences) && UiPreferences.LoadLanguage(preferences) == "en-US", "theme preserves language");
            var preferenceBytes = File.ReadAllBytes(preferences);
            Check(!UiPreferences.SaveLanguage("../bad", preferences) && File.ReadAllBytes(preferences).SequenceEqual(preferenceBytes), "invalid preference does not overwrite file");
            Check(!UiPreferences.SaveLanguage("en-US", directory), "preference failure reported");
            string englishPack = Pack("en-US.json", new Dictionary<string, string> { [cancel] = "Dismiss" });
            Check(UiText.LoadExternalPack("en-US", englishPack, out _) && UiText.Get(cancel) == "Dismiss", "external partial language overlay");
            string invalidPack = Pack("invalid.json", new Dictionary<string, string> { [cancel] = "Changed", ["unknown.key"] = "Invalid" });
            Check(!UiText.LoadExternalPack("en-US", invalidPack, out _) && UiText.Get(cancel) == "Dismiss", "invalid batch rejected atomically");
            string placeholder = "message.fe34f7d7e8a1";
            Check(!UiText.LoadExternalPack("en-US", Pack("placeholder.json", new Dictionary<string, string> { [placeholder] = "Missing argument" }), out _), "placeholder loss rejected");
            var structuredKey = "message.604344d85161";
            string flatSummary = UiText.EnglishResources[structuredKey].Replace('\n', ' ');
            Check(!UiText.LoadExternalPack("en-US", Pack("flat-summary.json", new Dictionary<string, string> { [structuredKey] = flatSummary }), out _), "structured summary line loss rejected");
            var duplicate = Path.Combine(directory, "duplicate.json"); File.WriteAllText(duplicate, "{\"" + cancel + "\":\"A\",\"" + cancel + "\":\"B\"}");
            Check(!UiText.LoadExternalPack("en-US", duplicate, out _), "duplicate keys rejected");
            var invalidUtf8 = Path.Combine(directory, "encoding.json"); File.WriteAllBytes(invalidUtf8, [0xFF, 0xFE, 0xFF]);
            Check(!UiText.LoadExternalPack("en-US", invalidUtf8, out _), "invalid UTF8 rejected");
            var oversized = Path.Combine(directory, "large.json"); using (var file = File.Create(oversized)) file.SetLength(4 * 1024 * 1024 + 1);
            Check(!UiText.LoadExternalPack("en-US", oversized, out _), "oversized pack rejected");
            foreach (var item in catalog.RootElement.EnumerateObject())
            {
                var value = item.Value;
                bool protects = value.TryGetProperty("ProtectedTokens", out var tokens) && tokens.GetArrayLength() > 0 || value.TryGetProperty("FilterPatterns", out _) || value.TryGetProperty("Opaque", out var isOpaque) && isOpaque.GetBoolean();
                if (!protects) continue;
                var changed = UiText.EnglishResources[item.Name].Replace("https://", "http://").Replace("*.xml", "*.exe");
                if (changed == UiText.EnglishResources[item.Name]) changed = "Changed";
                Check(!UiText.LoadExternalPack("en-US", Pack("protected.json", new Dictionary<string, string> { [item.Name] = changed }), out _), "protected resource rejected: " + item.Name);
            }
            Check(UiText.LoadExternalPack("zh-CN", Pack("zh-CN.json", new Dictionary<string, string> { [cancel] = "返回" }), out _), "Chinese display revision loaded");
            UiText.SetLanguage("zh-CN");
            Check(UiText.Get(cancel) == "返回" && UiText.Source(cancel) == "取消", "Chinese revisions cannot change compatibility values");
            var manifest = Pack("languages.json", new[] { new { Code = "zh-CN", Name = "简体中文" }, new { Code = "en-US", Name = "English" }, new { Code = "fr-FR", Name = "Français" } });
            Check(UiText.LoadLanguageManifest(manifest, out _), "new language needs no program change");
            Check(UiText.LoadExternalPack("fr-FR", Pack("fr-FR.json", new Dictionary<string, string> { [cancel] = "Annuler" }), out _), "third-language pack loaded");
            UiText.SetLanguage("fr-FR");
            Check(UiText.Get(cancel) == "Annuler" && UiText.Get(Key("用户名")) == UiText.EnglishResources[Key("用户名")], "new language uses English fallback for missing keys");
            Check(!UiText.LoadLanguageManifest(Pack("bad-manifest.json", new[] { new { Code = "../bad", Name = "Bad" } }), out _), "path traversal manifest rejected");
            Check(!UiText.LoadLanguageManifest(Pack("null-manifest.json", new object?[] { null }), out _), "null manifest rejected");
        }
        finally
        {
            UiText.LoadExternalPack("zh-CN", Pack("reset-zh.json", UiText.SourceResources), out _);
            UiText.LoadExternalPack("en-US", Pack("reset-en.json", UiText.EnglishResources), out _);
            var manifest = Pack("reset-languages.json", new[] { new { Code = "zh-CN", Name = "简体中文" }, new { Code = "en-US", Name = "English" } });
            UiText.SetLanguage("zh-CN"); UiText.LoadLanguageManifest(manifest, out _); UiText.SetLanguage(previousLanguage);
            Directory.Delete(directory, true);
        }
        if(missing.Count>0)throw new Exception(string.Join("\n", missing));
        Console.WriteLine($"V1.20 localization: {count} checks passed.");
        return count;
    }
}
