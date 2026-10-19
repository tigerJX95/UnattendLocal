using System.Text.Json;
using System.Text.RegularExpressions;
using System.Xml;
namespace UnattendLocal.Core;

public static class PeLogSettings
{
    private const string Component = "Microsoft-Windows-Setup";
    public const string ReadOnlyNotice = "原 XML 的 Windows PE 日志目录或 PE 流程未完整识别，原文保留；日志目录区域只读，不能用默认值覆盖。";
    private const string MigrationBlocker = "旧方案的 PE 日志目录编辑基准无法可靠迁移，请重新导入原 XML；当前待处理修改保留。";
    public const string Description = "默认不指定日志目录。启用后设置 Microsoft-Windows-Setup/LogPath，仅记录 windowsPE 阶段相关事件，不改变安装后系统的 Panther 日志位置，也不是本程序日志。须填写目标电脑固定磁盘上的本地绝对目录，例如 D:\\SetupLogs，最长 259 字符；不支持 UNC、变量、设备路径或文件名。本工具不检查当前电脑目录，也不保证目标路径可写或盘符相同。请提前准备可写目录，避免被清空/格式化的磁盘或分区；本工具拒绝常见 PE 内存盘 X:。修改此项会取消已有磁盘风险确认。日志可能含设备、路径和部署信息，分享前自行检查；本功能不复制、上传或自动清理日志。自定义 PE/DISM 流程及未知日志设置只读保留。";
    public record Source(bool Present, bool Known, string Path, string Architecture = "amd64");
    private static XmlElement[] Nodes(XmlDocument d) => d.SelectNodes("//*[local-name()='LogPath']")!.Cast<XmlElement>().ToArray();
    public static bool CanEdit(InstallPlan p) => p.ImportedXml?.PeLogReadOnly != true;
    public static void ValidatePath(string? path)
    {
        if (path == null || path.Length is < 3 or > 259 || path != path.Trim() || !Regex.IsMatch(path, @"\A[A-Za-z]:\\", RegexOptions.CultureInvariant) || path.StartsWith("X:", StringComparison.OrdinalIgnoreCase) || path.Any(char.IsControl) || path.Any(c => "\"<>|?*%/".Contains(c)) || path[2..].Contains(':'))
            throw new ArgumentException("PE 日志须为最长 259 字符的本地盘符绝对目录；不支持 UNC、相对路径、变量、设备路径或 PE 内存盘 X:。");
        try { XmlConvert.VerifyXmlChars(path); } catch (XmlException) { throw new ArgumentException("PE 日志目录含 XML 非法字符。"); }
        var parts = path[3..].Split('\\');
        for (int i = 0; i < parts.Length; i++)
        {
            string s = parts[i]; if (s.Length == 0 && i == parts.Length - 1) continue;
            if (s.Length is 0 or > 255 || s is "." or ".." || s.EndsWith(' ') || s.EndsWith('.') || Regex.IsMatch(s.Split('.')[0], @"\A(?:CON|PRN|AUX|NUL|COM[1-9]|LPT[1-9])\z", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)) throw new ArgumentException("PE 日志目录含相对层级、空目录段、保留设备名、超长目录名或末尾空格/点。");
        }
        if (new[] { ".log", ".txt", ".etl" }.Any(e => path.EndsWith(e, StringComparison.OrdinalIgnoreCase))) throw new ArgumentException("请填写保存 PE 日志的目录，不要填写单个日志文件。");
    }
    public static void Validate(InstallPlan p) { if (p.UsePeLogPath) ValidatePath(p.PeLogPath); }
    public static string Summary(InstallPlan p) => !CanEdit(p) ? "PE 日志目录：原 XML 只读保留，请核对原文。" : p.UsePeLogPath ? "PE 日志目录：" + p.PeLogPath + "（仅 windowsPE）" : "PE 日志目录：遵循安装程序默认位置。";
    public static void Apply(XmlDocument doc, InstallPlan p)
    {
        Validate(p); if (!p.UsePeLogPath) return;
        if (Nodes(doc).Length > 0 || SetupDriverSettings.CustomPe(doc)) throw new ArgumentException("生成基准已有日志目录或自定义 PE 流程，不能重复添加 LogPath。");
        var components = NativeSetupXml.Components(doc, Component);
        if (components.Length != 1 || !NativeSetupXml.ValidComponent(doc, components[0], Component, "windowsPE") || components[0].GetAttribute("processorArchitecture") != p.Architecture) throw new ArgumentException("缺少唯一匹配的 Windows Setup 组件，不能指定 PE 日志目录。");
        NativeSetupXml.Add(components[0], "LogPath", p.PeLogPath);
    }
    public static Source Inspect(XmlDocument doc)
    {
        var nodes = Nodes(doc); var unknown = new Source(nodes.Length > 0, false, "");
        if (SetupDriverSettings.CustomPe(doc)) return unknown;
        if (nodes.Length == 0) return new(false, true, "");
        var components = NativeSetupXml.Components(doc, Component);
        if (nodes.Length != 1 || components.Length != 1) return unknown;
        var n = nodes[0]; var c = components[0];
        if (n.NamespaceURI != XmlMerge.Namespace || n.ParentNode != c || !NativeSetupXml.NoAttributes(n) || n.ChildNodes.OfType<XmlElement>().Any() || !NativeSetupXml.ValidComponent(doc, c, Component, "windowsPE")) return unknown;
        try { ValidatePath(n.InnerText); } catch (ArgumentException) { return unknown; }
        return new(true, true, n.InnerText, c.GetAttribute("processorArchitecture"));
    }
    private static void Assign(InstallPlan p, Source source) { p.UsePeLogPath = source.Known && source.Present; if (p.UsePeLogPath) p.PeLogPath = source.Path; }
    public static Source ReadIntoPlan(XmlDocument d, InstallPlan p, List<string> notes) { var s = Inspect(d); Assign(p, s); if (!s.Known) notes.Add(ReadOnlyNotice); return s; }
    public static void UpdateImportState(ImportedXml data, Source s) { data.PeLogReadOnly = !s.Known; data.PeLogImportVersion = 1; }
    public static void ValidateImported(InstallPlan p, XmlDocument source) { if (p.UsePeLogPath && !Inspect(source).Known) throw new ArgumentException(ReadOnlyNotice); }
    public static void NormalizeLoaded(InstallPlan p, JsonElement root)
    {
        if (p.ImportedXml is not { OriginalOmitted: false, SourceXml.Length: > 0 } data) return;
        var state = Inspect(XmlMerge.Read(data.SourceXml));
        if (data.PeLogImportVersion < 1 && state is { Present: true, Known: true })
        {
            bool missing = !root.TryGetProperty(nameof(p.UsePeLogPath), out _) && !root.TryGetProperty(nameof(p.PeLogPath), out _);
            if (missing && data.BaselineXml.Length > 0 && Nodes(XmlMerge.Read(data.BaselineXml)).Length == 0)
            {
                try { var before = XmlMerge.Read(data.BaselineXml); Apply(before, new InstallPlan { Architecture = state.Architecture, UsePeLogPath = true, PeLogPath = state.Path }); Assign(p, state); data.BaselineXml = before.OuterXml; }
                catch (ArgumentException) { data.Blockers = data.Blockers.Append(MigrationBlocker).Distinct().ToArray(); }
            }
            else data.Blockers = data.Blockers.Append(MigrationBlocker).Distinct().ToArray();
        }
        data.Notices = data.Notices.Where(n => n != ReadOnlyNotice).ToArray(); if (!state.Known) data.Notices = [..data.Notices, ReadOnlyNotice]; UpdateImportState(data, state);
    }
}
