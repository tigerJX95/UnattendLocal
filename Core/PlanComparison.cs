using System.Reflection;

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
