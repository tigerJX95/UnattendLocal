using System.Text;
using System.Text.Json.Nodes;
using System.Xml;
using UnattendLocal.Core;
using UnattendLocal.Desktop;
using UnattendLocal.Localization;

static class GeneratedPeTests
{
    private static InstallPlan Plan()
    {
        var p = InstallPlan.New();
        foreach (var user in p.Users) user.Password = "PeTest123!";
        p.KeepSensitiveFiles = true;
        p.UseGeneratedPe = true; p.Architecture = "amd64";
        p.DiskMode = InstallationSettings.Wipe; p.DiskLayout = "UEFI / GPT";
        p.TargetDiskId = 0; p.ImageSelection = UiText.Source("text.9cbbdbe44e5d"); p.ImageIndex = 1;
        p.EditionMode = UiText.Source("text.5056d73b13ec");
        p.BypassHardware = p.BypassTPMCheck = p.BypassSecureBootCheck = p.BypassRAMCheck = false;
        p.CompactOsMode = CompactOsSettings.Default;
        p.ConfirmWipeDisk = true;
        return p;
    }
    private static string Script(byte[] xml) => XmlMerge.Read(Encoding.UTF8.GetString(xml)).SelectSingleNode("//*[local-name()='PEScriptCopy']")?.InnerText ?? "";
    public static int Run(GeneratorService g)
    {
        int count = 0;
        void Check(bool condition, string name) { if (!condition) throw new Exception("PE regression: " + name); count++; }
        void Reject(Action<InstallPlan> change, string name)
        {
            var p = Plan(); change(p);
            try { _ = g.Generate(p); throw new Exception("PE regression: accepted " + name); }
            catch (ArgumentException) { count++; }
        }
        var p = Plan(); var xml = g.Generate(p); string script = Script(xml);
        Check(script.Contains("rem UnattendLocal generated PE v1"), "upstream script mode exported");
        Check(System.Text.RegularExpressions.Regex.IsMatch(script, @"select disk[= ]%TARGET_DISK%", System.Text.RegularExpressions.RegexOptions.IgnoreCase), "selected disk pipeline");
        Check(script.Contains("Const FixedIndex = 0"), "explicit target index WMI query");
        Check(script.Contains("create partition efi", StringComparison.OrdinalIgnoreCase) && script.Contains("create partition msr", StringComparison.OrdinalIgnoreCase), "GPT boot and MSR partitions");
        Check(script.Contains("/CheckIntegrity /Verify"), "image verification retained");
        Check(script.Contains("bcdboot.exe") && script.Contains("wpeutil.exe reboot"), "boot files and reboot");
        foreach (string check in new[] { "%PROCESSOR_ARCHITECTURE%", "UL_PE_FIRMWARE", "/Get-ImageInfo", "UL_IMAGE_CHECK_EXIT" })
            Check(script.IndexOf(check, StringComparison.Ordinal) >= 0 && script.IndexOf(check, StringComparison.Ordinal) < script.IndexOf("diskpart.exe /s", StringComparison.Ordinal), "preflight before DiskPart: " + check);
        Check(script.Contains("/Index:1") && script.Contains("Architecture field is missing or duplicated."), "index and image architecture preflight");
        Check(!Encoding.UTF8.GetString(xml).Contains("<ImageInstall>"), "no competing Setup installation path");
        Check(!p.UseSetupDrivers && !p.UsePeLogPath, "unsupported switches unchanged");
        Check(InstallationSettings.DiskSummary(p).Contains("PE"), "PE pipeline appears in summary");
        var vm = new MainViewModel { Plan = p };
        Check(ConfigurationErrorLocation.FromField(nameof(InstallPlan.UseGeneratedPe))?.Setting.Target == "GeneratedPeMode", "validation field maps to mode control");
        Check(SettingsSearchCatalog.Search("PE install image index").Any(x => x.Target == "GeneratedPeMode"), "PE mode searchable");
        p.ConfirmWipeDisk = true; p.ImageIndex = 2; Check(!p.ConfirmWipeDisk, "image changes clear erase consent");
        p.ConfirmWipeDisk = true; p.UseGeneratedPe = false; Check(!p.ConfirmWipeDisk, "mode changes clear erase consent");
        p = Plan(); p.PauseBeforePeReboot = true; p.ConfirmWipeDisk = true;
        script = Script(g.Generate(p)); Check(script.Contains("Computer will now reboot\"\r\npause") || script.Contains("Computer will now reboot\"\npause"), "optional pause before reboot");
        var json = PlanStore.Serialize(p, true); var loaded = PlanStore.Deserialize(json);
        Check(loaded.UseGeneratedPe && loaded.PauseBeforePeReboot && loaded.TargetDiskId == 0 && loaded.ImageIndex == 1 && !loaded.ConfirmWipeDisk, "JSON roundtrip without consent");
        var node = JsonNode.Parse(json)!; node.AsObject().Remove(nameof(InstallPlan.UseGeneratedPe));
        Check(!PlanStore.Deserialize(node.ToJsonString()).UseGeneratedPe, "older JSON remains on Setup mode");
        var imported = XmlPlanImport.Import(Encoding.UTF8.GetString(xml), g);
        Check(imported.UseGeneratedPe && imported.DiskMode == InstallationSettings.Wipe && imported.ImageIndex == 1 && !imported.ConfirmWipeDisk, "own XML restores mode without consent");
        Check(imported.ImportedXml!.Blockers.Length == 0, "own XML import has no blockers: " + string.Join(" | ", imported.ImportedXml.Blockers));
        Check(!imported.ImportedXml.AutoPartitionReadOnly && GeneratedPeSettings.OwnsImported(imported), "own PE pipeline editable");
        Check(new MainViewModel { Plan = imported }.CanEditPeInstall, "own imported mode control remains enabled");
        imported.ConfirmWipeDisk = true; imported.ConfirmImportedXmlRisk = true;
        Check(Script(g.Generate(imported)) == Script(xml), "own XML preserves exact guarded pipeline");
        imported.ImageIndex = 2; imported.ConfirmWipeDisk = true; imported.ConfirmImportedXmlRisk = true;
        var edited = Script(g.Generate(imported));
        Check(edited.Contains("/Index:2") && !edited.Contains("/Index:1"), "editing imported image updates both checks and apply command");
        imported.UseGeneratedPe = false; imported.ConfirmWipeDisk = true; imported.ConfirmImportedXmlRisk = true;
        var ordinary = g.Generate(imported);
        Check(Script(ordinary).Length == 0 && Encoding.UTF8.GetString(ordinary).Contains("<ImageInstall>"), "switching imported mode removes owned PE pipeline");
        var noSnapshot = XmlMerge.Read(Encoding.UTF8.GetString(xml));
        foreach (XmlComment c in noSnapshot.SelectNodes("//comment()")!.Cast<XmlComment>().Where(c => c.Value!.StartsWith("UnattendLocal:v1 ")).ToArray()) c.ParentNode!.RemoveChild(c);
        var foreign = XmlPlanImport.Import(noSnapshot.OuterXml, g);
        Check(!foreign.UseGeneratedPe && foreign.ImportedXml!.AutoPartitionReadOnly, "unrecognized PE remains protected");
        Check(!new MainViewModel { Plan = foreign }.CanEditPeInstall, "foreign PE mode cannot be changed in UI");
        foreign.UseGeneratedPe = true;
        try { _ = g.Generate(foreign); throw new Exception("PE regression: overwrote unrecognized PE pipeline"); } catch (ArgumentException) { count++; }
        Reject(x => x.Architecture = "arm64", "ARM64 initial mode");
        Reject(x => x.Architecture = "x86", "x86 initial mode");

        Reject(x => x.DiskMode = InstallationSettings.Existing, "existing partition");
        Reject(x => x.DiskMode = InstallationSettings.Manual, "interactive disk mode");
        Reject(x => x.ImageSelection = UiText.Source("text.5965e61aee4c"), "image name");
        Reject(x => x.ImageIndex = null, "missing image index");
        Reject(x => x.ImageIndex = 0, "zero image index");
        Reject(x => x.TargetDiskId = null, "missing target");
        Reject(x => x.ConfirmWipeDisk = false, "missing erase consent");
        Reject(x => x.BypassTPMCheck = true, "Setup hardware bypass");
        Reject(x => x.CompactOsMode = CompactOsSettings.Enabled, "Compact OS");
        Reject(x => x.EditionMode = UiText.Source("text.42a0cbd1b22f"), "firmware installation key");
        Reject(x => x.UseSetupDrivers = true, "Setup driver path");
        Reject(x => x.UsePeLogPath = true, "Setup logging path");
        Reject(x => x.SetupDynamicUpdateMode = SetupDynamicUpdateSettings.Disabled, "Dynamic Update");
        Reject(x => x.SetupDynamicUpdateUi = SetupDynamicUpdateSettings.UiNever, "Dynamic Update UI");
        Reject(x => x.XmlComponents.Add(new XmlComponentEntry { Enabled = true, Pass = "windowsPE", Component = "Microsoft-Windows-Setup", Content = "<LogPath>C:\\Logs</LogPath>" }), "extra windowsPE XML");
        foreach (int disk in new[] { 0, 1, 3 }) foreach (int image in new[] { 1, 2, 9 })
        {
            var t = Plan(); t.TargetDiskId = disk; t.ImageIndex = image; t.ConfirmWipeDisk = true;
            string value = Script(g.Generate(t));
            Check(value.Contains("Const FixedIndex = " + disk) && value.Contains("/Index:" + image), "selected disk/image pair " + disk + "/" + image);
        }
        return count;
    }
}
