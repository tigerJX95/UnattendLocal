using System.Xml;
namespace UnattendLocal.Core;

internal static class NativeSetupXml
{
    internal static XmlElement[] Components(XmlDocument doc, string name) => doc.SelectNodes("//*[local-name()='component' and @name='" + name + "']")!.Cast<XmlElement>().ToArray();
    internal static bool NoAttributes(XmlElement node) => node.Attributes.Cast<XmlAttribute>().All(a => a.NamespaceURI == "http://www.w3.org/2000/xmlns/");
    internal static bool NoSignificantText(XmlElement node) => node.ChildNodes.Cast<XmlNode>().Where(n => n is not XmlElement and not XmlComment).All(n => string.IsNullOrWhiteSpace(n.Value));
    internal static bool ValidComponent(XmlDocument doc, XmlElement c, string name, string pass)
    {
        if (c.NamespaceURI != XmlMerge.Namespace || c.ParentNode is not XmlElement s || s.LocalName != "settings" || s.NamespaceURI != XmlMerge.Namespace || s.ParentNode != doc.DocumentElement || s.GetAttribute("pass") != pass) return false;
        if (doc.DocumentElement!.ChildNodes.OfType<XmlElement>().Count(n => n.LocalName == "settings" && n.NamespaceURI == XmlMerge.Namespace && n.GetAttribute("pass") == pass) != 1) return false;
        if (c.GetAttribute("processorArchitecture") is not ("amd64" or "arm64" or "x86")) return false;
        var expected = new Dictionary<string, string> { ["name"] = name, ["processorArchitecture"] = c.GetAttribute("processorArchitecture"), ["publicKeyToken"] = "31bf3856ad364e35", ["language"] = "neutral", ["versionScope"] = "nonSxS" };
        return c.Attributes.Cast<XmlAttribute>().Where(a => a.NamespaceURI != "http://www.w3.org/2000/xmlns/").All(a => a.NamespaceURI.Length == 0 && expected.TryGetValue(a.Name, out var v) && v == a.Value)
            && expected.Keys.Where(k => k != "versionScope").All(c.HasAttribute);
    }
    internal static XmlElement GetComponent(XmlDocument doc, string name, string pass, string architecture)
    {
        var groups = doc.DocumentElement!.ChildNodes.OfType<XmlElement>().Where(e => e.LocalName == "settings" && e.NamespaceURI == XmlMerge.Namespace && e.GetAttribute("pass") == pass).ToArray();
        if (groups.Length != 1) throw new ArgumentException(global::UnattendLocal.Localization.UiText.Concat(new object?[] { global::UnattendLocal.Localization.UiText.Source("text.9984cc4e7538"), pass, global::UnattendLocal.Localization.UiText.Source("text.71692392a2ed") },new bool[]{false,false,false}));
        var group = groups[0];
        var components = group.ChildNodes.OfType<XmlElement>().Where(e => e.LocalName == "component" && e.NamespaceURI == XmlMerge.Namespace && e.GetAttribute("name") == name && e.GetAttribute("processorArchitecture") == architecture).ToArray();
        if (components.Length > 1) throw new ArgumentException(global::UnattendLocal.Localization.UiText.Concat(new object?[] { global::UnattendLocal.Localization.UiText.Source("text.61e90159559b"), name },new bool[]{false,true}));
        if (components.Length == 1) return components[0];
        var c = doc.CreateElement("component", XmlMerge.Namespace);
        foreach (var item in new Dictionary<string, string> { ["name"] = name, ["processorArchitecture"] = architecture, ["publicKeyToken"] = "31bf3856ad364e35", ["language"] = "neutral", ["versionScope"] = "nonSxS" }) c.SetAttribute(item.Key, item.Value);
        group.AppendChild(c); return c;
    }
    internal static XmlElement Add(XmlNode parent, string name, string? value = null)
    {
        var e = parent.OwnerDocument!.CreateElement(name, XmlMerge.Namespace);
        if (value != null) e.InnerText = value;
        parent.AppendChild(e); return e;
    }
}
