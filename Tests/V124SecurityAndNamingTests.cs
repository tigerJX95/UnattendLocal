using System.Diagnostics;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using UnattendLocal.Core;
using UnattendLocal.Desktop;
using UnattendLocal.Localization;

static class V124SecurityAndNamingTests
{
    public static int Run(GeneratorService generator)
    {
        int count = 0;
        void Check(bool ok, string label) { if (!ok) throw new Exception(label); count++; }
        var original = new Regex(@"(?<!\{)\{\d+(?:,[^{}]+)?(?::[^{}]+)?\}(?!\})", RegexOptions.None, TimeSpan.FromMilliseconds(200));
        string Legacy(string text) => string.Join("|", original.Matches(text).Select(m => m.Value).Order(StringComparer.Ordinal));
        foreach (string text in new[] { "", "{0}", "{{0}}", "{0,10:N2}", "{0,-10}", "{1:yyyy-MM-dd} {0}", "{{{0}}}", "{0}:{1}", "x{0}y", "{0,::::x", "{not a token}", "{0}{0}", "{{0}{1}}" })
            Check(UiText.Placeholders(text) == Legacy(text), "placeholder behavior retained: " + text);
        var random = new Random(124);
        const string alphabet = "{}012,: abcXYZ";
        for (int i = 0; i < 500; i++) {
            string text = new(Enumerable.Range(0, 60).Select(_ => alphabet[random.Next(alphabet.Length)]).ToArray());
            Check(UiText.Placeholders(text) == Legacy(text), "short-input differential check " + i);
        }
        foreach (int length in new[] { 128000, 512000, 2000000 }) {
            var timer = Stopwatch.StartNew();
            _ = UiText.IsValidEntry("text.fac2a67ad878", "{0," + new string(':', length) + "x");
            Check(timer.Elapsed < TimeSpan.FromSeconds(3), "adversarial placeholder is bounded: " + length);
        }
        string directory = Path.Combine(Path.GetTempPath(), "unattend-v124-" + Guid.NewGuid().ToString("N")); Directory.CreateDirectory(directory);
        string previousLanguage = UiText.Language;
        try {
            UiText.SetLanguage("en-US");
            string valid = Path.Combine(directory, "valid.json");
            File.WriteAllText(valid, JsonSerializer.Serialize(new Dictionary<string,string> { ["text.fac2a67ad878"] = "Confirmed" }));
            Check(UiText.LoadExternalPack("en-US", valid, out _) && UiText.Get("text.fac2a67ad878") == "Confirmed", "valid overlay loaded");
            string invalid = Path.Combine(directory, "invalid.json");
            File.WriteAllText(invalid, JsonSerializer.Serialize(new Dictionary<string,string> { ["text.fac2a67ad878"] = "{0}" }));
            Check(!UiText.LoadExternalPack("en-US", invalid, out _) && UiText.Get("text.fac2a67ad878") == "Confirmed", "rejected overlay leaves prior state intact");
            string slow = Path.Combine(directory, "previous-repro.json");
            File.WriteAllText(slow, JsonSerializer.Serialize(new Dictionary<string,string> { ["text.fac2a67ad878"] = "{0," + new string(':',128000) + "x" }));
            var timer = Stopwatch.StartNew();_ = UiText.LoadExternalPack("en-US", slow, out _);
            Check(timer.Elapsed < TimeSpan.FromSeconds(3), "complete external pack load handles prior 10-second repro promptly");
        }
        finally {
            string reset = Path.Combine(directory, "reset.json");File.WriteAllText(reset, JsonSerializer.Serialize(UiText.EnglishResources));UiText.LoadExternalPack("en-US", reset, out _);
            UiText.SetLanguage(previousLanguage);Directory.Delete(directory,true);
        }
        foreach (string architecture in new[] { "amd64", "arm64", "x86" }) {
            var plan = QuickPresets.Create(QuickPresets.LocalAccount);plan.Architecture = architecture;plan.ComputerNameMode = ComputerNamingSettings.Mac;
            string xml = Encoding.UTF8.GetString(generator.Generate(plan));
            Check(xml.Contains(ComputerNamingSettings.MacScript.Trim().Split('\n')[0]), "MAC getter exported " + architecture);
            Check(PlanStore.Deserialize(PlanStore.Serialize(plan,true)).ComputerNameMode == ComputerNamingSettings.Mac, "MAC mode JSON restoration " + architecture);
            var restored = XmlPlanImport.Import(xml,generator);
            Check(restored.ComputerNameMode == ComputerNamingSettings.Mac, "MAC mode actual XML restoration " + architecture);
            restored.ComputerNameMode = ComputerNamingSettings.Existing;restored.ComputerName = "EXAMPLE-PC";
            Check(!Encoding.UTF8.GetString(generator.Generate(restored)).Contains(ComputerNamingSettings.MacScript.Trim().Split('\n')[0]), "MAC mode can be removed " + architecture);
        }
        var vm = new MainViewModel();vm.Plan.ComputerNameMode = ComputerNamingSettings.Mac;
        Check(vm.IsMacComputerName && !vm.IsScriptComputerName && !vm.IsFixedComputerName, "MAC-specific UI explanation state");
        Check(SettingsSearchCatalog.Search("MAC").Any(s => s.Target == "SettingAnchor006"), "MAC mode searchable");
        Check(SettingsSearchCatalog.Search("one hour").Any(s => s.Target == "ScriptTemplateSelector"), "temporary update pause searchable");
        return count;
    }
}
