using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Xml;
using Schneegans.Unattend;
namespace UnattendLocal.Core;

public static class VmGuestToolsSettings
{
    private const string Root = @"C:\Windows\Setup\Scripts\";
    private const string Stage = "FirstLogon.ps1";
    private const string BlockPattern = @"(?m)^\t\{\n(?<body>(?:\t\t[^\n]*\n)*)\t\};\n";
    public record Item(string Property, string Label, string Resource, string HelpId, string Media);
    public static IReadOnlyList<Item> Items { get; } = new Item[]
    {
        new(nameof(InstallPlan.VMwareTools), "VMware Tools", "VMwareTools.ps1", "vmwaretools", "VMware Tools 光盘（windows.iso），内含产品名为 VMware Tools 的 setup.exe"),
        new(nameof(InstallPlan.VBoxGuestAdditions), "VirtualBox 增强功能", "VBoxGuestAdditions.ps1", "vboxtools", "VBoxGuestAdditions.iso，内含 VBoxWindowsAdditions.exe 和 cert 目录"),
        new(nameof(InstallPlan.VirtIoGuestTools), "VirtIO Guest Tools", "VirtIoGuestTools.ps1", "virtiotools", "virtio-win 工具光盘，内含 virtio-win-guest-tools.exe"),
        new(nameof(InstallPlan.ParallelsTools), "Parallels Tools", "ParallelsTools.ps1", "parallelstools", "Parallels Tools 光盘，内含 PTAgent.exe")
    };
    public static string Description(string property)
    {
        var item = Items.Single(i => i.Property == property);
        string detail = property switch
        {
            nameof(InstallPlan.VBoxGuestAdditions) => "安装脚本会从介质导入 VirtualBox 工具证书，再静默安装增强功能。",
            nameof(InstallPlan.VirtIoGuestTools) => "这里只安排安装后的全机首次登录工具安装；不能替代 Windows PE 中用于识别磁盘或网络的 VirtIO 驱动，必要时请在安装介质中另行准备。导入 XML 中已有的 PE 驱动脚本会按原文保留，此开关不删除这些脚本。",
            nameof(InstallPlan.ParallelsTools) => "部分集成功能可能需要安装后重启。",
            _ => "使用静默安装参数，不在界面中更换安装器或版本。"
        };
        return "默认不勾选。选择后在目标 Windows 的全机首次登录阶段，从已挂载的 " + item.Media + " 查找并运行安装器。" + detail + "请提前挂载对应的官方介质，并确认与目标 Windows 和架构兼容。脚本扫描 D:–Z:；未找到时输出提示而不安装，不自动联网下载。本工具不附带工具安装包，不修改当前电脑；取消只影响新配置，不卸载已安装工具。";
    }
    public static bool Selected(InstallPlan p, Item item) => (bool)typeof(InstallPlan).GetProperty(item.Property)!.GetValue(p)!;
    public static string Summary(InstallPlan p)
    {
        var names = Items.Where(i => Selected(p, i)).Select(i => i.Label).ToArray();
        return "虚拟机集成工具：" + (names.Length == 0 ? "未选择（不添加安装命令）" : string.Join("、", names) + "；全机首次登录安装，需挂载对应工具介质");
    }
    public static string HelpState(InstallPlan p, string helpId)
    {
        var item = Items.Single(i => i.HelpId == helpId);
        return Selected(p, item) ? "当前：安装 " + item.Label + "，需挂载对应工具介质。" : "当前：不添加 " + item.Label + " 安装命令。";
    }
    private static string Normal(string body) => string.Join('\n', body.Replace("\r\n", "\n").Split('\n').Select(l => l.Trim()).Where(l => l.Length > 0));
    private static string WindowStyle(string command) => Regex.Replace(command, "-WindowStyle \"(?:Hidden|Normal)\"", "-WindowStyle \"Hidden\"", RegexOptions.None, TimeSpan.FromSeconds(1));
    private static string Skeleton(string body) => Normal(Regex.Replace(body.Replace("\r\n", "\n"), BlockPattern, "", RegexOptions.None, TimeSpan.FromSeconds(1)));
    private static XmlElement[] Files(XmlDocument doc, string name) => doc.SelectNodes("//*[local-name()='File']")!.Cast<XmlElement>().Where(f => f.GetAttribute("path").Equals(Root + name, StringComparison.OrdinalIgnoreCase)).ToArray();
    private record Template(string StageWrapper, string Extraction, string ExtractCall, string StageCall, Dictionary<string,string> Helpers);
    private static readonly Lazy<Template> Expected = new(() =>
    {
        var doc = new UnattendGenerator().GenerateXml(Configuration.Default with { VMwareTools = true, VBoxGuestAdditions = true, VirtIoGuestTools = true, ParallelsTools = true });
        var ns = Namespace(doc);
        var extractCall = doc.SelectNodes("//u:settings[@pass='specialize']//u:Path", ns)!.Cast<XmlElement>().Single(n => n.InnerText.Contains("Extensions.ExtractScript", StringComparison.Ordinal)).InnerText;
        var stageCall = doc.SelectNodes("//u:settings[@pass='oobeSystem']//u:FirstLogonCommands/u:SynchronousCommand/u:CommandLine", ns)!.Cast<XmlElement>().Single(n => n.InnerText.Contains(Root + Stage, StringComparison.Ordinal)).InnerText;
        return new(Skeleton(Files(doc, Stage).Single().InnerText), Normal(doc.SelectSingleNode("//*[local-name()='ExtractScript']")!.InnerText), extractCall, stageCall,
            Items.ToDictionary(i => i.Property, i => Normal(Files(doc, i.Resource).Single().InnerText)));
    });
    private static XmlNamespaceManager Namespace(XmlDocument doc) { var ns = new XmlNamespaceManager(doc.NameTable); ns.AddNamespace("u", XmlMerge.Namespace); return ns; }
    public record State(bool Enabled, bool Ambiguous);
    public static Dictionary<string, State> Inspect(XmlDocument doc)
    {
        var expected = Expected.Value; var ns = Namespace(doc);
        var files = doc.SelectNodes("//*[local-name()='File']")!.Cast<XmlElement>().ToArray();
        var stage = Files(doc, Stage);
        var extraction = doc.SelectNodes("//*[local-name()='ExtractScript']")!.Cast<XmlElement>().ToArray();
        var specializePaths = doc.SelectNodes("//u:settings[@pass='specialize']//u:Path", ns)!.Cast<XmlElement>().ToArray();
        var firstCalls = doc.SelectNodes("//u:settings[@pass='oobeSystem']//u:FirstLogonCommands/u:SynchronousCommand/u:CommandLine", ns)!.Cast<XmlElement>().ToArray();
        bool extracted = extraction.Length == 1 && Normal(extraction[0].InnerText) == expected.Extraction && specializePaths.Count(p => WindowStyle(p.InnerText) == WindowStyle(expected.ExtractCall)) == 1;
        bool active = firstCalls.Count(p => WindowStyle(p.InnerText) == WindowStyle(expected.StageCall)) == 1;
        bool nativeStage = extracted && active && stage.Length == 1 && Skeleton(stage[0].InnerText) == expected.StageWrapper;
        var blocks = nativeStage ? Regex.Matches(stage[0].InnerText.Replace("\r\n", "\n"), BlockPattern, RegexOptions.None, TimeSpan.FromSeconds(1)).Select(m => Normal(m.Groups["body"].Value)).ToArray() : [];
        var xmlCommands = doc.SelectNodes("//u:Path|//u:CommandLine", ns)!.Cast<XmlElement>().Select(n => n.InnerText).ToArray();
        var states = new Dictionary<string, State>();
        foreach (var item in Items)
        {
            var helper = Files(doc, item.Resource);
            var references = files.Where(f => !helper.Contains(f)).SelectMany(f => Normal(f.InnerText).Split('\n')).Concat(xmlCommands).Where(l => l.Contains(item.Resource, StringComparison.OrdinalIgnoreCase)).ToArray();
            string call = "& '" + Root + item.Resource + "';";
            bool mentioned = references.Length > 0 || files.Any(f => f.GetAttribute("path").EndsWith(item.Resource, StringComparison.OrdinalIgnoreCase));
            bool enabled = nativeStage && helper.Length == 1 && Normal(helper[0].InnerText) == expected.Helpers[item.Property] && blocks.Count(b => b == call) == 1 && references.Length == 1 && references[0] == call;
            states[item.Property] = new(enabled, mentioned && !enabled);
        }
        return states;
    }
    public static void ReadIntoPlan(XmlDocument doc, InstallPlan p, List<string> notes)
    {
        var states = Inspect(doc);
        if (doc.InnerText.Contains("VIRTIO_DRIVE", StringComparison.OrdinalIgnoreCase))
            notes.Add("原 XML 含 VirtIO 的 Windows PE 驱动脚本，按原文保留；虚拟机工具开关只编辑已识别的首次登录工具安装，不删除 PE 驱动脚本。");
        foreach (var item in Items)
        {
            typeof(InstallPlan).GetProperty(item.Property)!.SetValue(p, states[item.Property].Enabled);
            if (states[item.Property].Ambiguous) notes.Add(item.Label + "安装脚本、阶段或执行管线未完整识别，原文保留；不能用此开关覆盖。");
        }
    }
    public static void ValidateImported(InstallPlan p, XmlDocument doc)
    {
        var states = Inspect(doc);
        foreach (var item in Items)
            if (Selected(p, item) && states[item.Property].Ambiguous)
                throw new ArgumentException(item.Label + "与原 XML 中未完整识别的安装脚本重叠，请用原工具调整后重新导入。");
    }
    public static void NormalizeLoaded(InstallPlan p, JsonElement json)
    {
        if (p.ImportedXml is not { OriginalOmitted: false } data || data.SourceXml.Length == 0 || data.BaselineXml.Length == 0) return;
        var missing = Items.Where(i => !json.TryGetProperty(i.Property, out _)).ToArray(); if (missing.Length == 0) return;
        var states = Inspect(XmlMerge.Read(data.SourceXml));
        var before = JsonSerializer.Deserialize<InstallPlan>(JsonSerializer.Serialize(p))!;
        before.ImportedXml = null; before.ConfirmWipeDisk = before.ConfirmFormatPartition = before.ConfirmDiskScript = true;
        foreach (var item in missing)
        {
            typeof(InstallPlan).GetProperty(item.Property)!.SetValue(p, states[item.Property].Enabled);
            if (states[item.Property].Ambiguous) data.Notices = data.Notices.Append(item.Label + "安装内容未完整识别，原文保留，不能覆盖。").Distinct().ToArray();
            else if (states[item.Property].Enabled) data.Notices = data.Notices.Where(n => !n.Contains("生成参数：" + item.Property + "；", StringComparison.Ordinal)).ToArray();
        }
        if (!missing.Any(i => states[i.Property].Enabled)) return;
        var after = JsonSerializer.Deserialize<InstallPlan>(JsonSerializer.Serialize(before))!;
        foreach (var item in missing) typeof(InstallPlan).GetProperty(item.Property)!.SetValue(after, states[item.Property].Enabled);
        try
        {
            var service = new GeneratorService();
            data.BaselineXml = XmlMerge.Apply(data.BaselineXml, Encoding.UTF8.GetString(service.GenerateImportBaseline(before)), XmlMerge.Read(Encoding.UTF8.GetString(service.GenerateImportBaseline(after)))).OuterXml;
        }
        catch (Exception e) when (e is ArgumentException or ConfigurationException or XmlException)
        {
            data.Blockers = data.Blockers.Append("旧方案的虚拟机工具编辑基准无法可靠迁移，请重新导入原 XML 后再导出。").Distinct().ToArray();
        }
    }
}
