using UnattendLocal.Core;
using UnattendLocal.Desktop;
using UnattendLocal.Localization;

static class SidebarNavigationTests
{
    public static int Run()
    {
        int count = 0;
        void Check(bool condition, string message)
        {
            if (!condition) throw new Exception("Sidebar navigation: " + message);
            count++;
        }

        var vm = new MainViewModel();
        var script = new ScriptEntry { Content = "Write-Output 'keep this draft';" };
        vm.Plan.Scripts.Add(script);
        vm.SelectedScript = script;
        vm.Plan.ConfirmWipeDisk = true;
        vm.Preview = "keep this preview";
        var plan = vm.Plan;
        string original = PlanStore.Serialize(plan, includeSecrets: true);
        Check(vm.IsSidebarPinned && vm.IsSidebarVisible, "navigation initially occupies a fixed sidebar");
        Check(vm.SidebarToggleText == UiText.Source("navigation.hideSidebar"), "fixed sidebar offers automatic hiding");

        vm.ToggleSidebarPin();
        Check(!vm.IsSidebarPinned && !vm.IsSidebarVisible, "unpinning immediately hides the sidebar");
        vm.ShowSidebarPreview();
        Check(!vm.IsSidebarPinned && vm.IsSidebarVisible, "edge reveal opens a temporary sidebar");
        Check(vm.SidebarToggleText == UiText.Source("navigation.pinSidebar"), "the temporary sidebar offers pinning");
        vm.ShowSidebarPreview();
        Check(!vm.IsSidebarPinned && vm.IsSidebarVisible, "repeated edge entry retains the temporary state");
        vm.HideSidebarPreview();
        Check(!vm.IsSidebarPinned && !vm.IsSidebarVisible, "leaving a temporary sidebar hides it");
        vm.HideSidebarPreview();
        Check(!vm.IsSidebarPinned && !vm.IsSidebarVisible, "repeated leave events retain the hidden state");

        vm.ShowSidebarPreview();
        vm.ToggleSidebarPin();
        Check(vm.IsSidebarPinned && vm.IsSidebarVisible, "pinning a temporary sidebar keeps it open");
        vm.HideSidebarPreview();
        Check(vm.IsSidebarPinned && vm.IsSidebarVisible, "a queued preview dismissal cannot hide a pinned sidebar");
        vm.ShowSidebarPreview();
        vm.ToggleSidebarPin();
        Check(!vm.IsSidebarPinned && !vm.IsSidebarVisible, "unpinning clears any earlier preview state");
        Check(ReferenceEquals(vm.Plan, plan) && vm.SelectedScript == script &&
              vm.Preview == "keep this preview" && plan.ConfirmWipeDisk &&
              PlanStore.Serialize(plan, includeSecrets: true) == original,
              "sidebar interactions preserve the plan, selected draft, preview, and disk consent");
        return count;
    }
}
