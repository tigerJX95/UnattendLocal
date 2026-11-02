using UnattendLocal.Core;

namespace UnattendLocal.Desktop;

// Resolve a hidden deployment editor to its actual prerequisite. This changes
// navigation only; it must never enable PE, select erasure, or grant consent.
public static class SettingsSearchPrerequisites
{
    public static bool HasExplicitTarget(string target) => target is
        "PeRecoveryPartitionCheck" or "SettingAnchor066" or "SystemPartitionSizeEditor" or
        "PauseBeforeFormattingCheck" or "SettingAnchor067" or "ConfirmWipeDiskCheck";

    public static string? HintKey(string requested, string resolved) => requested == resolved ? null : resolved switch
    {
        "GeneratedPeMode" => "v133.search.enablePe",
        "SettingAnchor061" => "v133.search.diskMode",
        "PeRecoveryPartitionCheck" => "v133.search.enableRecovery",
        _ => null
    };

    public static string Resolve(string target, InstallPlan plan)
    {
        if (target == "PeRecoveryPartitionCheck")
        {
            if (!plan.UseGeneratedPe) return "GeneratedPeMode";
            if (plan.DiskMode != InstallationSettings.Wipe) return "SettingAnchor061";
        }
        if (target is "SettingAnchor066" or "SystemPartitionSizeEditor" or "PauseBeforeFormattingCheck" or "SettingAnchor067" or "ConfirmWipeDiskCheck")
        {
            if (plan.DiskMode != InstallationSettings.Wipe) return "SettingAnchor061";
            if (target == "SettingAnchor067" && plan.UseGeneratedPe && !plan.PeCreateRecoveryPartition)
                return "PeRecoveryPartitionCheck";
        }
        return target;
    }
}
