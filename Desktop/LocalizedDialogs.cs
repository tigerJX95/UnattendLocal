using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Microsoft.Win32;
using UnattendLocal.Localization;

namespace UnattendLocal.Desktop;

internal static class LocalizedMessageBox
{
    public static MessageBoxResult Show(Window owner, string text, string caption, MessageBoxButton buttons = MessageBoxButton.OK, MessageBoxImage image = MessageBoxImage.None, MessageBoxResult defaultResult = MessageBoxResult.None)
    {
        var choices = buttons switch
        {
            MessageBoxButton.YesNo => new[] { MessageBoxResult.Yes, MessageBoxResult.No },
            MessageBoxButton.YesNoCancel => new[] { MessageBoxResult.Yes, MessageBoxResult.No, MessageBoxResult.Cancel },
            MessageBoxButton.OKCancel => new[] { MessageBoxResult.OK, MessageBoxResult.Cancel },
            _ => new[] { MessageBoxResult.OK }
        };
        var cancel = choices.Contains(MessageBoxResult.Cancel) ? MessageBoxResult.Cancel : choices.Contains(MessageBoxResult.No) ? MessageBoxResult.No : MessageBoxResult.OK;
        if (!choices.Contains(defaultResult)) defaultResult = cancel;
        var result = cancel;
        var window = new Window { Owner = owner, Title = UiText.Translate(caption), Width = 640, MinWidth = 380, SizeToContent = SizeToContent.Height, MaxHeight = Math.Max(250, Math.Min(760, SystemParameters.WorkArea.Height - 64)), WindowStartupLocation = WindowStartupLocation.CenterOwner, ShowInTaskbar = false, FontFamily = owner.FontFamily, FontSize = 14 };
        window.SetResourceReference(Control.BackgroundProperty, "SurfaceBrush"); window.SetResourceReference(Control.ForegroundProperty, "ForegroundBrush");
        var panel = new StackPanel { Margin = new Thickness(24) };
        var message = new TextBlock { Text = UiText.Translate(text), TextWrapping = TextWrapping.Wrap, LineHeight = 24 };
        if (image == MessageBoxImage.Warning || image == MessageBoxImage.Error) message.SetResourceReference(TextBlock.ForegroundProperty, "DangerBrush");
        panel.Children.Add(new ScrollViewer { Content = message, VerticalScrollBarVisibility = ScrollBarVisibility.Auto, MaxHeight = Math.Max(150, window.MaxHeight - 160) });
        var bar = new WrapPanel { HorizontalAlignment = HorizontalAlignment.Right, Margin = new Thickness(0, 20, 0, 0) };
        foreach (var choice in choices)
        {
            string key = choice switch { MessageBoxResult.Yes => "text.b5141d3d19e9", MessageBoxResult.No => "text.0c70665b6eb6", MessageBoxResult.Cancel => "text.2cd0f3be8738", _ => "text.fac2a67ad878" };
            var button = new Button { Content = UiText.Get(key), ContentTemplate = null, Width = double.NaN, Height = double.NaN, MinWidth = 94, Padding = new Thickness(16, 8, 16, 8), IsDefault = choice == defaultResult, IsCancel = choice == cancel };
            button.Click += (_, _) => { result = choice; window.DialogResult = true; }; bar.Children.Add(button);
        }
        panel.Children.Add(bar); window.Content = panel;
        window.SourceInitialized += (_, _) => UiTheme.UpdateTitleBar(window);
        window.ShowDialog(); return result;
    }
}
internal static class LocalizedFileDialog
{
    public static bool? ShowLocalized(this FileDialog dialog, Window owner)
    {
        dialog.Title = UiText.Translate(dialog.Title); dialog.Filter = UiText.Translate(dialog.Filter);
        return dialog.ShowDialog(owner);
    }
}
public static class LocalizedItems
{
    public static readonly DependencyProperty DisplayPathProperty = DependencyProperty.RegisterAttached("DisplayPath", typeof(string), typeof(LocalizedItems), new PropertyMetadata(""));
    public static string GetDisplayPath(DependencyObject item) => (string)item.GetValue(DisplayPathProperty);
    public static void SetDisplayPath(DependencyObject item, string path) => item.SetValue(DisplayPathProperty, path);
}
