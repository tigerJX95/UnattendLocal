using System.Globalization;
using System.Xml;
namespace UnattendLocal.Core;

// Presentation only. Never changes the answer file or decides where Setup installs Windows.
public static class InstallationReview
{
    private sealed class ReviewCache { public ReviewCache() {} internal string? Source, Baseline; internal Target? Value; }
    private static readonly System.Runtime.CompilerServices.ConditionalWeakTable<ImportedXml,ReviewCache> Cache = new();
    public record Target(string Description, bool MayNeedSelection);
    private static Target Unspecified => new("未指定安装目标分区；安装时可能需要选择磁盘与分区。", true);
    public static Target FromXml(XmlDocument doc)
    {
        var ns = new XmlNamespaceManager(doc.NameTable); ns.AddNamespace("u", XmlMerge.Namespace);
        var images = doc.SelectNodes("/u:unattend/u:settings[@pass='windowsPE']/u:component[@name='Microsoft-Windows-Setup']/u:ImageInstall/u:OSImage", ns)!.Cast<XmlElement>().ToArray();
        if (images.Length > 1) return new("XML 含多个安装目标配置，请核对原文；不能确定是否需要人工选择。", true);
        bool automatic = AutoPartitionSettings.Inspect(doc).Known || doc.SelectNodes("//u:DiskConfiguration/u:Disk/u:CreatePartitions/u:CreatePartition", ns)!.Count > 0;
        if (images.Length == 0) return automatic ? new("配置包含自动分区，但未指定 Windows 安装目标；安装时可能需要选择目标分区。", true) : Unspecified;
        var image = images[0]; var targets = image.SelectNodes("u:InstallTo", ns)!; var available = image.SelectNodes("u:InstallToAvailablePartition", ns)!;
        bool autoTarget = available.Count == 1 && available[0]!.InnerText.Trim() is "true" or "1";
        bool validAvailable = available.Count <= 1 && (available.Count == 0 || available[0]!.InnerText.Trim() is "true" or "false" or "0" or "1");
        if (!validAvailable || targets.Count > 1 || autoTarget && targets.Count > 0)
            return new("安装目标配置重复或冲突，请核对原 XML；不能确定是否需要人工选择。", true);
        string partition = automatic ? "配置含自动分区操作。" : "此目标设置本身不代表自动分区；其他分区配置和脚本仍需核对。";
        if (autoTarget) return new("安装程序自动选择可用分区；" + partition, false);
        if (targets.Count == 1) {
            var target = (XmlElement)targets[0]!;
            var disks = target.SelectNodes("u:DiskID", ns)!; var parts = target.SelectNodes("u:PartitionID", ns)!;
            if (disks.Count == 1 && parts.Count == 1 && int.TryParse(disks[0]!.InnerText, NumberStyles.None, CultureInfo.InvariantCulture, out int disk) && disk >= 0 && int.TryParse(parts[0]!.InnerText, NumberStyles.None, CultureInfo.InvariantCulture, out int part) && part >= 1)
                return new($"已指定安装目标：磁盘 {disk} / 分区 {part}；" + partition, false);
            return new("安装目标配置不完整，请核对原 XML；不能确定是否需要人工选择。", true);
        }
        return automatic ? new("配置包含自动分区，但未指定 Windows 安装目标；安装时可能需要选择目标分区。", true) : Unspecified;
    }
    public static Target ForPlan(InstallPlan p)
    {
        if (p.DiskMode != InstallationSettings.Manual) {
            if (p.TargetDiskId is null or < 0) return new("安装目标尚未填写完整，请核对磁盘与分区。", true);
            if (p.DiskMode == InstallationSettings.Wipe)
                return new($"自动分区：清空磁盘 {p.TargetDiskId} 后按 {p.DiskLayout} 创建分区；Windows 目标为分区 {(p.DiskLayout == "UEFI / GPT" ? 3 : 2)}。", false);
            if (p.TargetPartitionId is null or < 1) return new("安装目标分区尚未填写完整。", true);
            return new($"已指定安装目标：磁盘 {p.TargetDiskId} / 分区 {p.TargetPartitionId}；" + (p.DiskMode == DiskScriptSettings.Mode ? "分区操作由自定义脚本决定，须核对全部命令。" : "使用已有分区，未选择自动分区。"), false);
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
                } catch (Exception e) when (e is ArgumentException or XmlException) { result = new("导入原文无法用于目标提示，请重新核对 XML。", true); }
                cache.Source=sourceXml;cache.Baseline=baselineXml;cache.Value=result;return result;
            }
        }
        if (p.ImportedXml?.OriginalOmitted == true) return new("导入原文未保存，须重新导入后核对安装目标。", true);
        return Unspecified;
    }
    public static string ExportSummary(byte[] bytes) => FromXml(XmlMerge.Read(System.Text.Encoding.UTF8.GetString(bytes))).Description;
}
