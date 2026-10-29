using System.Xml;
using Schneegans.Unattend;
using UnattendLocal.Localization;
namespace UnattendLocal.Core;

// Explicit opt-in to the pinned upstream PE engine. Existing Setup plans retain
// their original path. No command in this class runs on the configuration host.
public static class GeneratedPeSettings
{
    private const string Marker = "rem UnattendLocal generated PE v1";
    private static void Fail(string field, string key) => throw new ConfigurationValidationException(field, UiText.Source(key));
    public static void Validate(InstallPlan p)
    {
        if (!p.UseGeneratedPe) return;
        if (p.Architecture != "amd64" || p.DiskMode != InstallationSettings.Wipe || (p.DiskLayout != "UEFI / GPT" && p.DiskLayout != "BIOS / MBR" && p.DiskLayout != PeDiskSafety.AutomaticLayout))
            Fail(nameof(p.UseGeneratedPe), "v125.pe.scope");
        PeDiskSafety.Validate(p);
        if (p.ImageSelection != UiText.Source("text.9cbbdbe44e5d") || p.ImageIndex is null or < 1)
            Fail(nameof(p.ImageIndex), "v125.pe.image");
        if (p.EditionMode != UiText.Source("text.5056d73b13ec"))
            Fail(nameof(p.EditionMode), "v125.pe.edition");
        if (HardwareSettings.Items.Any(i => HardwareSettings.Enabled(p, i)))
            Fail(nameof(p.UseGeneratedPe), "v125.pe.hardware");
        if (p.UseSetupDrivers || p.UsePeLogPath || p.SetupDynamicUpdateMode != SetupDynamicUpdateSettings.Default || p.SetupDynamicUpdateUi != SetupDynamicUpdateSettings.UiDefault || p.CompactOsMode != CompactOsSettings.Default || p.XmlComponents.Any(e => e.Enabled && e.Pass == "windowsPE"))
            Fail(nameof(p.UseGeneratedPe), "v125.pe.conflict");
        if (p.ImportedXml != null && !OwnsImported(p)) Fail(nameof(p.UseGeneratedPe), "v125.pe.import");
    }
    public static IPESettings Build(InstallPlan p, IEditionSettings edition) => Build(p, edition, false);
    internal static IPESettings Build(InstallPlan p, IEditionSettings edition, bool legacyTargetChain, bool unguardedSelector = false) => !p.UseGeneratedPe
        ? new DefaultPESettings(edition, HardwareSettings.AllEnabled(p))
        : new GeneratePESettings(
            new UnattendedPartitionSettings(new ScriptTargetDiskSettings(PeDiskSafety.Selector(p, legacyTargetChain, unguardedSelector)), PeDiskSafety.Layout(p), RecoveryMode.Partition, AutoPartitionSettings.SystemSize(p), p.RecoverySizeMB),
            new IndexInstallFromSettings(p.ImageIndex!.Value), new AutomaticPagingFileSettings(),
            DisableDefender: false, Disable8Dot3Names: false, PauseBeforeFormatting: p.PauseBeforeFormatting,
            PauseBeforeReboot: p.PauseBeforePeReboot, CompactOs: false, SkipIntegrityCheck: false);

