using System.Text;
using System.Text.Json;
using System.Xml;
using Schneegans.Unattend;
namespace UnattendLocal.Core;

public static class ProcessAuditSettings
{
    public const string Default = "遵循 Windows 默认设置";
    public const string Enabled = "启用审计（不配置命令行记录）";
    public const string WithCommandLine = "启用审计并记录命令行";
    public static IReadOnlyList<string> Modes { get; } = new[] { Default, Enabled, WithCommandLine };
    public const string AuditCommand = "auditpol.exe /set /subcategory:\"{0CCE922B-69AE-11D9-BED3-505054503030}\" /success:enable /failure:enable;";
    public const string CommandLineCommand = "reg.exe add \"HKLM\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Policies\\System\\Audit\" /v ProcessCreationIncludeCmdLine_Enabled /t REG_DWORD /d 1 /f;";
    public const string Description = "在目标 Windows 的 specialize 阶段沿用原生成器设置进程创建审计，启用后可在安全日志查看进程创建事件（4688）。只启用审计时，不写入命令行记录策略覆盖；已有镜像或组织策略仍可能记录命令行。选择记录命令行时，参数会明文写入安全日志，可能包含密码、密钥或令牌，有权读取安全日志的人可查看；日志量也可能增加。不是屏幕录制或程序运行报告。默认不配置，不修改当前电脑。切回默认仅影响新生成配置，不关闭已安装系统的审计、不删除已有日志。";
    public static IProcessAuditSettings Build(InstallPlan p) => p.ProcessAuditMode switch
    {
        Default => new DisabledProcessAuditSettings(),
        Enabled => new EnabledProcessAuditSettings(false),
        WithCommandLine => new EnabledProcessAuditSettings(true),
        _ => throw new ArgumentException("请选择有效的进程创建审计模式。")
    };
    public static string ModeSummary(InstallPlan p) => p.ProcessAuditMode switch
    {
        Default => "遵循 Windows 默认设置（不写入覆盖）",
        Enabled => "启用进程创建审计；命令行记录遵循已有策略",
        WithCommandLine => "启用进程创建审计并明文记录命令行参数",
        _ => "模式无效，请重新选择"
    };
    public static string Summary(InstallPlan p) => "进程创建审计：" + ModeSummary(p);
    private static string Normal(string body) => string.Join('\n', body.Replace("\r\n", "\n").Split('\n').Select(l => l.Trim()).Where(l => l.Length > 0));
    public record Source(string Mode, bool Ambiguous);
    public static Source Inspect(XmlDocument doc)
    {
        var blocks = SystemMaintenanceSettings.KnownSpecializeBlocks(doc);
        var files = doc.SelectNodes("//*[local-name()='File']")!.Cast<XmlElement>();
        var ns = new XmlNamespaceManager(doc.NameTable); ns.AddNamespace("u", XmlMerge.Namespace);
        var paths = doc.SelectNodes("//u:settings[@pass='specialize']//u:Path", ns)!.Cast<XmlElement>();
        string[] markers = ["0CCE922B-69AE-11D9-BED3-505054503030", "ProcessCreationIncludeCmdLine", "Process Creation", "进程创建"];
        var relevant = files.SelectMany(f => Normal(f.InnerText).Split('\n')).Concat(paths.Select(p => p.InnerText)).Where(l => markers.Any(m => l.Contains(m, StringComparison.OrdinalIgnoreCase))).ToArray();
        bool audit = blocks.Count(b => b == AuditCommand) == 1 && relevant.Count(l => l == AuditCommand) == 1;
        bool commandLine = blocks.Count(b => b == CommandLineCommand) == 1 && relevant.Count(l => l == CommandLineCommand) == 1;
        bool complete = audit && relevant.Length == (commandLine ? 2 : 1);
        return new(complete ? (commandLine ? WithCommandLine : Enabled) : Default, relevant.Length > 0 && !complete);
    }
    public static void ReadIntoPlan(XmlDocument doc, InstallPlan p, List<string> notes)
    {
        var state = Inspect(doc); p.ProcessAuditMode = state.Mode;
        if (state.Ambiguous) notes.Add("进程创建审计命令或执行管线未完整识别，原文保留；不能用该设置覆盖。");
    }
    public static void ValidateImported(InstallPlan p, XmlDocument doc)
    {
        if (p.ProcessAuditMode != Default && Inspect(doc).Ambiguous)
            throw new ArgumentException("进程创建审计与原 XML 中未完整识别的设置重叠，请用原工具调整后重新导入。");
    }
    public static void NormalizeLoaded(InstallPlan p, JsonElement json)
    {
        if (json.TryGetProperty(nameof(InstallPlan.ProcessAuditMode), out _) || p.ImportedXml is not { OriginalOmitted: false } data || data.SourceXml.Length == 0 || data.BaselineXml.Length == 0) return;
        var state = Inspect(XmlMerge.Read(data.SourceXml));
        var before = JsonSerializer.Deserialize<InstallPlan>(JsonSerializer.Serialize(p))!;
        before.ImportedXml = null; before.ConfirmWipeDisk = before.ConfirmFormatPartition = before.ConfirmDiskScript = true;
        p.ProcessAuditMode = state.Mode;
        if (state.Ambiguous) data.Notices = data.Notices.Append("进程创建审计未完整识别，原文保留，不能覆盖。").Distinct().ToArray();
        if (state.Mode == Default) return;
        var after = JsonSerializer.Deserialize<InstallPlan>(JsonSerializer.Serialize(before))!; after.ProcessAuditMode = state.Mode;
        try
        {
            var service = new GeneratorService();
            data.BaselineXml = XmlMerge.Apply(data.BaselineXml, Encoding.UTF8.GetString(service.GenerateStandalone(before)), XmlMerge.Read(Encoding.UTF8.GetString(service.GenerateStandalone(after)))).OuterXml;
        }
        catch (Exception e) when (e is ArgumentException or ConfigurationException or XmlException)
        {
            data.Blockers = data.Blockers.Append("旧方案的进程审计编辑基准无法可靠迁移，请重新导入原 XML 后再导出。").Distinct().ToArray();
        }
    }
}
