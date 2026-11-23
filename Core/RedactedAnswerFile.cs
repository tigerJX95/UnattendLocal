using System.Globalization;
using System.Text;
using System.Xml;
using UnattendLocal.Localization;

namespace UnattendLocal.Core;

public static class RedactedAnswerFile
{
    public const string FileName = "unattend-diagnostic.xml";
    private const string DiagnosticRoot = "UnattendLocalDiagnostic";
    private const string DiagnosticNamespace = "urn:unattendlocal:diagnostic-xml:v1";
    private const string Marker = "UnattendLocal:diagnostic-xml:v1";
    private const string Redacted = "[REDACTED]";
    private const string Unattend = XmlMerge.Namespace;
    private const string Wcm = "http://schemas.microsoft.com/WMIConfig/2002/State";
    private const string Setup = "Microsoft-Windows-Setup";
    private const string Shell = "Microsoft-Windows-Shell-Setup";
    private const string Deployment = "Microsoft-Windows-Deployment";
    private const string DriversPe = "Microsoft-Windows-PnpCustomizationsWinPE";
    private const string DriversOffline = "Microsoft-Windows-PnpCustomizationsNonWinPE";
    private const string LocalAccounts = "UserAccounts/LocalAccounts/LocalAccount/";
    private const string OsImage = "ImageInstall/OSImage/";
    private const string Disk = "DiskConfiguration/Disk/";

