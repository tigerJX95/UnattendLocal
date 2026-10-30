using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
namespace UnattendLocal.Desktop;

public partial class MainWindow
{
    private int searchNavigationGeneration;
    private string lastSettingsQuery = "";
    private int? lastSettingsCategory;
    private AdornerLayer? searchHighlightLayer;
    private SearchHighlight? searchHighlight;
    private DispatcherTimer? searchHighlightTimer;
    private void InitializeSettingsSearch()
    {
        PreviewKeyDown += (_, e) => { if (e.Key == Key.F && Keyboard.Modifiers == ModifierKeys.Control) { e.Handled = true; OpenSettingsSearch(); } };
        Pages.SelectionChanged += (_, e) => { if (ReferenceEquals(e.Source, Pages)) { searchNavigationGeneration++; ClearSearchHighlight(); } };
        Closed += (_, _) => { searchNavigationGeneration++; ClearSearchHighlight(); };
    }
    private void CompactOsSettingsClick(object sender, RoutedEventArgs e) => NavigateToSetting(
        SettingsSearchCatalog.All.Single(item => item.Target == "SettingAnchor059"), focusEditor: true);
    private void PageFileSettingsClick(object sender, RoutedEventArgs e) => NavigateToSetting(
        SettingsSearchCatalog.All.Single(item => item.Target == "CustomPageFileEditor"), focusEditor: true);
    private void SettingsSearchClick(object sender, RoutedEventArgs e) => OpenSettingsSearch();
    private void OpenSettingsSearch()
    {
        searchNavigationGeneration++; ClearSearchHighlight();
        var dialog = new SettingsSearchWindow(lastSettingsQuery, lastSettingsCategory) { Owner = this };
        bool? accepted = dialog.ShowDialog(); lastSettingsQuery = dialog.Query; lastSettingsCategory = dialog.PageIndex;
        if (accepted == true && dialog.Selected is {} selected) NavigateToSetting(selected);
    }
    private void NavigateToSetting(SettingSearchResult result, bool focusEditor = false)
    {
        ClearSearchHighlight();
        if (result.AppId.Length > 0) vm.AppRemovalFilter = result.AppId;
        Pages.SelectedIndex = result.PageIndex;
        int ticket = ++searchNavigationGeneration;
        Dispatcher.BeginInvoke(DispatcherPriority.Loaded, new Action(() =>
        {
            if (ticket != searchNavigationGeneration || Pages.SelectedIndex != result.PageIndex) return;
            UpdateLayout();
            FrameworkElement? target = FindName(result.Target) as FrameworkElement;
            if (result.AppId.Length > 0)
            {
                var choice = vm.AppRemovalChoices.FirstOrDefault(i => i.Id == result.AppId);
                target = AppRemovalItems.ItemContainerGenerator.ContainerFromItem(choice) as FrameworkElement ?? target;
            }
            // A protocol tab is presentation state only; search selects it without
            // enabling either protocol or changing any entered network values.
            if (result.Target.StartsWith("StaticIPv6",StringComparison.Ordinal)) IPProtocolTabs.SelectedItem=IPv6ProtocolTab;
            else if(result.Target.StartsWith("StaticIPv4",StringComparison.Ordinal)&&result.Target!="StaticIPv4MacEditor"&&result.Target!="StaticIPv4Section")IPProtocolTabs.SelectedItem=IPv4ProtocolTab;
            UpdateLayout();
            bool hidden = target?.IsVisible == false;
            // Locate the visible containing section when a mode hides a conditional editor.
            FrameworkElement? hiddenBranch = null;
            while (target != null && !target.IsVisible) { hiddenBranch = target; target = target.Parent as FrameworkElement ?? VisualTreeHelper.GetParent(target) as FrameworkElement; }
            if (hiddenBranch != null && target is Panel panel)
            {
                int position = panel.Children.IndexOf(hiddenBranch);
                var previous = panel.Children.OfType<FrameworkElement>().Take(Math.Max(0, position)).LastOrDefault(c => c.IsVisible && c is Control);
                if (previous != null) target = previous;
            }
            if (target == null) { vm.Status = global::UnattendLocal.Localization.UiText.Source("text.324b12466f2a"); return; }
            target.BringIntoView();
            if (focusEditor && target.IsVisible && target.IsEnabled && target.Focusable) {
                target.Focus(); Keyboard.Focus(target);
                if (target is TextBox editor) editor.SelectAll();
            }
            vm.Status = global::UnattendLocal.Localization.UiText.Concat(new object?[] { global::UnattendLocal.Localization.UiText.Format("message.0b48e5b9a48d", new string[] { global::UnattendLocal.Localization.UiText.Arg((result.Page), 0, null), global::UnattendLocal.Localization.UiText.Arg((result.Title), 0, null) }), (hidden ? global::UnattendLocal.Localization.UiText.Source("text.04014bdbee98") : !target.IsEnabled ? global::UnattendLocal.Localization.UiText.Source("text.bbaca395d5c1") : "") },new bool[]{false,false});
            var anchor = target;
            Dispatcher.BeginInvoke(DispatcherPriority.ContextIdle, new Action(() => { if (ticket == searchNavigationGeneration && Pages.SelectedIndex == result.PageIndex && anchor.IsVisible) HighlightSearchTarget(anchor); }));
        }));
    }
    private void ClearSearchHighlight()
    {
        searchHighlightTimer?.Stop(); searchHighlightTimer = null;
        if (searchHighlight != null) searchHighlightLayer?.Remove(searchHighlight);
        searchHighlight = null; searchHighlightLayer = null;
    }
    private void HighlightSearchTarget(FrameworkElement target)
    {
        ClearSearchHighlight(); searchHighlightLayer = AdornerLayer.GetAdornerLayer(target);
        if (searchHighlightLayer == null) return;
        searchHighlight = new SearchHighlight(target, (Brush)FindResource("AccentTextBrush")); searchHighlightLayer.Add(searchHighlight);
        searchHighlightTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(4) };
        searchHighlightTimer.Tick += (_, _) => ClearSearchHighlight(); searchHighlightTimer.Start();
    }
    private sealed class SearchHighlight : Adorner
    {
        private readonly Brush brush;
        public SearchHighlight(UIElement target, Brush brush) : base(target) { this.brush = brush; IsHitTestVisible = false; }
        protected override void OnRender(DrawingContext drawingContext) => drawingContext.DrawRoundedRectangle(null, new Pen(brush, 3), new Rect(new Point(), AdornedElement.RenderSize), 4, 4);
    }
}
