using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Microsoft.Win32;
using UnattendLocal.Core;
using UnattendLocal.Localization;

namespace UnattendLocal.Desktop;

public partial class SetupLogWindow : Window
{
    private SetupLogPreview? preview;

    public SetupLogWindow()
    {
        InitializeComponent();
        SourceInitialized += (_, _) => UiTheme.UpdateTitleBar(this);
        Loaded += (_, _) => OpenButton.Focus();
    }

    private void CloseExecuted(object sender, ExecutedRoutedEventArgs e) => Close();

    private void OpenClick(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog
        {
            Title = UiText.Get("setupLogs.choose"),
            Filter = UiText.Get("setupLogs.fileFilter"),
            CheckFileExists = true
        };
        if (dialog.ShowLocalized(this) != true) return;

        try
        {
            preview = SetupLogPreview.Read(dialog.FileName);
            LogName.Text = Path.GetFileName(dialog.FileName);
            LogFilter.Clear();
            LogFilter.IsEnabled = true;
            TruncatedNotice.Visibility = preview.Truncated ? Visibility.Visible : Visibility.Collapsed;
            RefreshPreview();
        }
        catch (Exception error) when (error is ArgumentException or IOException or UnauthorizedAccessException)
        {
            ErrorDialog.Show(this, error, UiText.Source("setupLogs.failed"));
        }
    }

    private void FilterChanged(object sender, TextChangedEventArgs e) => RefreshPreview();

    private void RefreshPreview()
    {
        if (preview is null) return;
        LogText.Text = preview.Filter(LogFilter.Text);
        LogStatus.Text = LogText.Text.Length > 0 ? "" :
            UiText.Get(string.IsNullOrWhiteSpace(LogFilter.Text) ? "setupLogs.empty" : "setupLogs.noMatches");
        LogText.ScrollToHome();
    }
}
