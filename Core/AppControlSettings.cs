using System.Text;
using System.Text.Json;
using System.Xml;
using System.Xml.Schema;
using Schneegans.Unattend;
namespace UnattendLocal.Core;

public static class AppControlSettings
{
    public const int MaxLength = 2 * 1024 * 1024;
    private const string PolicyPath = @"C:\Windows\Setup\Scripts\AppLockerPolicy.xml";
    public const string Example = """
        <AppLockerPolicy Version="1">
          <RuleCollection Type="Exe" EnforcementMode="AuditOnly">
            <FilePathRule Id="d3b66501-7907-4ad5-9ad6-165630844817" Name="Windows directory audit example" Description="Audit example only; adapt and test before enforcing." UserOrGroupSid="S-1-1-0" Action="Allow">
              <Conditions><FilePathCondition Path="%WINDIR%\*" /></Conditions>
            </FilePathRule>
          </RuleCollection>
        </AppLockerPolicy>
        """;
    public const string Description = "默认关闭。启用后使用完整 AppLockerPolicy XML，在目标 specialize 阶段启动 Application Identity（AppIDSvc）服务并执行 Set-AppLockerPolicy -XmlPolicy，不带 -Merge，可能替换现有本地策略。规则的审核/强制模式由 XML 决定，工具不会改为审核模式。强制策略可能拦截安装脚本、系统工具或管理员应用，应先在虚拟机审核并准备恢复方案；结构校验不能判断规则是否适合目标电脑。实际生效受 Windows 版本、组件和组织策略影响，AppLocker 不等于智能应用控制（SAC）或防病毒。策略始终按原文保存在 JSON 中，省略凭据不移除策略里的路径、SID 或其他自行填写的信息；预览整体遮蔽策略 XML。取消仅改变新配置，不删除或恢复已安装系统的策略。本工具不在当前电脑启动服务或应用策略。";
    public static XmlDocument ReadPolicy(string text)
    {
        if (string.IsNullOrWhiteSpace(text) || text.Length > MaxLength) throw new ArgumentException("请填写完整 AppLocker 策略 XML，最多 2 Mi 字符；示例需明确载入。");
        var doc = new XmlDocument { XmlResolver = null };
        try
        {
            using var reader = XmlReader.Create(new StringReader(text), new XmlReaderSettings { DtdProcessing = DtdProcessing.Prohibit, XmlResolver = null, MaxCharactersInDocument = MaxLength, MaxCharactersFromEntities = 0 });
            doc.Load(reader);
            if (doc.DocumentElement?.Name != "AppLockerPolicy" || doc.DocumentElement.NamespaceURI.Length != 0 || doc.SelectNodes("//processing-instruction()")!.Count > 0) throw new ArgumentException("请选择无命名空间的 AppLockerPolicy XML，不支持处理指令。");
            var pending = new Stack<(XmlElement Element, int Depth)>(); pending.Push((doc.DocumentElement, 1)); int count = 0;
            while (pending.Count > 0) { var (element, depth) = pending.Pop(); if (depth > 64 || ++count > 50000) throw new ArgumentException("AppLocker XML 层级或项目过多。"); foreach (var child in element.ChildNodes.OfType<XmlElement>()) pending.Push((child, depth + 1)); }
            using var schema = typeof(UnattendGenerator).Assembly.GetManifestResourceStream("Schneegans.Unattend.resource.AppLocker.xsd")!;
            doc.Schemas.XmlResolver = null; doc.Schemas.Add(null, XmlReader.Create(schema, new XmlReaderSettings { DtdProcessing = DtdProcessing.Prohibit, XmlResolver = null }));
            doc.Validate((_, e) => throw new ArgumentException("AppLocker XML 结构无效：" + e.Message));
            return doc;
        }
        catch (XmlException e) { throw new ArgumentException("AppLocker XML 无法读取：" + e.Message, e); }
    }
    public static IAppLockerSettings Build(InstallPlan p) => p.UseAppLocker ? new ConfigureAppLockerSettings(ReadPolicy(p.AppLockerPolicyXml).OuterXml) : new SkipAppLockerSettings();
    public static string Summary(InstallPlan p)
    {
        if (!p.UseAppLocker) return "AppLocker：不添加策略";
        try { var doc = ReadPolicy(p.AppLockerPolicyXml); var collections = doc.DocumentElement!.ChildNodes.OfType<XmlElement>().Where(e => e.Name == "RuleCollection").ToArray(); return "AppLocker：" + collections.Length + " 组规则集合；" + string.Join("、", collections.Select(e => e.GetAttribute("Type") + "=" + e.GetAttribute("EnforcementMode"))) + "（可能替换本地策略）"; }
        catch (ArgumentException) { return "AppLocker：已启用，策略 XML 待补充或校验"; }
    }
    private static string Normal(string text) => string.Join('\n', text.Replace("\r\n", "\n").Split('\n').Select(l => l.Trim()).Where(l => l.Length > 0));
    private static bool Related(string line) => new[] { "AppLockerPolicy.xml", "AppIDSvc", "Set-AppLockerPolicy" }.Any(m => line.Contains(m, StringComparison.OrdinalIgnoreCase));
    private static readonly Lazy<string> Expected = new(() => AdditionalSetupSettings.KnownBlocks(new UnattendGenerator().GenerateXml(Configuration.Default with { AppLockerSettings = new ConfigureAppLockerSettings(Example) }), "Specialize.ps1").Single(Related));
    public record State(bool Enabled, bool Ambiguous, string Xml);
    public static State Inspect(XmlDocument doc)
    {
        var files = doc.SelectNodes("//*[local-name()='File']")!.Cast<XmlElement>().ToArray();
        var policies = files.Where(f => f.GetAttribute("path").Equals(PolicyPath, StringComparison.OrdinalIgnoreCase)).ToArray();
        var ns = new XmlNamespaceManager(doc.NameTable); ns.AddNamespace("u", XmlMerge.Namespace);
        var refs = files.Except(policies).SelectMany(f => Normal(f.InnerText).Split('\n')).Concat(doc.SelectNodes("//u:Path|//u:CommandLine", ns)!.Cast<XmlElement>().Select(n => n.InnerText)).Where(Related).ToArray();
        var expected = Expected.Value; var needed = expected.Split('\n').Where(Related).ToArray();
        bool valid = false;
        if (policies.Length == 1) { try { _ = ReadPolicy(policies[0].InnerText); valid = true; } catch (ArgumentException) { } }
        bool enabled = valid && AdditionalSetupSettings.KnownBlocks(doc, "Specialize.ps1").Count(b => b == expected) == 1 && refs.Length == needed.Length && needed.All(l => refs.Count(v => v == l) == needed.Count(v => v == l));
        return new(enabled, (policies.Length > 0 || refs.Length > 0) && !enabled, enabled ? policies[0].InnerText : "");
    }
    public static void ReadIntoPlan(XmlDocument doc, InstallPlan p, List<string> notes)
    {
        var state = Inspect(doc); p.UseAppLocker = state.Enabled; p.AppLockerPolicyXml = state.Xml;
        if (state.Ambiguous) notes.Add("AppLocker 策略或执行管线未完整识别，原文保留；不能用新增设置覆盖。");
    }
    public static void ValidateImported(InstallPlan p, XmlDocument doc)
    {
        if (p.UseAppLocker && Inspect(doc).Ambiguous) throw new ArgumentException("原 XML 的 AppLocker 流程未完整识别，不能覆盖；请用原工具调整后重新导入。");
    }
    public static void NormalizeLoaded(InstallPlan p, JsonElement json)
    {
        if (json.TryGetProperty(nameof(InstallPlan.UseAppLocker), out _) || p.ImportedXml is not { OriginalOmitted: false } data || data.SourceXml.Length == 0 || data.BaselineXml.Length == 0) return;
        var source = XmlMerge.Read(data.SourceXml); var state = Inspect(source); if (!state.Enabled) return;
        var before = JsonSerializer.Deserialize<InstallPlan>(JsonSerializer.Serialize(p))!; before.ImportedXml = null; before.ConfirmWipeDisk = before.ConfirmFormatPartition = before.ConfirmDiskScript = true;
        var after = JsonSerializer.Deserialize<InstallPlan>(JsonSerializer.Serialize(before))!; ReadIntoPlan(source, after, new()); p.UseAppLocker = after.UseAppLocker; p.AppLockerPolicyXml = after.AppLockerPolicyXml;
        try { var g = new GeneratorService(); data.BaselineXml = XmlMerge.Apply(data.BaselineXml, Encoding.UTF8.GetString(g.GenerateImportBaseline(before)), XmlMerge.Read(Encoding.UTF8.GetString(g.GenerateImportBaseline(after)))).OuterXml; }
        catch (Exception e) when (e is ArgumentException or ConfigurationException or XmlException) { data.Blockers = data.Blockers.Append("旧方案的 AppLocker 编辑基准无法可靠迁移，请重新导入原 XML。").Distinct().ToArray(); }
    }
}
