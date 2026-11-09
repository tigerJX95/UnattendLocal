using System.Text;
using System.Text.Json.Nodes;
using System.Xml;
using UnattendLocal.Core;
using UnattendLocal.Desktop;
using UnattendLocal.Localization;

static class V1411AuditTests
{
    public static int Run(GeneratorService generator)
    {
        int count = 0;
        void Check(bool result, string message)
        {
            if (!result) throw new Exception(message);
            count++;
        }

        XmlDocument Source(InstallPlan plan)
        {
            var doc = XmlMerge.Read(Encoding.UTF8.GetString(generator.Generate(plan)));
            foreach (var comment in doc.SelectNodes("//comment()")!.Cast<XmlComment>().ToArray())
                comment.ParentNode!.RemoveChild(comment);
            return doc;
        }

        void RequiresDiskConsent(InstallPlan plan)
        {
            try { generator.Generate(plan); }
            catch (ConfigurationValidationException error) when (error.Field == nameof(InstallPlan.ConfirmImportedXmlRisk))
            {
                count++;
                return;
            }
            throw new Exception("Expected renewed consent for retained WinPE commands.");
        }

        var plan = QuickPresets.Create(QuickPresets.Minimal);
        plan.BypassTPMCheck = true;
        var doc = Source(plan);
        const string original = "reg.exe add \"HKLM\\SYSTEM\\Setup\\LabConfig\" /v BypassTPMCheck /t REG_DWORD /d 1 /f";
        const string replacement = "reg.exe add \"HKLM\\SOFTWARE\\LabConfig\\BypassTPMCheck\" /v AuditFixture /t REG_DWORD /d 1 /f";
        string altered = doc.OuterXml.Replace(original, replacement);
        var imported = XmlPlanImport.Import(altered, generator);
        Check(altered != doc.OuterXml && imported.ImportedXml!.RequiresDiskReview, "Unrelated registry write requires WinPE review.");
        Check(!XmlPlanImport.Import(doc.OuterXml, generator).ImportedXml!.RequiresDiskReview, "Exact generated hardware check is exempt.");
        RequiresDiskConsent(imported);
        imported.ConfirmImportedXmlRisk = true;
        Check(Encoding.UTF8.GetString(generator.Generate(imported)).Contains(replacement), "Reviewed command survives export.");

        const string customCommand = "cmd.exe /c echo V1411AuditFixture";
        var custom = XmlPlanImport.Import(doc.OuterXml.Replace(original, customCommand), generator);
        Check(custom.ImportedXml!.RequiresDiskReview, "Unknown WinPE command requires review on XML import.");
        var json = JsonNode.Parse(PlanStore.Serialize(custom, true))!;
        json[nameof(InstallPlan.ImportedXml)]!.AsObject().Remove(nameof(ImportedXml.RequiresDiskReview));
        var restored = PlanStore.Deserialize(json.ToJsonString());
        Check(restored.ImportedXml!.RequiresDiskReview && !restored.ConfirmImportedXmlRisk, "JSON load recomputes a missing disk review flag.");
        RequiresDiskConsent(restored);

        plan = QuickPresets.Create(QuickPresets.Minimal);
        plan.UseAppLocker = true;
        foreach (string mode in new[] { "AuditOnly", "Enabled" })
        {
            plan.AppLockerPolicyXml = AppControlSettings.Example.Replace("AuditOnly", mode);
            doc = Source(plan);
            var legacyPaths = doc.OuterXml.Replace(@"C:\Windows\Setup\Scripts\", @"C:\\Windows\\Setup\\Scripts\\");
            imported = XmlPlanImport.Import(legacyPaths, generator);
            Check(imported.UseAppLocker, "Legacy AppLocker pipeline is recognized when imported.");
            var review = InstallationReview.ForExport(generator.Generate(imported)).AppLocker;
            Check(review != null && review.Message.Contains("允许 1 条") && review.HasWarnings == (mode == "Enabled"),
                "Export recognizes the same legacy AppLocker pipeline and mode as import.");
        }

        const string wifiSecret = "SyntheticWlanV1411";
        plan = QuickPresets.Create(QuickPresets.Minimal);
        plan.WifiMode = WifiSettings.Wpa2;
        plan.Ssid = "AuditFixture";
        plan.WifiPassword = wifiSecret;
        doc = Source(plan);
        var renamed = doc.OuterXml.Replace("Wifi.xml", "wifi-private-v1411.xml", StringComparison.OrdinalIgnoreCase);
        var bytes = generator.Generate(XmlPlanImport.Import(renamed, generator));
        Check(Encoding.UTF8.GetString(bytes).Contains(wifiSecret), "Renamed WLAN profile is kept intact in deployment XML.");
        foreach (bool english in new[] { false, true })
        {
            var preview = GeneratorService.Preview(bytes, english: english);
            Check(!preview.Contains(wifiSecret) && preview.Contains("wifi-private-v1411.xml"), "Preview redacts renamed WLAN profile by XML root.");
        }

        plan = QuickPresets.Create(QuickPresets.Minimal);
        plan.DiskMode = InstallationSettings.Wipe;
        plan.TargetDiskId = 2;
        plan.ConfirmWipeDisk = true;
        var model = new MainViewModel();
        foreach (var (minimum, desired) in new[] { ("2147483648", "2147483648"), ("0", "0"), ("65537", "65537"), ("1536", "-1") })
        {
            doc = Source(plan);
            var path = AutoPartitionSettings.Inspect(doc).Path!;
            string input = $"shrink minimum={plan.RecoverySizeMB} desired={plan.RecoverySizeMB}";
            string invalidCommand = $"shrink minimum={minimum} desired={desired}";
            path.InnerText = path.InnerText.Replace(input, invalidCommand);
            Check(model.ImportXml(doc.OuterXml, () => true) && model.HasReadonlyAutoPartition && model.HasImportedXmlRisk,
                "Unsupported recovery size is retained as read-only.");
            imported = model.Plan;
            Check(model.XmlImportNotices.Contains("恢复分区大小超出支持范围") &&
                UiText.EnglishText(model.XmlImportNotices).Contains("recovery partition size in the imported XML is outside the supported range"),
                "Import notice identifies the recovery size in both languages.");
            if (minimum == "2147483648")
            {
                var saved = JsonNode.Parse(PlanStore.Serialize(imported, true))!;
                saved[nameof(InstallPlan.ImportedXml)]![nameof(ImportedXml.Notices)] = new JsonArray(JsonValue.Create(AutoPartitionSettings.ReadOnlyNotice));
                var reloaded = PlanStore.Deserialize(saved.ToJsonString());
                Check(reloaded.ImportedXml!.Notices.Any(n => n.Contains("恢复分区大小超出支持范围")) &&
                    !reloaded.ImportedXml.Notices.Contains(AutoPartitionSettings.ReadOnlyNotice),
                    "Reopening a saved import refreshes the generic notice to the localized cause.");
            }
            imported.ConfirmWipeDisk = true;
            RequiresDiskConsent(imported);
            imported.ConfirmImportedXmlRisk = true;
            Check(Encoding.UTF8.GetString(generator.Generate(imported)).Contains(invalidCommand),
                "Reviewed partition command remains unchanged.");
        }
        string diagnostic = DiagnosticText.ForImportError(new OverflowException("raw runtime message"));
        Check(diagnostic.Contains("导入文件中的数值超出允许范围") &&
            UiText.EnglishText(diagnostic).Contains("A number in the imported file is outside the allowed range"),
            "Other import overflow errors use the localized diagnostic in both languages.");
        Check(DiagnosticText.ForImportError(new ArgumentException("existing diagnostic")) == "existing diagnostic",
            "Unrelated import errors retain their diagnostics.");
        return count;
    }
}
