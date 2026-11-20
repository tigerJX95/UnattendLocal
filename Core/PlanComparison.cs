using System.Reflection;
using System.Xml;
using System.Xml.Linq;
using UnattendLocal.Localization;

namespace UnattendLocal.Core;

public static class PlanComparison
{
    private static readonly HashSet<string> Metadata = new(StringComparer.Ordinal)
    {
        nameof(InstallPlan.SchemaVersion),
        nameof(InstallPlan.RecommendationId),
        nameof(InstallPlan.RecommendationVersion),
        nameof(InstallPlan.RecommendationWindows),
        nameof(InstallPlan.ConfirmAdditionalXml),
        nameof(InstallPlan.ConfirmImportedXmlRisk),
        nameof(InstallPlan.ConfirmDiskScript),
        nameof(InstallPlan.ConfirmFormatPartition),
        nameof(InstallPlan.ConfirmWipeDisk)
    };

    public static IReadOnlyList<string> FindChangedFields(InstallPlan current, InstallPlan reference)
    {
        var fields = new List<string>();
        foreach (var property in Fields<InstallPlan>.Scalar)
        {
            if (!Metadata.Contains(property.Name) && !Equals(property.GetValue(current), property.GetValue(reference)))
                fields.Add(property.Name);
        }

        if (!RowsEqual(current.Users, reference.Users)) fields.Add(nameof(InstallPlan.Users));
        if (!RowsEqual(current.Scripts, reference.Scripts)) fields.Add(nameof(InstallPlan.Scripts));
        if (!RowsEqual(current.XmlComponents, reference.XmlComponents)) fields.Add(nameof(InstallPlan.XmlComponents));
        if (!current.RemovedApps.ToHashSet(StringComparer.Ordinal).SetEquals(reference.RemovedApps))
            fields.Add(nameof(InstallPlan.RemovedApps));

        var currentSource = current.ImportedXml;
        var referenceSource = reference.ImportedXml;
        if ((currentSource is null) != (referenceSource is null) ||
            currentSource?.OriginalOmitted != referenceSource?.OriginalOmitted ||
            currentSource?.SourceXml != referenceSource?.SourceXml)
            fields.Add(nameof(InstallPlan.ImportedXml));

        return fields;
    }

    public static IReadOnlyList<string> FindChangedAnswerSections(string currentXml, string referenceXml)
    {
        XElement current = ReadAnswer(currentXml);
        XElement reference = ReadAnswer(referenceXml);
        XName settings = XName.Get("settings", XmlMerge.Namespace);
        var sections = new List<string>();
        foreach (var phase in AdditionalXmlSettings.Passes)
        {
            var currentSettings = current.Elements(settings).Where(node => (string?)node.Attribute("pass") == phase.Id).ToArray();
            var referenceSettings = reference.Elements(settings).Where(node => (string?)node.Attribute("pass") == phase.Id).ToArray();
            if (!currentSettings.SequenceEqual(referenceSettings, XNode.EqualityComparer))
                sections.Add(phase.Id);
            currentSettings.Remove();
            referenceSettings.Remove();
        }

        if (!XNode.DeepEquals(current, reference))
            sections.Add(UiText.Source("plan.compare.otherXml"));
        return sections;
    }

    private static XElement ReadAnswer(string xml)
    {
        using var reader = new XmlNodeReader(XmlMerge.Read(xml));
        var answer = XDocument.Load(reader).Root!;
        // Editable snapshots are XML comments. Compare installation content only.
        answer.DescendantNodes().OfType<XComment>().Remove();
        return answer;
    }

    private static bool RowsEqual<T>(IReadOnlyList<T> left, IReadOnlyList<T> right)
    {
        if (left.Count != right.Count) return false;
        for (int index = 0; index < left.Count; index++)
        {
            foreach (var property in Fields<T>.Scalar)
            {
                if (!Equals(property.GetValue(left[index]), property.GetValue(right[index]))) return false;
            }
        }
        return true;
    }

    private static class Fields<T>
    {
        internal static readonly PropertyInfo[] Scalar = typeof(T).GetProperties()
            .Where(p => p.CanRead && p.CanWrite && (p.PropertyType.IsValueType || p.PropertyType == typeof(string)))
            .OrderBy(p => p.Name, StringComparer.Ordinal)
            .ToArray();
    }
}
