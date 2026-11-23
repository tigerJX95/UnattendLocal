using System.Text;
using System.Xml.Linq;
using UnattendLocal.Core;
using UnattendLocal.Desktop;
using UnattendLocal.Localization;

static class TimeZoneTests
{
    public static int Run(GeneratorService generator)
    {
        int count = 0;
        void Check(bool condition, string message)
        {
            if (!condition) throw new Exception("Time zones: " + message);
            count++;
        }

        var zones = generator.TimeZones.ToDictionary(zone => zone.Id, StringComparer.Ordinal);
        bool Matches(string id, string query)
        {
            var zone = zones[id];
            return SearchTextMatcher.MatchesChoice(SearchTextMatcher.SearchText(zone.Label, id), zone.Label, query);
        }

        var canadianZones = new[]
        {
            (Id: "British Columbia Standard Time", Query: "utc−7 British Columbia", Label: "(UTC-07:00) Pacific Time (British Columbia)"),
            (Id: "Alberta Standard Time", Query: "UTC-06 Alberta", Label: "(UTC-06:00) Mountain Time (Alberta)"),
            (Id: "Manitoba Standard Time", Query: "UTC-05:00 马尼托巴", Label: "(UTC-05:00) Manitoba"),
        };
        var viewModel = new MainViewModel();
        XNamespace ns = "urn:schemas-microsoft-com:unattend";
        string automatic = UiText.Source("text.499ff33c1078");
        string specified = UiText.Source("text.980f34c4425d");
        string previousLanguage = UiText.Language;
        try
        {
            foreach (var (language, label) in new[] { ("zh-CN", "由 Windows 决定"), ("en-US", "Let Windows decide") })
            {
                UiText.SetLanguage(language);
                var defaultPlan = InstallPlan.New();
                defaultPlan.Users[0].Password = "FixturePassword123!";
                viewModel.Plan = defaultPlan;
                Check(defaultPlan.TimeZoneMode == automatic && UiText.Translate(defaultPlan.TimeZoneMode) == label &&
                    defaultPlan.TimeZone == "" && !viewModel.IsFixedTimeZone, language + " starts with the localized automatic mode");
                Check(!XDocument.Parse(Encoding.UTF8.GetString(generator.Generate(defaultPlan))).Descendants(ns + "TimeZone").Any(),
                    language + " default XML omits a fixed time zone");
                Check(new[] { InstallPlan.NewCustom(), QuickPresets.Create(QuickPresets.Minimal), QuickPresets.Create(QuickPresets.LocalAccount) }
                    .All(plan => plan.TimeZoneMode == automatic && plan.TimeZone == ""), language + " new custom and preset plans share the default");
                defaultPlan.TimeZoneMode = specified;
                defaultPlan.TimeZone = "India Standard Time";
                Recommendations.SelectNetworkScene(defaultPlan, "wifi");
                Check(defaultPlan.TimeZoneMode == specified && defaultPlan.TimeZone == "India Standard Time" &&
                    defaultPlan.RecommendationVersion == Recommendations.CurrentVersion, language + " changing network scenes preserves the selected time zone");
                Recommendations.Restore(defaultPlan, nameof(InstallPlan.TimeZoneMode));
                Check(defaultPlan.TimeZoneMode == automatic && defaultPlan.TimeZone == "India Standard Time",
                    language + " restoring the setting returns to automatic and retains the previous choice");
            }
        }
        finally { UiText.SetLanguage(previousLanguage); }

        var olderPlan = InstallPlan.New();
        olderPlan.RecommendationVersion = 3;
        olderPlan.TimeZoneMode = specified;
        olderPlan.TimeZone = "India Standard Time";
        var loadedPlan = PlanStore.Deserialize(PlanStore.Serialize(olderPlan, true));
        Check(loadedPlan.TimeZoneMode == specified && loadedPlan.TimeZone == "India Standard Time" &&
            !Recommendations.Differences(loadedPlan).Any(field => field.Property == nameof(InstallPlan.TimeZoneMode)),
            "an older saved recommendation retains its configured time zone and original baseline");
        olderPlan.Users[0].Password = "FixturePassword123!";
        var sourceWithZone = XDocument.Parse(Encoding.UTF8.GetString(generator.Generate(olderPlan)));
        sourceWithZone.Descendants(ns + "TimeZone").Single().AddAfterSelf(new XComment("Preserve source comment"));
        var importedWithZone = XmlPlanImport.Import(sourceWithZone.ToString(), generator);
        importedWithZone.TimeZoneMode = automatic;
        var resetXml = XDocument.Parse(Encoding.UTF8.GetString(generator.Generate(importedWithZone)));
        Check(!resetXml.Descendants(ns + "TimeZone").Any() &&
            resetXml.DescendantNodes().OfType<XComment>().Any(comment => comment.Value == "Preserve source comment"),
            "reset removes the fixed time zone and preserves imported component content");

        foreach (var (id, query, label) in canadianZones)
        {
            Check(zones.ContainsKey(id), id + " is selectable");
            Check(Matches(id, query), id + " matches localized names and shorthand offsets");
            Check(UiText.EnglishText(zones[id].Label) == label, id + " renders the English label");
            var plan = InstallPlan.New();
            plan.Users[0].Password = "FixturePassword123!";
            plan.TimeZoneMode = specified;
            plan.TimeZone = id;
            viewModel.Plan = plan;
            Check(viewModel.ShowCanadianTimeZoneUpdateHint, id + " shows the compatibility hint");
            var xml = Encoding.UTF8.GetString(generator.Generate(plan));
            Check(XDocument.Parse(xml).Descendants(ns + "TimeZone").Single().Value == id, id + " is exported unchanged");
            var imported = XmlPlanImport.Import(xml, generator);
            Check(imported.TimeZoneMode == specified && imported.TimeZone == id, id + " is restored from XML");
            var loaded = PlanStore.Deserialize(PlanStore.Serialize(plan, true));
            Check(loaded.TimeZoneMode == specified && loaded.TimeZone == id, id + " is restored from JSON");
            plan.TimeZoneMode = automatic;
            Check(!viewModel.ShowCanadianTimeZoneUpdateHint, "automatic time zone hides the hint");
        }

        viewModel.Plan = InstallPlan.New();
        Check(!viewModel.ShowCanadianTimeZoneUpdateHint, "existing time zone hides the hint");
        Check(zones["Paraguay Standard Time"].Label.StartsWith("(UTC-03:00)", StringComparison.Ordinal), "Paraguay keeps its ID and updates its offset");
        Check(zones["Greenland Standard Time"].Label.StartsWith("(UTC-02:00)", StringComparison.Ordinal), "Greenland keeps its ID and updates its offset");
        Check(Matches("UTC", "UTC+0"), "zero offset matches the bare UTC label");
        Check(Matches("India Standard Time", "ＵＴＣ＋５：３０"), "full-width input matches a fractional offset");
        Check(!Matches("India Standard Time", "UTC+5"), "integer hours do not match fractional offsets");
        Check(!Matches("China Standard Time", "UTC+5"), "different offsets do not match");
        Check(SearchTextMatcher.MatchesChoice("Windows 11 Pro", "Windows 11 Pro", "11 Pro"), "ordinary dropdown searches still work");
        var nativeDocument = XmlMerge.Read("""
            <unattend xmlns="urn:schemas-microsoft-com:unattend">
              <settings pass="specialize"><component name="Microsoft-Windows-Shell-Setup"><TimeZone>Alberta Standard Time</TimeZone></component></settings>
              <settings pass="oobeSystem"><component name="Microsoft-Windows-Shell-Setup"><OOBE><HideEULAPage>0</HideEULAPage></OOBE></component></settings>
            </unattend>
            """);
        var namespaces = new System.Xml.XmlNamespaceManager(nativeDocument.NameTable);
        namespaces.AddNamespace("u", XmlMerge.Namespace);
        var editablePlan = InstallPlan.New();
        editablePlan.HideEula = true;
        var importNotices = new List<string>();
        XmlPlanImport.ReconcileNativeSnapshot(nativeDocument, namespaces, editablePlan, importNotices);
        Check(editablePlan.TimeZone == "Alberta Standard Time" && !editablePlan.HideEula,
            "recognized XML values are authoritative in the editor");
        Check(importNotices.Count == 1, "differing editor metadata triggers one notice");
        return count;
    }
}
