using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;

namespace UnattendLocal.Desktop;

public partial class AboutWindow : Window
{
    private record LicenseDocument(string Title, string Resource);
    private static readonly LicenseDocument[] Documents = [
        new(global::UnattendLocal.Localization.UiText.Source("text.91fc1a7e06ae"), "ProjectLicense.txt"),
        new(global::UnattendLocal.Localization.UiText.Source("text.cab4acee63ab"), "Components.txt"),
        new("UnattendGenerator · MIT", "UnattendGenerator.txt"),
        new("Newtonsoft.Json 13.0.4 · MIT", "Newtonsoft.Json.txt"),
        new("DiscUtils 0.16.13 · MIT", "DiscUtils.txt"),
        new(".NET Runtime · MIT", "DotNetRuntime.txt"),
        new("Windows Desktop / WPF · MIT", "WindowsDesktop.txt"),
        new(global::UnattendLocal.Localization.UiText.Source("text.6ac0b322bfd3"), "DotNetThirdParty.txt"),
        new(global::UnattendLocal.Localization.UiText.Source("text.848e33ae763c"), "WpfThirdParty.txt")
    ];
    private static readonly Assembly ProductAssembly = typeof(AboutWindow).Assembly;
    private static string ProductVersion => ProductIdentity.Version;

    public AboutWindow(int section = 0)
    {
        InitializeComponent();
        ProductIcon.Source = LoadLargeIcon();
        VersionText.Text = "V" + ProductVersion;
        LegalText.Text = UnattendLocal.Localization.UiText.Get("about.legal");
        LicensePicker.ItemsSource = Documents;
        LicensePicker.SelectedIndex = 0;
        Sections.SelectedIndex = Math.Clamp(section, 0, 2);
        SourceInitialized += (_, _) => UiTheme.UpdateTitleBar(this);
    }

    private static BitmapFrame LoadLargeIcon()
    {
        // A direct ICO URI can select the 16 px frame and stretch it in the about card.
        using var stream = Application.GetResourceStream(new Uri("/UnattendLocal;component/Assets/App.ico", UriKind.Relative))!.Stream;
        var decoder = new IconBitmapDecoder(stream, BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.OnLoad);
        var frame = decoder.Frames.OrderByDescending(f => (long)f.PixelWidth * f.PixelHeight).First();
        frame.Freeze();
        return frame;
    }

    private static string Read(string name)
    {
        using var stream = ProductAssembly.GetManifestResourceStream(name)
            ?? throw new InvalidOperationException(global::UnattendLocal.Localization.UiText.Concat(new object?[] { global::UnattendLocal.Localization.UiText.Source("text.cc3efc6b4315"), name },new bool[]{false,true}));
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }

    private void LicenseChanged(object sender, SelectionChangedEventArgs e)
    {
        if (LicenseText != null && LicensePicker.SelectedItem is LicenseDocument document)
        {
            LicenseText.Text = Read(document.Resource == "ProjectLicense.txt" ? "UnattendLocal.ProjectLicense.txt" : "UnattendLocal.Licenses." + document.Resource);
            LicenseText.ScrollToHome();
        }
    }

    private void CopyClick(object sender, RoutedEventArgs e)
    {
        string text = Sections.SelectedIndex switch {
            1 => LegalText.Text,
            2 => ((LicenseDocument)LicensePicker.SelectedItem).Title + "\n\n" + LicenseText.Text,
            _ => global::UnattendLocal.Localization.UiText.Concat(new object?[] { "UnattendLocal V", ProductVersion, global::UnattendLocal.Localization.UiText.Source("text.80d3a73956b0"), global::UnattendLocal.Localization.UiText.Source("text.4b325768e579"), global::UnattendLocal.Localization.UiText.Source("text.8d7b0cc2866e"), global::UnattendLocal.Localization.UiText.Source("text.6174d5406876"), UpstreamLinks.Text },new bool[]{false,false,false,false,false,false,false})
        };
        try { Clipboard.SetText(text); StatusText.SetResourceReference(System.Windows.Controls.TextBlock.ForegroundProperty, "SecondaryBrush"); StatusText.Text = global::UnattendLocal.Localization.UiText.Translate(global::UnattendLocal.Localization.UiText.Source("text.a3e5b4480d67")); }
        catch (System.Runtime.InteropServices.ExternalException) { StatusText.SetResourceReference(System.Windows.Controls.TextBlock.ForegroundProperty, "ErrorBrush"); StatusText.Text = global::UnattendLocal.Localization.UiText.Translate(global::UnattendLocal.Localization.UiText.Source("text.337d75a0cfda")); }
    }
}
