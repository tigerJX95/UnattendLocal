using System.Text.RegularExpressions;
using Schneegans.Unattend;
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

    [GeneratedRegex(@"\A[0-9A-Fa-f]{64}\z", RegexOptions.CultureInvariant)]
    private static partial Regex Sha256();

    private sealed record Package(string Name, string Hash);
    internal static void Validate(InstallPlan plan) => ReadPackages(plan);

    private static Package[] ReadPackages(InstallPlan plan)
    {
        var lines = (plan.PeOfflinePackageNames ?? "").ReplaceLineEndings("\n")
            .Split('\n', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        if (lines.Length is < 1 or > 32) throw InvalidList();
        var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var packages = new List<Package>();
        foreach (string line in lines)
        {
            var parts = line.Split('|', 2, StringSplitOptions.TrimEntries);
            string name = parts[0];
            string hash = parts.Length == 2 ? parts[1] : "";
            if (!PackageName().IsMatch(name) || DeviceName().IsMatch(name) || !names.Add(name) ||
                (parts.Length == 2 && !Sha256().IsMatch(hash))) throw InvalidList();
            packages.Add(new(name, hash));
        }
        return packages.ToArray();
    }

    private static ConfigurationValidationException InvalidList() =>
        new(nameof(InstallPlan.PeOfflinePackageNames), UiText.Source("v157.pePackages.invalid"));

    internal static string Preflight(InstallPlan plan, bool legacyMedia)
    {
        var lines = new List<string> { """for %%I in ("%IMAGE_FILE%") do set "UL_PACKAGE_ROOT=%%~dI\WinPEUpdates" """ };
        var packages = ReadPackages(plan);
        if (!legacyMedia)
        {
            using var stream = typeof(PeOfflinePackageSettings).Assembly.GetManifestResourceStream("UnattendLocal.Core.Resources.PePackageCheck.vbs")!;
            using var reader = new StreamReader(stream);
            string packageEntries = string.Join("\n", packages.Select(package =>
                $"    package = \"{package.Name}\"\n    packages.Add package, \"{package.Hash}\""));
            string script = reader.ReadToEnd().Replace("@@PACKAGES@@", packageEntries, StringComparison.Ordinal);
            lines.Add(@">X:\UL-packagecheck.vbs (");
            lines.AddRange(EchoProcessor.Process(script.Split('\n')));
            lines.Add(")");
            AddPackageCheck(lines, "read");
        }
        else
        {
            foreach (var package in packages)
            {
                lines.Add($"""set "UL_PACKAGE_NAME={package.Name}" """);
                AddFileChecks(lines);
            }
        }
        return string.Join("\r\n", lines.Select(line => line.TrimEnd()));
    }

    internal static string InsertServicing(string script, InstallPlan plan, bool legacyMedia)
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
        if (!legacyMedia) AddPackageCheck(servicing, "list");
        foreach (var package in ReadPackages(plan))
        {
            servicing.Add($"""set "UL_PACKAGE_NAME={package.Name}" """);
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

    private static void AddPackageCheck(List<string> lines, string mode)
    {
        lines.Add("""set "ERRORLEVEL=" """);
        lines.Add($"""cscript.exe //E:vbscript //Nologo X:\UL-packagecheck.vbs "%UL_PACKAGE_ROOT%" {mode}""");
        string failure = mode == "read" ? "Offline package verification failed. No disk was formatted." : "Offline package verification failed.";
        lines.Add($"""if not "%errorlevel%"=="0" call :fail "{failure}" """);
    }

    internal static string[] ReviewNames(string script)
    {
        int start = script.IndexOf(Marker, StringComparison.Ordinal);
        if (start < 0) return [];
        const string assignment = """set "UL_PACKAGE_NAME=""";
        return script[start..].Split('\n').Select(line => line.Trim())
            .TakeWhile(line => !line.StartsWith(BootFiles, StringComparison.Ordinal))
            .Where(line => line.StartsWith(assignment, StringComparison.Ordinal) && line.EndsWith('"'))
            .Select(line => line[assignment.Length..^1])
            .Where(name => PackageName().IsMatch(name) && !DeviceName().IsMatch(name))
            .Take(32).ToArray();
    }

    private static void AddFileChecks(List<string> lines)
    {
        lines.Add("""if exist "%UL_PACKAGE_ROOT%\%UL_PACKAGE_NAME%\" call :fail "Expected an update file: %UL_PACKAGE_NAME%" """);
        lines.Add("""if not exist "%UL_PACKAGE_ROOT%\%UL_PACKAGE_NAME%" call :fail "Missing update file: %UL_PACKAGE_NAME%" """);
    }
}
