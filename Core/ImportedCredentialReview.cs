using System.Text;
using System.Xml;
using UnattendLocal.Localization;

namespace UnattendLocal.Core;

internal static class ImportedCredentialReview
{
    private const string Shell = "/u:unattend/u:settings/u:component[@name='Microsoft-Windows-Shell-Setup']";
    private const string Accounts = Shell + "/u:UserAccounts/u:LocalAccounts/u:LocalAccount";
    private const string Join = "/u:unattend/u:settings/u:component[@name='Microsoft-Windows-UnattendedJoin']";
    private static readonly string[] Masks = ["text.a87e466de86e", "text.069d51fb67fa", "text.ce9d3225eb28", "text.b8de0a664760",
        "text.abf82a99491a", "text.838cc513a5e2", "text.c67ed423c4cf"];

    private static string? Text(XmlNode node, string path, XmlNamespaceManager ns) => node.SelectSingleNode(path, ns)?.InnerText;
    internal static bool IsRedacted(string text)
    {
        string value = text.Trim();
        return value.Equals("[REDACTED]", StringComparison.OrdinalIgnoreCase) ||
            Masks.Any(key => value == UiText.Source(key) || value == UiText.EnglishResources[key]);
    }

    // On failure, preserve the original text so it can be inspected without decoding it as a password.
    internal static bool ReadPassword(XmlElement? element, XmlNamespaceManager ns, out string value, out bool obscure)
    {
        value = element == null ? "" : Text(element, "u:Value", ns) ?? "";
        string? plain = element == null ? null : Text(element, "u:PlainText", ns);
        obscure = plain is not ("true" or "1");
        if (IsRedacted(value)) return false;
        if (value.Length == 0 || !obscure) return true;
        try
        {
            string decoded = new UnicodeEncoding(false, false, true).GetString(Convert.FromBase64String(value));
            if (!decoded.EndsWith(element!.LocalName, StringComparison.Ordinal)) return false;
            decoded = decoded[..^element.LocalName.Length];
            if (IsRedacted(decoded) || decoded.Any(c => !XmlConvert.IsXmlChar(c) && !char.IsSurrogate(c))) return false;
            value = decoded;
            return true;
        }
        catch (FormatException) { return false; }
        catch (DecoderFallbackException) { return false; }
    }

    internal static string[] FindErrors(XmlDocument document, XmlNamespaceManager ns)
    {
        XmlElement[] Nodes(string path) => document.SelectNodes(path, ns)!.Cast<XmlElement>().ToArray();
        bool InvalidPassword(XmlElement element) => !ReadPassword(element, ns, out _, out _);
        var errors = new List<string>();
        var emptyAccounts = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        bool invalidLocalPassword = false;
        foreach (var account in Nodes(Accounts))
        {
            var password = account.SelectSingleNode("u:Password", ns) as XmlElement;
            if (!ReadPassword(password, ns, out string value, out _)) invalidLocalPassword = true;
            else if (value.Length == 0 && Text(account, "u:Name", ns) is string name) emptyAccounts.Add(name);
        }
        if (invalidLocalPassword) errors.Add(UiText.Source("import.credentials.local"));
        var knownBuiltin = new Lazy<bool>(() => FirstLogonSettings.Inspect(document).Known);
        bool EmptyPassword(XmlElement? element) => ReadPassword(element, ns, out string value, out _) && value.Length == 0;
        if (Nodes(Shell + "/u:UserAccounts/u:AdministratorPassword").Any(node =>
            InvalidPassword(node) || EmptyPassword(node) && !knownBuiltin.Value))
            errors.Add(UiText.Source("import.credentials.administrator"));
        bool UnmatchedEmptyAutoLogon(XmlElement auto)
        {
            if (!EmptyPassword(auto.SelectSingleNode("u:Password", ns) as XmlElement)) return false;
            string? name = Text(auto, "u:Username", ns);
            if (name == "Administrator" && knownBuiltin.Value) return false;
            return name == null || !emptyAccounts.Contains(name);
        }
        if (Nodes(Shell + "/u:AutoLogon/u:Password").Any(InvalidPassword) ||
            Nodes(Shell + "/u:AutoLogon[u:Enabled='true' or u:Enabled='1'][u:Username]").Any(UnmatchedEmptyAutoLogon))
            errors.Add(UiText.Source("import.credentials.autoLogon"));
        if (Nodes(Join + "//u:Credentials/u:Password | " + Join + "//u:MachinePassword | " + Join + "//u:Provisioning/u:AccountData")
            .Any(node => string.IsNullOrWhiteSpace(node.InnerText) || IsRedacted(node.InnerText)))
            errors.Add(UiText.Source("import.credentials.domain"));
        if (Nodes("//u:component[@name='Microsoft-Windows-PnpCustomizationsWinPE']//u:Credentials/u:Password").Any(node => IsRedacted(node.InnerText)))
            errors.Add(UiText.Source("import.credentials.driver"));
        if (Nodes("//u:component[@name='Microsoft-Windows-Setup']/u:UserData/u:ProductKey/u:Key")
            .Any(node => !GeneratorService.HasProductKeyFormat(node.InnerText.Trim())))
            errors.Add(UiText.Source("import.credentials.installKey"));
        if (Nodes(Shell + "/u:ProductKey").Any(node => !GeneratorService.HasProductKeyFormat(node.InnerText.Trim())))
            errors.Add(UiText.Source("import.credentials.activationKey"));
        if (Nodes("//*[local-name()='File']").Any(file => WifiSettings.HasUnusableImportedKey(file) ||
            file.GetAttribute("path").EndsWith("\\Wifi.xml", StringComparison.OrdinalIgnoreCase) &&
            (string.IsNullOrWhiteSpace(file.InnerText) || IsRedacted(file.InnerText))))
            errors.Add(UiText.Source("import.credentials.wifi"));
        return errors.ToArray();
    }