    private static readonly IReadOnlyDictionary<string, string> SetupFields = new Dictionary<string, string>
    {
        [OsImage + "InstallTo/DiskID"] = "number",
        [OsImage + "InstallTo/PartitionID"] = "number",
        [OsImage + "InstallToAvailablePartition"] = "boolean",
        [OsImage + "WillShowUI"] = "Always|OnError|Never",
        [OsImage + "InstallFrom/Path"] = "redact",
        [OsImage + "InstallFrom/MetaData/Key"] = "/IMAGE/INDEX|/IMAGE/NAME|/IMAGE/DESCRIPTION",
        [OsImage + "InstallFrom/MetaData/Value"] = "image",
        [Disk + "DiskID"] = "number",
        [Disk + "WillWipeDisk"] = "boolean",
        [Disk + "CreatePartitions/CreatePartition/Order"] = "number",
        [Disk + "CreatePartitions/CreatePartition/Size"] = "number",
        [Disk + "CreatePartitions/CreatePartition/Extend"] = "boolean",
        [Disk + "CreatePartitions/CreatePartition/Type"] = "EFI|MSR|Primary|Extended|Logical",
        [Disk + "ModifyPartitions/ModifyPartition/Order"] = "number",
        [Disk + "ModifyPartitions/ModifyPartition/PartitionID"] = "number",
        [Disk + "ModifyPartitions/ModifyPartition/Active"] = "boolean",
        [Disk + "ModifyPartitions/ModifyPartition/Extend"] = "boolean",
        [Disk + "ModifyPartitions/ModifyPartition/Format"] = "NTFS|FAT32",
        [Disk + "ModifyPartitions/ModifyPartition/Label"] = "redact",
        [Disk + "ModifyPartitions/ModifyPartition/Letter"] = "letter",
        [Disk + "ModifyPartitions/ModifyPartition/TypeID"] = "partitionType",
        ["DiskConfiguration/WillShowUI"] = "Always|OnError|Never",
        ["UseConfigurationSet"] = "boolean",
        ["DynamicUpdate/Enable"] = "boolean",
        ["DynamicUpdate/WillShowUI"] = "Always|OnError|Never",
        ["UserData/AcceptEula"] = "boolean",
        ["UserData/ProductKey/WillShowUI"] = "Always|OnError|Never",
        ["UserData/ProductKey/Key"] = "installationKey",
        ["LogPath"] = "redact",
    };
    private static readonly IReadOnlyDictionary<string, string> ShellFields = new Dictionary<string, string>
    {
        ["ComputerName"] = "computer",
        ["TimeZone"] = "timeZone",
        ["ProductKey"] = "redact",
        ["OOBE/HideEULAPage"] = "boolean",
        ["OOBE/HideLocalAccountScreen"] = "boolean",
        ["OOBE/HideOEMRegistrationScreen"] = "boolean",
        ["OOBE/HideOnlineAccountScreens"] = "boolean",
        ["OOBE/HideWirelessSetupInOOBE"] = "boolean",
        ["OOBE/ProtectYourPC"] = "number",
        [LocalAccounts + "Name"] = "account",
        [LocalAccounts + "DisplayName"] = "displayName",
        [LocalAccounts + "Group"] = "Administrators|Users",
        [LocalAccounts + "Password/Value"] = "empty",
        [LocalAccounts + "Password/PlainText"] = "boolean",
        ["UserAccounts/AdministratorPassword/Value"] = "empty",
        ["UserAccounts/AdministratorPassword/PlainText"] = "boolean",
        ["AutoLogon/Enabled"] = "boolean",
        ["AutoLogon/Username"] = "account",
        ["AutoLogon/Domain"] = "redact",
        ["AutoLogon/LogonCount"] = "number",
        ["AutoLogon/Password/Value"] = "empty",
        ["AutoLogon/Password/PlainText"] = "boolean",
        ["FirstLogonCommands/SynchronousCommand/Order"] = "number",
        ["FirstLogonCommands/SynchronousCommand/CommandLine"] = "redact",
        ["LogonCommands/AsynchronousCommand/Order"] = "number",
        ["LogonCommands/AsynchronousCommand/CommandLine"] = "redact",
    };
    private static readonly IReadOnlyDictionary<string, string> CommandFields = new Dictionary<string, string>
    {
        ["RunSynchronous/RunSynchronousCommand/Order"] = "number",
        ["RunSynchronous/RunSynchronousCommand/Path"] = "redact",
        ["RunSynchronous/RunSynchronousCommand/WillReboot"] = "Always|OnRequest|Never",
        ["RunAsynchronous/RunAsynchronousCommand/Order"] = "number",
        ["RunAsynchronous/RunAsynchronousCommand/Path"] = "redact",
    };
    private static readonly IReadOnlyDictionary<string, string> DriverFields = new Dictionary<string, string>
    {
        ["DriverPaths/PathAndCredentials/Path"] = "redact",
        ["DriverPaths/PathAndCredentials/Credentials/Domain"] = "redact",
        ["DriverPaths/PathAndCredentials/Credentials/Username"] = "redact",
        ["DriverPaths/PathAndCredentials/Credentials/Password"] = "redact",
    };
    private static readonly IReadOnlyDictionary<string, string> LocaleFields = new Dictionary<string, string>
    {
        ["InputLocale"] = "inputLocale",
        ["SystemLocale"] = "locale",
        ["UILanguage"] = "language",
        ["UILanguageFallback"] = "language",
        ["UserLocale"] = "locale",
        ["SetupUILanguage/UILanguage"] = "language",
        ["SetupUILanguage/WillShowUI"] = "Always|OnError|Never",
    };
    private static readonly Dictionary<string, IReadOnlyDictionary<string, string>> Fields = new()
    {
        [Setup] = SetupFields.Concat(CommandFields).ToDictionary(),
        [Shell] = ShellFields,
        [Deployment] = CommandFields,
        [DriversPe] = DriverFields,
        [DriversOffline] = DriverFields,
        ["Microsoft-Windows-International-Core"] = LocaleFields,
        ["Microsoft-Windows-International-Core-WinPE"] = LocaleFields,
    };

    private static XmlComment? DiagnosticMarker(XmlDocument doc) =>
        doc.DocumentElement!.ChildNodes.OfType<XmlComment>().FirstOrDefault(comment => comment.Value == Marker);

    internal static bool IsDiagnostic(XmlDocument doc) => DiagnosticMarker(doc) != null;

