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
    private string? logPath;

    public SetupLogWindow()
    {
        InitializeComponent();
        SourceInitialized += (_, _) => UiTheme.UpdateTitleBar(this);
        Loaded += (_, _) => OpenButton.Focus();
    }

    private void CloseExecuted(object sender, ExecutedRoutedEventArgs e) => Close();

    private async void OpenClick(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog
        {
            Title = UiText.Get("setupLogs.choose"),
            Filter = UiText.Get("setupLogs.title") + "|" + SetupLogPreview.FilePatterns,
            CheckFileExists = true
        };
        if (dialog.ShowLocalized(this) == true)
            await ReadLogAsync(dialog.FileName, clearFilter: true);
    }

    private async void ReloadClick(object sender, RoutedEventArgs e)
    {
        if (logPath is { } path)
            await ReadLogAsync(path, clearFilter: false);
    }

    private async Task ReadLogAsync(string path, bool clearFilter)
    {
        OpenButton.IsEnabled = ReloadButton.IsEnabled = false;
        try
        {
            var loaded = await Task.Run(() => SetupLogPreview.Read(path));
            if (!IsVisible) return;

            if (clearFilter) LogFilter.Clear();
            preview = loaded;
            logPath = path;
            LogName.Text = Path.GetFileName(path);
            LogName.Visibility = Visibility.Visible;
            LogFilter.IsEnabled = true;
            TruncatedNotice.Visibility = loaded.Truncated ? Visibility.Visible : Visibility.Collapsed;
            RefreshPreview();
        }
        catch (Exception error) when (error is ArgumentException or IOException or UnauthorizedAccessException)
        {
            if (IsVisible)
                ErrorDialog.Show(this, error, UiText.Source("setupLogs.failed"));
        }
        finally
        {
            OpenButton.IsEnabled = true;
            ReloadButton.IsEnabled = logPath != null;
        }
    }

    private void FilterChanged(object sender, TextChangedEventArgs e) => RefreshPreview();

    private void RefreshPreview()
    {
        if (preview is null) return;
        LogText.Text = preview.Filter(LogFilter.Text, out int? matchingLines);
        LogStatus.Text = matchingLines switch
        {
            0 => UiText.Get("setupLogs.noMatches"),
            int count => UiText.Translate(UiText.Format("setupLogs.matches", UiText.Arg(count))),
            _ => LogText.Text.Length == 0 ? UiText.Get("setupLogs.empty") : ""
        };
        LogText.ScrollToHome();
    }
}
