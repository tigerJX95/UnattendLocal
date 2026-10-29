using System.Reflection;
using System.Text;
using System.Xml;
using UnattendLocal.Core;
using UnattendLocal.Desktop;
using UnattendLocal.Localization;

static class PeImportCompatibilityTests
{
    private static InstallPlan Plan(string mode, string layout)
    {
        var p = QuickPresets.Create(QuickPresets.LocalAccount);
        p.UseGeneratedPe = true; p.Architecture = "amd64"; p.KeepSensitiveFiles = true;
        p.DiskMode = InstallationSettings.Wipe; p.DiskLayout = layout; p.TargetDiskId = 0;
        p.ImageSelection = UiText.Source("text.9cbbdbe44e5d"); p.ImageIndex = 3;
        p.EditionMode = UiText.Source("text.5056d73b13ec");
        p.BypassHardware = p.BypassTPMCheck = p.BypassSecureBootCheck = p.BypassRAMCheck = false;
        p.PeTargetMode = mode; p.PeMinDiskGiB = 40; p.PeMaxDiskGiB = 60;
        p.PeTargetScript = "WScript.Echo 0"; p.PauseBeforeFormatting = true; p.ConfirmWipeDisk = true;
        return p;
    }
    private static XmlDocument Legacy(GeneratorService service, InstallPlan p)
    {
        var method = typeof(GeneratorService).GetMethod("GenerateLegacyPeBaseline", BindingFlags.Instance | BindingFlags.NonPublic)!;
        var bytes = (byte[])method.Invoke(service, [p])!;
        var doc = XmlMerge.Read(Encoding.UTF8.GetString(bytes));
        XmlPlanImport.AddSnapshot(doc, p);
        return doc;
    }
    public static int Run(GeneratorService service)
    {
        int count = 0;
        void Check(bool ok, string message) { if (!ok) throw new Exception("PE import compatibility: " + message); count++; }
        foreach (string mode in new[] { "fixed", "match", "interactive", "script" })
        foreach (string layout in new[] { "UEFI / GPT", "BIOS / MBR", PeDiskSafety.AutomaticLayout })
        {
            var original = Plan(mode, layout); var legacy = Legacy(service, original);
            string before = legacy.SelectSingleNode("//*[local-name()='PEScriptCopy']")!.InnerText;
            Check(!before.Contains("UL_TARGET_EXIT") && before.Contains("UL-verify.out ||"), "fixture reproduces the shipped predecessor");
            var imported = XmlPlanImport.Import(legacy.OuterXml, service);
            var view = new MainViewModel { Plan = imported };
            Check(view.CanEditPeInstall && view.CanEditAutoPartition && !view.HasReadonlyAutoPartition, "known old pipeline remains editable");
            Check(imported.PeTargetMode == mode && imported.DiskLayout == layout && imported.PeMinDiskGiB == 40 && imported.PeMaxDiskGiB == 60 && imported.PauseBeforeFormatting, "import preserves selection, capacities, layout and pause");
            Check(!imported.ConfirmWipeDisk && !imported.ConfirmImportedXmlRisk, "recognition grants no disk consent");
            var saved = PlanStore.Deserialize(PlanStore.Serialize(imported, true));
            Check(new MainViewModel { Plan = saved }.CanEditPeInstall && !saved.ImportedXml!.AutoPartitionReadOnly, "recognized import stays editable through JSON save/load");
            saved.PeMinDiskGiB = null; saved.PeMaxDiskGiB = 80; saved.ImageIndex = 2;
            saved.PeTargetMode = "fixed"; saved.TargetDiskId = 1; saved.ConfirmWipeDisk = true; saved.ConfirmImportedXmlRisk = true;
            var updated = service.Generate(saved); var doc = XmlMerge.Read(Encoding.UTF8.GetString(updated));
            string script = doc.SelectSingleNode("//*[local-name()='PEScriptCopy']")!.InnerText;
            Check(script.Contains("UL_TARGET_EXIT") && script.Contains("UL_VERIFY_EXIT") && script.Contains("WScript.FullName"), "re-export upgrades every old target gate and host path");
            Check(script.Contains("Const FixedIndex = 1") && script.Contains("Const MaxGiB = 80") && script.Contains("/Index:2"), "edits reach the actual deployment script");
            var reimported = XmlPlanImport.Import(Encoding.UTF8.GetString(updated), service);
            Check(new MainViewModel { Plan = reimported }.CanEditPeInstall && reimported.PeTargetMode == "fixed" && reimported.ImageIndex == 2, "updated XML reimports without relocking");
        }
        foreach (string change in new[] { "script", "command", "missing-note" })
        {
            var original = Plan("match", "UEFI / GPT"); var doc = Legacy(service, original);
            if (change == "script") doc.SelectSingleNode("//*[local-name()='PEScriptCopy']")!.InnerText += "\necho modified\n";
            if (change == "command") doc.SelectSingleNode("//*[local-name()='RunSynchronousCommand']/*[local-name()='Path']")!.InnerText += " & echo modified";
            // Rehash a modified document to prove a recovery comment alone never unlocks it.
            XmlPlanImport.AddSnapshot(doc, original);
            if (change == "missing-note") foreach (var comment in doc.SelectNodes("//comment()")!.Cast<XmlComment>().ToArray()) comment.ParentNode!.RemoveChild(comment);
            var imported = XmlPlanImport.Import(doc.OuterXml, service);
            Check(!new MainViewModel { Plan = imported }.CanEditPeInstall, "unknown or altered source stays protected: " + change);
        }
        return count;
    }
}
