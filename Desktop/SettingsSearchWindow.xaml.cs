using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
namespace UnattendLocal.Desktop;

public partial class SettingsSearchWindow : Window
{
    public SettingSearchResult? Selected { get; private set; }
    public string Query => QueryBox.Text;
    public SettingsSearchWindow(string query)
    {
        InitializeComponent();
        QueryBox.Text = query;
        Refresh();
        SourceInitialized += (_, _) => UiTheme.UpdateTitleBar(this);
        Loaded += (_, _) => { QueryBox.Focus(); QueryBox.SelectAll(); };
    }
    private void QueryChanged(object sender, TextChangedEventArgs e) { if (Results != null) Refresh(); }
    private void Refresh()
    {
        var found = SettingsSearchCatalog.Search(QueryBox.Text);
        Results.ItemsSource = found;
        Results.SelectedIndex = found.Count > 0 ? 0 : -1;
        CountText.Text = found.Count == 0 ? "未找到匹配设置，请换用简短名称或中英文关键词。" : $"共 {found.Count} 项；支持中英文名称、别名与多个关键词。";
    }
    private void LocateClick(object sender, RoutedEventArgs e) => Locate();
    private void Locate() { if (Results.SelectedItem is SettingSearchResult result) { Selected = result; DialogResult = true; } }
    private void ResultClicked(object sender, MouseButtonEventArgs e)
    {
        // Clicking the scroll bar or empty space must not activate the previous selection.
        DependencyObject? node = e.OriginalSource as DependencyObject;
        while (node != null && node is not ListBoxItem) node = node is Visual ? VisualTreeHelper.GetParent(node) : LogicalTreeHelper.GetParent(node);
        if (node is ListBoxItem) Locate();
    }
    private void SearchKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter) { Locate(); e.Handled = true; }
        else if ((e.Key == Key.Down || e.Key == Key.Up) && QueryBox.IsKeyboardFocusWithin && Results.Items.Count > 0)
        {
            Results.SelectedIndex = Math.Clamp(Results.SelectedIndex + (e.Key == Key.Down ? 1 : -1), 0, Results.Items.Count - 1);
            Results.ScrollIntoView(Results.SelectedItem); e.Handled = true;
        }
    }
}
