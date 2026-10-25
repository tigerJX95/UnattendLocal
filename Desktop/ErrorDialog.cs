using System.Windows;
using System.Windows.Controls;

namespace UnattendLocal.Desktop;

// One themed path for operational errors; confirmation and risk dialogs keep their own semantics.
internal static class ErrorDialog
{
    public static bool Show(Window owner, string message, string? title = null, string? location = null, string? actionText = null)
    {
        title ??= UnattendLocal.Localization.UiText.Source("text.47b3e2cd0b17");
        actionText ??= UnattendLocal.Localization.UiText.Source("text.90e01acb5c9e");
        bool navigate = false;
        var dialog = new Window {
            Owner = owner, Title = global::UnattendLocal.Localization.UiText.Translate(title), Width = 620, MinWidth = 360, MinHeight = 180,
            MaxHeight = 640, SizeToContent = SizeToContent.Height, WindowStartupLocation = WindowStartupLocation.CenterOwner,
            ShowInTaskbar = false, FontFamily = owner.FontFamily, FontSize = 14
        };
        dialog.SetResourceReference(Control.BackgroundProperty, "SurfaceBrush");
        dialog.SetResourceReference(Control.ForegroundProperty, "ForegroundBrush");
        var layout = new Grid { Margin = new Thickness(24) };
        layout.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        layout.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        var content = new TextBlock { Text = global::UnattendLocal.Localization.UiText.Translate(message), TextWrapping = TextWrapping.Wrap, LineHeight = 23, FontWeight = FontWeights.SemiBold };
        content.SetResourceReference(TextBlock.ForegroundProperty, "ErrorBrush");
        var text = new StackPanel(); text.Children.Add(content);
        if (location != null) {
            var position = new TextBlock { Text = global::UnattendLocal.Localization.UiText.Translate(global::UnattendLocal.Localization.UiText.Concat(new object?[] { global::UnattendLocal.Localization.UiText.Source("text.0871f0abbd53"), location },new bool[]{false,false})), TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 12, 0, 0) };
            position.SetResourceReference(TextBlock.ForegroundProperty, "ForegroundBrush"); text.Children.Add(position);
        }
        var scroll = new ScrollViewer { Content = text, VerticalScrollBarVisibility = ScrollBarVisibility.Auto, MaxHeight = 420, HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled };
        layout.Children.Add(scroll);
        var buttons = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right, Margin = new Thickness(0, 16, 0, 0) };
        if (location != null) {
            var locate = new Button { Content = actionText, ContentTemplate = null, MinWidth = 100 };
            locate.ToolTip = new ToolTip { ContentTemplate = null, Content = global::UnattendLocal.Localization.UiText.Concat(new object?[] { global::UnattendLocal.Localization.UiText.Source("text.d8da6d237352"), location },new bool[]{false,false}) };
            locate.Click += (_, _) => { navigate = true; dialog.DialogResult = true; }; buttons.Children.Add(locate);
        }
        var close = new Button { Content = global::UnattendLocal.Localization.UiText.Source("text.fac2a67ad878"), ContentTemplate = null, IsDefault = true, IsCancel = true, MinWidth = 80 };
        close.ToolTip = new ToolTip { ContentTemplate = null, Content = global::UnattendLocal.Localization.UiText.Source("text.a21f184fa54a") };
        close.Click += (_, _) => dialog.DialogResult = true;
        buttons.Children.Add(close); Grid.SetRow(buttons, 1); layout.Children.Add(buttons);
        dialog.Content = layout;
        dialog.SourceInitialized += (_, _) => UiTheme.UpdateTitleBar(dialog);
        dialog.Loaded += (_, _) => close.Focus();
        dialog.ShowDialog();
        return navigate;
    }
}
