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
        new("UnattendLocal · MIT 项目许可证", "ProjectLicense.txt"),
        new("组件与授权说明", "Components.txt"),
        new("UnattendGenerator · MIT", "UnattendGenerator.txt"),
        new("Newtonsoft.Json 13.0.4 · MIT", "Newtonsoft.Json.txt"),
        new(".NET Runtime · MIT", "DotNetRuntime.txt"),
        new("Windows Desktop / WPF · MIT", "WindowsDesktop.txt"),
        new(".NET Runtime · 第三方声明", "DotNetThirdParty.txt"),
        new("WPF · 第三方声明", "WpfThirdParty.txt")
    ];
    private static readonly Assembly ProductAssembly = typeof(AboutWindow).Assembly;
    private static string ProductVersion => ProductIdentity.Version;

    public AboutWindow(int section = 0)
    {
        InitializeComponent();
        ProductIcon.Source = LoadLargeIcon();
        VersionText.Text = "V" + ProductVersion;
        LegalText.Text = Read("UnattendLocal.LegalInformation.txt");
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
            ?? throw new InvalidOperationException("内置说明文件缺失：" + name);
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
            _ => "UnattendLocal V" + ProductVersion + "\nWindows 无人值守配置工具\nWindows x64 · .NET 10\n\n" +
                 "本地离线编辑、保存和生成 Windows 安装配置，不在当前电脑执行生成的安装脚本。\n" +
                 "生成核心：Christoph Schneegans · UnattendGenerator。独立本地中文界面与适配。\n" +
                 "MIT License · Copyright (c) 2026 tigerJX95\n感谢 ChatGPT Codex 提供开发协助。\n\n" + UpstreamLinks.Text
        };
        try { Clipboard.SetText(text); StatusText.SetResourceReference(System.Windows.Controls.TextBlock.ForegroundProperty, "SecondaryBrush"); StatusText.Text = "当前页面内容已复制。"; }
        catch (System.Runtime.InteropServices.ExternalException) { StatusText.SetResourceReference(System.Windows.Controls.TextBlock.ForegroundProperty, "ErrorBrush"); StatusText.Text = "暂时无法访问剪贴板，请稍后重试，或选择文字手动复制。"; }
    }
}
