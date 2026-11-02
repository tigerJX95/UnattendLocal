using System.Xml.Linq;
using UnattendLocal.Core;
using UnattendLocal.Desktop;

static class SearchPrerequisiteTests
{
    public static int Run()
    {
        int count = 0;
        void Check(bool ok, string message) { if (!ok) throw new Exception("Search prerequisite: " + message); count++; }
        var doc = XDocument.Load(Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../Desktop/MainWindow.xaml")));
        XNamespace x = "http://schemas.microsoft.com/winfx/2006/xaml";
        XElement Element(string name) => doc.Descendants().Single(e => (string?)e.Attribute(x + "Name") == name);
        bool Visible(string name, MainViewModel vm) => Element(name).AncestorsAndSelf().All(e => ((string?)e.Attribute("Visibility")) switch
        {
            "{Binding IsWipeDisk,Converter={StaticResource VisibleWhen}}" => vm.IsWipeDisk,
            "{Binding Plan.UseGeneratedPe,Converter={StaticResource VisibleWhen}}" => vm.Plan.UseGeneratedPe,
            "{Binding ShowRecoverySize,Converter={StaticResource VisibleWhen}}" => vm.ShowRecoverySize,
            _ => true
        });
        foreach (bool pe in new[] { false, true })
        foreach (string mode in new[] { InstallationSettings.Manual, InstallationSettings.Wipe, InstallationSettings.Existing, DiskScriptSettings.Mode })
        foreach (bool recovery in new[] { false, true })
        {
            var p = new InstallPlan { UseGeneratedPe = pe, DiskMode = mode, PeCreateRecoveryPartition = recovery };
            var vm = new MainViewModel { Plan = p };
            foreach (string field in new[] { "PeRecoveryPartitionCheck", "SettingAnchor067" })
            {
                string before = PlanStore.Serialize(p, true);
                string resolved = SettingsSearchPrerequisites.Resolve(field, p);
                string expected = field == "PeRecoveryPartitionCheck"
                    ? !pe ? "GeneratedPeMode" : mode != InstallationSettings.Wipe ? "SettingAnchor061" : field
                    : mode != InstallationSettings.Wipe ? "SettingAnchor061" : pe && !recovery ? "PeRecoveryPartitionCheck" : field;
                Check(SettingsSearchPrerequisites.HasExplicitTarget(field), "recovery navigation must never use a nearby control");
                Check(SettingsSearchPrerequisites.HintKey(field, resolved) == (resolved == field ? null : resolved == "GeneratedPeMode" ? "v133.search.enablePe" : resolved == "SettingAnchor061" ? "v133.search.diskMode" : "v133.search.enableRecovery"), "status describes the actual prerequisite");
                Check(resolved == expected && Visible(resolved, vm), $"visible prerequisite for {field}, PE={pe}, mode={mode}, recovery={recovery}");
                Check(resolved == field || !Visible(field, vm), "visible editor remains the destination");
                Check(before == PlanStore.Serialize(p, true), "navigation does not change installation choices or consent");
            }
        }
        foreach (string target in new[] { "SettingAnchor066", "SystemPartitionSizeEditor", "PauseBeforeFormattingCheck", "ConfirmWipeDiskCheck" })
        {
            Check(SettingsSearchPrerequisites.Resolve(target, new InstallPlan()) == "SettingAnchor061", "hidden automatic-partition editor resolves to disk mode");
            Check(SettingsSearchPrerequisites.Resolve(target, new InstallPlan { DiskMode = InstallationSettings.Wipe }) == target, "visible automatic-partition editor retained");
        }
        Check(SettingsSearchPrerequisites.Resolve("PeImageIntegrityCheck", new InstallPlan()) == "PeImageIntegrityCheck", "unrelated hidden-editor behavior retained");
        return count;
    }
}
