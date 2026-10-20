using System.Windows;
using System.Windows.Controls;

namespace UnattendLocal.Desktop;

// One themed path for operational errors; confirmation and risk dialogs keep their own semantics.
internal static class ErrorDialog
{
    public static void Show(Window owner, string message, string title = "请检查配置")
    {
        var dialog = new Window {
            Owner = owner, Title = title, Width = 620, MinWidth = 360, MinHeight = 180,
            MaxHeight = 640, SizeToContent = SizeToContent.Height, WindowStartupLocation = WindowStartupLocation.CenterOwner,
            ShowInTaskbar = false, FontFamily = owner.FontFamily, FontSize = 14
        };
        dialog.SetResourceReference(Control.BackgroundProperty, "SurfaceBrush");
        dialog.SetResourceReference(Control.ForegroundProperty, "ForegroundBrush");
        var layout = new Grid { Margin = new Thickness(24) };
        layout.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        layout.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        var content = new TextBlock { Text = message, TextWrapping = TextWrapping.Wrap, LineHeight = 23, FontWeight = FontWeights.SemiBold };
        content.SetResourceReference(TextBlock.ForegroundProperty, "ErrorBrush");
        var scroll = new ScrollViewer { Content = content, VerticalScrollBarVisibility = ScrollBarVisibility.Auto, MaxHeight = 420, HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled };
        layout.Children.Add(scroll);
        var buttons = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right, Margin = new Thickness(0, 16, 0, 0) };
        var close = new Button { Content = "确定", IsDefault = true, IsCancel = true, MinWidth = 80 };
        close.Click += (_, _) => dialog.DialogResult = true;
        buttons.Children.Add(close); Grid.SetRow(buttons, 1); layout.Children.Add(buttons);
        dialog.Content = layout;
        dialog.SourceInitialized += (_, _) => UiTheme.UpdateTitleBar(dialog);
        dialog.Loaded += (_, _) => close.Focus();
        dialog.ShowDialog();
    }
}