    public static XmlDocument Generate(Schneegans.Unattend.UnattendGenerator engine, Configuration config, InstallPlan p) => Generate(engine, config, p, false);
    internal static XmlDocument Generate(Schneegans.Unattend.UnattendGenerator engine, Configuration config, InstallPlan p, bool legacyTargetChain)
    {
        var doc = engine.GenerateXml(config);
        if (!p.UseGeneratedPe) return doc;
        var script = doc.SelectSingleNode("//*[local-name()='PEScriptCopy']")?.InnerText ?? throw new InvalidOperationException("Missing PE installation script.");
        script = PePreflight.Keyboard(script);
        const string anchor = "set \"OS_VERSION=11\"";
        int at = script.IndexOf(anchor, StringComparison.Ordinal);
        if (at < 0 || script.IndexOf(anchor, at + anchor.Length, StringComparison.Ordinal) >= 0) throw new InvalidOperationException("Unexpected PE installation script.");
        // Validate boot environment and the selected image before target discovery
        // and DiskPart. Re-emit the complete script through the upstream writer so
        // PEScriptCopy and every encoded RunSynchronous command stay consistent.
        string guard = $"""
            {Marker}
            if /I not "%PROCESSOR_ARCHITECTURE%"=="AMD64" call :fail "This PE installation mode requires x64 Windows PE."
            wpeutil.exe UpdateBootInfo || call :fail "Could not determine the firmware mode."
            set "UL_PE_FIRMWARE="
            for /f "tokens=3" %%t in ('reg.exe query HKLM\System\CurrentControlSet\Control /v PEFirmwareType 2^>nul') do set "UL_PE_FIRMWARE=%%t"
            if /I not "%UL_PE_FIRMWARE%"=="0x1" if /I not "%UL_PE_FIRMWARE%"=="0x2" call :fail "Cannot identify BIOS or UEFI. No disk was formatted."
            set "LAYOUT="
            set "FIRMWARE="
            dism.exe /English /Get-ImageInfo /ImageFile:"%IMAGE_FILE%" /Index:{p.ImageIndex!.Value.ToString(System.Globalization.CultureInfo.InvariantCulture)} >X:\UL-imageinfo.txt 2>&1
            if errorlevel 1 (
                type X:\UL-imageinfo.txt
                call :fail "Cannot read selected image. No disk was formatted."
            )
            {PePreflight.ImageCheckerWriter()}
            set "ERRORLEVEL="
            cscript.exe //E:vbscript //Nologo X:\UL-imagecheck.vbs X:\UL-imageinfo.txt {p.ImageIndex.Value.ToString(System.Globalization.CultureInfo.InvariantCulture)} >X:\UL-imagecheck.out 2>&1
            set "UL_IMAGE_CHECK_EXIT=%errorlevel%"
            type X:\UL-imagecheck.out
            if "%UL_IMAGE_CHECK_EXIT%"=="42" call :fail "Selected image is not x64. No disk was formatted."
            if not "%UL_IMAGE_CHECK_EXIT%"=="0" call :fail "Cannot verify selected image information. No disk was formatted."

            """;
        return engine.GenerateXml(config with { PESettings = new ScriptPESetttings(PeDiskSafety.Guard(script.Insert(at, guard + "\r\n"), p, legacyTargetChain)) });
    }
    private static string Pipeline(XmlDocument d)
    {
        var parts = d.SelectNodes("/*[local-name()='unattend']/*[local-name()='settings'][@pass='windowsPE'] | //*[local-name()='PEScriptCopy']")!.Cast<XmlElement>();
        return string.Join("\n", parts.Select(e => XmlMerge.Canonical(e)));
    }
    // A valid recovery note alone is insufficient: compare every PE command and
    // PEScriptCopy against an exact, known predecessor generated from that plan.
    // Retaining that predecessor as the baseline lets the ordinary three-way
    // merge upgrade it on export without discarding unrelated source content.
    internal static void RestoreLegacyImport(InstallPlan p, InstallPlan baselinePlan, GeneratorService service)
    {
        if (!p.UseGeneratedPe || OwnsImported(p) || p.ImportedXml is not { OriginalOmitted: false, Blockers.Length: 0 } data) return;
        var sourcePipeline = Pipeline(XmlMerge.Read(data.SourceXml));
        XmlDocument? predecessor = null;
        foreach (bool oldChain in new[] { false, true })
        {
            var candidate = XmlMerge.Read(System.Text.Encoding.UTF8.GetString(oldChain
                ? service.GenerateLegacyPeBaseline(baselinePlan)
                : service.GeneratePreviousPeBaseline(baselinePlan)));
            if (sourcePipeline == Pipeline(candidate)) { predecessor = candidate; break; }
        }
        if (predecessor == null) return;
        // Keep all non-PE baseline adjustments made by the other import readers.
        var baseline = XmlMerge.Read(data.BaselineXml);
        const string paths = "/*[local-name()='unattend']/*[local-name()='settings'][@pass='windowsPE'] | //*[local-name()='PEScriptCopy']";
        var currentParts = baseline.SelectNodes(paths)!.Cast<XmlElement>().ToArray();
        var oldParts = predecessor.SelectNodes(paths)!.Cast<XmlElement>().ToArray();
        if (currentParts.Length != oldParts.Length) return;
        for (int i = 0; i < currentParts.Length; i++)
            currentParts[i].ParentNode!.ReplaceChild(baseline.ImportNode(oldParts[i], true), currentParts[i]);
        data.BaselineXml = baseline.OuterXml;
    }
    public static bool OwnsImported(InstallPlan p)
    {
        if (p.ImportedXml is not { OriginalOmitted: false } data || data.SourceXml.Length == 0 || data.BaselineXml.Length == 0) return false;
        if (!data.BaselineXml.Contains(Marker, StringComparison.Ordinal)) return false;
        try
        {
            var source = XmlMerge.Read(data.SourceXml); var baseline = XmlMerge.Read(data.BaselineXml);
            return baseline.SelectSingleNode("//*[local-name()='PEScriptCopy']")?.InnerText.Contains(Marker, StringComparison.Ordinal) == true && Pipeline(source) == Pipeline(baseline);
        }
        catch (XmlException) { return false; }
    }
    public static void NormalizeLoaded(InstallPlan p)
    {
        bool owned = OwnsImported(p);
        if (p.ImportedXml != null) p.ImportedXml.GeneratedPeEditable = owned;
        if (owned)
        {
            p.ImportedXml!.AutoPartitionReadOnly = false;
            p.ImportedXml.Notices = p.ImportedXml.Notices.Where(n => n != AutoPartitionSettings.ReadOnlyNotice).ToArray();
        }
    }
    public static string Summary(InstallPlan p) => PeDiskSafety.Summary(p);
}
