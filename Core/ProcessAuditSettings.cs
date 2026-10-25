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
    public static readonly string Description = global::UnattendLocal.Localization.UiText.Source("text.00b2b1d95cc5");
    public static IProcessAuditSettings Build(InstallPlan p) => p.ProcessAuditMode switch
    {
        Default => new DisabledProcessAuditSettings(),
        Enabled => new EnabledProcessAuditSettings(false),
        WithCommandLine => new EnabledProcessAuditSettings(true),
        _ => throw new ArgumentException(global::UnattendLocal.Localization.UiText.Source("text.2f176f30d848"))
    };
    public static string ModeSummary(InstallPlan p) => p.ProcessAuditMode switch
    {
        Default => global::UnattendLocal.Localization.UiText.Source("text.51b24d043869"),
        Enabled => global::UnattendLocal.Localization.UiText.Source("text.8deb3f230a96"),
        WithCommandLine => global::UnattendLocal.Localization.UiText.Source("text.97aa91689fe7"),
        _ => global::UnattendLocal.Localization.UiText.Source("text.d56a00583df4")
    };
    public static string Summary(InstallPlan p) => global::UnattendLocal.Localization.UiText.Concat(new object?[] { global::UnattendLocal.Localization.UiText.Source("text.e973c65acea3"), ModeSummary(p) },new bool[]{false,false});
    private static string Normal(string body) => global::UnattendLocal.Localization.UiText.Join('\n', body.Replace("\r\n", "\n").Split('\n').Select(l => l.Trim()).Where(l => l.Length > 0), false);
    public record Source(string Mode, bool Ambiguous);
    public static Source Inspect(XmlDocument doc)
    {
        var blocks = SystemMaintenanceSettings.KnownSpecializeBlocks(doc);
        var files = doc.SelectNodes("//*[local-name()='File']")!.Cast<XmlElement>();
        var ns = new XmlNamespaceManager(doc.NameTable); ns.AddNamespace("u", XmlMerge.Namespace);
        var paths = doc.SelectNodes("//u:settings[@pass='specialize']//u:Path", ns)!.Cast<XmlElement>();
        string[] markers = ["0CCE922B-69AE-11D9-BED3-505054503030", "ProcessCreationIncludeCmdLine", "Process Creation", global::UnattendLocal.Localization.UiText.Source("text.6d7b5be63c89")];
        var relevant = files.SelectMany(f => Normal(f.InnerText).Split('\n')).Concat(paths.Select(p => p.InnerText)).Where(l => markers.Any(m => l.Contains(m, StringComparison.OrdinalIgnoreCase))).ToArray();
        bool audit = blocks.Count(b => b == AuditCommand) == 1 && relevant.Count(l => l == AuditCommand) == 1;
        bool commandLine = blocks.Count(b => b == CommandLineCommand) == 1 && relevant.Count(l => l == CommandLineCommand) == 1;
        bool complete = audit && relevant.Length == (commandLine ? 2 : 1);
        return new(complete ? (commandLine ? WithCommandLine : Enabled) : Default, relevant.Length > 0 && !complete);
    }
    public static void ReadIntoPlan(XmlDocument doc, InstallPlan p, List<string> notes)
    {
        var state = Inspect(doc); p.ProcessAuditMode = state.Mode;
        if (state.Ambiguous) notes.Add(global::UnattendLocal.Localization.UiText.Source("text.c3c2c55cb022"));
    }
    public static void ValidateImported(InstallPlan p, XmlDocument doc)
    {
        if (p.ProcessAuditMode != Default && Inspect(doc).Ambiguous)
            throw new ArgumentException(global::UnattendLocal.Localization.UiText.Source("text.e5cca573d529"));
    }
    public static void NormalizeLoaded(InstallPlan p, JsonElement json)
    {
        if (json.TryGetProperty(nameof(InstallPlan.ProcessAuditMode), out _) || p.ImportedXml is not { OriginalOmitted: false } data || data.SourceXml.Length == 0 || data.BaselineXml.Length == 0) return;
        var state = Inspect(XmlMerge.Read(data.SourceXml));
        var before = JsonSerializer.Deserialize<InstallPlan>(JsonSerializer.Serialize(p))!;
        before.ImportedXml = null; before.ConfirmWipeDisk = before.ConfirmFormatPartition = before.ConfirmDiskScript = true;
        p.ProcessAuditMode = state.Mode;
        if (state.Ambiguous) data.Notices = data.Notices.Append(global::UnattendLocal.Localization.UiText.Source("text.d003e816ce06")).Distinct().ToArray();
        if (state.Mode == Default) return;
        var after = JsonSerializer.Deserialize<InstallPlan>(JsonSerializer.Serialize(before))!; after.ProcessAuditMode = state.Mode;
        try
        {
            var service = new GeneratorService();
            data.BaselineXml = XmlMerge.Apply(data.BaselineXml, Encoding.UTF8.GetString(service.GenerateImportBaseline(before)), XmlMerge.Read(Encoding.UTF8.GetString(service.GenerateImportBaseline(after)))).OuterXml;
        }
        catch (Exception e) when (e is ArgumentException or ConfigurationException or XmlException)
        {
            data.Blockers = data.Blockers.Append(global::UnattendLocal.Localization.UiText.Source("text.84a95ebeca49")).Distinct().ToArray();
        }
    }
}
