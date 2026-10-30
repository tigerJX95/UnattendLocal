using System.Text;
using System.Xml;
using UnattendLocal.Core;
using UnattendLocal.Desktop;
using UnattendLocal.Localization;

static class V131DeploymentTests
{
    private static InstallPlan Plan(string layout)
    {
        var p = QuickPresets.Create(QuickPresets.LocalAccount);
        p.UseGeneratedPe = true; p.KeepSensitiveFiles = true;
        p.DiskMode = InstallationSettings.Wipe; p.DiskLayout = layout;
        p.TargetDiskId = 0; p.ImageSelection = UiText.Source("text.9cbbdbe44e5d"); p.ImageIndex = 1;
        p.EditionMode = UiText.Source("text.5056d73b13ec");
        p.BypassHardware = p.BypassTPMCheck = p.BypassSecureBootCheck = p.BypassRAMCheck = false;
        p.ConfirmWipeDisk = true;
        return p;
    }
    private static XmlDocument Document(byte[] bytes) => XmlMerge.Read(Encoding.UTF8.GetString(bytes));
    private static string ApplyLine(XmlDocument doc) => doc.SelectSingleNode("//*[local-name()='PEScriptCopy']")!.InnerText
        .Split('\n').Single(line => line.TrimStart().StartsWith("dism.exe /Apply-Image", StringComparison.Ordinal));
    public static int Run(GeneratorService g)
    {
        int count = 0;
        void Check(bool ok, string message) { if (!ok) throw new Exception("V1.31: " + message); count++; }
        foreach (string layout in new[] { "UEFI / GPT", "BIOS / MBR", PeDiskSafety.AutomaticLayout })
        foreach (string mode in new[] { CompactOsSettings.Default, CompactOsSettings.Enabled, CompactOsSettings.Disabled })
        {
            var p = Plan(layout); p.CompactOsMode = mode;
            p.UseCustomPageFile = true; p.PageFileInitialMiB = 2048; p.PageFileMaximumMiB = 4096;
            p.Scripts.Add(new ScriptEntry { Phase = "System", Content = "Write-Output 'KEEP-V131-SCRIPT'" });
            var doc = Document(g.Generate(p));
            Check(ApplyLine(doc).Contains(" /Compact ") == (mode == CompactOsSettings.Enabled), "PE compression follows selected mode: " + layout + mode);
            Check(doc.SelectSingleNode("//*[local-name()='OSImage']/*[local-name()='Compact']") == null, "no ineffective Setup Compact override");
            Check(doc.InnerText.Contains("/v PagingFiles ") && !doc.InnerText.Contains("Win32_PageFileSetting"), "paging remains native without duplicate script");
            var imported = XmlPlanImport.Import(doc.OuterXml, g);
            Check(imported.CompactOsMode == mode && CompactOsSettings.CanEdit(imported), "own XML retains mode and remains editable");
            var json = PlanStore.Deserialize(PlanStore.Serialize(imported, true));
            Check(json.CompactOsMode == mode && CompactOsSettings.CanEdit(json) && !json.ConfirmWipeDisk, "imported JSON keeps editable mode without consent");
            json.ConfirmWipeDisk = true; json.ConfirmImportedXmlRisk = true;
            json.CompactOsMode = mode == CompactOsSettings.Enabled ? CompactOsSettings.Disabled : CompactOsSettings.Enabled;
            // A CompactOS edit clears disk consent, just like other deployment edits.
            json.ConfirmWipeDisk = true; json.ConfirmImportedXmlRisk = true;
            var edited = Document(g.Generate(json));
            Check(ApplyLine(edited).Contains(" /Compact ") == (json.CompactOsMode == CompactOsSettings.Enabled), "owned imported PE can change compression");
            Check(edited.InnerText.Contains("KEEP-V131-SCRIPT") && edited.InnerText.Contains("C:\\pagefile.sys 2048 4096"), "edit preserves scripts and paging");
            var reopened = XmlPlanImport.Import(edited.OuterXml, g);
            Check(reopened.CompactOsMode == json.CompactOsMode && CompactOsSettings.CanEdit(reopened), "edited XML reimports");
        }
        var plan = Plan("UEFI / GPT");
        var vm = new MainViewModel { Plan = plan };
        var changes = new List<string?>(); vm.PropertyChanged += (_, e) => changes.Add(e.PropertyName);
        Check(vm.PageFileStatus == UiText.Source("v131.pagefile.default"), "unchecked pagefile means retain image settings");
        plan.UseCustomPageFile = true; plan.PageFileInitialMiB = 3072; plan.PageFileMaximumMiB = 6144;
        Check(vm.PageFileStatus.Contains("3072") && vm.PageFileStatus.Contains("6144") && changes.Contains(nameof(MainViewModel.PageFileStatus)), "pagefile status updates live");
        plan.PageFileMaximumMiB = 2048;
        Check(vm.PageFileStatus == UiText.Source("v131.pagefile.invalid"), "invalid sizes are not presented as deployable");
        plan.UseCustomPageFile = false;
        Check(vm.PageFileStatus == UiText.Source("v131.pagefile.default") && plan.PageFileInitialMiB == 3072, "turning off retains custom draft");
        var target = SettingsSearchCatalog.All.Single(item => item.Target == "CustomPageFileEditor");
        Check(target.PageIndex == 6 && target.Page == "OOBE", "navigation resolves existing single editor");
        plan.CompactOsMode = CompactOsSettings.Enabled; plan.ConfirmWipeDisk = true;
        var original = Document(g.Generate(plan));
        // A marked but modified script must not acquire edit rights.
        var copy = original.SelectSingleNode("//*[local-name()='PEScriptCopy']")!;
        copy.InnerText += "\r\nrem external-change";
        var modified = XmlPlanImport.Import(original.OuterXml, g);
        Check(!CompactOsSettings.CanEdit(modified), "modified PE script remains protected");
        return count;
    }
}
