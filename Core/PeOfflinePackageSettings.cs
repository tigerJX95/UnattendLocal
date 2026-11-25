using System.Text.RegularExpressions;
using UnattendLocal.Localization;

namespace UnattendLocal.Core;

internal static partial class PeOfflinePackageSettings
{
    internal const string Marker = "rem UnattendLocal offline packages v1";
    private const string ApplyImage = """dism.exe /Apply-Image /ImageFile:%IMAGE_FILE% %SWM_PARAM% %IMG_PARAM% /ApplyDir:W:\""";
    private const string ApplyFailure = """ || call :fail "dism.exe encountered an error." """;
    private const string BootFiles = """bcdboot.exe W:\Windows /s S:""";

    [GeneratedRegex(@"\A[A-Za-z0-9][A-Za-z0-9._ -]{0,119}\.(?:[mM][sS][uU]|[cC][aA][bB])\z", RegexOptions.CultureInvariant)]
    private static partial Regex PackageName();

    [GeneratedRegex(@"\A(?:CON|PRN|AUX|NUL|COM[0-9]|LPT[0-9]) *(?:\.|\z)", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex DeviceName();

    internal static void Validate(InstallPlan plan) => ReadNames(plan);

    private static string[] ReadNames(InstallPlan plan)
    {
        var names = (plan.PeOfflinePackageNames ?? "").ReplaceLineEndings("\n")
            .Split('\n', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        if (names.Length is < 1 or > 32 || names.Any(name => !PackageName().IsMatch(name) || DeviceName().IsMatch(name)) ||
            names.Distinct(StringComparer.OrdinalIgnoreCase).Count() != names.Length)
            throw new ConfigurationValidationException(nameof(InstallPlan.PeOfflinePackageNames), UiText.Source("v156.pePackages.invalid"));
        return names;
    }

    internal static string Preflight(InstallPlan plan)
    {
        var lines = new List<string> { """for %%I in ("%IMAGE_FILE%") do set "UL_PACKAGE_ROOT=%%~dI\WinPEUpdates" """ };
        foreach (string name in ReadNames(plan))
        {
            lines.Add($"""set "UL_PACKAGE_NAME={name}" """);
            AddFileChecks(lines);
        }
        return string.Join("\r\n", lines.Select(line => line.TrimEnd()));
    }

    internal static string InsertServicing(string script, InstallPlan plan)
    {
        var lines = script.ReplaceLineEndings("\n").Split('\n').ToList();
        int index = lines.FindIndex(line => line.TrimStart().StartsWith(ApplyImage, StringComparison.Ordinal));
        string failure = ApplyFailure.TrimEnd();
        if (index < 0 || !lines[index].EndsWith(failure, StringComparison.Ordinal) ||
            lines.Count(line => line.TrimStart().StartsWith(ApplyImage, StringComparison.Ordinal)) != 1 ||
            lines.FindIndex(line => line.TrimStart().StartsWith(BootFiles, StringComparison.Ordinal)) <= index)
            throw new InvalidOperationException("Unexpected PE image deployment structure.");

        lines[index] = lines[index][..^failure.Length];
        lines.Insert(index, "set \"ERRORLEVEL=\"");
        var servicing = new List<string>
        {
            """set "UL_APPLY_EXIT=%errorlevel%" """,
            """if not "%UL_APPLY_EXIT%"=="0" call :fail "Could not apply the Windows image (%UL_APPLY_EXIT%)." """,
            "",
            Marker,
            """call :print "Applying offline Windows packages" """,
            @"if not exist W:\Windows\Logs\DISM\ mkdir W:\Windows\Logs\DISM",
            """if not exist W:\Windows\Logs\DISM\ call :fail "Could not create the offline servicing log directory." """,
            @"if not exist W:\Windows\Temp\ mkdir W:\Windows\Temp",
            """if not exist W:\Windows\Temp\ call :fail "Could not create the offline servicing scratch directory." """
        };
        foreach (string name in ReadNames(plan))
        {
            servicing.Add($"""set "UL_PACKAGE_NAME={name}" """);
            AddFileChecks(servicing);
            servicing.Add("""echo:Applying "%UL_PACKAGE_NAME%" """);
            servicing.Add("""set "ERRORLEVEL=" """);
            servicing.Add("""dism.exe /Image:W:\ /Add-Package /PackagePath:"%UL_PACKAGE_ROOT%\%UL_PACKAGE_NAME%" /ScratchDir:W:\Windows\Temp /LogPath:W:\Windows\Logs\DISM\OfflineUpdates.log""");
            servicing.Add("""set "UL_PACKAGE_EXIT=%errorlevel%" """);
            servicing.Add("""if not "%UL_PACKAGE_EXIT%"=="0" if not "%UL_PACKAGE_EXIT%"=="3010" call :fail "Offline package failed (%UL_PACKAGE_EXIT%). See W:\Windows\Logs\DISM\OfflineUpdates.log." """);
        }
        lines.InsertRange(index + 2, servicing.Select(line => line.TrimEnd()));
        return string.Join("\r\n", lines);
    }

    private static void AddFileChecks(List<string> lines)
    {
        lines.Add("""if exist "%UL_PACKAGE_ROOT%\%UL_PACKAGE_NAME%\" call :fail "Expected an update file: %UL_PACKAGE_NAME%" """);
        lines.Add("""if not exist "%UL_PACKAGE_ROOT%\%UL_PACKAGE_NAME%" call :fail "Missing update file: %UL_PACKAGE_NAME%" """);
    }
}
