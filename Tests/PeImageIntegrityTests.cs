using System.Text;
using System.Text.Json.Nodes;
using System.Xml;
using UnattendLocal.Core;
using UnattendLocal.Desktop;
using UnattendLocal.Localization;

static class PeImageIntegrityTests
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
    private static XmlDocument Document(byte[] data) => XmlMerge.Read(Encoding.UTF8.GetString(data));
    private static string Script(XmlDocument doc) => doc.SelectSingleNode("//*[local-name()='PEScriptCopy']")!.InnerText;
    private static string Apply(string script) => script.Split('\n').Single(line => line.TrimStart().StartsWith("dism.exe /Apply-Image", StringComparison.Ordinal));
    public static int Run(GeneratorService g)
    {
        int count = 0;
        void Check(bool ok, string message) { if (!ok) throw new Exception("PE image integrity: " + message); count++; }
        foreach (string layout in new[] { "UEFI / GPT", "BIOS / MBR", PeDiskSafety.AutomaticLayout })
        foreach (bool verify in new[] { true, false })
        {
            var p = Plan(layout); p.PeCheckImageIntegrity = verify;
            p.CompactOsMode = CompactOsSettings.Enabled; p.UseCustomPageFile = true;
            p.ConfirmWipeDisk = true;
            var doc = Document(g.Generate(p)); string pe = Script(doc); string apply = Apply(pe);
            Check(apply.Contains(" /CheckIntegrity /Verify") == verify && apply.Contains(" /CheckIntegrity") == verify && apply.Contains(" /Verify") == verify, "paired DISM options follow choice: " + layout + verify);
            Check(apply.Contains(" /Compact ") && pe.Contains("/v PagingFiles "), "other deployment settings retained");
            Check(pe.Contains("/Get-ImageInfo") && pe.Contains("UL_IMAGE_CHECK_EXIT") && pe.Contains("Selected image is not x64"), "image metadata and architecture checks remain");
            Check(pe.Contains("UL_TARGET_EXIT") && pe.Contains("UL_VERIFY_EXIT") && pe.Contains("--verify"), "target selection and destructive recheck remain");
            var imported = XmlPlanImport.Import(doc.OuterXml, g);
            Check(imported.PeCheckImageIntegrity == verify && GeneratedPeSettings.OwnsImported(imported), "XML restores editable choice");
            var saved = PlanStore.Deserialize(PlanStore.Serialize(imported, true));
            Check(saved.PeCheckImageIntegrity == verify && !saved.ConfirmWipeDisk, "JSON restores choice without consent");
            saved.PeCheckImageIntegrity = !verify; saved.ConfirmWipeDisk = saved.ConfirmImportedXmlRisk = true;
            Check(Apply(Script(Document(g.Generate(saved)))).Contains(" /CheckIntegrity /Verify") == !verify, "imported choice can be changed");
            var vm = new MainViewModel { Plan = p };
            Check(vm.IsPeIntegrityCheckDisabled == !verify && vm.DiskRisk.Contains(UiText.Source(verify ? "v132.integrity.on" : "v132.integrity.off")), "warning and deployment summary follow selection");
        }
        var plan = Plan("UEFI / GPT");
        var json = JsonNode.Parse(PlanStore.Serialize(plan, true))!.AsObject(); json.Remove(nameof(InstallPlan.PeCheckImageIntegrity));
        Check(PlanStore.Deserialize(json.ToJsonString()).PeCheckImageIntegrity, "old JSON defaults to verification enabled");
        var old = Document(g.Generate(plan));
        var note = old.SelectNodes("//comment()")!.Cast<XmlComment>().Single(c => c.Value!.StartsWith("UnattendLocal:v1 "));
        var parts = note.Value!.Split(' '); var snapshot = JsonNode.Parse(Encoding.UTF8.GetString(Convert.FromBase64String(parts[2])))!.AsObject();
        snapshot.Remove(nameof(InstallPlan.PeCheckImageIntegrity)); parts[2] = Convert.ToBase64String(Encoding.UTF8.GetBytes(snapshot.ToJsonString())); note.Value = string.Join(' ', parts);
        var oldPlan = XmlPlanImport.Import(old.OuterXml, g);
        Check(oldPlan.PeCheckImageIntegrity && GeneratedPeSettings.OwnsImported(oldPlan), "old XML retains verification and editability");
        plan.ConfirmWipeDisk = plan.ConfirmImportedXmlRisk = true; plan.PeCheckImageIntegrity = false;
        Check(!plan.ConfirmWipeDisk && !plan.ConfirmImportedXmlRisk, "verification change revokes deployment consent");
        plan.ImageIndex = null; plan.ConfirmWipeDisk = true;
        try { g.Generate(plan); throw new Exception("Missing index accepted."); }
        catch (ConfigurationValidationException e) { Check(e.Field == nameof(InstallPlan.ImageIndex), "disabled integrity check does not permit a missing index"); }
        var regular = Plan("UEFI / GPT"); regular.UseGeneratedPe = false; regular.ConfirmWipeDisk = true;
        string before = Document(g.GenerateStandalone(regular)).OuterXml;
        regular.PeCheckImageIntegrity = false; regular.ConfirmWipeDisk = true;
        Check(XmlMerge.Equal(XmlMerge.Read(before).DocumentElement, Document(g.GenerateStandalone(regular)).DocumentElement), "regular Setup is unaffected");
        Check(SettingsSearchCatalog.Search("PeCheckImageIntegrity").Single().Target == "PeImageIntegrityCheck", "search locates a single checkbox");
        var modified = Document(g.Generate(Plan("UEFI / GPT")));
        modified.SelectSingleNode("//*[local-name()='PEScriptCopy']")!.InnerText += "\r\nrem external edit";
        Check(!GeneratedPeSettings.OwnsImported(XmlPlanImport.Import(modified.OuterXml, g)), "modified scripts remain protected");
        return count;
    }
}
