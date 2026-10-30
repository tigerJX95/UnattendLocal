using System.Reflection;
using System.Text;
using System.Xml;
using UnattendLocal.Core;
using UnattendLocal.Desktop;
using UnattendLocal.Localization;

static class PePagingTests
{
    private static InstallPlan Plan(string layout)
    {
        var p = QuickPresets.Create(QuickPresets.LocalAccount);
        p.UseGeneratedPe = true; p.Architecture = "amd64"; p.KeepSensitiveFiles = true;
        p.DiskMode = InstallationSettings.Wipe; p.DiskLayout = layout;
        p.TargetDiskId = 0; p.ImageSelection = UiText.Source("text.9cbbdbe44e5d"); p.ImageIndex = 1;
        p.EditionMode = UiText.Source("text.5056d73b13ec");
        p.BypassHardware = p.BypassTPMCheck = p.BypassSecureBootCheck = p.BypassRAMCheck = false;
        p.UseCustomPageFile = true; p.PageFileInitialMiB = 2048; p.PageFileMaximumMiB = 4096;
        p.ConfirmWipeDisk = true;
        return p;
    }
    private static XmlDocument Document(byte[] bytes) => XmlMerge.Read(Encoding.UTF8.GetString(bytes));
    private static string Pe(XmlDocument doc) => doc.SelectSingleNode("//*[local-name()='PEScriptCopy']")!.InnerText;
    public static int Run(GeneratorService generator)
    {
        int count = 0;
        void Check(bool ok, string message) { if (!ok) throw new Exception("PE paging: " + message); count++; }
        foreach (string layout in new[] { "UEFI / GPT", "BIOS / MBR", PeDiskSafety.AutomaticLayout })
        {
            var p = Plan(layout);
            p.Scripts.Add(new ScriptEntry { Content = "Write-Output 'PAGING-USER-SCRIPT'", Phase = "System" });
            var doc = Document(generator.Generate(p));
            string pe = Pe(doc);
            Check(pe.Contains("/v PagingFiles /t REG_MULTI_SZ /f /d \"C:\\pagefile.sys 2048 4096\""), "native initial and maximum sizes: " + layout);
            Check(pe.Split("/v PagingFiles ").Length == 2, "one native paging command only");
            Check(pe.IndexOf("/v PagingFiles ", StringComparison.Ordinal) > pe.IndexOf("dism.exe /Apply-Image", StringComparison.Ordinal), "paging follows image application");
            Check(!doc.InnerText.Contains("Win32_PageFileSetting"), "no duplicate System-phase pagefile script");
            Check(doc.InnerText.Contains("PAGING-USER-SCRIPT"), "custom user scripts preserved");
            var saved = PlanStore.Deserialize(PlanStore.Serialize(p, true));
            Check(saved.UseCustomPageFile && saved.PageFileInitialMiB == 2048 && saved.PageFileMaximumMiB == 4096 && !saved.ConfirmWipeDisk, "JSON retains settings without consent");
            var imported = XmlPlanImport.Import(doc.OuterXml, generator);
            Check(new MainViewModel { Plan = imported }.CanEditPeInstall && imported.UseCustomPageFile, "current owned PE XML remains editable");
            imported.ConfirmWipeDisk = true; imported.ConfirmImportedXmlRisk = true;
            Check(Pe(Document(generator.Generate(imported))) == pe, "current PE XML roundtrip");
            p.UseCustomPageFile = false; p.ConfirmWipeDisk = true;
            Check(!Pe(Document(generator.Generate(p))).Contains("/v PagingFiles "), "system-managed default adds no offline override");
        }
        // Exact old full documents must migrate both the PE script and the old
        // System payload, rather than preserving a second pagefile writer.
        foreach (string method in new[] { "GeneratePrePagingPeBaseline", "GeneratePreviousPeBaseline", "GenerateLegacyPeBaseline" })
        {
            var p = Plan("UEFI / GPT");
            p.Scripts.Add(new ScriptEntry { Content = "Write-Output 'PAGING-LEGACY-USER'", Phase = "System" });
            var oldBytes = (byte[])typeof(GeneratorService).GetMethod(method, BindingFlags.NonPublic | BindingFlags.Instance)!.Invoke(generator, [p])!;
            var old = Document(oldBytes); XmlPlanImport.AddSnapshot(old, p);
            Check(old.InnerText.Contains("Win32_PageFileSetting") && !Pe(old).Contains("/v PagingFiles "), "fixture retains the shipped System implementation");
            var imported = XmlPlanImport.Import(old.OuterXml, generator);
            Check(new MainViewModel { Plan = imported }.CanEditPeInstall, "recognized old XML is editable: " + method);
            imported.ConfirmWipeDisk = true; imported.ConfirmImportedXmlRisk = true;
            var updated = Document(generator.Generate(imported));
            Check(Pe(updated).Contains("/v PagingFiles ") && !updated.InnerText.Contains("Win32_PageFileSetting"), "old XML upgrades without duplicate paging writers");
            Check(updated.InnerText.Contains("PAGING-LEGACY-USER"), "upgrade preserves user scripts");
            var reopened = XmlPlanImport.Import(updated.OuterXml, generator);
            Check(new MainViewModel { Plan = reopened }.CanEditPeInstall && reopened.PageFileMaximumMiB == 4096, "upgraded XML reimports");
        }
        var regular = Plan("UEFI / GPT"); regular.UseGeneratedPe = false; regular.ConfirmWipeDisk = true;
        var normal = Document(generator.Generate(regular));
        Check(normal.InnerText.Contains("Win32_PageFileSetting") && normal.SelectSingleNode("//*[local-name()='PEScriptCopy']") == null, "ordinary Setup keeps System configuration");
        var invalid = Plan("UEFI / GPT"); invalid.PageFileInitialMiB = 8192;
        try { generator.Generate(invalid); throw new Exception("Invalid paging size accepted."); }
        catch (ConfigurationValidationException e) { Check(ConfigurationErrorLocation.FromException(e) != null, "invalid native PE sizes are blocked and locatable"); }
        return count;
    }
}
