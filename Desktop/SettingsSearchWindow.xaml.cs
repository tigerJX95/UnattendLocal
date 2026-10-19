using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
namespace UnattendLocal.Desktop;

public partial class SettingsSearchWindow : Window
{
    private readonly SettingsSearchSession session = new();
    private readonly DispatcherTimer refreshTimer = new() { Interval = TimeSpan.FromMilliseconds(120) };
    private bool ready, composing;
    public SettingSearchResult? Selected { get; private set; }
    public string Query => QueryBox.Text;
    public int? PageIndex => (CategoryPicker.SelectedItem as SettingsSearchCatalog.Category)?.PageIndex;

    public SettingsSearchWindow(string query, int? pageIndex = null)
    {
        InitializeComponent();
        CategoryPicker.ItemsSource = SettingsSearchCatalog.Categories;
        CategoryPicker.SelectedItem = SettingsSearchCatalog.Categories.First(c => c.PageIndex == SettingsSearchCatalog.NormalizeCategory(pageIndex));
        QueryBox.Text = query;
        refreshTimer.Tick += RefreshTick;
        TextCompositionManager.AddPreviewTextInputStartHandler(QueryBox, CompositionStarted);
        TextCompositionManager.AddPreviewTextInputHandler(QueryBox, CompositionCompleted);
        ready = true;
        RequestRefresh(immediate: true);
        SourceInitialized += (_, _) => UiTheme.UpdateTitleBar(this);
        Loaded += (_, _) => FocusQuery();
        Closed += (_, _) =>
        {
            ready = false;
            refreshTimer.Stop(); refreshTimer.Tick -= RefreshTick;
            TextCompositionManager.RemovePreviewTextInputStartHandler(QueryBox, CompositionStarted);
            TextCompositionManager.RemovePreviewTextInputHandler(QueryBox, CompositionCompleted);
        };
    }

    private void FocusQuery() { QueryBox.Focus(); QueryBox.SelectAll(); }
    private void QueryChanged(object sender, TextChangedEventArgs e) => RequestRefresh();
    private void CategoryChanged(object sender, SelectionChangedEventArgs e)
    {
        if (ReferenceEquals(e.Source, CategoryPicker)) RequestRefresh(immediate: true);
    }
    private void RequestRefresh(bool immediate = false)
    {
        if (!ready) return;
        refreshTimer.Stop();
        session.Request(Query, PageIndex);
        Results.IsEnabled = false; LocateButton.IsEnabled = false;
        CountText.Text = composing ? "请完成输入法选词。" : "正在筛选设置…";
        if (composing) return;
        if (immediate) Refresh(); else refreshTimer.Start();
    }
    private void RefreshTick(object? sender, EventArgs e) => Refresh();
    private void Refresh()
    {
        refreshTimer.Stop();
        if (!ready || composing || !session.IsPending) return;
        session.Refresh();
        Results.ItemsSource = session.Results;
        Results.IsEnabled = true;
        Results.SelectedIndex = session.Results.Count > 0 ? 0 : -1;
        session.Select(Results.SelectedItem as SettingSearchResult);
        LocateButton.IsEnabled = session.Selected != null;
        string category = ((SettingsSearchCatalog.Category)CategoryPicker.SelectedItem).Label;
        CountText.Text = session.Results.Count == 0
            ? (PageIndex.HasValue ? $"“{category}”中没有匹配设置。可切换为全部分类，或更换关键词。" : "未找到匹配设置，请换用简短名称或中英文关键词。")
            : $"{category} · 共 {session.Results.Count} 项；支持中英文名称、别名与多个关键词。";
    }
    private void CompositionStarted(object sender, TextCompositionEventArgs e) { composing = true; RequestRefresh(); }
    private void CompositionCompleted(object sender, TextCompositionEventArgs e)
    {
        if (!composing) return;
        composing = false;
        // TextInput is previewed before the committed text reaches TextBox.
        Dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(() => RequestRefresh()));
    }
    private void ResetClick(object sender, RoutedEventArgs e)
    {
        composing = false;
        QueryBox.Clear(); CategoryPicker.SelectedIndex = 0;
        RequestRefresh(immediate: true); FocusQuery();
    }
    private void ResultSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (!ready || !ReferenceEquals(e.Source, Results)) return;
        session.Select(Results.SelectedItem as SettingSearchResult);
        LocateButton.IsEnabled = session.Selected != null;
    }
    private void LocateClick(object sender, RoutedEventArgs e) => Locate();
    private void Locate()
    {
        if (composing) return;
        Refresh(); // Enter immediately after typing must use the latest query.
        if (session.Selected is {} result) { Selected = result; DialogResult = true; }
    }
    private void ResultClicked(object sender, MouseButtonEventArgs e)
    {
        if (session.IsPending) return;
        // A scrollbar or empty-space click must not activate the previous selection.
        DependencyObject? node = e.OriginalSource as DependencyObject;
        while (node != null && node is not ListBoxItem) node = node is Visual ? VisualTreeHelper.GetParent(node) : LogicalTreeHelper.GetParent(node);
        if (node is ListBoxItem) Locate();
    }
    private void SearchKeyDown(object sender, KeyEventArgs e)
    {
        if (composing || e.Key == Key.ImeProcessed) return;
        if (e.Key == Key.L && Keyboard.Modifiers == ModifierKeys.Control) { FocusQuery(); e.Handled = true; }
        else if (e.Key == Key.Enter && (QueryBox.IsKeyboardFocusWithin || Results.IsKeyboardFocusWithin || LocateButton.IsKeyboardFocusWithin)) { Locate(); e.Handled = true; }
        else if ((e.Key == Key.Down || e.Key == Key.Up) && QueryBox.IsKeyboardFocusWithin)
        {
            if (session.IsPending) Refresh();
            if (Results.Items.Count > 0)
            {
                Results.SelectedIndex = Math.Clamp(Results.SelectedIndex + (e.Key == Key.Down ? 1 : -1), 0, Results.Items.Count - 1);
                Results.ScrollIntoView(Results.SelectedItem);
            }
            e.Handled = true;
        }
    }
}
