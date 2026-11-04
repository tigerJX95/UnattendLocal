using Schneegans.Unattend;
using UnattendLocal.Localization;

namespace UnattendLocal.Core;

/// <summary>Optional target-system configuration; never executed on the authoring PC.</summary>
public static class DeploymentTuningSettings
{
    public static bool DisablesPageFile(InstallPlan plan) => plan.UseGeneratedPe && plan.PeDisablePageFile;

    public static void Validate(InstallPlan plan)
    {
        if (!plan.UseCustomPageFile || DisablesPageFile(plan)) return;
        if (plan.PageFileInitialMiB < 16 || plan.PageFileMaximumMiB > 1048576 ||
            plan.PageFileMaximumMiB < plan.PageFileInitialMiB)
            throw new ConfigurationValidationException(nameof(InstallPlan.PageFileInitialMiB),
                global::UnattendLocal.Localization.UiText.Source("v121.pagefile.invalid"));
    }

    public static string Summary(InstallPlan plan) =>
        global::UnattendLocal.Localization.UiText.Concat(new object?[] {
            global::UnattendLocal.Localization.UiText.Source("v121.ntfs"), ": ",
            global::UnattendLocal.Localization.UiText.Source(plan.DisableNewNtfsShortNames ? "v121.enabled" : "v121.unchanged"), "\n",
            PageFileStatus(plan)
        }, new bool[] {false,true,false,true,false});

    public static string PageFileStatus(InstallPlan plan)
    {
        if (DisablesPageFile(plan)) return UiText.Source("v137.pagefile.disabled");
        if (!plan.UseCustomPageFile) return UiText.Source("v131.pagefile.default");
        if (plan.PageFileInitialMiB < 16 || plan.PageFileMaximumMiB > 1048576 ||
            plan.PageFileMaximumMiB < plan.PageFileInitialMiB)
            return UiText.Source("v131.pagefile.invalid");
        return UiText.Format("v131.pagefile.custom", [
            plan.PageFileInitialMiB.ToString(System.Globalization.CultureInfo.InvariantCulture),
            plan.PageFileMaximumMiB.ToString(System.Globalization.CultureInfo.InvariantCulture)]);
    }

    public static ScriptSettings Build(InstallPlan plan, bool legacyPePaging = false)
    {
        Validate(plan);
        var existing = CustomScripts.Build(plan);
        var additions = new List<Script>();
        if (plan.DisableNewNtfsShortNames)
        {
            // Global 8dot3 creation policy only. Does NOT strip any existing short names.
            additions.Add(new Script(@"$ErrorActionPreference = 'Stop'; New-ItemProperty -Path 'HKLM:\SYSTEM\CurrentControlSet\Control\FileSystem' -Name 'NtfsDisable8dot3NameCreation' -Value 1 -PropertyType DWord -Force | Out-Null;", ScriptPhase.System, ScriptType.Ps1));
        }
        if (plan.UseCustomPageFile && (!plan.UseGeneratedPe || legacyPePaging))
        {
            // Apply to installed OS during setup, not to the running configuration editor.
            // Changes normally require a reboot. The no-page-file option uses only the PE path.
            var script = "$ErrorActionPreference = 'Stop'; " +
                "$system = Get-CimInstance -ClassName Win32_ComputerSystem; " +
                "Set-CimInstance -InputObject $system -Property @{AutomaticManagedPagefile=$false} | Out-Null; " +
                "$page = Get-CimInstance -ClassName Win32_PageFileSetting | Where-Object { $_.Name -ieq 'C:\\pagefile.sys' }; " +
                $"$sizes = @{{InitialSize=[uint32]{plan.PageFileInitialMiB}; MaximumSize=[uint32]{plan.PageFileMaximumMiB}}}; " +
                "if ($page) { $page | Set-CimInstance -Property $sizes | Out-Null } " +
                "else { $sizes.Name='C:\\pagefile.sys'; New-CimInstance -ClassName Win32_PageFileSetting -Property $sizes | Out-Null };";
            additions.Add(new Script(script, ScriptPhase.System, ScriptType.Ps1));
        }
        return existing with { Scripts = existing.Scripts.Concat(additions).ToArray() };
    }
}
