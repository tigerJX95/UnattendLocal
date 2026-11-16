using System.Xml;
using UnattendLocal.Localization;

namespace UnattendLocal.Core;

internal static class DeploymentPhaseReview
{
    private const string Setup = "u:component[@name='Microsoft-Windows-Setup']";
    private const string Shell = "u:component[@name='Microsoft-Windows-Shell-Setup']";

    public static string Describe(XmlDocument doc, bool hasAppLocker)
    {
        var ns = new XmlNamespaceManager(doc.NameTable);
        ns.AddNamespace("u", XmlMerge.Namespace);
        ns.AddNamespace("s", "https://schneegans.de/windows/unattend-generator/");
        var lines = new List<string> { UiText.Source("review.phases.title") };
        foreach (var phase in AdditionalXmlSettings.Passes)
        {
            string scope = "/u:unattend/u:settings[@pass='" + phase.Id + "']/";
            int count = doc.SelectNodes(scope + "u:component", ns)!.Count;
            if (count == 0) continue;

            bool Has(string path) => doc.SelectSingleNode(scope + path, ns) != null;
            var notes = new List<string>();
            void Add(bool present, string key)
            {
                if (present) notes.Add(UiText.Source(key));
            }

            if (phase.Id == "windowsPE")
            {
                bool peScript = doc.SelectSingleNode(
                    "/u:unattend/s:Extensions/s:Build/s:PEScriptCopy[normalize-space(.) != '']", ns) != null;
                Add(peScript, "review.phases.peScript");
                Add(Has(Setup + "/u:DiskConfiguration") || (!peScript && AutoPartitionSettings.Inspect(doc).Known), "review.phases.disk");
                Add(Has(Setup + "/u:ImageInstall/u:OSImage/u:InstallFrom/u:MetaData"), "review.phases.image");
                Add(Has(Setup + "/u:ImageInstall/u:OSImage/u:InstallFrom/u:Path"), "review.phases.imagePath");
                Add(Has("u:component[@name='Microsoft-Windows-PnpCustomizationsWinPE']/u:DriverPaths/*"), "review.phases.peDrivers");
                Add(Has(Setup + "/u:UseConfigurationSet[normalize-space(.)='true' or normalize-space(.)='1']"), "review.phases.configurationSet");
                Add(Has(Setup + "/u:DynamicUpdate/u:Enable[normalize-space(.)='true' or normalize-space(.)='1']"), "review.phases.dynamicUpdate");
                Add(Has(Setup + "/u:LogPath[normalize-space(.) != '']"), "review.phases.logPath");
            }
            Add(Has("u:component[@name='Microsoft-Windows-PnpCustomizationsNonWinPE']/u:DriverPaths/*"), "review.phases.offlineDrivers");
            Add(Has("u:component/u:RunSynchronous/u:RunSynchronousCommand"), "review.phases.commands");
            Add(phase.Id == "specialize" && hasAppLocker, "review.phases.appLocker");
            Add(Has(Shell + "/u:UserAccounts/u:LocalAccounts/u:LocalAccount"), "review.phases.localAccounts");
            Add(Has(Shell + "/u:FirstLogonCommands/u:SynchronousCommand"), "review.phases.firstLogon");

            var row = UiText.Format("review.phases.row", phase.Id, phase.Label, UiText.Arg(count));
            if (notes.Count > 0)
                row = UiText.Concat([row, "\n", UiText.Join(" · ", notes)], [false, false, false]);
            lines.Add(row);
        }
        if (lines.Count == 1) lines.Add(UiText.Source("review.phases.empty"));
        lines.Add(UiText.Source("review.phases.scope"));
        return UiText.Join("\n", lines);
    }
}
