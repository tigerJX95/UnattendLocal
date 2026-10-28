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
    [JsonIgnore] public string Label => global::UnattendLocal.Localization.UiText.Concat(new object?[] { global::UnattendLocal.Localization.UiText.Concat(new object?[] { (Enabled ? global::UnattendLocal.Localization.UiText.Source("text.ffb37c8f4af0") : global::UnattendLocal.Localization.UiText.Source("text.2737a5a13905")), Pass },new bool[] { false, false }), " · ", Component },new bool[]{false,false,false});
}

public static class AdditionalXmlSettings
{
    public const int MaxCount = 15, MaxLength = 131072;
    public static readonly string Example = global::UnattendLocal.Localization.UiText.Source("text.01c258b845e1");
    private static readonly Lazy<UnattendGenerator> Catalog = new(() => new());
    public static IReadOnlyList<string> Components => Catalog.Value.Components.Keys.Order(StringComparer.Ordinal).ToArray();
    public record PhaseChoice(string Id, string Label);
    public static PhaseChoice[] Passes => [new("windowsPE", global::UnattendLocal.Localization.UiText.Source("text.48694c417434")), new("offlineServicing", global::UnattendLocal.Localization.UiText.Source("text.1c69f09085d1")), new("generalize", global::UnattendLocal.Localization.UiText.Source("text.e0bfcb6182f5")), new("specialize", global::UnattendLocal.Localization.UiText.Source("text.74b1377ca623")), new("auditSystem", global::UnattendLocal.Localization.UiText.Source("text.b62eef04e8bd")), new("auditUser", global::UnattendLocal.Localization.UiText.Source("text.7df9ef1246ad")), new("oobeSystem", global::UnattendLocal.Localization.UiText.Source("text.6fe4b8c2d667"))];

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
            throw new ConfigurationValidationException(nameof(InstallPlan.XmlComponents), global::UnattendLocal.Localization.UiText.Source("text.88472f8d1d84"));
        if (string.IsNullOrWhiteSpace(row.Content) || row.Content.Length > MaxLength)
            throw new ConfigurationValidationException(nameof(InstallPlan.XmlComponents), global::UnattendLocal.Localization.UiText.Source("text.e2e5b70a48cc"));
        try
        {
            using var reader = XmlReader.Create(new StringReader("<root xmlns='" + XmlMerge.Namespace + "' xmlns:wcm='http://schemas.microsoft.com/WMIConfig/2002/State' xmlns:xsi='http://www.w3.org/2001/XMLSchema-instance'>" + row.Content + "</root>"),
                new XmlReaderSettings { DtdProcessing = DtdProcessing.Prohibit, XmlResolver = null, MaxCharactersInDocument = MaxLength + 1024 });
            var doc = new XmlDocument { XmlResolver = null }; doc.Load(reader);
            if(doc.SelectNodes("//processing-instruction()")!.Count>0)throw new ArgumentException(global::UnattendLocal.Localization.UiText.Source("text.bd1c5b82683e"));
            var pending=new Stack<(XmlElement Node,int Depth)>();pending.Push((doc.DocumentElement!,0));int count=0;
            while(pending.Count>0){var item=pending.Pop();if(item.Depth>64||++count>2048)throw new ArgumentException(global::UnattendLocal.Localization.UiText.Source("text.ca275b850bc5"));foreach(var child in item.Node.ChildNodes.OfType<XmlElement>())pending.Push((child,item.Depth+1));}
            var elements = doc.DocumentElement!.ChildNodes.OfType<XmlElement>().ToArray();
            if (elements.Length == 0 || doc.DocumentElement.ChildNodes.Cast<XmlNode>().Any(n => n is not (XmlElement or XmlComment or XmlWhitespace or XmlSignificantWhitespace) && !string.IsNullOrWhiteSpace(n.Value)))
                throw new ArgumentException(global::UnattendLocal.Localization.UiText.Source("text.59606a826069"));
            if (doc.SelectNodes("//*")!.Cast<XmlElement>().Any(e => e != doc.DocumentElement && (e.NamespaceURI != XmlMerge.Namespace || e.LocalName is "unattend" or "settings" or "component" or "Extensions")))
                throw new ArgumentException(global::UnattendLocal.Localization.UiText.Source("text.c1bf6135a33a"));
            if (elements.GroupBy(e => e.LocalName).Any(g => g.Count() > 1))
                throw new ArgumentException(global::UnattendLocal.Localization.UiText.Source("text.7fd7dde81ef1"));
            if(Managed.TryGetValue(row.Component ?? "",out var managed)&&elements.Any(element=>managed.Contains(element.LocalName)))
                throw new ArgumentException(global::UnattendLocal.Localization.UiText.Source("text.e5e224fffa61"));
            return doc;
        }
        catch (Exception e) when (e is XmlException or ArgumentException)
        {
            throw new ConfigurationValidationException(nameof(InstallPlan.XmlComponents), global::UnattendLocal.Localization.UiText.Concat(new object?[] { global::UnattendLocal.Localization.UiText.Source("text.3bfa38fae34b"), e.Message },new bool[]{false,false}));
        }
    }

    public static void Validate(InstallPlan plan, bool requireConsent)
    {
        if (plan.XmlComponents == null || plan.XmlComponents.Count > MaxCount || plan.XmlComponents.Any(row => row == null))
            throw new ConfigurationValidationException(nameof(InstallPlan.XmlComponents), global::UnattendLocal.Localization.UiText.Source("text.c21c4601cc0d"));
        foreach (var row in plan.XmlComponents.Where(row => row.Enabled)) _ = Parse(row);
        if (requireConsent && plan.XmlComponents.Any(row => row.Enabled) && !plan.ConfirmAdditionalXml)
            throw new ConfigurationValidationException(nameof(InstallPlan.XmlComponents), global::UnattendLocal.Localization.UiText.Source("text.a71066b92d40"));
    }

    // Add independent child settings; never replace content managed by existing controls.
    public static void Apply(XmlDocument doc, InstallPlan plan)
    {
        foreach (var row in plan.XmlComponents.Where(row => row.Enabled))
        {
            var fragment = Parse(row);
            var groups = doc.DocumentElement!.ChildNodes.OfType<XmlElement>().Where(e => e.LocalName == "settings" && e.NamespaceURI == XmlMerge.Namespace && e.GetAttribute("pass") == row.Pass).ToArray();
            if (groups.Length > 1) throw new ArgumentException(global::UnattendLocal.Localization.UiText.Source("text.6f30b6c91229"));
            if (groups.Length == 0)
            {
                var group = doc.CreateElement("settings", XmlMerge.Namespace); group.SetAttribute("pass", row.Pass); var extensions=doc.DocumentElement.ChildNodes.OfType<XmlElement>().FirstOrDefault(e=>e.NamespaceURI!=XmlMerge.Namespace); doc.DocumentElement.InsertBefore(group,extensions);
            }
            var target = NativeSetupXml.GetComponent(doc, row.Component, row.Pass, plan.Architecture);
            foreach (var child in fragment.DocumentElement!.ChildNodes.OfType<XmlElement>())
            {
                if (target.ChildNodes.OfType<XmlElement>().Any(e => e.LocalName == child.LocalName && e.NamespaceURI == child.NamespaceURI))
                    throw new ConfigurationValidationException(nameof(InstallPlan.XmlComponents), global::UnattendLocal.Localization.UiText.Concat(new object?[] { global::UnattendLocal.Localization.UiText.Source("text.985780fa809e"), row.Pass, " / ", row.Component, " / ", child.LocalName, global::UnattendLocal.Localization.UiText.Source("text.07a755a5147c") },new bool[]{false,false,false,false,false,true,false}));
                target.AppendChild(doc.ImportNode(child, true));
            }
        }
    }

    public static void RedactPreview(XmlDocument preview, InstallPlan plan, bool english = false)
    {
        var ns = new XmlNamespaceManager(preview.NameTable); ns.AddNamespace("u", XmlMerge.Namespace);
        foreach (var row in plan.XmlComponents.Where(row => row.Enabled))
        {
            var fragment = Parse(row);
            foreach (var child in fragment.DocumentElement!.ChildNodes.OfType<XmlElement>())
                foreach (XmlElement value in preview.SelectNodes("/u:unattend/u:settings[@pass='" + row.Pass + "']/u:component[@name='" + row.Component + "']/u:" + child.LocalName, ns)!)
                {
                    value.RemoveAll(); value.InnerText = english ? global::UnattendLocal.Localization.UiText.EnglishText(global::UnattendLocal.Localization.UiText.Source("text.9b148fe92613")) : global::UnattendLocal.Localization.UiText.Source("text.9b148fe92613");
                }
        }
    }
}
