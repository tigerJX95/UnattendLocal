using System.Text.Json;
using System.Text.RegularExpressions;
using System.Xml;
namespace UnattendLocal.Core;

public static class WorkgroupSettings
{
    private const string Component = "Microsoft-Windows-UnattendedJoin";
    public static readonly string ReadOnlyNotice = global::UnattendLocal.Localization.UiText.Source("text.0be8bf11c431");
    private static readonly string MigrationBlocker = global::UnattendLocal.Localization.UiText.Source("text.2bc1ca99d59c");
    public static readonly string Description = global::UnattendLocal.Localization.UiText.Source("text.af34fa2c43c9");
    public record Source(bool Present, bool Known, string Name, string Architecture = "amd64");
    private static XmlElement[] Nodes(XmlDocument d) => d.SelectNodes("//*[local-name()='JoinWorkgroup' or local-name()='JoinDomain']")!.Cast<XmlElement>().ToArray();
    public static bool CanEdit(InstallPlan p) => p.ImportedXml?.WorkgroupReadOnly != true;
    public static void ValidateName(string? name)
    {
        if (name == null || !Regex.IsMatch(name, @"\A[A-Za-z0-9](?:[A-Za-z0-9-]{0,13}[A-Za-z0-9])?\z", RegexOptions.CultureInvariant) || !name.Any(char.IsAsciiLetter))
            throw new ArgumentException(global::UnattendLocal.Localization.UiText.Source("text.2da4b17730ad"));
    }
    public static void Validate(InstallPlan p)
    {
        if (!p.UseWorkgroup) return;
        ValidateName(p.WorkgroupName);
        if (p.ComputerNameMode == ComputerNamingSettings.Existing && p.WorkgroupName.Equals(p.ComputerName, StringComparison.OrdinalIgnoreCase)) throw new ArgumentException(global::UnattendLocal.Localization.UiText.Source("text.26eac63f6428"));
    }
    public static string Summary(InstallPlan p) => !CanEdit(p) ? global::UnattendLocal.Localization.UiText.Source("text.82740df2e975") : p.UseWorkgroup ? global::UnattendLocal.Localization.UiText.Concat(new object?[] { global::UnattendLocal.Localization.UiText.Source("text.59d05a089dd9"), p.WorkgroupName, "（specialize）" },new bool[]{false,true,false}) : global::UnattendLocal.Localization.UiText.Source("text.5fb3fcbc70b7");
    public static void Apply(XmlDocument doc, InstallPlan p)
    {
        Validate(p);
        bool retain = p.ImportedXml is { WorkgroupReadOnly: false, SourceXml.Length: > 0 } imported && Inspect(XmlMerge.Read(imported.SourceXml)) is { Present: true, Known: true };
        if (!p.UseWorkgroup && !retain) return;
        if (Nodes(doc).Length > 0 || NativeSetupXml.Components(doc, Component).Length > 0) throw new ArgumentException(global::UnattendLocal.Localization.UiText.Source("text.f0134570f556"));
        var component = NativeSetupXml.GetComponent(doc, Component, "specialize", p.Architecture);
        var id = NativeSetupXml.Add(component, "Identification");
        if (p.UseWorkgroup) NativeSetupXml.Add(id, "JoinWorkgroup", p.WorkgroupName);
    }
    public static Source Inspect(XmlDocument doc)
    {
        var components = NativeSetupXml.Components(doc, Component); var nodes = Nodes(doc);
        var unknown = new Source(components.Length > 0 || nodes.Length > 0, false, "");
        if (components.Length == 0 && nodes.Length == 0) return new(false, true, "");
        if (components.Length != 1 || nodes.Length != 1 || nodes[0].LocalName != "JoinWorkgroup") return unknown;
        var c = components[0]; var node = nodes[0];
        if (!NativeSetupXml.ValidComponent(doc, c, Component, "specialize") || node.NamespaceURI != XmlMerge.Namespace || !NativeSetupXml.NoAttributes(node) || node.ChildNodes.OfType<XmlElement>().Any()) return unknown;
        var children = c.ChildNodes.OfType<XmlElement>().ToArray();
        if (children.Length != 1 || children[0].LocalName != "Identification" || children[0].NamespaceURI != XmlMerge.Namespace || !NativeSetupXml.NoAttributes(children[0]) || !NativeSetupXml.NoSignificantText(c) || !NativeSetupXml.NoSignificantText(children[0]) || node.ParentNode != children[0] || children[0].ChildNodes.OfType<XmlElement>().Count() != 1) return unknown;
        try { ValidateName(node.InnerText); } catch (ArgumentException) { return unknown; }
        return new(true, true, node.InnerText, c.GetAttribute("processorArchitecture"));
    }
    // Keep the container during the three-way merge, then remove only the
    // emptied component created by cancelling a fully recognized workgroup.
    internal static void FinishImported(XmlDocument doc, InstallPlan p)
    {
        if (p.UseWorkgroup || p.ImportedXml is not { WorkgroupReadOnly: false, SourceXml.Length: > 0 } data || Inspect(XmlMerge.Read(data.SourceXml)) is not { Present: true, Known: true }) return;
        foreach (var c in NativeSetupXml.Components(doc, Component))
        {
            var children = c.ChildNodes.OfType<XmlElement>().ToArray();
            if (NativeSetupXml.ValidComponent(doc, c, Component, "specialize") && children.Length == 1 && children[0].LocalName == "Identification" && children[0].NamespaceURI == XmlMerge.Namespace && NativeSetupXml.NoAttributes(children[0]) && !children[0].ChildNodes.OfType<XmlElement>().Any() && string.IsNullOrWhiteSpace(children[0].InnerText)) c.ParentNode!.RemoveChild(c);
        }
    }
    private static void Assign(InstallPlan p, Source source) { p.UseWorkgroup = source.Known && source.Present; if (p.UseWorkgroup) p.WorkgroupName = source.Name; }
    public static Source ReadIntoPlan(XmlDocument doc, InstallPlan p, List<string> notes) { var s = Inspect(doc); Assign(p, s); if (!s.Known) notes.Add(ReadOnlyNotice); return s; }
    public static void UpdateImportState(ImportedXml data, Source s) { data.WorkgroupReadOnly = !s.Known; data.WorkgroupImportVersion = 1; }
    public static void ValidateImported(InstallPlan p, XmlDocument source) { if (p.UseWorkgroup && !Inspect(source).Known) throw new ArgumentException(ReadOnlyNotice); }
    public static void NormalizeLoaded(InstallPlan p, JsonElement root)
    {
        if (p.ImportedXml is not { OriginalOmitted: false, SourceXml.Length: > 0 } data) return;
        var state = Inspect(XmlMerge.Read(data.SourceXml));
        if (data.WorkgroupImportVersion < 1 && state is { Present: true, Known: true })
        {
            bool missing = !root.TryGetProperty(nameof(p.UseWorkgroup), out _) && !root.TryGetProperty(nameof(p.WorkgroupName), out _);
            if (missing && data.BaselineXml.Length > 0 && !Inspect(XmlMerge.Read(data.BaselineXml)).Present)
            {
                try { var before = XmlMerge.Read(data.BaselineXml); Apply(before, new InstallPlan { Architecture = state.Architecture, UseWorkgroup = true, WorkgroupName = state.Name }); Assign(p, state); data.BaselineXml = before.OuterXml; }
                catch (ArgumentException) { data.Blockers = data.Blockers.Append(MigrationBlocker).Distinct().ToArray(); }
            }
            else data.Blockers = data.Blockers.Append(MigrationBlocker).Distinct().ToArray();
        }
        data.Notices = data.Notices.Where(n => n != ReadOnlyNotice).ToArray(); if (!state.Known) data.Notices = [..data.Notices, ReadOnlyNotice]; UpdateImportState(data, state);
    }
}
