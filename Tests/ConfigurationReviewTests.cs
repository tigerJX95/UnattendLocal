using System.Text;
using UnattendLocal.Core;
using UnattendLocal.Desktop;
using UnattendLocal.Localization;

static class ConfigurationReviewTests
{
    public static int Run(GeneratorService generator)
    {
        int count = 0;
        void Check(bool ok, string message)
        {
            if (!ok) throw new Exception("Configuration review: " + message);
            count++;
        }

        var vm = new MainViewModel();
        var plan = vm.Plan;
        plan.ConfirmWipeDisk = true;
        string baseline = PlanStore.Serialize(plan, includeSecrets: true);
        Check(vm.CompareSavedPlan(baseline).Count == 0 && plan.ConfirmWipeDisk,
            "unchanged plan matches serialized copy despite transient consent");
        plan.ComputerName = "PrivateMachine";
        plan.WifiPassword = "PrivateCredential";
        plan.Users[0].Password = "PrivateUserCredential";
        plan.Scripts.Add(new ScriptEntry { Content = "PrivateScript", Enabled = false });
        plan.XmlComponents.Add(new XmlComponentEntry { Content = "<PrivateExtra />" });
        plan.ConfirmWipeDisk = true;
        vm.Preview = "previous preview";
        var fields = vm.CompareSavedPlan(baseline);
        Check(fields.ToHashSet(StringComparer.Ordinal).SetEquals(new[]
        {
            nameof(plan.ComputerName), nameof(plan.WifiPassword), nameof(plan.Users),
            nameof(plan.Scripts), nameof(plan.XmlComponents)
        }), "comparison returns changed field names without their values");
        Check(ReferenceEquals(plan, vm.Plan) && plan.ConfirmWipeDisk && vm.Preview == "previous preview" &&
            plan.WifiPassword == "PrivateCredential" && plan.Scripts[0].Content == "PrivateScript",
            "comparison does not replace the plan, clear consent, or invalidate the preview");
        var reference = PlanStore.Deserialize(PlanStore.Serialize(plan, includeSecrets: true));
        plan.Scripts.Add(new ScriptEntry { Content = "SecondScript", Enabled = false });
        reference.Scripts.Insert(0, new ScriptEntry { Content = "SecondScript", Enabled = false });
        Check(PlanComparison.FindChangedFields(plan, reference).SequenceEqual(new[] { nameof(plan.Scripts) }),
            "script order is significant");
        try
        {
            _ = vm.CompareSavedPlan("{");
            throw new Exception("Expected a malformed JSON failure");
        }
        catch (System.Text.Json.JsonException)
        {
            Check(ReferenceEquals(plan, vm.Plan) && plan.Scripts.Count == 2, "invalid comparison leaves the current plan intact");
        }

        const string xml = """
            <unattend xmlns="urn:schemas-microsoft-com:unattend" xmlns:s="https://schneegans.de/windows/unattend-generator/">
              <settings pass="offlineServicing">
                <component name="Microsoft-Windows-PnpCustomizationsNonWinPE">
                  <DriverPaths><PathAndCredentials><Path>PRIVATE_DRIVER_PATH</Path></PathAndCredentials></DriverPaths>
                </component>
              </settings>
              <settings pass="windowsPE">
                <component name="Microsoft-Windows-Setup">
                  <UseConfigurationSet>true</UseConfigurationSet><DynamicUpdate><Enable>true</Enable></DynamicUpdate>
                  <ImageInstall><OSImage><InstallFrom><Path>PRIVATE_IMAGE_PATH</Path><MetaData /></InstallFrom></OSImage></ImageInstall>
                  <LogPath>PRIVATE_LOG_PATH</LogPath><DiskConfiguration />
                </component>
              </settings>
              <settings pass="specialize">
                <component name="Microsoft-Windows-Deployment">
                  <RunSynchronous><RunSynchronousCommand><Path>PRIVATE_COMMAND</Path></RunSynchronousCommand></RunSynchronous>
                </component>
              </settings>
              <settings pass="auditSystem" />
              <s:Extensions><s:Build><s:PEScriptCopy>PRIVATE_PE_SCRIPT</s:PEScriptCopy></s:Build></s:Extensions>
            </unattend>
            """;
        var review = InstallationReview.ForExport(Encoding.UTF8.GetBytes(xml));
        string english = UiText.EnglishText(review.Phases);
        Check(english.IndexOf("windowsPE", StringComparison.Ordinal) < english.IndexOf("offlineServicing", StringComparison.Ordinal) &&
            english.Contains("specialize") && !english.Contains("auditSystem"), "phases follow catalog order and omit empty entries");
        Check(english.Contains("PE script deployment") && english.Contains("Disk partitioning") &&
            english.Contains("Image path") && english.Contains("Dynamic Update") && english.Contains("Configuration set") &&
            english.Contains("Target Windows driver paths") && english.Contains("Synchronous commands"),
            "phase review describes actual deployment operations and dependencies");
        Check(!english.Contains("PRIVATE_") && !review.Phases.Contains("PRIVATE_"), "phase summary hides paths and command text in both languages");
        var ordinary = QuickPresets.Create(QuickPresets.LocalAccount);
        var preview = new MainViewModel { Plan = ordinary };
        var prepared = preview.ShowXmlPreview(generator.Generate(ordinary));
        Check(prepared.Phases.Contains("oobeSystem") && preview.Preview.Contains(prepared.Phases),
            "preview displays the same stage summary as export");
        return count;
    }
}
