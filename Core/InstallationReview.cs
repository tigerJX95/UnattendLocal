using System.Globalization;
using System.Xml;
namespace UnattendLocal.Core;

// Presentation only. Never changes the answer file or decides where Setup installs Windows.
public static class InstallationReview
{
    private sealed class ReviewCache { public ReviewCache() {} internal string? Source, Baseline; internal Target? Value; }
    private static readonly System.Runtime.CompilerServices.ConditionalWeakTable<ImportedXml,ReviewCache> Cache = new();
    public record Target(string Description, bool MayNeedSelection);
    private static Target Unspecified => new(global::UnattendLocal.Localization.UiText.Source("text.551f5c31a0e6"), true);
    public static Target FromXml(XmlDocument doc)
    {
        var ns = new XmlNamespaceManager(doc.NameTable); ns.AddNamespace("u", XmlMerge.Namespace);
        var images = doc.SelectNodes("/u:unattend/u:settings[@pass='windowsPE']/u:component[@name='Microsoft-Windows-Setup']/u:ImageInstall/u:OSImage", ns)!.Cast<XmlElement>().ToArray();
        if (images.Length > 1) return new(global::UnattendLocal.Localization.UiText.Source("text.cfd8e60d2e33"), true);
        bool automatic = AutoPartitionSettings.Inspect(doc).Known || doc.SelectNodes("//u:DiskConfiguration/u:Disk/u:CreatePartitions/u:CreatePartition", ns)!.Count > 0;
        if (images.Length == 0) return automatic ? new(global::UnattendLocal.Localization.UiText.Source("text.d5a9d230f460"), true) : Unspecified;
        var image = images[0]; var targets = image.SelectNodes("u:InstallTo", ns)!; var available = image.SelectNodes("u:InstallToAvailablePartition", ns)!;
        bool autoTarget = available.Count == 1 && available[0]!.InnerText.Trim() is "true" or "1";
        bool validAvailable = available.Count <= 1 && (available.Count == 0 || available[0]!.InnerText.Trim() is "true" or "false" or "0" or "1");
        if (!validAvailable || targets.Count > 1 || autoTarget && targets.Count > 0)
            return new(global::UnattendLocal.Localization.UiText.Source("text.4ba51c6459c9"), true);
        string partition = automatic ? global::UnattendLocal.Localization.UiText.Source("text.aae97e3c3ff9") : global::UnattendLocal.Localization.UiText.Source("text.3088508267ed");
        if (autoTarget) return new(global::UnattendLocal.Localization.UiText.Concat(new object?[] { global::UnattendLocal.Localization.UiText.Source("text.18dcf7a99fa7"), partition },new bool[]{false,false}), false);
        if (targets.Count == 1) {
            var target = (XmlElement)targets[0]!;
            var disks = target.SelectNodes("u:DiskID", ns)!; var parts = target.SelectNodes("u:PartitionID", ns)!;
            if (disks.Count == 1 && parts.Count == 1 && int.TryParse(disks[0]!.InnerText, NumberStyles.None, CultureInfo.InvariantCulture, out int disk) && disk >= 0 && int.TryParse(parts[0]!.InnerText, NumberStyles.None, CultureInfo.InvariantCulture, out int part) && part >= 1)
                return new(global::UnattendLocal.Localization.UiText.Concat(new object?[] { global::UnattendLocal.Localization.UiText.Format("message.f0ff8c59d0c0", new string[] { global::UnattendLocal.Localization.UiText.Arg((disk), 0, null), global::UnattendLocal.Localization.UiText.Arg((part), 0, null) }), partition },new bool[]{false,false}), false);
            return new(global::UnattendLocal.Localization.UiText.Source("text.6f66b4a3555d"), true);
        }
        return automatic ? new(global::UnattendLocal.Localization.UiText.Source("text.d5a9d230f460"), true) : Unspecified;
    }
    public static Target ForPlan(InstallPlan p)
    {
        if (p.DiskMode != InstallationSettings.Manual) {
            if (p.TargetDiskId is null or < 0) return new(global::UnattendLocal.Localization.UiText.Source("text.9c48322114b0"), true);
            if (p.DiskMode == InstallationSettings.Wipe)
                return new(global::UnattendLocal.Localization.UiText.Format("message.ec690dda702d", new string[] { global::UnattendLocal.Localization.UiText.Arg((p.TargetDiskId), 0, null), global::UnattendLocal.Localization.UiText.Arg((p.DiskLayout), 0, null), global::UnattendLocal.Localization.UiText.Arg(((p.DiskLayout == "UEFI / GPT" ? 3 : 2)), 0, null) }), false);
            if (p.TargetPartitionId is null or < 1) return new(global::UnattendLocal.Localization.UiText.Source("text.01b46e091353"), true);
            return new(global::UnattendLocal.Localization.UiText.Concat(new object?[] { global::UnattendLocal.Localization.UiText.Format("message.f0ff8c59d0c0", new string[] { global::UnattendLocal.Localization.UiText.Arg((p.TargetDiskId), 0, null), global::UnattendLocal.Localization.UiText.Arg((p.TargetPartitionId), 0, null) }), (p.DiskMode == DiskScriptSettings.Mode ? global::UnattendLocal.Localization.UiText.Source("text.58d81744fe7d") : global::UnattendLocal.Localization.UiText.Source("text.e1f2ffe5eb63")) },new bool[]{false,false}), false);
        }
        if (p.ImportedXml is { OriginalOmitted: false } data && data.SourceXml.Length > 0 && data.BaselineXml.Length > 0) {
            var cache=Cache.GetValue(data,static _=>new ReviewCache());
            lock(cache) {
                string sourceXml=data.SourceXml,baselineXml=data.BaselineXml;
                if (ReferenceEquals(cache.Source,sourceXml) && ReferenceEquals(cache.Baseline,baselineXml) && cache.Value != null) return cache.Value;
                Target result;
                try {
                    var source = XmlMerge.Read(sourceXml); var baseline = XmlMerge.Read(baselineXml);
                    // An owned target is removed when its editor is switched to manual.
                    // A source-only target outside the editor remains in the actual XML.
                    var ns = new XmlNamespaceManager(baseline.NameTable); ns.AddNamespace("u", XmlMerge.Namespace);
                    result = baseline.SelectNodes("//u:OSImage/u:InstallTo | //u:OSImage/u:InstallToAvailablePartition[text()='true' or text()='1']", ns)!.Count == 0 ? FromXml(source) : Unspecified;
                } catch (Exception e) when (e is ArgumentException or XmlException) { result = new(global::UnattendLocal.Localization.UiText.Source("text.9cb0c8aa5953"), true); }
                cache.Source=sourceXml;cache.Baseline=baselineXml;cache.Value=result;return result;
            }
        }
        if (p.ImportedXml?.OriginalOmitted == true) return new(global::UnattendLocal.Localization.UiText.Source("text.07919e9655f6"), true);
        return Unspecified;
    }
    public static string ExportSummary(byte[] bytes) => FromXml(XmlMerge.Read(System.Text.Encoding.UTF8.GetString(bytes))).Description;
}
