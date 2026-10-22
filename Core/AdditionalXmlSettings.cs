using System.Text.Json.Serialization;
using System.Xml;
using Schneegans.Unattend;

namespace UnattendLocal.Core;

public class XmlComponentEntry : Observable
{
    private bool enabled;
    public bool Enabled { get => enabled; set { Set(ref enabled, value); Notify(nameof(Label)); } }
    private string component = "Microsoft-Windows-Shell-Setup";
    public string Component { get => component; set { Set(ref component, value); Notify(nameof(Label)); } }
    private string pass = "specialize";
    public string Pass { get => pass; set { Set(ref pass, value); Notify(nameof(Label)); } }
    private string content = "";
    public string Content { get => content; set => Set(ref content, value); }
    [JsonIgnore] public string Label => (Enabled ? "启用 · " : "停用 · ") + Pass + " · " + Component;
}

public static class AdditionalXmlSettings
{
    public const int MaxCount = 15, MaxLength = 131072;
    public const string Example = "<OEMInformation>\n  <SupportProvider>示例技术支持</SupportProvider>\n  <SupportURL>https://example.com/support</SupportURL>\n</OEMInformation>";
    private static readonly Lazy<UnattendGenerator> Catalog = new(() => new());
    public static IReadOnlyList<string> Components => Catalog.Value.Components.Keys.Order(StringComparer.Ordinal).ToArray();
    public record PhaseChoice(string Id, string Label);
    public static PhaseChoice[] Passes => [new("windowsPE", "Windows PE 安装环境（windowsPE）"), new("offlineServicing", "脱机维护（offlineServicing）"), new("generalize", "系统通用化（generalize）"), new("specialize", "系统安装配置（specialize）"), new("auditSystem", "审核模式系统配置（auditSystem）"), new("auditUser", "审核模式用户配置（auditUser）"), new("oobeSystem", "首次开机准备（oobeSystem）")];

    // These fields are parsed by existing UI controls, including after a snapshot import.
    // Letting fragments own them would create two editors for one setting on re-import.
    private static readonly Dictionary<string,string[]> Managed = new(StringComparer.Ordinal)
    {
        ["Microsoft-Windows-Setup"]=["DiskConfiguration","ImageInstall","UserData","DynamicUpdate","UseConfigurationSet","LogPath","RunSynchronous"],
        ["Microsoft-Windows-Shell-Setup"]=["ComputerName","TimeZone","ProductKey","RegisteredOwner","RegisteredOrganization","AutoLogon","UserAccounts","OOBE","FirstLogonCommands"],
        ["Microsoft-Windows-Deployment"]=["RunSynchronous"],
        ["Microsoft-Windows-International-Core"]=["InputLocale","SystemLocale","UILanguage","UILanguageFallback","UserLocale","GeoID"],
        ["Microsoft-Windows-International-Core-WinPE"]=["InputLocale","SystemLocale","UILanguage","UILanguageFallback","UserLocale","SetupUILanguage"],
        ["Microsoft-Windows-PnpCustomizationsWinPE"]=["DriverPaths"],
        ["Microsoft-Windows-TCPIP"]=["Interfaces"],
        ["Microsoft-Windows-DNS-Client"]=["Interfaces"],
        ["Microsoft-Windows-UnattendedJoin"]=["Identification"]
    };

    public static XmlDocument Parse(XmlComponentEntry row)
    {
        if (!Catalog.Value.Components.TryGetValue(row.Component ?? "", out var component) ||
            !Enum.TryParse<Pass>(row.Pass, out var pass) || !Enum.IsDefined(pass) || row.Pass != pass.ToString() || !component.Passes.Contains(pass))
            throw new ConfigurationValidationException(nameof(InstallPlan.XmlComponents), "额外 XML 的组件与执行阶段不匹配，请核对当前组件目录及微软文档。");
        if (string.IsNullOrWhiteSpace(row.Content) || row.Content.Length > MaxLength)
            throw new ConfigurationValidationException(nameof(InstallPlan.XmlComponents), "请填写额外 XML 的子元素，每项最多 131,072 个字符。");
        try
        {
            using var reader = XmlReader.Create(new StringReader("<root xmlns='" + XmlMerge.Namespace + "' xmlns:wcm='http://schemas.microsoft.com/WMIConfig/2002/State' xmlns:xsi='http://www.w3.org/2001/XMLSchema-instance'>" + row.Content + "</root>"),
                new XmlReaderSettings { DtdProcessing = DtdProcessing.Prohibit, XmlResolver = null, MaxCharactersInDocument = MaxLength + 1024 });
            var doc = new XmlDocument { XmlResolver = null }; doc.Load(reader);
            if(doc.SelectNodes("//processing-instruction()")!.Count>0)throw new ArgumentException("额外 XML 不支持处理指令。");
            var pending=new Stack<(XmlElement Node,int Depth)>();pending.Push((doc.DocumentElement!,0));int count=0;
            while(pending.Count>0){var item=pending.Pop();if(item.Depth>64||++count>2048)throw new ArgumentException("额外 XML 层级或项目过多，每项最多 64 层、2,048 个元素。");foreach(var child in item.Node.ChildNodes.OfType<XmlElement>())pending.Push((child,item.Depth+1));}
            var elements = doc.DocumentElement!.ChildNodes.OfType<XmlElement>().ToArray();
            if (elements.Length == 0 || doc.DocumentElement.ChildNodes.Cast<XmlNode>().Any(n => n is not (XmlElement or XmlComment or XmlWhitespace or XmlSignificantWhitespace) && !string.IsNullOrWhiteSpace(n.Value)))
                throw new ArgumentException("额外 XML 需包含一个或多个设置子元素，不能只有文本。");
            if (doc.SelectNodes("//*")!.Cast<XmlElement>().Any(e => e != doc.DocumentElement && (e.NamespaceURI != XmlMerge.Namespace || e.LocalName is "unattend" or "settings" or "component" or "Extensions")))
                throw new ArgumentException("只填写应答文件命名空间中的设置子元素，不要包含 unattend、settings、component 或扩展文件包装。");
            if (elements.GroupBy(e => e.LocalName).Any(g => g.Count() > 1))
                throw new ArgumentException("额外 XML 的顶层设置名称不能重复；列表项目应放在同一个设置元素内。");
            if(Managed.TryGetValue(row.Component ?? "",out var managed)&&elements.Any(element=>managed.Contains(element.LocalName)))
                throw new ArgumentException("此顶层设置已有对应界面或生成流程，请使用现有控件，不能通过额外 XML 重复配置。");
            return doc;
        }
        catch (Exception e) when (e is XmlException or ArgumentException)
        {
            throw new ConfigurationValidationException(nameof(InstallPlan.XmlComponents), "额外 XML 无效：" + e.Message);
        }
    }

