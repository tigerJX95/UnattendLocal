using System.Collections.Immutable;
using System.Collections.ObjectModel;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Xml;
using Schneegans.Unattend;
namespace UnattendLocal.Core;

public static partial class AppRemovalSettings
{
    public record Item(string Id, string Label, string Group, string Description);
    public static IReadOnlyList<Item> Items { get; } = new Item[]
    {
        new("RemoveInternetExplorer","Internet Explorer","系统能力与组件","移除此项对应的预装应用或系统能力，相关功能将不可用；需要时须重新安装。 上游适用标记：Windows 10 / Windows 11；实际可用项由介质决定，不存在的项会跳过。 匹配项：Browser.InternetExplorer。"),
        new("RemoveWindowsMediaPlayer","Windows Media Player（经典版）","系统能力与组件","移除此项对应的预装应用或系统能力，相关功能将不可用；需要时须重新安装。 上游适用标记：Windows 10 / Windows 11；实际可用项由介质决定，不存在的项会跳过。 匹配项：Media.WindowsMediaPlayer。"),
        new("RemoveZuneMusic","Windows Media Player（新版）","应用与服务","移除此项对应的预装应用或系统能力，相关功能将不可用；需要时须重新安装。 上游适用标记：Windows 10 / Windows 11；实际可用项由介质决定，不存在的项会跳过。 匹配项：Microsoft.ZuneMusic。"),
        new("RemoveMediaFeatures","媒体功能","系统能力与组件","移除媒体可选功能，可能影响音视频播放及依赖媒体组件的应用。 上游适用标记：Windows 10 / Windows 11；实际可用项由介质决定，不存在的项会跳过。 匹配项：MediaPlayback。"),
        new("RemoveWordPad","写字板","系统能力与组件","移除此项对应的预装应用或系统能力，相关功能将不可用；需要时须重新安装。 上游适用标记：Windows 10 / Windows 11；实际可用项由介质决定，不存在的项会跳过。 匹配项：Microsoft.Windows.WordPad。"),
        new("RemoveNotepadClassic","记事本（经典版）","系统能力与组件","移除经典记事本能力；若同时移除新版，将失去内置记事本。 上游适用标记：Windows 10 / Windows 11；实际可用项由介质决定，不存在的项会跳过。 匹配项：Microsoft.Windows.Notepad；Microsoft.Windows.Notepad.System。"),
        new("RemoveNotepad","记事本（新版）","应用与服务","移除新版记事本，保留经典文本文件关联并隐藏商店横幅；经典版可另行选择。 上游适用标记：Windows 11；实际可用项由介质决定，不存在的项会跳过。 匹配项：Microsoft.WindowsNotepad。"),
        new("RemoveStepsRecorder","步骤记录器","系统能力与组件","移除此项对应的预装应用或系统能力，相关功能将不可用；需要时须重新安装。 上游适用标记：Windows 10 / Windows 11；实际可用项由介质决定，不存在的项会跳过。 匹配项：App.StepsRecorder。"),
        new("RemovePaint","画图","系统能力与组件","移除此项对应的预装应用或系统能力，相关功能将不可用；需要时须重新安装。 上游适用标记：Windows 10 / Windows 11；实际可用项由介质决定，不存在的项会跳过。 匹配项：Microsoft.Windows.MSPaint；Microsoft.Paint。"),
        new("RemovePaint3D","画图 3D","应用与服务","移除此项对应的预装应用或系统能力，相关功能将不可用；需要时须重新安装。 上游适用标记：Windows 10；实际可用项由介质决定，不存在的项会跳过。 匹配项：Microsoft.MSPaint。"),
        new("RemoveSkype","Skype","应用与服务","移除此项对应的预装应用或系统能力，相关功能将不可用；需要时须重新安装。 上游适用标记：Windows 10；实际可用项由介质决定，不存在的项会跳过。 匹配项：Microsoft.SkypeApp。"),
        new("RemoveSnippingTool","截图工具","系统能力与组件","移除此项对应的预装应用或系统能力，相关功能将不可用；需要时须重新安装。 上游适用标记：Windows 10 / Windows 11；实际可用项由介质决定，不存在的项会跳过。 匹配项：Microsoft.ScreenSketch；Microsoft-SnippingTool；Microsoft.Windows.SnippingTool。"),
        new("RemoveZuneVideo","电影和电视","应用与服务","移除此项对应的预装应用或系统能力，相关功能将不可用；需要时须重新安装。 上游适用标记：Windows 10 / Windows 11；实际可用项由介质决定，不存在的项会跳过。 匹配项：Microsoft.ZuneVideo。"),
        new("RemoveXboxApps","Xbox 应用与录制","应用与服务（含额外设置）","移除 Xbox 相关应用，并关闭新用户的游戏录制捕获；可能影响 Xbox 游戏体验。 上游适用标记：Windows 10 / Windows 11；实际可用项由介质决定，不存在的项会跳过。 匹配项：Microsoft.Xbox.TCUI；Microsoft.XboxApp；Microsoft.XboxGameOverlay；Microsoft.XboxGamingOverlay；Microsoft.XboxIdentityProvider；Microsoft.XboxSpeechToTextOverlay；Microsoft.GamingApp。"),
        new("RemovePhotos","照片","应用与服务","移除此项对应的预装应用或系统能力，相关功能将不可用；需要时须重新安装。 上游适用标记：Windows 10 / Windows 11；实际可用项由介质决定，不存在的项会跳过。 匹配项：Microsoft.Windows.Photos。"),
        new("RemoveNews","资讯","应用与服务","移除此项对应的预装应用或系统能力，相关功能将不可用；需要时须重新安装。 上游适用标记：Windows 11；实际可用项由介质决定，不存在的项会跳过。 匹配项：Microsoft.BingNews。"),
        new("RemoveWeather","天气","应用与服务","移除此项对应的预装应用或系统能力，相关功能将不可用；需要时须重新安装。 上游适用标记：Windows 10 / Windows 11；实际可用项由介质决定，不存在的项会跳过。 匹配项：Microsoft.BingWeather。"),
        new("RemovePeople","人脉","应用与服务","移除此项对应的预装应用或系统能力，相关功能将不可用；需要时须重新安装。 上游适用标记：Windows 10 / Windows 11；实际可用项由介质决定，不存在的项会跳过。 匹配项：Microsoft.People。"),
        new("RemoveToDo","Microsoft To Do","应用与服务","移除此项对应的预装应用或系统能力，相关功能将不可用；需要时须重新安装。 上游适用标记：Windows 11；实际可用项由介质决定，不存在的项会跳过。 匹配项：Microsoft.Todos。"),
        new("RemoveSolitaire","纸牌合集","应用与服务","移除此项对应的预装应用或系统能力，相关功能将不可用；需要时须重新安装。 上游适用标记：Windows 10 / Windows 11；实际可用项由介质决定，不存在的项会跳过。 匹配项：Microsoft.MicrosoftSolitaireCollection。"),
        new("RemoveStickyNotes","便笺","应用与服务","移除此项对应的预装应用或系统能力，相关功能将不可用；需要时须重新安装。 上游适用标记：Windows 10 / Windows 11；实际可用项由介质决定，不存在的项会跳过。 匹配项：Microsoft.MicrosoftStickyNotes。"),
        new("Remove3DViewer","3D 查看器","应用与服务","移除此项对应的预装应用或系统能力，相关功能将不可用；需要时须重新安装。 上游适用标记：Windows 10；实际可用项由介质决定，不存在的项会跳过。 匹配项：Microsoft.Microsoft3DViewer。"),
        new("RemoveMaps","地图","应用与服务","移除此项对应的预装应用或系统能力，相关功能将不可用；需要时须重新安装。 上游适用标记：Windows 10 / Windows 11；实际可用项由介质决定，不存在的项会跳过。 匹配项：Microsoft.WindowsMaps。"),
        new("RemoveVoiceRecorder","录音机","应用与服务","移除此项对应的预装应用或系统能力，相关功能将不可用；需要时须重新安装。 上游适用标记：Windows 10 / Windows 11；实际可用项由介质决定，不存在的项会跳过。 匹配项：Microsoft.WindowsSoundRecorder。"),
        new("RemoveClipchamp","Clipchamp 视频编辑","应用与服务","移除此项对应的预装应用或系统能力，相关功能将不可用；需要时须重新安装。 上游适用标记：Windows 11；实际可用项由介质决定，不存在的项会跳过。 匹配项：Clipchamp.Clipchamp。"),
        new("RemovePowerAutomate","Power Automate 自动化","应用与服务","移除此项对应的预装应用或系统能力，相关功能将不可用；需要时须重新安装。 上游适用标记：Windows 11；实际可用项由介质决定，不存在的项会跳过。 匹配项：Microsoft.PowerAutomateDesktop。"),
        new("RemoveWindowsTerminal","Windows 终端","应用与服务","移除此项对应的预装应用或系统能力，相关功能将不可用；需要时须重新安装。 上游适用标记：Windows 11；实际可用项由介质决定，不存在的项会跳过。 匹配项：Microsoft.WindowsTerminal。"),
        new("RemoveOneNote","OneNote","应用与服务","移除此项对应的预装应用或系统能力，相关功能将不可用；需要时须重新安装。 上游适用标记：Windows 10；实际可用项由介质决定，不存在的项会跳过。 匹配项：Microsoft.Office.OneNote。"),
        new("RemoveClock","时钟","应用与服务","移除此项对应的预装应用或系统能力，相关功能将不可用；需要时须重新安装。 上游适用标记：Windows 10 / Windows 11；实际可用项由介质决定，不存在的项会跳过。 匹配项：Microsoft.WindowsAlarms。"),
        new("RemoveCalculator","计算器","应用与服务","移除此项对应的预装应用或系统能力，相关功能将不可用；需要时须重新安装。 上游适用标记：Windows 10 / Windows 11；实际可用项由介质决定，不存在的项会跳过。 匹配项：Microsoft.WindowsCalculator。"),
        new("RemoveCamera","相机","应用与服务","移除此项对应的预装应用或系统能力，相关功能将不可用；需要时须重新安装。 上游适用标记：Windows 10 / Windows 11；实际可用项由介质决定，不存在的项会跳过。 匹配项：Microsoft.WindowsCamera。"),
        new("RemoveYourPhone","手机连接","应用与服务","移除此项对应的预装应用或系统能力，相关功能将不可用；需要时须重新安装。 上游适用标记：Windows 10 / Windows 11；实际可用项由介质决定，不存在的项会跳过。 匹配项：Microsoft.YourPhone。"),
        new("RemoveGetHelp","获取帮助","应用与服务","移除此项对应的预装应用或系统能力，相关功能将不可用；需要时须重新安装。 上游适用标记：Windows 10 / Windows 11；实际可用项由介质决定，不存在的项会跳过。 匹配项：Microsoft.GetHelp。"),
        new("RemoveFeedbackHub","反馈中心","应用与服务","移除此项对应的预装应用或系统能力，相关功能将不可用；需要时须重新安装。 上游适用标记：Windows 10 / Windows 11；实际可用项由介质决定，不存在的项会跳过。 匹配项：Microsoft.WindowsFeedbackHub。"),
        new("RemoveOffice365","Office / Microsoft 365 入口","应用与服务","移除此项对应的预装应用或系统能力，相关功能将不可用；需要时须重新安装。 上游适用标记：Windows 10 / Windows 11；实际可用项由介质决定，不存在的项会跳过。 匹配项：Microsoft.MicrosoftOfficeHub。"),
        new("RemoveGetStarted","提示","应用与服务","移除此项对应的预装应用或系统能力，相关功能将不可用；需要时须重新安装。 上游适用标记：Windows 10 / Windows 11；实际可用项由介质决定，不存在的项会跳过。 匹配项：Microsoft.Getstarted。"),
        new("RemoveCortana","Cortana","应用与服务","移除此项对应的预装应用或系统能力，相关功能将不可用；需要时须重新安装。 上游适用标记：Windows 10 / Windows 11；实际可用项由介质决定，不存在的项会跳过。 匹配项：Microsoft.549981C3F5F10。"),
        new("RemoveMathInputPanel","数学输入面板","系统能力与组件","移除此项对应的预装应用或系统能力，相关功能将不可用；需要时须重新安装。 上游适用标记：Windows 10 / Windows 11；实际可用项由介质决定，不存在的项会跳过。 匹配项：MathRecognizer。"),
        new("RemovePowerShellISE","PowerShell ISE","系统能力与组件","移除 PowerShell ISE 编辑器；不代表移除 Windows PowerShell。 上游适用标记：Windows 10 / Windows 11；实际可用项由介质决定，不存在的项会跳过。 匹配项：Microsoft.Windows.PowerShell.ISE。"),
        new("RemoveOpenSSHClient","OpenSSH 客户端","系统能力与组件","移除内置 SSH 客户端，影响依赖系统 ssh/scp 的操作。 上游适用标记：Windows 10 / Windows 11；实际可用项由介质决定，不存在的项会跳过。 匹配项：OpenSSH.Client。"),
        new("RemoveOneDrive","OneDrive","应用与服务（含额外设置）","删除内置 OneDrive 安装程序及默认用户启动项；不保证移除已另行安装的版本，也不删除云端文件。 上游适用标记：Windows 10 / Windows 11；实际可用项由介质决定，不存在的项会跳过。"),
        new("RemoveTeams","Teams","应用与服务（含额外设置）","移除 Teams 应用，并阻止聊天组件自动安装。 上游适用标记：Windows 11 / Windows 11 23H2 / Windows 11 24H2；实际可用项由介质决定，不存在的项会跳过。 匹配项：MicrosoftTeams；MSTeams。"),
        new("RemoveQuickAssist","快速助手","系统能力与组件","移除此项对应的预装应用或系统能力，相关功能将不可用；需要时须重新安装。 上游适用标记：Windows 10 / Windows 11；实际可用项由介质决定，不存在的项会跳过。 匹配项：App.Support.QuickAssist；MicrosoftCorporationII.QuickAssist。"),
        new("RemoveFamily","家庭","应用与服务","移除此项对应的预装应用或系统能力，相关功能将不可用；需要时须重新安装。 上游适用标记：Windows 11；实际可用项由介质决定，不存在的项会跳过。 匹配项：MicrosoftCorporationII.MicrosoftFamily。"),
        new("RemoveMailCalendar","邮件和日历","应用与服务","移除此项对应的预装应用或系统能力，相关功能将不可用；需要时须重新安装。 上游适用标记：Windows 10 / Windows 11；实际可用项由介质决定，不存在的项会跳过。 匹配项：microsoft.windowscommunicationsapps。"),
        new("RemoveOutlook","Outlook（Windows）","应用与服务（含额外设置）","移除新版 Outlook，并移除其 OOBE 更新调度项；不等同于卸载独立 Office 桌面版 Outlook。 上游适用标记：Windows 11 23H2 / Windows 11 24H2；实际可用项由介质决定，不存在的项会跳过。 匹配项：Microsoft.OutlookForWindows。"),
        new("RemoveDevHome","开发者主页","应用与服务（含额外设置）","移除开发者主页，并移除其 OOBE 更新调度项。 上游适用标记：Windows 11 23H2 / Windows 11 24H2；实际可用项由介质决定，不存在的项会跳过。 匹配项：Microsoft.Windows.DevHome。"),
        new("RemoveCopilot","Copilot","应用与服务（含额外设置）","移除 Copilot 应用，并为新用户关闭 Windows Copilot；不同系统版本可能使用不同组件。 上游适用标记：Windows 11 / Windows 11 25H2；实际可用项由介质决定，不存在的项会跳过。 匹配项：Microsoft.Copilot。"),
        new("RemoveBingSearch","Bing 搜索","应用与服务","移除此项对应的预装应用或系统能力，相关功能将不可用；需要时须重新安装。 上游适用标记：Windows 11 24H2；实际可用项由介质决定，不存在的项会跳过。 匹配项：Microsoft.BingSearch。"),
        new("RemoveMixedReality","混合现实","应用与服务","移除此项对应的预装应用或系统能力，相关功能将不可用；需要时须重新安装。 上游适用标记：Windows 10；实际可用项由介质决定，不存在的项会跳过。 匹配项：Microsoft.MixedReality.Portal。"),
        new("RemoveRecall","Recall","系统能力与组件","移除 Recall 可选功能，实际是否存在取决于安装介质。 上游适用标记：Windows 11 24H2；实际可用项由介质决定，不存在的项会跳过。 匹配项：Recall。"),
        new("RemoveRdpClient","远程桌面客户端","系统能力与组件","移除远程桌面客户端可选功能，可能影响远程连接；不等同于关闭远程桌面服务端。 上游适用标记：Windows 10 / Windows 11；实际可用项由介质决定，不存在的项会跳过。 匹配项：Microsoft-RemoteDesktopConnection。"),
        new("RemoveWallet","钱包","应用与服务","移除此项对应的预装应用或系统能力，相关功能将不可用；需要时须重新安装。 上游适用标记：Windows 10；实际可用项由介质决定，不存在的项会跳过。 匹配项：Microsoft.Wallet。"),
        new("RemoveWindowsHello","Windows Hello 人脸识别","系统能力与组件","移除人脸识别能力，将影响 Windows Hello 人脸登录；不代表禁用所有 Hello 登录方式。 上游适用标记：Windows 10 / Windows 11；实际可用项由介质决定，不存在的项会跳过。 匹配项：Hello.Face.18967；Hello.Face.Migration.18967；Hello.Face.20134。"),
        new("RemovePowerShell2","PowerShell 2.0","系统能力与组件","移除旧版 PowerShell 2.0 引擎，可能影响旧脚本依赖。 上游适用标记：Windows 10 / Windows 11；实际可用项由介质决定，不存在的项会跳过。 匹配项：MicrosoftWindowsPowerShellV2Root。"),
        new("RemoveHandwriting","手写（所有语言）","系统能力与组件","移除所有语言的手写能力，影响手写输入和识别。 上游适用标记：Windows 10 / Windows 11；实际可用项由介质决定，不存在的项会跳过。 匹配项：Language.Handwriting。"),
        new("RemoveSpeech","语音（所有语言）","系统能力与组件","移除所有语言的语音及文本转语音能力，可能影响语音输入和辅助功能。 上游适用标记：Windows 10 / Windows 11；实际可用项由介质决定，不存在的项会跳过。 匹配项：Language.Speech；Language.TextToSpeech。"),
        new("RemoveOneSync","OneSync 同步能力","系统能力与组件","移除同步能力，可能影响邮件、联系人及日历同步。 上游适用标记：Windows 10 / Windows 11；实际可用项由介质决定，不存在的项会跳过。 匹配项：OneCoreUAP.OneSync。"),
        new("RemoveGameAssist","游戏助手","应用与服务","移除此项对应的预装应用或系统能力，相关功能将不可用；需要时须重新安装。 上游适用标记：Windows 11 27902.1000；实际可用项由介质决定，不存在的项会跳过。 匹配项：Microsoft.Edge.GameAssist。"),
        new("RemoveStore","Microsoft Store","应用与服务（含额外设置）","移除商店和购买组件，可能影响应用安装、更新及重新获取；部分应用依赖商店。 上游适用标记：Windows 10 / Windows 11；实际可用项由介质决定，不存在的项会跳过。 匹配项：Microsoft.WindowsStore；Microsoft.StorePurchaseApp。"),
    };
    public static void Validate(InstallPlan p)
    {
        if (p.RemovedApps == null || p.RemovedApps.Count > Items.Count || p.RemovedApps.Any(id => id == null || !Items.Any(i => i.Id == id)) || p.RemovedApps.Distinct().Count() != p.RemovedApps.Count)
            throw new ArgumentException("应用移除列表无效，请选择清单中的项目，不能重复或含未知项目。");
    }
    public static ImmutableList<Bloatware> Build(UnattendGenerator g, InstallPlan p)
    {
        Validate(p);
        return p.RemovedApps.Select(id => g.Lookup<Bloatware>(id)).ToImmutableList();
    }
    public static string Selection(InstallPlan p) => p.ImportedXml?.AppRemovalReadOnly==true ? $"原 XML 移除脚本保留（清单只读）；已识别 {p.RemovedApps.Count} 项" : p.RemovedApps.Count == 0 ? "未选择（不添加移除命令）" : $"已选 {p.RemovedApps.Count} 项：" + string.Join("、", Items.Where(i => p.RemovedApps.Contains(i.Id)).Select(i => i.Label));
    public static string Summary(InstallPlan p) => "应用移除：" + Selection(p);
    private const string Root = @"C:\Windows\Setup\Scripts\";
    private static readonly string[] Stages = ["Specialize.ps1", "DefaultUser.ps1", "UserOnce.ps1"];
    private static string Normal(string s) => string.Join('\n', s.Replace("\r\n", "\n").Split('\n').Select(l => l.Trim()).Where(l => l.Length > 0));
    private static XmlElement[] Files(XmlDocument d, string name) => d.SelectNodes("//*[local-name()='File']")!.Cast<XmlElement>().Where(f => f.GetAttribute("path").Equals(Root + name, StringComparison.OrdinalIgnoreCase)).ToArray();
    private static List<string> Blocks(string body) => Regex.Matches(body.Replace("\r\n", "\n"), @"(?m)^\t\{\n(?<body>(?:\t\t[^\n]*\n)*)\t\};\n", RegexOptions.None, TimeSpan.FromSeconds(1)).Select(m => Normal(m.Groups["body"].Value)).ToList();
    private static Dictionary<string,string> wrappers = new();
    private static readonly Dictionary<string,string> helperTails = new();
    private static string oneDriveBlock = "";
    private static HashSet<string> runOnceCommands = new();
    private static string Skeleton(string body) => Normal(Regex.Replace(body.Replace("\r\n","\n"), @"(?m)^\t\{\n(?:\t\t[^\n]*\n)*\t\};\n", "", RegexOptions.None, TimeSpan.FromSeconds(1)));
    private record Template(Dictionary<string,string> Helpers, Dictionary<string,string[]> Custom, Dictionary<string,string[]> Selectors);
    private static readonly Lazy<Dictionary<string,Template>> Templates = new(CreateTemplates);
    private static Dictionary<string,Template> CreateTemplates()
    {
        var g = new UnattendGenerator();
        XmlDocument Make(ImmutableList<Bloatware> apps) => g.GenerateXml(Configuration.Default with { Bloatwares = apps });
        var baseline = Make([]);
        var all = Make(g.Bloatwares.Values.ToImmutableList());
        var alternate=g.GenerateXml(Configuration.Default with {Bloatwares=g.Bloatwares.Values.ToImmutableList(),HidePowerShellWindows=!Configuration.Default.HidePowerShellWindows});
        runOnceCommands=new[]{all,alternate}.SelectMany(d=>Files(d,"DefaultUser.ps1")).SelectMany(f=>Blocks(f.InnerText)).Where(b=>b.Contains("RunOnce")&&b.Contains("UnattendedSetup")&&b.Contains(Root+"UserOnce.ps1")).ToHashSet();
        wrappers=Stages.ToDictionary(stage=>stage,stage=>Skeleton(Files(all,stage).Single().InnerText));
        var empty = Stages.ToDictionary(s => s, s => Files(baseline,s).SelectMany(f => Blocks(f.InnerText)).ToHashSet());
        var result = new Dictionary<string,Template>();
        foreach (var item in Items)
        {
            var bw = g.Lookup<Bloatware>(item.Id); var doc = Make([bw]);
            var helpers = new Dictionary<string,string>(); var selectors = new Dictionary<string,string[]>();
            foreach (string type in new[]{"Package", "Capability", "Feature"})
            {
                string name = "Remove" + type + ".ps1"; var f = Files(doc,name);
                if (f.Length == 0) continue;
                var match = Regex.Match(f[0].InnerText.Replace("\r\n","\n"), @"\A\s*\$selectors = @\(\n(?<values>(?:\t'[^'\n]+';\n)+)\);\n(?<tail>[\s\S]+)\z", RegexOptions.None, TimeSpan.FromSeconds(1));
                if (!match.Success) throw new InvalidOperationException("应用移除模板不匹配。");
                helpers[name] = Normal(match.Groups["tail"].Value);
                helperTails.TryAdd(name, match.Groups["tail"].Value);
                selectors[name] = Regex.Matches(match.Groups["values"].Value,@"'([^']+)';").Select(m => m.Groups[1].Value).ToArray();
            }
            var custom = Stages.ToDictionary(s => s, s => Files(doc,s).SelectMany(f=>Blocks(f.InnerText)).Where(b => !empty[s].Contains(b) && !(b.Contains("RunOnce") && b.Contains(Root+"UserOnce.ps1")) && !b.StartsWith("& '" + Root + "Remove", StringComparison.Ordinal)).ToArray());
            if (item.Id == "RemoveOneDrive") oneDriveBlock = RawBlocks(Files(doc,"Specialize.ps1").Single().InnerText).Single(m => Normal(m.Groups["body"].Value) == custom["Specialize.ps1"].Single()).Value;
            result[item.Id] = new(helpers, custom, selectors);
        }
        return result;
    }
    public record Source(string[] Selected, bool Ambiguous);
    public static Source Inspect(XmlDocument doc)
    {
        doc=XmlImportPaths.Inspection(doc);
        var normalized = NormalizeLegacy(doc);
        if (normalized != null) return InspectCurrent(normalized);
        var state = InspectCurrent(doc);
        return HasLegacyRemoval(doc) ? state with { Ambiguous = true } : state;
    }
    private static Source InspectCurrent(XmlDocument doc)
    {
        var templates = Templates.Value; var selectors = new Dictionary<string,HashSet<string>>(); bool ambiguous = false;
        var blocks = Stages.ToDictionary(s=>s,s=>Files(doc,s).SelectMany(f=>Blocks(f.InnerText)).ToArray());
        foreach(string stage in Stages)
        {
            var files=Files(doc,stage);
            if(files.Length!=1 || Skeleton(files[0].InnerText)!=wrappers[stage])blocks[stage]=[];
            if(stage!="UserOnce.ps1")
            {
                var ns=new XmlNamespaceManager(doc.NameTable);ns.AddNamespace("u",XmlMerge.Namespace);
                var paths=doc.SelectNodes("//u:settings[@pass='specialize']//u:Path",ns)!.Cast<XmlElement>();
                bool active=paths.Any(p=>Regex.IsMatch(p.InnerText, "^powershell\\.exe -WindowStyle \"(?:Hidden|Normal)\" -ExecutionPolicy \"Unrestricted\" -NoProfile -File \""+Regex.Escape(Root+stage)+"\"$",RegexOptions.IgnoreCase,TimeSpan.FromSeconds(1)));
                if(!active)blocks[stage]=[];
            }
            else if(blocks["DefaultUser.ps1"].Count(runOnceCommands.Contains)!=1)blocks[stage]=[];
        }
        foreach (string name in new[]{"RemovePackage.ps1","RemoveCapability.ps1","RemoveFeature.ps1"})
        {
            var files = Files(doc,name); string call = "& '" + Root + name + "';";
            bool mentioned = files.Length > 0 || Files(doc,"Specialize.ps1").Any(f=>f.InnerText.Contains(Root+name,StringComparison.OrdinalIgnoreCase));
            if (!mentioned) continue;
            var match = files.Length==1 ? Regex.Match(files[0].InnerText.Replace("\r\n","\n"), @"\A\s*\$selectors = @\(\n(?<values>(?:\t'[^'\n]+';\n)+)\);\n(?<tail>[\s\S]+)\z", RegexOptions.None, TimeSpan.FromSeconds(1)) : Match.Empty;
            var expected = templates.Values.First(t=>t.Helpers.ContainsKey(name));
            bool valid = match.Success && Normal(match.Groups["tail"].Value)==expected.Helpers[name] && blocks["Specialize.ps1"].Count(b=>b==call)==1 && Files(doc,"Specialize.ps1").Length==1;
            if (!valid) { ambiguous=true; continue; }
            var values = Regex.Matches(match.Groups["values"].Value,@"'([^']+)';").Select(m=>m.Groups[1].Value).ToArray();
            selectors[name] = values.ToHashSet();
            if(values.Distinct().Count()!=values.Length)ambiguous=true;
        }
        var chosen = new List<string>();
        foreach(var pair in templates)
        {
            var t=pair.Value;
            bool selected = t.Selectors.All(p=>selectors.TryGetValue(p.Key,out var values)&&p.Value.All(values.Contains)) && t.Custom.All(p=>p.Value.All(b=>blocks[p.Key].Count(v=>v==b)==1));
            if(selected)chosen.Add(pair.Key);
        }
        foreach(var pair in selectors)
        {
            var accounted=chosen.SelectMany(id=>templates[id].Selectors.GetValueOrDefault(pair.Key,[])).ToHashSet();
            if(pair.Value.Any(v=>!accounted.Contains(v)))ambiguous=true;
        }
        // A partial or altered custom side effect must never be replaced by a new checkbox setting.
        foreach(var pair in templates.Where(p=>p.Value.Custom.Any(c=>c.Value.Length>0)))
        {
            if(chosen.Contains(pair.Key))continue;
            if(pair.Value.Custom.Any(p=>p.Value.Any(b=>blocks[p.Key].Contains(b))))ambiguous=true;
        }
        // Detect recognizable custom markers even if their command/block was edited.
        foreach(string marker in new[]{"OneDrive.lnk", "txtfilelegacy", "notepad.exe,-470", "Microsoft\\Notepad", "Communications", "GameDVR", "UScheduler_Oobe", "OneDriveSetup", "ConfigureChatAutoInstall", "ShowStoreBanner", "OutlookUpdate", "DevHomeUpdate", "TurnOffWindowsCopilot", "AppCaptureEnabled", "Microsoft.Windows.Ai.Copilot.Provider", "Internet Explorer\\LowRegistry"})
        {
            var owner=templates.Where(p=>p.Value.Custom.Any(c=>c.Value.Any(b=>b.Contains(marker,StringComparison.OrdinalIgnoreCase)))).Select(p=>p.Key).ToArray();
            if(owner.Length>0 && Stages.Any(stage=>Files(doc,stage).Any(f=>{
                string rest=Normal(f.InnerText);
                foreach(var id in chosen)foreach(var command in templates[id].Custom[stage])rest=rest.Replace(command,"",StringComparison.Ordinal);
                return rest.Contains(marker,StringComparison.OrdinalIgnoreCase);
            })))ambiguous=true;
        }
        return new(chosen.ToArray(),ambiguous);
    }
    public static Source ReadIntoPlan(XmlDocument doc,InstallPlan p,List<string> notes)
    {
        var state=Inspect(doc); p.RemovedApps=new(state.Selected);
        if (!state.Ambiguous && HasLegacyRemoval(doc)) notes.Add(LegacyDescription);
        if(state.Ambiguous)notes.Add(ReadOnlyDescription);
        return state;
    }
    public const string ReadOnlyDescription = "应用移除脚本与当前可编辑模板不一致，原脚本完整保留并继续随 XML 导出；本次应用移除清单只读。该功能已支持，但此文件的脚本暂不能通过清单编辑。";
    public static void ValidateImported(InstallPlan p,XmlDocument doc)
    {
        var state=Inspect(doc);
        if(state.Ambiguous&&!state.Selected.Order().SequenceEqual(p.RemovedApps.Order()))throw new ArgumentException("原 XML 的应用移除脚本未完整识别，不能可靠修改移除列表；请用原工具调整后重新导入。");
    }
    public static void NormalizeLoaded(InstallPlan p,JsonElement json)
    {
        UpgradeLegacyPlan(p);
        if(json.TryGetProperty(nameof(InstallPlan.RemovedApps),out _) || p.ImportedXml is not {OriginalOmitted:false} data || data.SourceXml.Length==0 || data.BaselineXml.Length==0)return;
        var before=JsonSerializer.Deserialize<InstallPlan>(JsonSerializer.Serialize(p))!;before.ImportedXml=null;before.ConfirmWipeDisk=before.ConfirmFormatPartition=before.ConfirmDiskScript=true;
        var notes=new List<string>();ReadIntoPlan(XmlMerge.Read(data.SourceXml),p,notes);data.Notices=data.Notices.Concat(notes).Distinct().ToArray();
        if(p.RemovedApps.Count==0)return;
        var after=JsonSerializer.Deserialize<InstallPlan>(JsonSerializer.Serialize(before))!;after.RemovedApps=new(p.RemovedApps);
        try {var g=new GeneratorService();data.BaselineXml=XmlMerge.Apply(data.BaselineXml,Encoding.UTF8.GetString(g.GenerateImportBaseline(before)),XmlMerge.Read(Encoding.UTF8.GetString(g.GenerateImportBaseline(after)))).OuterXml;}
        catch(Exception e)when(e is ArgumentException or ConfigurationException or XmlException){data.Blockers=data.Blockers.Append("旧方案的应用移除编辑基准无法可靠迁移，请重新导入原 XML 后再导出。").Distinct().ToArray();}
    }
}
