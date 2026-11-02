using System.Text;
using System.Text.Json.Nodes;
using System.Xml;
using UnattendLocal.Core;
using UnattendLocal.Desktop;
using UnattendLocal.Localization;

static class PeRecoveryTests
{
    private static InstallPlan Plan(string layout = "UEFI / GPT")
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
    private static XmlDocument Document(byte[] data) => XmlMerge.Read(Encoding.UTF8.GetString(data));
    private static string Script(XmlDocument doc) => doc.SelectSingleNode("//*[local-name()='PEScriptCopy']")!.InnerText;
    public static int Run(GeneratorService g)
    {
        int count = 0;
        void Check(bool ok, string message) { if (!ok) throw new Exception("PE recovery: " + message); count++; }
        foreach (string layout in new[] { "UEFI / GPT", "BIOS / MBR", PeDiskSafety.AutomaticLayout })
        foreach (bool recovery in new[] { true, false })
        {
            var p = Plan(layout); p.RecoverySizeMB = 2048; p.PeCreateRecoveryPartition = recovery;
            p.CompactOsMode = CompactOsSettings.Enabled; p.UseCustomPageFile = true; p.ConfirmWipeDisk = true;
            var doc = Document(g.Generate(p)); var script = Script(doc);
            string[] lines = script.ReplaceLineEndings("\n").Split('\n').Select(l => l.Trim()).ToArray();
            Check(lines.Any(l => l.StartsWith("echo:SHRINK MINIMUM=2048", StringComparison.Ordinal)) == recovery, "reserve space only when enabled: " + layout + recovery);
            Check(script.Contains("LABEL=\"Recovery\"", StringComparison.Ordinal) == recovery, "recovery partition follows choice");
            Check(lines.Contains(@"del W:\Windows\System32\Recovery\winre.wim") == !recovery, "remove WinRE only when disabled");
            Check(script.Contains("bcdboot.exe W:\\Windows /s S:") && script.Contains("LABEL=\"System\"") && script.Contains("LABEL=\"Windows\""), "boot and Windows partitions retained");
            Check(script.Contains(" /Compact /CheckIntegrity /Verify") && script.Contains("/v PagingFiles "), "CompactOS, verification and paging retained");
            Check(script.Contains("UL_TARGET_EXIT") && script.Contains("UL_VERIFY_EXIT") && script.Contains("UL_IMAGE_CHECK_EXIT"), "all safety gates retained");
            var imported = XmlPlanImport.Import(doc.OuterXml, g);
            Check(imported.PeCreateRecoveryPartition == recovery && GeneratedPeSettings.OwnsImported(imported), "XML roundtrip remains editable");
            var saved = PlanStore.Deserialize(PlanStore.Serialize(imported, true));
            Check(saved.PeCreateRecoveryPartition == recovery && !saved.ConfirmWipeDisk && saved.RecoverySizeMB == 2048, "JSON keeps draft and size without consent");
            saved.PeCreateRecoveryPartition = !recovery; saved.ConfirmWipeDisk = saved.ConfirmImportedXmlRisk = true;
            Check(Script(Document(g.Generate(saved))).Contains(@"del W:\Windows\System32\Recovery\winre.wim") == recovery, "known imported XML can switch choice");
            var vm = new MainViewModel { Plan = p };
            Check(vm.IsPeRecoveryDisabled == !recovery && vm.ShowRecoverySize == recovery, "warning and capacity visibility follow choice");
            Check(vm.DiskRisk.Contains(UiText.Source(recovery ? "v133.recovery.on" : "v133.recovery.off")), "risk summary reports recovery choice");
            if (!recovery) Check(!vm.AutoPartitionSummary.Contains("2048") && vm.AutoPartitionSummary.Contains("Windows RE"), "partition summary does not claim unused recovery space");
        }
        var plan = Plan();
        var source = Document(g.Generate(plan));
        var note = source.SelectNodes("//comment()")!.Cast<XmlComment>().Single(c => c.Value!.StartsWith("UnattendLocal:v1 "));
        var parts = note.Value!.Split(' ');
        var snapshot = JsonNode.Parse(Encoding.UTF8.GetString(Convert.FromBase64String(parts[2])))!.AsObject();
        snapshot.Remove(nameof(InstallPlan.PeCreateRecoveryPartition));
        parts[2] = Convert.ToBase64String(Encoding.UTF8.GetBytes(snapshot.ToJsonString())); note.Value = string.Join(' ', parts);
        var old = XmlPlanImport.Import(source.OuterXml, g);
        Check(old.PeCreateRecoveryPartition && GeneratedPeSettings.OwnsImported(old), "legacy XML defaults to recovery and stays editable");
        var json = JsonNode.Parse(PlanStore.Serialize(plan, true))!.AsObject(); json.Remove(nameof(InstallPlan.PeCreateRecoveryPartition));
        Check(PlanStore.Deserialize(json.ToJsonString()).PeCreateRecoveryPartition && InstallPlan.NewCustom().PeCreateRecoveryPartition, "legacy JSON and new plans retain recovery");
        plan.ConfirmWipeDisk = plan.ConfirmImportedXmlRisk = true; plan.PeCreateRecoveryPartition = false;
        Check(!plan.ConfirmWipeDisk && !plan.ConfirmImportedXmlRisk, "changing layout revokes consent");
        plan.ConfirmWipeDisk = true; plan.PeCreateRecoveryPartition = false;
        Check(plan.ConfirmWipeDisk, "unchanged choice does not revoke consent");
        var vm2 = new MainViewModel { Plan = plan }; var changes = new HashSet<string?>();
        vm2.PropertyChanged += (_, e) => changes.Add(e.PropertyName);
        plan.PeCreateRecoveryPartition = true;
        Check(vm2.ShowRecoverySize && !vm2.IsPeRecoveryDisabled && changes.Contains(nameof(MainViewModel.ShowRecoverySize)), "live edit updates visibility");
        plan.PeCreateRecoveryPartition = false; plan.RecoverySizeMB = 0; plan.ConfirmWipeDisk = true;
        Check(!Script(Document(g.Generate(plan))).Contains("echo:SHRINK"), "inactive size is not validated or generated");
        plan.PeCreateRecoveryPartition = true; plan.ConfirmWipeDisk = true;
        try { g.Generate(plan); throw new Exception("Invalid active size accepted."); }
        catch (ConfigurationValidationException e) { Check(e.Field == nameof(InstallPlan.RecoverySizeMB), "reenabling validates retained size"); }
        foreach (bool enabled in new[] { true, false })
        {
            var p = Plan(); p.PeCreateRecoveryPartition = enabled;
            long bytes = ((long)AutoPartitionSettings.SystemSize(p) + (enabled ? p.RecoverySizeMB : 0) + 1024) * 1048576;
            Check(PeDiskSafety.Selector(p).Contains(bytes + ".0"), "capacity gate follows actual layout");
        }
        var regular = Plan(); regular.UseGeneratedPe = false; regular.ConfirmWipeDisk = true;
        var before = Document(g.GenerateStandalone(regular));
        regular.PeCreateRecoveryPartition = false; regular.ConfirmWipeDisk = true;
        Check(XmlMerge.Equal(before.DocumentElement, Document(g.GenerateStandalone(regular)).DocumentElement), "regular Setup output unchanged");
        var regularVm = new MainViewModel { Plan = regular };
        Check(regularVm.ShowRecoverySize && !regularVm.IsPeRecoveryDisabled, "regular Setup still displays recovery size");
        regular.RecoverySizeMB = 0; regular.ConfirmWipeDisk = true;
        try { g.Generate(regular); throw new Exception("Invalid regular size accepted."); }
        catch (ConfigurationValidationException e) { Check(e.Field == nameof(InstallPlan.RecoverySizeMB), "regular Setup still validates recovery size"); }
        var result = SettingsSearchCatalog.Search("PeCreateRecoveryPartition").Single();
        Check(result.Target == "PeRecoveryPartitionCheck", "unique search points to checkbox");
        Check(result.Description == UiText.Source("v133.recovery.help"), "search retains dedicated help text");
        Check(SettingsSearchCatalog.Search("PeCheckImageIntegrity").Single().Description == UiText.Source("v132.integrity.help"), "existing image-integrity help is retained");
        var modified = Document(g.Generate(Plan()));
        modified.SelectSingleNode("//*[local-name()='PEScriptCopy']")!.InnerText += "\r\nrem external change";
        var external = XmlPlanImport.Import(modified.OuterXml, g);
        Check(!GeneratedPeSettings.OwnsImported(external) && !new MainViewModel { Plan = external }.CanEditPeInstall, "modified PE XML remains protected");
        return count;
    }
}