    internal static XmlDocument ReadForImport(string xml)
    {
        var doc = XmlMerge.Parse(xml);
        if (doc.DocumentElement is { LocalName: DiagnosticRoot, NamespaceURI: DiagnosticNamespace } diagnostic &&
            diagnostic.ChildNodes.OfType<XmlElement>().ToArray() is [var answerFile] && XmlMerge.IsAnswerFile(answerFile))
        {
            diagnostic.RemoveChild(answerFile);
            doc.ReplaceChild(answerFile, diagnostic);
            if (!IsDiagnostic(doc))
                answerFile.PrependChild(doc.CreateComment(Marker));
        }
        return XmlMerge.RequireAnswerFile(doc);
    }

    internal static bool HasPlaceholders(XmlDocument doc) =>
        doc.DocumentElement!.SelectNodes(".//*[not(*)]")!.Cast<XmlElement>()
            .Any(element => string.Equals(element.InnerText.Trim(), Redacted, StringComparison.OrdinalIgnoreCase));

    internal static void FinishDeployment(XmlDocument doc)
    {
        if (!IsDiagnostic(doc)) return;
        if (HasPlaceholders(doc)) throw new ArgumentException(UiText.Source("review.diagnostic.placeholders"));
        foreach (var marker in doc.DocumentElement!.ChildNodes.OfType<XmlComment>().Where(comment => comment.Value == Marker).ToArray())
            doc.DocumentElement.RemoveChild(marker);
    }