    internal static void SeedEmptyPasswords(XmlDocument baseline, XmlDocument source, InstallPlan plan, XmlNamespaceManager ns)
    {
        // An omitted or cleared password must be replaced against its actual source shape.
        // Use an empty password in the internal baseline only. The editable plan still needs consent.
        void Seed(XmlElement target, XmlElement original, string name)
        {
            var existing = target.SelectSingleNode("u:" + name, ns);
            var actual = original.SelectSingleNode("u:" + name, ns);
            if (existing == null) return;
            if (actual == null) target.RemoveChild(existing);
            else target.ReplaceChild(baseline.ImportNode(actual, true), existing);
        }

        var emptyUsers = plan.Users.Where(user => user.NeedsPassword).Select(user => user.Name).ToHashSet(StringComparer.Ordinal);
        if (emptyUsers.Count == 0 && !plan.NeedsBuiltinAdministratorPassword) return;
        var originals = source.SelectNodes(Accounts, ns)!.Cast<XmlElement>().ToLookup(node => Text(node, "u:Name", ns), StringComparer.Ordinal);
        foreach (XmlElement target in baseline.SelectNodes(Accounts, ns)!)
        {
            string? name = Text(target, "u:Name", ns);
            if (name != null && emptyUsers.Contains(name) && originals[name].ToArray() is [var original])
                Seed(target, original, "Password");
        }
        var generatedAuto = baseline.SelectNodes(Shell + "/u:AutoLogon", ns)!.Cast<XmlElement>().ToArray();
        var sourceAuto = source.SelectNodes(Shell + "/u:AutoLogon", ns)!.Cast<XmlElement>().ToArray();
        if (generatedAuto is [var auto] && sourceAuto is [var actualAuto])
        {
            string? username = Text(auto, "u:Username", ns);
            if (username != null && username == Text(actualAuto, "u:Username", ns) &&
                (emptyUsers.Contains(username) || plan.NeedsBuiltinAdministratorPassword && username == "Administrator"))
                Seed(auto, actualAuto, "Password");
        }
        if (!plan.NeedsBuiltinAdministratorPassword) return;
        var generatedAccounts = baseline.SelectNodes(Shell + "/u:UserAccounts", ns)!.Cast<XmlElement>().ToArray();
        var sourceAccounts = source.SelectNodes(Shell + "/u:UserAccounts", ns)!.Cast<XmlElement>().ToArray();
        if (generatedAccounts is [var accountRoot] && sourceAccounts is [var actualAccounts])
            Seed(accountRoot, actualAccounts, "AdministratorPassword");
    }
}
