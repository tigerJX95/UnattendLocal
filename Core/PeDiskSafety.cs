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
    public static string Selector(InstallPlan p)
    {
        Validate(p);
        using var stream = typeof(PeDiskSafety).Assembly.GetManifestResourceStream("UnattendLocal.Core.Resources.PeTargetDisk.vbs")!;
        using var reader = new StreamReader(stream);
        string source = reader.ReadToEnd();
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
    public static string Guard(string script, InstallPlan p)
    {
        var calls = Regex.Matches(script, @"(?m)^diskpart\.exe /s ");
        if (calls.Count != 1) throw new InvalidOperationException("Unexpected PE DiskPart execution chain.");
        // Gate only an executable command line, never matching user script text
        // echoed into the selector. The optional pause precedes this insertion.
        return script.Insert(calls[0].Index,
            "rem UnattendLocal destructive gate v2\r\n" +
            "cscript.exe //E:vbscript X:\\target.vbs //Nologo --verify >X:\\UL-verify.out || call :fail \"Disk safety recheck failed. No disk was formatted.\"\r\n");
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
