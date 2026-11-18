using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;

namespace UnattendLocal.Desktop;

public partial class MainWindow
{
    private readonly DispatcherTimer sidebarRevealTimer = new() { Interval = TimeSpan.FromMilliseconds(200) };
    private readonly DispatcherTimer sidebarHideTimer = new() { Interval = TimeSpan.FromMilliseconds(350) };
    private Border sidebarPane = null!;
    private Button sidebarRevealEdge = null!;
    private Button sidebarToggle = null!;
    private ContentPresenter sidebarContent = null!;

    private void InitializeSidebar()
    {
        Pages.ApplyTemplate();
        sidebarPane = (Border)Pages.Template.FindName("NavigationPane", Pages);
        sidebarRevealEdge = (Button)Pages.Template.FindName("SidebarRevealEdge", Pages);
        sidebarToggle = (Button)Pages.Template.FindName("SidebarToggleButton", Pages);
        sidebarContent = (ContentPresenter)Pages.Template.FindName("PART_SelectedContentHost", Pages);

        sidebarToggle.Click += SidebarToggleClick;
        sidebarRevealEdge.Click += SidebarRevealClick;
        sidebarRevealEdge.MouseEnter += SidebarEdgeEntered;
        sidebarRevealEdge.MouseLeave += (_, _) => sidebarRevealTimer.Stop();
        sidebarPane.MouseEnter += (_, _) => sidebarHideTimer.Stop();
        sidebarPane.MouseLeave += SidebarPaneLeft;
        sidebarRevealTimer.Tick += RevealSidebarTick;
        sidebarHideTimer.Tick += HideSidebarTick;
        Deactivated += (_, _) =>
        {
            StopSidebarTimers();
            vm.HideSidebarPreview();
        };
        Closed += (_, _) => StopSidebarTimers();
    }

    private void SidebarToggleClick(object sender, RoutedEventArgs e)
    {
        StopSidebarTimers();
        if (vm.IsSidebarPinned) FocusSidebarContent();
        vm.ToggleSidebarPin();
    }

    private void SidebarRevealClick(object sender, RoutedEventArgs e)
    {
        StopSidebarTimers();
        vm.ShowSidebarPreview();
        sidebarToggle.Focus();
    }

    private void SidebarEdgeEntered(object sender, MouseEventArgs e)
    {
        sidebarHideTimer.Stop();
        if (!vm.IsSidebarPinned) sidebarRevealTimer.Start();
    }

    private void SidebarPaneLeft(object sender, MouseEventArgs e)
    {
        if (!vm.IsSidebarPinned && vm.IsSidebarVisible) sidebarHideTimer.Start();
    }

    private void RevealSidebarTick(object? sender, EventArgs e)
    {
        sidebarRevealTimer.Stop();
        if (IsActive && sidebarRevealEdge.IsMouseOver && !vm.IsSidebarPinned)
            vm.ShowSidebarPreview();
    }

    private void HideSidebarTick(object? sender, EventArgs e)
    {
        sidebarHideTimer.Stop();
        if (!vm.IsSidebarPinned && !sidebarPane.IsMouseOver)
        {
            FocusSidebarContent();
            vm.HideSidebarPreview();
        }
    }

    private void FocusSidebarContent()
    {
        if (sidebarPane.IsKeyboardFocusWithin)
            sidebarContent.MoveFocus(new TraversalRequest(FocusNavigationDirection.First));
    }

    private void StopSidebarTimers()
    {
        sidebarRevealTimer.Stop();
        sidebarHideTimer.Stop();
    }
}
