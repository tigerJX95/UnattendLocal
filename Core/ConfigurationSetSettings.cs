using System.Xml;
using System.Text;
using System.Text.Json;
using Schneegans.Unattend;
namespace UnattendLocal.Core;
public static class ConfigurationSetSettings
{
    public const string Description = "默认不勾选。启用后在 windowsPE 的 Microsoft-Windows-Setup 中生成 UseConfigurationSet=true，让 Windows Setup 使用配置集。按安装介质的标准布局准备 $OEM$，例如 $$ 对应 Windows 目录、$1 对应系统盘根目录；与 autounattend.xml 配套放置，具体搜索与复制行为由 Windows Setup 决定。本工具不创建、打包、检查或复制这些文件，也不会把应用安装器自动变成安装命令；需要执行的脚本仍应另行配置。复制可能覆盖同路径文件，介质内容和敏感资料需自行核对。导入的自定义 PE 安装脚本若自行复制 $OEM$，原文保留，不能通过此开关可靠控制。取消仅影响新配置，不删除已经复制到目标系统的文件。本工具不修改当前电脑。";
    public record State(bool Enabled, bool Ambiguous);
    private static XmlElement[] Nodes(XmlDocument doc) { var ns = new XmlNamespaceManager(doc.NameTable); ns.AddNamespace("u", XmlMerge.Namespace); return doc.SelectNodes("//u:settings[@pass='windowsPE']/u:component[@name='Microsoft-Windows-Setup']/u:UseConfigurationSet", ns)!.Cast<XmlElement>().ToArray(); }
    public static State Inspect(XmlDocument doc)
    {
        var nodes = Nodes(doc);
        bool scripted = doc.InnerText.Contains("ROBOCOPY_ARGS", StringComparison.OrdinalIgnoreCase) || doc.InnerText.Contains("Copying contents of %OEM_FOLDER%", StringComparison.OrdinalIgnoreCase);
        bool invalid = nodes.Length > 1 || nodes.Any(n => n.InnerText is not ("true" or "false" or "1" or "0"));
        return new(!scripted && !invalid && nodes.SingleOrDefault()?.InnerText is "true" or "1", scripted || invalid);
    }
    public static string Summary(InstallPlan p) => "$OEM$ 配置集：" + (p.UseConfigurationSet ? "启用（需另行准备介质文件，可能覆盖同路径内容）" : "不添加启用设置");
    public static void ReadIntoPlan(XmlDocument doc, InstallPlan p, List<string> notes) { var state = Inspect(doc); p.UseConfigurationSet = state.Enabled; if (state.Ambiguous) notes.Add("配置集设置或自定义 PE 复制流程未完整识别，原文保留；不能用此开关控制。"); }
    public static void ValidateImported(InstallPlan p, XmlDocument doc) { if (p.UseConfigurationSet && Inspect(doc).Ambiguous) throw new ArgumentException("原 XML 的配置集设置由未识别内容或自定义 PE 流程控制，不能覆盖；请用原工具调整。"); }
    public static void NormalizeLoaded(InstallPlan p, JsonElement json)
    {
        if (json.TryGetProperty(nameof(InstallPlan.UseConfigurationSet), out _) || p.ImportedXml is not { OriginalOmitted: false } data || data.SourceXml.Length == 0 || data.BaselineXml.Length == 0 || !Inspect(XmlMerge.Read(data.SourceXml)).Enabled) return;
        var before = JsonSerializer.Deserialize<InstallPlan>(JsonSerializer.Serialize(p))!; before.ImportedXml = null; before.ConfirmWipeDisk = before.ConfirmFormatPartition = before.ConfirmDiskScript = true;
        var after = JsonSerializer.Deserialize<InstallPlan>(JsonSerializer.Serialize(before))!; p.UseConfigurationSet = after.UseConfigurationSet = true;
        try { var g = new GeneratorService(); data.BaselineXml = XmlMerge.Apply(data.BaselineXml, Encoding.UTF8.GetString(g.GenerateImportBaseline(before)), XmlMerge.Read(Encoding.UTF8.GetString(g.GenerateImportBaseline(after)))).OuterXml; }
        catch (Exception e) when (e is ArgumentException or ConfigurationException or XmlException) { data.Blockers = data.Blockers.Append("旧方案的配置集编辑基准无法可靠迁移，请重新导入原 XML。").Distinct().ToArray(); }
    }
}
