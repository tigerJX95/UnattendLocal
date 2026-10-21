using System.Text;
using System.Text.Json;
using System.Xml;
using Schneegans.Unattend;
namespace UnattendLocal.Core;

public static class ComputerNamingSettings
{
    public const string Existing = "Existing", Prompt = "Prompt", Script = "Script";
    public const string Example = "return 'DESKTOP-{0:D3}' -f ( Get-Random -Minimum 0 -Maximum 999 );";
    private static readonly Lazy<string> PromptText = new(() => { using var stream = typeof(ComputerNamingSettings).Assembly.GetManifestResourceStream("UnattendLocal.Core.Resources.ComputerNamePrompt.ps1")!; using var reader = new StreamReader(stream, Encoding.UTF8, true); return reader.ReadToEnd(); });
    public static string PromptScript => PromptText.Value;
    private const string Root = @"C:\Windows\Setup\Scripts\";
    public static IComputerNameSettings Build(InstallPlan p)
    {
        if (p.ComputerNameMode == Existing) return string.IsNullOrEmpty(p.ComputerName) ? new RandomComputerNameSettings() : new CustomComputerNameSettings(p.ComputerName);
        if (p.ComputerNameMode == Prompt)
        {
            return new ScriptComputerNameSettings(PromptScript);
        }
        if (p.ComputerNameMode != Script) throw new ArgumentException("请选择有效的计算机命名方式。");
        if (string.IsNullOrWhiteSpace(p.ComputerNameScript) || p.ComputerNameScript.Length > 65536) throw new ArgumentException("请填写计算机命名脚本，最多 65,536 个字符；淡色示例不参与生成。");
        return new ScriptComputerNameSettings(p.ComputerNameScript);
    }
    public const string Description = "固定名称留空时由 Windows 自动生成，继续兼容既有方案。安装时输入或脚本生成沿用原生脚本命名流程：安装阶段暂用 TEMPNAME，再读取脚本返回的名称并应用。输入模式使用独立对话框，可配合隐藏 PowerShell；无效或取消输入时继续提示，需完成输入才能继续。返回值应为一个有效名称：1–15 字符，不使用空白、纯数字或 Windows 不允许的符号，也不应与本地账户同名。脚本结果在目标安装时才能确定，本工具不执行脚本，不能预先保证名称有效。脚本需自行处理失败、网络及设备信息；不要把密码或令牌写入脚本。取消仅影响新生成配置，不重命名当前电脑或已安装系统。";
    public static string Summary(InstallPlan p) => "计算机名：" + (p.ComputerNameMode switch
    {
        Existing => string.IsNullOrEmpty(p.ComputerName) ? "由 Windows 自动生成" : p.ComputerName,
        Prompt => "安装时通过对话框输入",
        Script => "安装时运行命名脚本（不在本机执行）",
        _ => "命名方式无效"
    });
    private static string Normal(string text) => string.Join('\n', text.Replace("\r\n", "\n").Split('\n').Select(l => l.Trim()).Where(l => l.Length > 0));
    private record Template(string Setter, string Block);
    private static readonly Lazy<Template> Expected = new(() =>
    {
        var doc = new UnattendGenerator().GenerateXml(Configuration.Default with { ComputerNameSettings = new ScriptComputerNameSettings(Example) });
        return new(Normal(Files(doc, "SetComputerName.ps1").Single().InnerText), SystemMaintenanceSettings.KnownSpecializeBlocks(doc).Single(b => b.Contains("GetComputerName.ps1", StringComparison.Ordinal)));
    });
    private static XmlElement[] Files(XmlDocument doc, string name) => doc.SelectNodes("//*[local-name()='File']")!.Cast<XmlElement>().Where(f => f.GetAttribute("path").Equals(Root + name, StringComparison.OrdinalIgnoreCase)).ToArray();
    private static string Name(XmlDocument doc) { var ns = new XmlNamespaceManager(doc.NameTable); ns.AddNamespace("u", XmlMerge.Namespace); return doc.SelectSingleNode("//u:settings[@pass='specialize']/u:component[@name='Microsoft-Windows-Shell-Setup']/u:ComputerName", ns)?.InnerText ?? ""; }
    public record State(bool Enabled, bool Ambiguous, string Getter);
    public static State Inspect(XmlDocument doc)
    {
        var getter = Files(doc, "GetComputerName.ps1"); var setter = Files(doc, "SetComputerName.ps1");
        var files = doc.SelectNodes("//*[local-name()='File']")!.Cast<XmlElement>().ToArray();
        var ns = new XmlNamespaceManager(doc.NameTable); ns.AddNamespace("u", XmlMerge.Namespace);
        var lines = files.Except(getter).Except(setter).SelectMany(f => Normal(f.InnerText).Split('\n')).Concat(doc.SelectNodes("//u:Path|//u:CommandLine", ns)!.Cast<XmlElement>().Select(n => n.InnerText));
        bool Related(string l) => new[] { "GetComputerName.ps1", "SetComputerName.ps1", "ComputerName.txt" }.Any(m => l.Contains(m, StringComparison.OrdinalIgnoreCase));
        var refs = lines.Where(Related).ToArray(); var expected = Expected.Value; var expectedRefs = expected.Block.Split('\n').Where(Related).ToArray();
        bool mentioned = getter.Length + setter.Length > 0 || refs.Length > 0;
        bool enabled = getter.Length == 1 && !string.IsNullOrWhiteSpace(getter[0].InnerText) && getter[0].InnerText.Length <= 65536 && setter.Length == 1 && Normal(setter[0].InnerText) == expected.Setter && Name(doc) == "TEMPNAME" && SystemMaintenanceSettings.KnownSpecializeBlocks(doc).Count(b => b == expected.Block) == 1 && refs.Length == expectedRefs.Length && expectedRefs.All(l => refs.Count(v => v == l) == 1);
        return new(enabled, mentioned && !enabled, enabled ? getter[0].InnerText : "");
    }
    public static void ReadIntoPlan(XmlDocument doc, InstallPlan p, List<string> notes)
    {
        var state = Inspect(doc); p.ComputerNameMode = Existing; p.ComputerNameScript = "";
        if (state.Enabled)
        {
            p.ComputerNameMode = Normal(state.Getter) == Normal(PromptScript) ? Prompt : Script;
            if (p.ComputerNameMode == Script) p.ComputerNameScript = state.Getter;
        }
        else if (state.Ambiguous) notes.Add("计算机命名脚本或执行管线未完整识别，原文保留；不能覆盖命名设置。");
    }
    public static void ValidateImported(InstallPlan p, XmlDocument source)
    {
        if (Inspect(source).Ambiguous && (p.ComputerNameMode != Existing || p.ComputerName != (Name(source) == "*" ? "" : Name(source)))) throw new ArgumentException("原 XML 的计算机命名流程未完整识别，请用原工具调整后重新导入，不能覆盖命名设置。");
    }
    public static void NormalizeLoaded(InstallPlan p, JsonElement json)
    {
        if (json.TryGetProperty(nameof(InstallPlan.ComputerNameMode), out _) || p.ImportedXml is not { OriginalOmitted: false } data || data.SourceXml.Length == 0 || data.BaselineXml.Length == 0) return;
        var source = XmlMerge.Read(data.SourceXml); if (!Inspect(source).Enabled) return;
        var before = JsonSerializer.Deserialize<InstallPlan>(JsonSerializer.Serialize(p))!; before.ImportedXml = null; before.ConfirmWipeDisk = before.ConfirmFormatPartition = before.ConfirmDiskScript = true;
        before.ComputerName = Name(XmlMerge.Read(data.BaselineXml)); if (before.ComputerName == "*") before.ComputerName = "";
        var after = JsonSerializer.Deserialize<InstallPlan>(JsonSerializer.Serialize(before))!; ReadIntoPlan(source, after, new());
        if (p.ComputerName == before.ComputerName) { p.ComputerNameMode = after.ComputerNameMode; p.ComputerNameScript = after.ComputerNameScript; }
        try { var g = new GeneratorService(); data.BaselineXml = XmlMerge.Apply(data.BaselineXml, Encoding.UTF8.GetString(g.GenerateImportBaseline(before)), XmlMerge.Read(Encoding.UTF8.GetString(g.GenerateImportBaseline(after)))).OuterXml; }
        catch (Exception e) when (e is ArgumentException or ConfigurationException or XmlException) { data.Blockers = data.Blockers.Append("旧方案的计算机命名基准无法可靠迁移，请重新导入原 XML。").Distinct().ToArray(); }
    }
}