    public static void Validate(InstallPlan plan, bool requireConsent)
    {
        if (plan.XmlComponents == null || plan.XmlComponents.Count > MaxCount || plan.XmlComponents.Any(row => row == null))
            throw new ConfigurationValidationException(nameof(InstallPlan.XmlComponents), "额外 XML 组件列表无效，最多 15 项。");
        foreach (var row in plan.XmlComponents.Where(row => row.Enabled)) _ = Parse(row);
        if (requireConsent && plan.XmlComponents.Any(row => row.Enabled) && !plan.ConfirmAdditionalXml)
            throw new ConfigurationValidationException(nameof(InstallPlan.XmlComponents), "请先核对额外 XML 的设置、执行阶段及实际作用，并勾选确认；结构检查不能代替目标镜像验证。");
    }

    // Add independent child settings; never replace content managed by existing controls.
    public static void Apply(XmlDocument doc, InstallPlan plan)
    {
        foreach (var row in plan.XmlComponents.Where(row => row.Enabled))
        {
            var fragment = Parse(row);
            var groups = doc.DocumentElement!.ChildNodes.OfType<XmlElement>().Where(e => e.LocalName == "settings" && e.NamespaceURI == XmlMerge.Namespace && e.GetAttribute("pass") == row.Pass).ToArray();
            if (groups.Length > 1) throw new ArgumentException("额外 XML 的执行阶段重复，不能可靠添加。");
            if (groups.Length == 0)
            {
                var group = doc.CreateElement("settings", XmlMerge.Namespace); group.SetAttribute("pass", row.Pass); var extensions=doc.DocumentElement.ChildNodes.OfType<XmlElement>().FirstOrDefault(e=>e.NamespaceURI!=XmlMerge.Namespace); doc.DocumentElement.InsertBefore(group,extensions);
            }
            var target = NativeSetupXml.GetComponent(doc, row.Component, row.Pass, plan.Architecture);
            foreach (var child in fragment.DocumentElement!.ChildNodes.OfType<XmlElement>())
            {
                if (target.ChildNodes.OfType<XmlElement>().Any(e => e.LocalName == child.LocalName && e.NamespaceURI == child.NamespaceURI))
                    throw new ConfigurationValidationException(nameof(InstallPlan.XmlComponents), "额外 XML 与已有设置或其他条目重复：" + row.Pass + " / " + row.Component + " / " + child.LocalName + "。请通过原有设置修改，或移除重复的额外 XML。");
                target.AppendChild(doc.ImportNode(child, true));
            }
        }
    }

    public static void RedactPreview(XmlDocument preview, InstallPlan plan)
    {
        var ns = new XmlNamespaceManager(preview.NameTable); ns.AddNamespace("u", XmlMerge.Namespace);
        foreach (var row in plan.XmlComponents.Where(row => row.Enabled))
        {
            var fragment = Parse(row);
            foreach (var child in fragment.DocumentElement!.ChildNodes.OfType<XmlElement>())
                foreach (XmlElement value in preview.SelectNodes("/u:unattend/u:settings[@pass='" + row.Pass + "']/u:component[@name='" + row.Component + "']/u:" + child.LocalName, ns)!)
                {
                    value.RemoveAll(); value.InnerText = "[额外 XML 内容已遮蔽，请核对实际导出文件]";
                }
        }
    }
}
