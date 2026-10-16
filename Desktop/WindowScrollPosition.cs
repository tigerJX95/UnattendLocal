using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Threading;
namespace UnattendLocal.Desktop;

// Keep page offsets across a minimize/restore cycle, without changing ordinary navigation.
internal sealed class WindowScrollPosition
{
    private readonly Window window;
    private readonly TabControl pages;
    private readonly Dictionary<ScrollViewer, Point> offsets = new();
    private HwndSource? source;
    private TabItem? suspendedPage;
    private bool suspended;
    private int generation;

    public WindowScrollPosition(Window window, TabControl pages)
    {
        this.window = window;
        this.pages = pages;
        foreach (var page in pages.Items.OfType<TabItem>())
            if (page.Content is ScrollViewer viewer)
            {
                offsets.Add(viewer, new Point());
                viewer.ScrollChanged += Remember;
                viewer.AddHandler(FrameworkElement.RequestBringIntoViewEvent, new RequestBringIntoViewEventHandler(BringIntoView), true);
            }
        window.SourceInitialized += SourceInitialized;
        window.StateChanged += StateChanged;
        window.PreviewMouseDown += UserInput;
        window.PreviewMouseWheel += UserInput;
        window.PreviewKeyDown += UserInput;
        window.PreviewTouchDown += UserInput;
        pages.SelectionChanged += PageChanged;
        window.Closed += Closed;
    }

    private void Remember(object sender, ScrollChangedEventArgs e)
    {
        // Ignore routed events from nested editors and transient minimized layout values.
        if (!suspended && window.WindowState != WindowState.Minimized && sender is ScrollViewer viewer &&
            ReferenceEquals(e.OriginalSource, viewer) && viewer.IsVisible && viewer.ViewportHeight > 0)
            offsets[viewer] = new Point(viewer.HorizontalOffset, viewer.VerticalOffset);
    }

    private void Suspend(bool captureCurrent)
    {
        if (suspended) return;
        suspendedPage = pages.SelectedItem as TabItem;
        if (captureCurrent && suspendedPage?.Content is ScrollViewer viewer && viewer.IsVisible && viewer.ViewportHeight > 0)
            offsets[viewer] = new Point(viewer.HorizontalOffset, viewer.VerticalOffset);
        suspended = true;
        generation++;
    }

    private void SourceInitialized(object? sender, EventArgs e)
    {
        source = HwndSource.FromHwnd(new WindowInteropHelper(window).Handle);
        source?.AddHook(WindowMessage);
    }

    private nint WindowMessage(nint hwnd, int message, nint wParam, nint lParam, ref bool handled)
    {
        const int WmSysCommand = 0x0112, ScMinimize = 0xF020;
        // Capture before native minimization can change the viewport or restore keyboard focus.
        if (message == WmSysCommand && (wParam.ToInt64() & 0xFFF0) == ScMinimize) Suspend(true);
        return 0;
    }

    private void StateChanged(object? sender, EventArgs e)
    {
        if (window.WindowState == WindowState.Minimized)
        {
            // Also covers taskbar/programmatic minimization, using the last visible offsets.
            Suspend(false);
            return;
        }
        if (!suspended) return;
        int ticket = ++generation;
        window.Dispatcher.BeginInvoke(DispatcherPriority.ContextIdle, new Action(() =>
        {
            if (ticket != generation || !suspended || window.WindowState == WindowState.Minimized) return;
            if (ReferenceEquals(pages.SelectedItem, suspendedPage) && suspendedPage?.Content is ScrollViewer viewer &&
                viewer.IsVisible && offsets.TryGetValue(viewer, out var position))
            {
                viewer.UpdateLayout();
                viewer.ScrollToHorizontalOffset(Math.Min(position.X, viewer.ScrollableWidth));
                viewer.ScrollToVerticalOffset(Math.Min(position.Y, viewer.ScrollableHeight));
            }
            // ScrollTo queues a layout command. Keep focus-triggered scrolling suppressed until it completes.
            window.Dispatcher.BeginInvoke(DispatcherPriority.ContextIdle, new Action(() =>
            {
                if (ticket == generation && window.WindowState != WindowState.Minimized) Resume();
            }));
        }));
    }

    private void BringIntoView(object sender, RequestBringIntoViewEventArgs e)
    {
        if (suspended) e.Handled = true;
    }

    private void UserInput(object? sender, InputEventArgs e)
    {
        if (suspended && window.WindowState != WindowState.Minimized) Resume();
    }

    private void PageChanged(object sender, SelectionChangedEventArgs e)
    {
        if (ReferenceEquals(e.OriginalSource, pages) && suspended && !ReferenceEquals(pages.SelectedItem, suspendedPage)) Resume();
    }

    private void Resume()
    {
        generation++;
        suspended = false;
        suspendedPage = null;
    }

    private void Closed(object? sender, EventArgs e)
    {
        Resume();
        source?.RemoveHook(WindowMessage);
        source = null;
        window.SourceInitialized -= SourceInitialized;
        window.StateChanged -= StateChanged;
        window.PreviewMouseDown -= UserInput;
        window.PreviewMouseWheel -= UserInput;
        window.PreviewKeyDown -= UserInput;
        window.PreviewTouchDown -= UserInput;
        pages.SelectionChanged -= PageChanged;
        window.Closed -= Closed;
        foreach (var viewer in offsets.Keys)
        {
            viewer.ScrollChanged -= Remember;
            viewer.RemoveHandler(FrameworkElement.RequestBringIntoViewEvent, new RequestBringIntoViewEventHandler(BringIntoView));
        }
        offsets.Clear();
    }
}
