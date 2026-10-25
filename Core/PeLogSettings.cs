using System.Text.Json;
using System.Text.RegularExpressions;
using System.Xml;
namespace UnattendLocal.Core;

public static class PeLogSettings
{
    private const string Component = "Microsoft-Windows-Setup";
    public static readonly string ReadOnlyNotice = global::UnattendLocal.Localization.UiText.Source("text.5a5e3876a543");
    private static readonly string MigrationBlocker = global::UnattendLocal.Localization.UiText.Source("text.54e1cbd98861");
    public static readonly string Description = global::UnattendLocal.Localization.UiText.Source("text.c3c7001b7e80");
    public record Source(bool Present, bool Known, string Path, string Architecture = "amd64");
    private static XmlElement[] Nodes(XmlDocument d) => d.SelectNodes("//*[local-name()='LogPath']")!.Cast<XmlElement>().ToArray();
    public static bool CanEdit(InstallPlan p) => p.ImportedXml?.PeLogReadOnly != true;
    public static void ValidatePath(string? path)
    {
        if (path == null || path.Length is < 3 or > 259 || path != path.Trim() || !Regex.IsMatch(path, @"\A[A-Za-z]:\\", RegexOptions.CultureInvariant) || path.StartsWith("X:", StringComparison.OrdinalIgnoreCase) || path.Any(char.IsControl) || path.Any(c => "\"<>|?*%/".Contains(c)) || path[2..].Contains(':'))
            throw new ArgumentException(global::UnattendLocal.Localization.UiText.Source("text.c63e92843588"));
        try { XmlConvert.VerifyXmlChars(path); } catch (XmlException) { throw new ArgumentException(global::UnattendLocal.Localization.UiText.Source("text.754765a57d04")); }
        var parts = path[3..].Split('\\');
        for (int i = 0; i < parts.Length; i++)
        {
            string s = parts[i]; if (s.Length == 0 && i == parts.Length - 1) continue;
            if (s.Length is 0 or > 255 || s is "." or ".." || s.EndsWith(' ') || s.EndsWith('.') || Regex.IsMatch(s.Split('.')[0], @"\A(?:CON|PRN|AUX|NUL|COM[1-9]|LPT[1-9])\z", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)) throw new ArgumentException(global::UnattendLocal.Localization.UiText.Source("text.eeb7a52cfe9f"));
        }
        if (new[] { ".log", ".txt", ".etl" }.Any(e => path.EndsWith(e, StringComparison.OrdinalIgnoreCase))) throw new ArgumentException(global::UnattendLocal.Localization.UiText.Source("text.6133d7387f7d"));
    }
    public static void Validate(InstallPlan p) { if (p.UsePeLogPath) ValidatePath(p.PeLogPath); }
    public static string Summary(InstallPlan p) => !CanEdit(p) ? global::UnattendLocal.Localization.UiText.Source("text.2827208a2731") : p.UsePeLogPath ? global::UnattendLocal.Localization.UiText.Concat(new object?[] { global::UnattendLocal.Localization.UiText.Source("text.aa7c1f31a0d0"), p.PeLogPath, global::UnattendLocal.Localization.UiText.Source("text.f8c14e582fc2") },new bool[]{false,true,false}) : global::UnattendLocal.Localization.UiText.Source("text.b85c2d1c6bca");
    public static void Apply(XmlDocument doc, InstallPlan p)
    {
        Validate(p); if (!p.UsePeLogPath) return;
        if (Nodes(doc).Length > 0 || SetupDriverSettings.CustomPe(doc)) throw new ArgumentException(global::UnattendLocal.Localization.UiText.Source("text.f67a259b9d36"));
        var components = NativeSetupXml.Components(doc, Component);
        if (components.Length != 1 || !NativeSetupXml.ValidComponent(doc, components[0], Component, "windowsPE") || components[0].GetAttribute("processorArchitecture") != p.Architecture) throw new ArgumentException(global::UnattendLocal.Localization.UiText.Source("text.49c91e454784"));
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
