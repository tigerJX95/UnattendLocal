using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;

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
        LegalText.Document = CreateDocument(UnattendLocal.Localization.UiText.Get("about.legal"), headings: true);
        LicensePicker.ItemsSource = Documents;
        LicensePicker.SelectedIndex = 0;
        Sections.SelectedIndex = Math.Clamp(section, 0, 2);
        SourceInitialized += (_, _) => UiTheme.UpdateTitleBar(this);
        PreviewKeyDown += (_, e) => { if (e.Key == System.Windows.Input.Key.Escape) { e.Handled = true; Close(); } };
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
            bool components = document.Resource == "Components.txt";
            string text = components ? UnattendLocal.Localization.UiText.Get("about.components") : Read(document.Resource == "ProjectLicense.txt" ? "UnattendLocal.ProjectLicense.txt" : "UnattendLocal.Licenses." + document.Resource);
            LicenseText.Document = CreateDocument(text, componentNames: components);
            LicenseText.ScrollToHome();
        }
    }

    private static readonly Regex WebAddress = new(@"https?://[^\s<>""“”‘’]+", RegexOptions.CultureInvariant | RegexOptions.NonBacktracking, TimeSpan.FromMilliseconds(250));
    private static readonly Regex SectionHeading = new(@"^(?:[一二三四五六七八九十]+、|[0-9]+\. )", RegexOptions.CultureInvariant | RegexOptions.NonBacktracking, TimeSpan.FromMilliseconds(250));

    private FlowDocument CreateDocument(string text, bool headings = false, bool componentNames = false)
    {
        // Render text as text, never as XAML/HTML. Preserve original licence wording
        // and line breaks; authored help supplies its own semantic paragraphs.
        var document = new FlowDocument { FontFamily = FontFamily, FontSize = 14, LineHeight = 23, PagePadding = new Thickness(0), TextAlignment = TextAlignment.Left };
        document.SetResourceReference(FlowDocument.ForegroundProperty, "ForegroundBrush");
        string normalized = text.Replace("\r\n", "\n");
        // Large external translations remain readable without expanding every
        // line or URL into a separate visual element. No content is discarded.
        if (normalized.Length > 262144 || normalized.Count(c => c == '\n') > 4096)
        {
            document.Blocks.Add(new Paragraph(new Run(normalized)) { Margin = new Thickness(0) });
            return document;
        }
        foreach (string block in normalized.Split("\n\n", StringSplitOptions.None))
        {
            var paragraph = new Paragraph { Margin = new Thickness(0, 0, 0, 10) };
            string[] lines = block.Split('\n');
            if (headings && SectionHeading.IsMatch(block)) paragraph.FontWeight = FontWeights.SemiBold;
            for (int i = 0; i < lines.Length; i++)
            {
                if (i > 0) paragraph.Inlines.Add(new LineBreak());
                var span = new Span();
                if (componentNames && i == 0 && lines.Length > 1) span.FontWeight = FontWeights.SemiBold;
                AddWebLinks(span.Inlines, lines[i]);
                paragraph.Inlines.Add(span);
            }
            document.Blocks.Add(paragraph);
        }
        return document;
    }

    private void AddWebLinks(InlineCollection inlines, string text)
    {
        int position = 0;
        foreach (Match match in WebAddress.Matches(text))
        {
            string address = match.Value.TrimEnd('.', ',', ';', ':', '!', '?', '。', '，', '；', '：', '！', '？', ')', ']', '）', '】');
            if (!Uri.TryCreate(address, UriKind.Absolute, out var uri) || (uri.Scheme != Uri.UriSchemeHttps && uri.Scheme != Uri.UriSchemeHttp)) continue;
            inlines.Add(new Run(text[position..match.Index]));
            var link = new Hyperlink(new Run(address)) { NavigateUri = uri, Cursor = System.Windows.Input.Cursors.Hand };
            link.SetResourceReference(Hyperlink.ForegroundProperty, "AccentTextBrush");
            link.RequestNavigate += RepositoryLinkNavigate;
            inlines.Add(link);
            position = match.Index + address.Length;
        }
        inlines.Add(new Run(text[position..]));
    }

    private void RepositoryLinkNavigate(object sender, RequestNavigateEventArgs e)
    {
        e.Handled = true;
        try
        {
            Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri) { UseShellExecute = true });
        }
        catch (Exception ex) when (ex is Win32Exception or InvalidOperationException or System.Security.SecurityException)
        {
            System.Windows.MessageBox.Show(this, global::UnattendLocal.Localization.UiText.Get("about.linkOpenError"), global::UnattendLocal.Localization.UiText.Get("text.4b9659ed544c"), MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }
}
