using System.Globalization;
using System.Xml;
using UnattendLocal.Localization;

namespace UnattendLocal.Core;

internal static class DeploymentPhaseReview
{
    private const string Setup = ImageSelectionXml.SetupComponent;
    private const string Shell = "u:component[@name='Microsoft-Windows-Shell-Setup']";
    private const string LocalAccounts = Shell + "/u:UserAccounts/u:LocalAccounts/u:LocalAccount";
    private const string AutoLogon = Shell + "/u:AutoLogon";

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
            XmlNodeList Read(string path) => doc.SelectNodes(scope + path, ns)!;
            bool IsEnabled(string path) =>
                NativeSetupXml.ReadScalar(Read(path))?.Trim(' ', '\t', '\r', '\n') is "true" or "1";
            var notes = new List<string>();
            void Add(bool present, string key)
            {
                if (present) notes.Add(UiText.Source(key));
            }
            void AddCount(string path, string key)
            {
                int entries = Read(path).Count;
                if (entries > 0) notes.Add(UiText.Format(key, UiText.Arg(entries)));
            }

            if (phase.Id == "windowsPE")
            {
                bool peScript = doc.SelectSingleNode(
                    "/u:unattend/s:Extensions/s:Build/s:PEScriptCopy[normalize-space(.) != '']", ns) != null;
                Add(peScript, "review.phases.peScript");
                Add(Has(Setup + "/u:DiskConfiguration") || (!peScript && AutoPartitionSettings.Inspect(doc).Known), "review.phases.disk");
                Add(Has(Setup + "/u:DiskConfiguration/u:Disk/u:WillWipeDisk[normalize-space(.)='true' or normalize-space(.)='1']"), "review.phases.wipeRequested");
                AddCount(Setup + "/u:DiskConfiguration/u:Disk/u:CreatePartitions/u:CreatePartition", "review.phases.createPartitions");
                if (DescribeImageSelection(ImageSelectionXml.Read(doc, ns)) is string imageSelection)
                    notes.Add(imageSelection);
                Add(Has(ImageSelectionXml.OsImage + "/u:InstallFrom/u:Path"), "review.phases.imagePath");
                Add(Has("u:component[@name='Microsoft-Windows-PnpCustomizationsWinPE']/u:DriverPaths/*"), "review.phases.peDrivers");
                Add(Has(Setup + "/u:UseConfigurationSet[normalize-space(.)='true' or normalize-space(.)='1']"), "review.phases.configurationSet");
                Add(Has(Setup + "/u:DynamicUpdate/u:Enable[normalize-space(.)='true' or normalize-space(.)='1']"), "review.phases.dynamicUpdate");
                Add(Has(Setup + "/u:LogPath[normalize-space(.) != '']"), "review.phases.logPath");
            }
            if (phase.Id == "oobeSystem")
            {
                Add(IsEnabled(Shell + "/u:OOBE/u:HideWirelessSetupInOOBE"), "review.phases.oobeNetwork");
                Add(IsEnabled(Shell + "/u:OOBE/u:HideOnlineAccountScreens"), "review.phases.oobeSignIn");
            }
            Add(Has("u:component[@name='Microsoft-Windows-PnpCustomizationsNonWinPE']/u:DriverPaths/*"), "review.phases.offlineDrivers");
            AddCount("u:component/u:RunSynchronous/u:RunSynchronousCommand", "review.phases.commands");
            AddCount("u:component/u:RunAsynchronous/u:RunAsynchronousCommand", "review.phases.asyncCommands");
            Add(phase.Id == "specialize" && hasAppLocker, "review.phases.appLocker");
            Add(Has(Shell + "/u:ComputerName[normalize-space(.) != '' and normalize-space(.) != '*']"), "review.phases.computerName");
            Add(Has(Shell + "/u:TimeZone[normalize-space(.) != '']"), "review.phases.timeZone");
            var accounts = Read(LocalAccounts);
            if (accounts.Count > 0)
            {
                int administrators = 0, users = 0;
                foreach (XmlElement account in accounts)
                {
                    // Recognize Windows group values without copying imported group names.
                    string? group = NativeSetupXml.ReadScalar(account.SelectNodes("u:Group", ns)!)?.Trim();
                    if (string.Equals(group, "Administrators", StringComparison.OrdinalIgnoreCase)) administrators++;
                    if (string.Equals(group, "Users", StringComparison.OrdinalIgnoreCase)) users++;
                }
                notes.Add(UiText.Format("review.phases.localAccounts", UiText.Arg(accounts.Count), UiText.Arg(administrators),
                    UiText.Arg(users), UiText.Arg(accounts.Count - administrators - users)));
            }
            if (IsEnabled(AutoLogon + "/u:Enabled"))
            {
                string? total = NativeSetupXml.ReadScalar(Read(AutoLogon + "/u:LogonCount"))?.Trim();
                notes.Add(int.TryParse(total, NumberStyles.None, CultureInfo.InvariantCulture, out int logons)
                    ? UiText.Format("review.phases.autoLogonCount", UiText.Arg(logons))
                    : UiText.Source("review.share.autoLogon"));
            }
            AddCount(Shell + "/u:FirstLogonCommands/u:SynchronousCommand", "review.phases.firstLogon");
            AddCount(Shell + "/u:LogonCommands/u:AsynchronousCommand", "review.phases.logonCommands");

            var row = UiText.Format("review.phases.row", phase.Id, phase.Label, UiText.Arg(count));
            if (notes.Count > 0)
                row = UiText.Concat([row, "\n", UiText.Join(" · ", notes)], [false, false, false]);
            lines.Add(row);
        }
        if (lines.Count == 1) lines.Add(UiText.Source("review.phases.empty"));
        lines.Add(UiText.Source("review.phases.scope"));
        return UiText.Join("\n", lines);
    }

    private static string? DescribeImageSelection(ImageSelectionXml.Selection selection) => selection.Method switch
    {
        ImageSelectionXml.Kind.Missing => null,
        ImageSelectionXml.Kind.Index => UiText.Format("review.phases.imageByIndex", UiText.Arg(selection.Index)),
        ImageSelectionXml.Kind.Name => UiText.Source("review.phases.imageByName"),
        ImageSelectionXml.Kind.Description => UiText.Source("review.phases.imageByDescription"),
        _ => UiText.Source("review.phases.image")
    };
}