    public static byte[] Create(byte[] generatedXml, GeneratorService service)
    {
        var source = XmlMerge.Read(Encoding.UTF8.GetString(generatedXml));
        var output = new XmlDocument();
        // Only the import entry point unwraps diagnostics. Windows Setup expects a native root.
        var diagnostic = output.CreateElement(DiagnosticRoot, DiagnosticNamespace);
        output.AppendChild(diagnostic);
        var root = output.CreateElement("unattend", Unattend);
        diagnostic.AppendChild(root);

        var ns = new XmlNamespaceManager(source.NameTable);
        ns.AddNamespace("u", Unattend);
        var accountNames = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        string Alias(string name)
        {
            if (!accountNames.TryGetValue(name, out string? alias))
                accountNames[name] = alias = "TestUser" + (accountNames.Count + 1).ToString(CultureInfo.InvariantCulture);
            return alias;
        }
        foreach (XmlElement name in source.SelectNodes("//u:LocalAccount/u:Name", ns)!)
            Alias(name.InnerText);

        var zones = service.TimeZones.Select(choice => choice.Id).ToHashSet(StringComparer.Ordinal);
        var installationKeys = service.Editions.Select(edition => service.GenericInstallationKey(edition.Id)).ToHashSet(StringComparer.Ordinal);
        var systemCatalog = new Schneegans.Unattend.UnattendGenerator();
        var languages = systemCatalog.ImageLanguages.Keys.ToHashSet(StringComparer.Ordinal);
        var locales = systemCatalog.UserLocales.Keys.ToHashSet(StringComparer.Ordinal);
        var localeIds = systemCatalog.UserLocales.Values.Select(locale => locale.LCID).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var keyboards = service.Keyboards.Select(choice => choice.Id).ToHashSet(StringComparer.Ordinal);
        bool KnownInputLocale(string value)
        {
            // IME identifiers already contain a colon, so recognize catalog entries first.
            if (keyboards.Contains(value) || languages.Contains(value)) return true;
            string[] parts = value.Split(':', 2);
            return parts is [var locale, var layout] && localeIds.Contains(locale) && keyboards.Contains(layout);
        }
        string? SafeValue(XmlElement element, string rule, string path)
        {
            string value = element.InnerText.Trim();
            // Preserve only these public partition type identifiers. A custom GUID is an input value.
            return rule switch
            {
                "number" => Number(value),
                "boolean" => value is "true" or "1" ? "true" : value is "false" or "0" ? "false" : null,
                "letter" => value.Length == 1 && value[0] is >= 'A' and <= 'Z' ? value : null,
                "partitionType" => value.Equals("de94bba4-06d1-4d40-a16a-bfd50179d6ac", StringComparison.OrdinalIgnoreCase) ||
                    value is "0x27" or "0x07" or "0xEF" ? value : Redacted,
                "empty" => "",
                "redact" => Redacted,
                "computer" => value == "*" ? "*" : "TestPC",
                "account" => path == "AutoLogon/Username" && value == "Administrator" &&
                    !accountNames.ContainsKey(value) ? "Administrator" : Alias(value),
                "displayName" => element.ParentNode!.SelectSingleNode("u:Name", ns) is XmlElement name
                    ? Alias(name.InnerText) : null,
                "image" => element.ParentNode!.SelectSingleNode("u:Key", ns)?.InnerText.Trim() == "/IMAGE/INDEX"
                    ? Number(value) ?? Redacted : Redacted,
                "installationKey" => installationKeys.Contains(value) ? value : Redacted,
                "timeZone" => zones.Contains(value) ? value : null,
                "language" => languages.Contains(value) ? value : null,
                "locale" => locales.Contains(value) ? value : null,
                "inputLocale" => value.Split(';').All(KnownInputLocale) ? value : null,
                _ => rule.Split('|').Contains(value, StringComparer.Ordinal) ? value : null,
            };
        }
        void CopyFields(XmlElement from, XmlElement to, string prefix, IReadOnlyDictionary<string, string> rules)
        {
            foreach (var child in from.ChildNodes.OfType<XmlElement>().Where(node => node.NamespaceURI == Unattend))
            {
                string path = prefix + child.LocalName;
                var entry = output.CreateElement(child.LocalName, Unattend);
                if (rules.TryGetValue(path, out string? rule))
                {
                    string? value = SafeValue(child, rule, path);
                    if (value == null) continue;
                    entry.InnerText = value;
                }
                else
                {
                    string next = path + "/";
                    if (!rules.Keys.Any(key => key.StartsWith(next, StringComparison.Ordinal))) continue;
                    CopyFields(child, entry, next, rules);
                    if (!entry.HasChildNodes) continue;
                }
                CopyListAttributes(child, entry);
                to.AppendChild(entry);
            }
        }
        foreach (XmlElement phase in source.SelectNodes("/u:unattend/u:settings", ns)!)
        {
            string pass = phase.GetAttribute("pass");
            if (!AdditionalXmlSettings.Passes.Any(choice => choice.Id == pass)) continue;
            var copiedPhase = NativeSetupXml.Add(root, "settings");
            copiedPhase.SetAttribute("pass", pass);
            foreach (XmlElement component in phase.SelectNodes("u:component", ns)!)
            {
                string name = component.GetAttribute("name");
                string architecture = component.GetAttribute("processorArchitecture");
                if (!AdditionalXmlSettings.SupportsPass(name, pass) || architecture is not ("amd64" or "arm64" or "x86")) continue;
                var copied = NativeSetupXml.Add(copiedPhase, "component");
                copied.SetAttribute("name", name);
                copied.SetAttribute("processorArchitecture", architecture);
                copied.SetAttribute("publicKeyToken", "31bf3856ad364e35");
                copied.SetAttribute("language", "neutral");
                copied.SetAttribute("versionScope", "nonSxS");
                if (Fields.TryGetValue(name, out var rules)) CopyFields(component, copied, "", rules);
            }
        }
        using var stream = new MemoryStream();
        using (var writer = XmlWriter.Create(stream, new XmlWriterSettings { Encoding = new UTF8Encoding(false), Indent = true }))
            output.Save(writer);
        return stream.ToArray();
    }

    private static string? Number(string value) => uint.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out uint number)
        ? number.ToString(CultureInfo.InvariantCulture) : null;

    private static void CopyListAttributes(XmlElement from, XmlElement to)
    {
        void Set(string name, string value)
        {
            var attribute = to.OwnerDocument!.CreateAttribute("wcm", name, Wcm);
            attribute.Value = value;
            to.Attributes.Append(attribute);
        }
        string action = from.GetAttribute("action", Wcm);
        if (action is "add" or "remove" or "modify") Set("action", action);
        if (Number(from.GetAttribute("keyValue", Wcm)) is string key) Set("keyValue", key);
    }
}
