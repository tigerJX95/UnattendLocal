using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Xml;
using Microsoft.Win32;
using UnattendLocal.Core;
using UnattendLocal.Localization;

namespace UnattendLocal.Desktop;

public enum DeploymentReviewKind { ConfigurationSet, AppLockerEvents }

public partial class DeploymentReviewWindow : Window
{
    private readonly DeploymentReviewKind kind;
    private CancellationTokenSource? pending;
    private bool closed;

    public DeploymentReviewWindow(DeploymentReviewKind kind)
    {
        this.kind = kind;
        InitializeComponent();
        string prefix = kind == DeploymentReviewKind.ConfigurationSet ? "oemReview" : "eventReview";
        Title = UiText.Get(prefix + ".title");
        OpenButton.Content = UiText.Get(prefix + ".choose");
        ScopeText.Text = UiText.Get(prefix + ".hint");
        SourceInitialized += (_, _) => UiTheme.UpdateTitleBar(this);
        Loaded += (_, _) => OpenButton.Focus();
        Closed += (_, _) => { closed = true; pending?.Cancel(); };
    }

    private async void OpenClick(object sender, RoutedEventArgs e)
    {
        string path;
        if (kind == DeploymentReviewKind.ConfigurationSet)
        {
            var dialog = new OpenFolderDialog { Title = UiText.Get("oemReview.choose"), Multiselect = false };
            if (dialog.ShowDialog(this) != true) return;
            path = dialog.FolderName;
        }
        else
        {
            var dialog = new OpenFileDialog
            {
                Title = UiText.Get("eventReview.choose"), Filter = UiText.Get("eventReview.fileFilter"), CheckFileExists = true
            };
            if (dialog.ShowLocalized(this) != true) return;
            path = dialog.FileName;
        }

        using var cancellation = new CancellationTokenSource();
        pending = cancellation;
        OpenButton.IsEnabled = false;
        StatusText.Text = UiText.Get("deploymentReview.loading");
        try
        {
            // Only filesystem/XML analysis runs in the worker; translate on the UI thread.
            object report = await Task.Run<object>(() => kind == DeploymentReviewKind.ConfigurationSet
                ? ConfigurationSetReview.Read(path, cancellation.Token)
                : AppLockerEventReview.Read(path, cancellation.Token), cancellation.Token);
            if (closed) return;
            string text = report is ConfigurationSetReview configuration ? configuration.Describe() : ((AppLockerEventReview)report).Describe();
            ReportText.Text = UiText.Translate(text);
            SourceName.Text = Path.GetFileName(Path.TrimEndingDirectorySeparator(path));
            StatusText.Text = UiText.Get("deploymentReview.complete");
            ReportText.ScrollToHome();
        }
        catch (OperationCanceledException) when (cancellation.IsCancellationRequested) { }
        catch (Exception error) when (error is ArgumentException or IOException or UnauthorizedAccessException or XmlException or NotSupportedException)
        {
            if (!closed)
            {
                // Clear the previous report so it cannot be mistaken for this selection.
                ReportText.Clear();
                SourceName.Text = "";
                StatusText.Text = UiText.Get("deploymentReview.failed");
                ErrorDialog.Show(this, error, UiText.Source("deploymentReview.failed"));
            }
        }
        finally
        {
            pending = null;
            if (!closed) OpenButton.IsEnabled = true;
        }
    }
}
