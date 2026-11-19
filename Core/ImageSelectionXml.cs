using System.Globalization;
using System.Xml;

namespace UnattendLocal.Core;

internal static class ImageSelectionXml
{
    internal const string SetupComponent = "u:component[@name='Microsoft-Windows-Setup']";
    internal const string OsImage = SetupComponent + "/u:ImageInstall/u:OSImage";
    private const string Wcm = "http://schemas.microsoft.com/WMIConfig/2002/State";

    internal enum Kind { Missing, Index, Name, Description, Unrecognized }

    internal readonly record struct Selection(Kind Method, int Index = 0, string Name = "")
    {
        internal bool IsReadOnly => Method is Kind.Description or Kind.Unrecognized;
    }

    internal static Selection Read(XmlDocument doc, XmlNamespaceManager ns)
    {
        const string imagePath = "/u:unattend/u:settings[@pass='windowsPE']/" + OsImage;
        const string installFromPath = imagePath + "/u:InstallFrom";
        var entries = doc.SelectNodes(installFromPath + "/u:MetaData", ns)!;
        if (entries.Count == 0) return new(Kind.Missing);

        if (entries.Count != 1 || doc.SelectNodes(imagePath, ns)!.Count != 1 ||
            doc.SelectNodes(installFromPath, ns)!.Count != 1 || entries[0] is not XmlElement metadata ||
            metadata.GetAttributeNode("action", Wcm) is { Value: not "add" } || !NativeSetupXml.NoSignificantText(metadata) ||
            metadata.ChildNodes.OfType<XmlElement>().Count() != 2)
            return new(Kind.Unrecognized);

        string? key = NativeSetupXml.ReadScalar(metadata.SelectNodes("u:Key", ns)!);
        string? value = NativeSetupXml.ReadScalar(metadata.SelectNodes("u:Value", ns)!);
        if (string.IsNullOrWhiteSpace(value)) return new(Kind.Unrecognized);

        return key switch
        {
            "/IMAGE/INDEX" when int.TryParse(value.Trim(' ', '\t', '\r', '\n'), NumberStyles.None,
                CultureInfo.InvariantCulture, out int index) && index > 0 =>
                new(Kind.Index, Index: index),
            "/IMAGE/NAME" when IsEditableName(value) => new(Kind.Name, Name: value),
            "/IMAGE/DESCRIPTION" => new(Kind.Description),
            _ => new(Kind.Unrecognized)
        };
    }

    internal static bool IsEditableName(string? name) =>
        !string.IsNullOrWhiteSpace(name) && name.Length <= 256 && !name.Any(char.IsControl);
}
