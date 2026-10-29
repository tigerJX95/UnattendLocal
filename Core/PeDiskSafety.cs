using System.Globalization;
using System.Text.RegularExpressions;
using Schneegans.Unattend;
using UnattendLocal.Localization;

namespace UnattendLocal.Core;

public static class PeDiskSafety
{
    public const string AutomaticLayout = "跟随启动方式（UEFI/GPT 或 BIOS/MBR）";
    public static PartitionLayout Layout(InstallPlan p) => p.DiskLayout switch
    {
        "UEFI / GPT" => PartitionLayout.GPT,
        "BIOS / MBR" => PartitionLayout.MBR,
        AutomaticLayout => PartitionLayout.Automatic,
        _ => throw new ConfigurationValidationException(nameof(p.DiskLayout), UiText.Source("v127.invalid"))
    };
    public static void Validate(InstallPlan p)
    {
        void Fail(string field) => throw new ConfigurationValidationException(field, UiText.Source("v127.invalid"));
        if (p.PeTargetMode is not ("fixed" or "match" or "interactive" or "script")) Fail(nameof(p.PeTargetMode));
        if (p.PeTargetMode == "fixed" && p.TargetDiskId is null or < 0) Fail(nameof(p.TargetDiskId));
        if (p.PeMinDiskGiB is < 1 or > 1048576 || p.PeMaxDiskGiB is < 1 or > 1048576 ||
            (p.PeMinDiskGiB.HasValue && p.PeMaxDiskGiB.HasValue && p.PeMinDiskGiB > p.PeMaxDiskGiB)) Fail(nameof(p.PeMinDiskGiB));
        if (p.PeMatchDiskIndex is < 0) Fail(nameof(p.PeMatchDiskIndex));
        if (p.PeTargetMode == "script" && (string.IsNullOrWhiteSpace(p.PeTargetScript) || p.PeTargetScript.Length > 65536 ||
            ScriptLines(p.PeTargetScript).Length > 1024 || ScriptLines(p.PeTargetScript).Any(l => l.Length > 400) ||
            p.PeTargetScript.Any(c => char.IsControl(c) && c is not ('\r' or '\n' or '\t')) ||
            p.PeTargetScript.Any(char.IsSurrogate))) Fail(nameof(p.PeTargetScript));
    }
    public static string Selector(InstallPlan p) => Selector(p, false);
    internal static string Selector(InstallPlan p, bool legacyTargetChain, bool unguardedSelector = false)
    {
        Validate(p);
        using var stream = typeof(PeDiskSafety).Assembly.GetManifestResourceStream("UnattendLocal.Core.Resources.PeTargetDisk.vbs")!;
        using var reader = new StreamReader(stream);
        string source = reader.ReadToEnd();
        // Recognition only: reconstruct each exact shipped predecessor.
        // Public exports always retain the explicit runtime failure boundary.
        if (legacyTargetChain || unguardedSelector)
        {
            source = source.ReplaceLineEndings("\n");
            const string start = "' UnattendLocal runtime boundary start\n";
            const string end = "' UnattendLocal runtime boundary end\n";
            int begin = source.IndexOf(start, StringComparison.Ordinal);
            int finish = source.IndexOf(end, begin, StringComparison.Ordinal) + end.Length;
            source = source.Remove(begin, finish - begin)
                .Replace("End Sub ' UnattendLocal selector entry\n", "", StringComparison.Ordinal);
        }
        if (legacyTargetChain)
            source = source.ReplaceLineEndings("\n").Replace("  If Not fso.FileExists(WScript.FullName) Then Fail \"The current script host is unavailable.\"\n", "", StringComparison.Ordinal)
                .Replace("command = quote & WScript.FullName", "command = quote & shell.ExpandEnvironmentStrings(\"%SystemRoot%\\System32\\cscript.exe\")", StringComparison.Ordinal);
        string Number(int? value) => (value ?? -1).ToString(CultureInfo.InvariantCulture);
        var values = new Dictionary<string, string>
        {
            ["MODE"] = p.PeTargetMode, ["FIXED"] = Number(p.TargetDiskId), ["INDEX"] = Number(p.PeMatchDiskIndex),
            ["MIN"] = Number(p.PeMinDiskGiB), ["MAX"] = Number(p.PeMaxDiskGiB),
            ["EMPTY"] = p.PeRequireEmptyDisk ? "True" : "False", ["INTERFACE"] = p.PeRequireInterfaceType ? "True" : "False",
            ["MEDIA"] = p.PeRequireFixedMedia ? "True" : "False", ["LAYOUT"] = Layout(p).ToString(),
            ["BYTES"] = (((long)AutoPartitionSettings.SystemSize(p) + p.RecoverySizeMB + 1024) * 1048576).ToString(CultureInfo.InvariantCulture) + ".0",
            ["CUSTOM"] = p.PeTargetMode == "script" ? CustomWriter(p.PeTargetScript) : "' No custom script."
        };
        // One pass prevents placeholder-looking text inside user scripts from being rewritten.
        return Regex.Replace(source, "@@([A-Z]+)@@", m => values[m.Groups[1].Value]);
    }
    private static string[] ScriptLines(string script) => script.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n');
    private static string CustomWriter(string script)
    {
        var lines = new List<string>();
        foreach (string line in ScriptLines(script))
        {
            // PE command lines are limited to 255 characters after two levels of
            // CMD escaping. Small literal chunks also handle quote-heavy scripts.
            for (int at = 0; at < line.Length; at += 16)
                lines.Add("custom.Write \"" + line.Substring(at, Math.Min(16, line.Length - at)).Replace("\"", "\"\"") + "\"");
            lines.Add("custom.WriteLine \"\"");
        }
        return string.Join("\r\n", lines);
    }
    public static string Guard(string script, InstallPlan p) => Guard(script, p, false);
    internal static string Guard(string script, InstallPlan p, bool legacyTargetChain)
    {
        // cscript runtime errors may return negative HRESULTs. CMD's || and
        // `if errorlevel 1` do not reliably cover those: require exactly zero.
        const string oldSelection = "(cscript.exe //E:vbscript \"X:\\target.vbs\" //Nologo >X:\\target.out) || (type X:\\target.out & call :fail \"Could not determine target disk. Windows Setup will halt to avoid potential data loss.\")\r\nfor /f %%t in (X:\\target.out) do set \"TARGET_DISK=%%t\"";
        if (script.Split(oldSelection).Length != 2) throw new InvalidOperationException("Unexpected PE target selection chain.");
        string selection = """
            rem UnattendLocal selection gate v1
            set "TARGET_DISK="
            set "ERRORLEVEL="
            cscript.exe //E:vbscript "X:\target.vbs" //Nologo >X:\target.out
            set "UL_TARGET_EXIT=%errorlevel%"
            if not "%UL_TARGET_EXIT%"=="0" (
                type X:\target.out
                call :fail "Target disk selection failed. No disk was formatted."
            )
            for /f %%t in (X:\target.out) do set "TARGET_DISK=%%t"
            if not defined TARGET_DISK call :fail "No target disk was selected. No disk was formatted."
            rem UnattendLocal selection gate end
            """;
        if (!legacyTargetChain) script = script.Replace(oldSelection, selection.ReplaceLineEndings("\r\n"), StringComparison.Ordinal);
        var calls = Regex.Matches(script, @"(?m)^diskpart\.exe /s ");
        if (calls.Count != 1) throw new InvalidOperationException("Unexpected PE DiskPart execution chain.");
        // Gate only an executable command line, never matching user script text
        // echoed into the selector. The optional pause precedes this insertion.
        if (legacyTargetChain) return script.Insert(calls[0].Index,
            "rem UnattendLocal destructive gate v2\r\n" +
            "cscript.exe //E:vbscript X:\\target.vbs //Nologo --verify >X:\\UL-verify.out || call :fail \"Disk safety recheck failed. No disk was formatted.\"\r\n");
        return script.Insert(calls[0].Index,
            "rem UnattendLocal destructive gate v2\r\n" +
            "set \"ERRORLEVEL=\"\r\n" +
            "cscript.exe //E:vbscript X:\\target.vbs //Nologo --verify >X:\\UL-verify.out 2>&1\r\n" +
            "set \"UL_VERIFY_EXIT=%errorlevel%\"\r\n" +
            "if not \"%UL_VERIFY_EXIT%\"==\"0\" (\r\n" +
            "    type X:\\UL-verify.out\r\n" +
            "    call :fail \"Disk safety recheck failed. No disk was formatted.\"\r\n" +
            ")\r\n");
    }

    public static string Summary(InstallPlan p)
    {
        string Flag(bool value) => UiText.Source(value ? "v127.yes" : "v127.no");
        string Number(int? value) => value?.ToString(CultureInfo.InvariantCulture) ?? UiText.Source("v127.unbounded");
        var main = UiText.Format("v127.summary",
            UiText.Source("v127.mode." + (p.PeTargetMode is "fixed" or "match" or "interactive" or "script" ? p.PeTargetMode : "fixed")),
            p.PeTargetMode == "fixed" ? p.TargetDiskId?.ToString(CultureInfo.InvariantCulture) ?? "?" : "—",
            p.DiskLayout, p.ImageIndex?.ToString(CultureInfo.InvariantCulture) ?? "?");
        var conditions = UiText.Format("v127.conditions", Number(p.PeMinDiskGiB), Number(p.PeMaxDiskGiB),
            p.PeTargetMode == "match" ? Number(p.PeMatchDiskIndex) : "—", Flag(p.PeRequireEmptyDisk),
            Flag(p.PeRequireInterfaceType), Flag(p.PeRequireFixedMedia));
        return UiText.Concat([main, "\n", conditions], [false, false, false]);
    }
}
