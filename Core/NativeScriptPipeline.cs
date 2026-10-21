using System.Globalization;
using System.Xml;

namespace UnattendLocal.Core;

// Native Order, rather than XML sibling order, defines execution. Recognition
// must fail closed when extraction or the default-user hive chain is inactive.
internal static class NativeScriptPipeline
{
    private const string Root = @"C:\Windows\Setup\Scripts\";
    private static XmlNamespaceManager Ns(XmlDocument doc)
    {
        var ns = new XmlNamespaceManager(doc.NameTable); ns.AddNamespace("u", XmlMerge.Namespace); return ns;
    }
    internal static XmlElement[] OrderedCommands(XmlDocument doc, string pass)
    {
        var ns = Ns(doc);
        string query = pass == "oobeSystem"
            ? "//u:settings[@pass='oobeSystem']/u:component[@name='Microsoft-Windows-Shell-Setup']/u:FirstLogonCommands/u:SynchronousCommand"
            : "//u:settings[@pass='" + pass + "']/u:component[@name='Microsoft-Windows-Deployment']/u:RunSynchronous/u:RunSynchronousCommand";
        var nodes = doc.SelectNodes(query, ns)!.Cast<XmlElement>().ToArray();
        var orders = nodes.Select(n => n.SelectNodes("u:Order", ns)!).ToArray();
        if (orders.Any(n => n.Count != 1)) return [];
        var values = orders.Select(n => int.TryParse(n[0]!.InnerText, NumberStyles.None, CultureInfo.InvariantCulture, out int order) ? order : 0).ToArray();
        if (values.Any(v => v < 1 || (pass != "oobeSystem" && v > 500)) || values.Distinct().Count() != values.Length) return [];
        return nodes.Zip(values).OrderBy(x => x.Second).Select(x => x.First).ToArray();
    }
    internal static string Text(XmlElement command) => command.ChildNodes.OfType<XmlElement>().FirstOrDefault(n => n.NamespaceURI == XmlMerge.Namespace && n.LocalName is "Path" or "CommandLine")?.InnerText ?? "";
    internal static bool StageActive(XmlDocument doc, string name)
    {
        var commands = OrderedCommands(doc, "specialize");
        var extract = commands.Select((n, i) => (Text: Text(n), Index: i)).Where(n => n.Text.Contains("Extensions.ExtractScript", StringComparison.OrdinalIgnoreCase)).ToArray();
        if (extract.Length != 1) return false;
        if (name == "UserOnce.ps1") return StageActive(doc, "DefaultUser.ps1");
        string pass = name == "FirstLogon.ps1" ? "oobeSystem" : "specialize";
        var execution = pass == "specialize" ? commands : OrderedCommands(doc, pass);
        var invoke = execution.Select((n, i) => (Text: Text(n), Index: i)).Where(n => n.Text.Contains(Root + name, StringComparison.OrdinalIgnoreCase)).ToArray();
        if (invoke.Length != 1 || (pass == "specialize" && extract[0].Index >= invoke[0].Index)) return false;
        if (name == "DefaultUser.ps1")
        {
            var hive = commands.Select((n, i) => (Text: Text(n), Index: i)).Where(n => n.Text.Contains(@"HKU\DefaultUser", StringComparison.OrdinalIgnoreCase)).ToArray();
            if (hive.Length != 2 || !hive[0].Text.StartsWith("reg.exe load ", StringComparison.OrdinalIgnoreCase) || !hive[1].Text.StartsWith("reg.exe unload ", StringComparison.OrdinalIgnoreCase)
                || hive[0].Index >= invoke[0].Index || invoke[0].Index >= hive[1].Index || extract[0].Index >= hive[0].Index) return false;
        }
        return true;
    }
}
