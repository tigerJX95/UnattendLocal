using System.Text;
using System.Xml;
using UnattendLocal.Core;
using UnattendLocal.Desktop;
using UnattendLocal.Localization;

static class PeSafetyTests
{
    private static InstallPlan Plan()
    {
        var p = QuickPresets.Create(QuickPresets.LocalAccount);
        p.UseGeneratedPe = true; p.Architecture = "amd64"; p.KeepSensitiveFiles = true;
        p.DiskMode = InstallationSettings.Wipe; p.DiskLayout = "UEFI / GPT";
        p.TargetDiskId = 0; p.ImageSelection = "按镜像索引"; p.ImageIndex = 1;
        p.EditionMode = "安装时输入";
        // Use the canonical interactive-key value, without depending on a translated label.
        p.EditionMode = UiText.Source("text.5056d73b13ec");
        p.BypassHardware = p.BypassTPMCheck = p.BypassSecureBootCheck = p.BypassRAMCheck = false;
        p.ConfirmWipeDisk = true;
        return p;
    }
    private static string Script(byte[] bytes) => XmlMerge.Read(Encoding.UTF8.GetString(bytes)).SelectSingleNode("//*[local-name()='PEScriptCopy']")!.InnerText;
    public static void ExportChainFixtures(GeneratorService generator, string directory)
    {
        Directory.CreateDirectory(directory);
        var p = Plan(); p.PeTargetMode = "script"; p.PeTargetScript = "WScript.Echo 0"; p.ConfirmWipeDisk = true;
        string script = Script(generator.Generate(p));
        int selection = script.IndexOf("rem UnattendLocal selection gate v1", StringComparison.Ordinal);
        int selectionEnd = script.IndexOf("rem UnattendLocal selection gate end", selection, StringComparison.Ordinal);
        int verify = script.IndexOf("rem UnattendLocal destructive gate v2", StringComparison.Ordinal);
        int diskpart = script.IndexOf("diskpart.exe /s ", verify, StringComparison.Ordinal);
        File.WriteAllText(Path.Combine(directory, "selection.cmd"), script[selection..selectionEnd], Encoding.ASCII);
        File.WriteAllText(Path.Combine(directory, "verify.cmd"), script[verify..diskpart], Encoding.ASCII);
    }
    public static int Run(GeneratorService generator)
    {
        int count = 0;
        void Check(bool value, string message) { if (!value) throw new Exception("PE safety: " + message); count++; }
        void Reject(Action<InstallPlan> change)
        {
            var p = Plan(); change(p); p.ConfirmWipeDisk = true;
            try { _ = generator.Generate(p); } catch (ArgumentException) { count++; return; }
            throw new Exception("PE safety accepted invalid configuration");
        }
        foreach (var mode in new[] { "fixed", "match", "interactive", "script" })
        foreach (var layout in new[] { "UEFI / GPT", "BIOS / MBR", PeDiskSafety.AutomaticLayout })
        {
            var p = Plan(); p.PeTargetMode = mode; p.DiskLayout = layout;
            p.PeTargetScript = "WScript.Echo 0"; p.PeMinDiskGiB = 64; p.PeMaxDiskGiB = 512;
            p.PeRequireEmptyDisk = true; p.PeMatchDiskIndex = 1; p.PauseBeforeFormatting = true;
            if (mode != "fixed") p.TargetDiskId = null;
            p.ConfirmWipeDisk = true;
            string selector = PeDiskSafety.Selector(p);
            Check(selector.ReplaceLineEndings("\n").Contains("On Error Resume Next\nRunSelector") && selector.Contains("runtimeError = Err.Number") && selector.Contains("If runtimeError <> 0 Then Fail") && selector.Contains("End Sub ' UnattendLocal selector entry"), "runtime errors are caught at the entry boundary and explicitly fail");
            Check(!selector.Contains("@@") && selector.Contains("Const Mode = \"" + mode + "\""), "mode expands without unresolved placeholders");
            Check(selector.Contains("CDbl(MinGiB) * 1073741824") && !selector.Contains("CInt("), "capacity comparison does not round GiB");
            Check(selector.Contains("accepted.Count <> 1") && selector.Contains("PNPDeviceID") && selector.Contains("saved(1) <> identity"), "unique selection and identity recheck are present");
            string script = Script(generator.Generate(p));
            int gate = script.IndexOf("rem UnattendLocal destructive gate v2", StringComparison.Ordinal);
            int execute = script.IndexOf("diskpart.exe /s ", StringComparison.Ordinal);
            int pause = script.LastIndexOf("\npause", gate, StringComparison.Ordinal);
            Check(gate > 0 && execute > gate && pause > 0 && pause < gate, "destructive call is gated after pause");
            Check(script.Contains("set \"UL_TARGET_EXIT=%errorlevel%\"") && script.Contains("if not \"%UL_TARGET_EXIT%\"==\"0\" ("), "every nonzero selector status including negative HRESULTs is rejected");
            Check(script.Contains("set \"UL_VERIFY_EXIT=%errorlevel%\"") && script.Contains("if not \"%UL_VERIFY_EXIT%\"==\"0\" ("), "every nonzero recheck status including negative HRESULTs is rejected");
            Check(script.Contains("set \"TARGET_DISK=\"\nset \"ERRORLEVEL=\"") && script.Contains("if not defined TARGET_DISK call :fail"), "stale target and shadowed status are cleared and empty output is rejected");
            Check(selector.Contains("command = quote & WScript.FullName") && !selector.Contains("ExpandEnvironmentStrings"), "nested script uses the actual running host without a SystemRoot dependency");
            Check(!script.Contains("target.out) ||") && !script.Contains("UL-verify.out ||"), "legacy sign-sensitive selector and recheck operators are removed");
            Check(script.Contains("\"X:\\target.vbs\" //Nologo >X:\\target.out\nset") && !script.Contains("UL-target-error.txt"), "interactive selection prompts and diagnostics remain visible on stderr");
            Check(script.Contains("if %LAYOUT% == GPT (") && script.Contains("bcdedit.exe /set {fwbootmgr}"), "firmware boot manager is GPT-only");
            if (layout == PeDiskSafety.AutomaticLayout) Check(script.Contains("X:\\%LAYOUT%.txt") && script.Contains("CONVERT GPT") && script.Contains("ACTIVE"), "automatic layout emits both firmware paths");
            var loaded = PlanStore.Deserialize(PlanStore.Serialize(p, true));
            Check(loaded.PeTargetMode == mode && loaded.PeMinDiskGiB == 64 && loaded.PeMaxDiskGiB == 512 && loaded.PeRequireEmptyDisk && !loaded.ConfirmWipeDisk, "JSON retains parameters but not consent");
            var imported = XmlPlanImport.Import(Encoding.UTF8.GetString(generator.Generate(p)), generator);
            Check(imported.UseGeneratedPe && imported.PeTargetMode == mode && imported.ImportedXml!.GeneratedPeEditable, "owned XML restores selection mode");
            imported.ConfirmWipeDisk = true; imported.ConfirmImportedXmlRisk = true;
            Check(Script(generator.Generate(imported)) == script, "XML roundtrip preserves guarded pipeline");
        }
        foreach (var mode in new[] { "fixed", "match", "interactive", "script" })
        foreach (var bounds in new (int? Min, int? Max)[] { (null, null), (64, null), (null, 512) })
        {
            var p = Plan(); p.PeTargetMode = mode; p.PeTargetScript = "WScript.Echo 0";
            p.PeMinDiskGiB = bounds.Min; p.PeMaxDiskGiB = bounds.Max; p.ConfirmWipeDisk = true;
            string selector = PeDiskSafety.Selector(p);
            Check(selector.Contains($"Const MinGiB = {bounds.Min ?? -1}") && selector.Contains($"Const MaxGiB = {bounds.Max ?? -1}"), "omitted bounds remain unbounded in every selector mode");
            Check(Script(generator.Generate(p)).Contains("destructive gate v2"), "optional bounds do not prevent guarded export");
            var loaded = PlanStore.Deserialize(PlanStore.Serialize(p, true));
            Check(loaded.PeMinDiskGiB == bounds.Min && loaded.PeMaxDiskGiB == bounds.Max, "optional bounds survive JSON roundtrip");
        }
        foreach (int? bad in new int?[] { -1, 0, 1048577 }) { Reject(p => p.PeMinDiskGiB = bad); Reject(p => p.PeMaxDiskGiB = bad); }
        Reject(p => { p.PeMinDiskGiB = 100; p.PeMaxDiskGiB = 99; });
        Reject(p => p.PeTargetMode = "first-match");
        Reject(p => p.PeMatchDiskIndex = -1);
        Reject(p => { p.PeTargetMode = "script"; p.PeTargetScript = ""; });
        Reject(p => { p.PeTargetMode = "script"; p.PeTargetScript = new string('a',401); });
        Reject(p => { p.PeTargetMode = "script"; p.PeTargetScript = "WScript.Echo 0\0"; });
        var old = PlanStore.Deserialize("{\"SchemaVersion\":1,\"UseGeneratedPe\":false,\"TargetDiskId\":3}");
        Check(old.PeTargetMode == "fixed" && old.PeMinDiskGiB == null && old.TargetDiskId == 3, "older plans keep fixed numbering and installation mode");
        var consent = Plan();
        foreach (Action<InstallPlan> change in new Action<InstallPlan>[] { p=>p.PeTargetMode="match",p=>p.PeMinDiskGiB=80,p=>p.PeMaxDiskGiB=900,p=>p.PeMatchDiskIndex=4,p=>p.PeRequireEmptyDisk=true,p=>p.PeRequireInterfaceType=true,p=>p.PeRequireFixedMedia=true,p=>p.PeTargetScript="WScript.Echo 4" })
        { consent.ConfirmWipeDisk = true; change(consent); Check(!consent.ConfirmWipeDisk, "selection edits revoke erase consent"); }
        var special = Plan(); special.PeTargetMode = "script"; special.PeTargetScript = "' @@MIN@@ & %TARGET_DISK%\r\nWScript.Echo 0";
        string rebuilt = "";
        foreach (var line in PeDiskSafety.Selector(special).Split('\n'))
        {
            var part = System.Text.RegularExpressions.Regex.Match(line.Trim(), "^custom\\.Write \"(.*)\"$");
            if (part.Success) rebuilt += part.Groups[1].Value.Replace("\"\"", "\"");
            else if (line.Trim() == "custom.WriteLine \"\"") rebuilt += "\n";
        }
        Check(rebuilt.TrimEnd('\n') == special.PeTargetScript.Replace("\r\n", "\n"), "custom text remains literal during substitution and chunking");
        special.PeTargetScript = "'" + new string('\"',399); special.ConfirmWipeDisk = true;
        Check(Script(generator.Generate(special)).Contains("destructive gate v2"), "quote-heavy script fits the PE command-line limit");
        var view = new MainViewModel { Plan = special };
        Check(view.IsPeScript && !view.IsTargetDisk, "dynamic selector does not expose an active fixed target input");
        Check(SettingsSearchCatalog.Search("动态磁盘").Any(x=>x.Target=="PeDiskSelectionMode") && ConfigurationErrorLocation.FromField(nameof(InstallPlan.PeTargetScript))?.Setting.Target=="PeTargetScriptEditor", "search and error location target new controls");
        return count;
    }
}
