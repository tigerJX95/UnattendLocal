using System.Text.Json;
using System.Xml;

namespace UnattendLocal.Core;

// Independent scalar settings share strict inspection and baseline migration.
public static class NativePreferences
{
    public const string Default = "不指定（遵循 Windows 默认）", Balanced = "平衡", HighPerformance = "高性能", PowerSaver = "节能", Custom = "自定义电源计划 GUID";
    public const string WerEnabled = "启用 Windows 错误报告", WerDisabled = "禁用 Windows 错误报告";
    public static string[] PowerModes => [Default, Balanced, HighPerformance, PowerSaver, Custom];
    public static string[] WerModes => [Default, WerEnabled, WerDisabled];
    public const string BalancedGuid = "381b4222-f694-41f0-9685-ff5bb260df2e", HighPerformanceGuid = "8c5e7fda-e8bf-4a96-9a85-a6e23a8c635c", PowerSaverGuid = "a1841308-3541-4fab-bc81-f71556f20b4a";
    public record Setting(bool Power, string Field, string Component, string Element, string Label);
    public static Setting Power { get; } = new(true, nameof(InstallPlan.PowerPlanMode), "Microsoft-Windows-powercpl", "PreferredPlan", "首选电源计划");
    public static Setting Wer { get; } = new(false, nameof(InstallPlan.ErrorReportingMode), "Microsoft-Windows-ErrorReportingCore", "DisableWER", "Windows 错误报告");
    public static Setting[] All => [Power, Wer];
    public record Source(bool Present, bool Known, string Value, string Architecture = "amd64");
    private static XmlElement[] Nodes(XmlDocument doc, Setting s) => doc.SelectNodes("//*[local-name()='" + s.Element + "']")!.Cast<XmlElement>().ToArray();
    private static string Mode(InstallPlan p, Setting s) => s.Power ? p.PowerPlanMode : p.ErrorReportingMode;
    public static bool ExtraOwns(InstallPlan p, Setting s, bool includeDisabled = false) => p.XmlComponents?.Any(row => row is not null && (includeDisabled || row.Enabled) && row.Component == s.Component)==true;
    public static bool CanEdit(InstallPlan p, Setting s) => (!ExtraOwns(p, s) || Mode(p,s)!=Default) && (s.Power ? p.ImportedXml?.PowerPlanReadOnly != true : p.ImportedXml?.ErrorReportingReadOnly != true);
    private static bool ValidGuid(string? value) => value is { Length: 36 } && Guid.TryParseExact(value, "D", out var guid) && guid != Guid.Empty;
    public static string? Value(InstallPlan p, Setting s)
    {
        string mode = Mode(p, s);
        if (mode == Default) return null;
        if (!s.Power) return mode switch { WerEnabled => "0", WerDisabled => "1", _ => throw new ArgumentException("请选择有效的 Windows 错误报告设置。") };
        string value = mode switch { Balanced => BalancedGuid, HighPerformance => HighPerformanceGuid, PowerSaver => PowerSaverGuid, Custom => p.CustomPowerPlanGuid, _ => throw new ArgumentException("请选择有效的首选电源计划。") };
        if (!ValidGuid(value)) throw new ConfigurationValidationException(nameof(InstallPlan.CustomPowerPlanGuid), "请输入有效的电源计划 GUID：36 个字符，不带大括号、空格或命令参数，且不能全为零。");
        // Preserve imported spelling so three-way removal does not mistake casing for an external edit.
        return ValidGuid(p.CustomPowerPlanGuid) && Guid.Parse(p.CustomPowerPlanGuid) == Guid.Parse(value) ? p.CustomPowerPlanGuid : value;
    }
    public static void Validate(InstallPlan p)
    {
        foreach (var s in All)
            ConfigurationValidationException.Check(s.Field, () =>
            {
                if (!(s.Power ? PowerModes : WerModes).Contains(Mode(p, s))) throw new ArgumentException("请选择有效的" + s.Label + "设置。");
                if (Value(p, s) != null && (!CanEdit(p, s)||ExtraOwns(p,s))) throw new ArgumentException(Summary(p, s));
            });
    }
    public static Source Inspect(XmlDocument doc, Setting s)
    {
        var nodes = Nodes(doc, s); var components = NativeSetupXml.Components(doc, s.Component);
        var unknown = new Source(nodes.Length > 0 || components.Length > 0, false, "");
        if (nodes.Length == 0 && components.Length == 0) return new(false, true, "");
        if (components.Length != 1 || !NativeSetupXml.ValidComponent(doc, components[0], s.Component, "specialize") || !NativeSetupXml.NoSignificantText(components[0])) return unknown;
        if (nodes.Length == 0) return new(false, true, "", components[0].GetAttribute("processorArchitecture"));
        if (nodes.Length != 1) return unknown;
        var node = nodes[0];
        if (node.ParentNode != components[0] || node.NamespaceURI != XmlMerge.Namespace || !NativeSetupXml.NoAttributes(node) || node.ChildNodes.OfType<XmlElement>().Any() || !NativeSetupXml.NoSignificantText(components[0])) return unknown;
        if (s.Power ? !ValidGuid(node.InnerText) : node.InnerText is not ("0" or "1")) return unknown;
        return new(true, true, node.InnerText, components[0].GetAttribute("processorArchitecture"));
    }
    private static void Assign(InstallPlan p, Setting s, Source source)
    {
        if (!s.Power) { p.ErrorReportingMode = source is { Present: true, Known: true } ? source.Value == "0" ? WerEnabled : WerDisabled : Default; return; }
        p.PowerPlanMode = Default;
        if (source is not { Present: true, Known: true }) return;
        p.CustomPowerPlanGuid = source.Value;
        var guid = Guid.Parse(source.Value);
        p.PowerPlanMode = guid == Guid.Parse(BalancedGuid) ? Balanced : guid == Guid.Parse(HighPerformanceGuid) ? HighPerformance : guid == Guid.Parse(PowerSaverGuid) ? PowerSaver : Custom;
    }
    public static Source[] ReadIntoPlan(XmlDocument doc, InstallPlan p, List<string> notes)
    {
        return All.Select(s =>
        {
            var source = Inspect(doc, s);
            if (!ExtraOwns(p, s, true)) Assign(p, s, source);
            else if (s.Power) p.PowerPlanMode = Default; else p.ErrorReportingMode = Default;
            if (!source.Known) notes.Add(ReadOnlyNotice(s));
            return source;
        }).ToArray();
    }
    private static string ReadOnlyNotice(Setting s) => "原 XML 的" + s.Label + "设置或执行阶段未完整识别，原文只读保留；不能用界面默认值覆盖。";
    public static void UpdateImportState(ImportedXml data, Source[] sources)
    { data.PowerPlanReadOnly = !sources[0].Known; data.ErrorReportingReadOnly = !sources[1].Known; data.NativePreferencesImportVersion = 1; }
    public static string Summary(InstallPlan p, Setting s) => s.Label + "：" + (ExtraOwns(p, s) ? Mode(p,s)==Default?"由启用的额外 XML 组件管理，请从该条目调整。":"与启用的额外 XML 组件重复，请选择不指定或停用该组件条目。" : !CanEdit(p, s) ? "原 XML 只读保留，请核对原文。" : s.Power&&Mode(p,s)==Custom&&!ValidGuid(p.CustomPowerPlanGuid)?"待配置；请填写有效的自定义电源计划 GUID。": Mode(p, s) + (s.Power && p.PowerPlanMode == Custom ? "（" + p.CustomPowerPlanGuid + "）" : "") + "。");
    private static XmlElement Component(XmlDocument doc,string name,string architecture)
    {
        if(!doc.DocumentElement!.ChildNodes.OfType<XmlElement>().Any(e=>e.LocalName=="settings"&&e.NamespaceURI==XmlMerge.Namespace&&e.GetAttribute("pass")=="specialize"))
        {
            var group=doc.CreateElement("settings",XmlMerge.Namespace);group.SetAttribute("pass","specialize");
            doc.DocumentElement.InsertBefore(group,doc.DocumentElement.ChildNodes.OfType<XmlElement>().FirstOrDefault(e=>e.NamespaceURI!=XmlMerge.Namespace));
        }
        return NativeSetupXml.GetComponent(doc,name,"specialize",architecture);
    }
    public static void Apply(XmlDocument doc, InstallPlan p)
    {
        var source=p.ImportedXml is {SourceXml.Length:>0} imported?XmlMerge.Read(imported.SourceXml):null;
        foreach (var s in All)
        {
            if (ExtraOwns(p, s)) continue;
            string? value = Value(p, s);
            bool retain = source!=null && Inspect(source,s).Known && NativeSetupXml.Components(source,s.Component).Length>0;
            if (value == null && !retain) continue;
            if (Nodes(doc, s).Length > 0) throw new ConfigurationValidationException(s.Field, "生成基准已包含" + s.Label + "，不能重复添加。");
            var component = Component(doc,s.Component,p.Architecture);
            if (value != null) NativeSetupXml.Add(component, s.Element, value);
        }
    }
    public static void ValidateImported(InstallPlan p, XmlDocument source)
    {
        foreach (var s in All) if (Value(p, s) != null && !Inspect(source, s).Known) throw new ConfigurationValidationException(s.Field, ReadOnlyNotice(s));
    }
    public static void SeedBaseline(XmlDocument baseline, XmlDocument source, InstallPlan p)
    {
        foreach (var s in All)
            if (Inspect(source, s).Known && NativeSetupXml.Components(source, s.Component).Length > 0 && NativeSetupXml.Components(baseline, s.Component).Length == 0)
                _ = Component(baseline,s.Component,p.Architecture);
    }
    public static void FinishImported(XmlDocument doc,InstallPlan p)
    {
        if(p.ImportedXml is not {SourceXml.Length:>0} data)return;
        var source=XmlMerge.Read(data.SourceXml);
        foreach (var s in All.Where(s=>Mode(p,s)==Default&&!ExtraOwns(p,s)&&Inspect(source,s) is {Known:true,Present:true})) foreach (var component in NativeSetupXml.Components(doc, s.Component))
            if (NativeSetupXml.ValidComponent(doc, component, s.Component, "specialize") && !component.ChildNodes.OfType<XmlElement>().Any() && !component.ChildNodes.OfType<XmlComment>().Any() && string.IsNullOrWhiteSpace(component.InnerText)) component.ParentNode!.RemoveChild(component);
    }
    public static void NormalizeLoaded(InstallPlan p, JsonElement root)
    {
        if (p.ImportedXml is not { OriginalOmitted: false, SourceXml.Length: > 0 } data) return;
        var source = XmlMerge.Read(data.SourceXml); var states = All.Select(s => Inspect(source, s)).ToArray();
        if (data.NativePreferencesImportVersion < 1 && data.BaselineXml.Length > 0)
        {
            var baseline = XmlMerge.Read(data.BaselineXml);
            for (int i = 0; i < All.Length; i++)
            {
                var s = All[i]; var state = states[i];
                if (!state.Known || ExtraOwns(p, s, true)) continue;
                var before = Inspect(baseline, s);
                if (state.Present && !before.Present)
                {
                    if (root.TryGetProperty(s.Field, out _)) { data.Blockers = data.Blockers.Append("旧方案的" + s.Label + "编辑基准无法可靠迁移，请重新导入原 XML；当前修改保留。").Distinct().ToArray(); continue; }
                    var template = new InstallPlan { Architecture = state.Architecture }; Assign(template, s, state);
                    NativeSetupXml.Add(Component(baseline,s.Component,state.Architecture), s.Element, Value(template, s));
                    Assign(p, s, state);
                }
                else if (state.Present && before.Known && before.Value == state.Value && !root.TryGetProperty(s.Field, out _)) Assign(p, s, state);
            }
            SeedBaseline(baseline, source, p); data.BaselineXml = baseline.OuterXml;
        }
        data.Notices = data.Notices.Where(n => All.All(s => n != ReadOnlyNotice(s))).ToArray();
        for (int i = 0; i < All.Length; i++) if (!states[i].Known) data.Notices = [..data.Notices, ReadOnlyNotice(All[i])];
        UpdateImportState(data, states);
    }
}
