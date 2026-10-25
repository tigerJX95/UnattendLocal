namespace UnattendLocal.Core;

public record ScriptTemplate(string Id, string Name, string Phase, string Purpose, string Requirements, string Resource)
{
    public string Stage => global::UnattendLocal.Localization.UiText.Concat(new object?[] { global::UnattendLocal.Localization.UiText.Source("text.2896b9f54516"), CustomScripts.PhaseLabel(Phase), "（", Phase, "）。" },new bool[]{false,false,false,false,false});
    public string Content
    {
        get
        {
            using var stream = typeof(ScriptTemplates).Assembly.GetManifestResourceStream("UnattendLocal.Core.Resources.Templates." + Resource)
                ?? throw new InvalidOperationException(global::UnattendLocal.Localization.UiText.Source("text.e228a135968c"));
            using var reader = new StreamReader(stream);
            return reader.ReadToEnd();
        }
    }
}

// Local adaptations of capabilities documented on the upstream sample page.
// Entries remain ordinary scripts, so existing JSON/XML import and editing apply.
public static class ScriptTemplates
{
    public static IReadOnlyList<ScriptTemplate> All { get; } = Array.AsReadOnly(new[]
    {
        new ScriptTemplate("netfx3", global::UnattendLocal.Localization.UiText.Source("text.4e708fe8033b"), "FirstLogon",
            global::UnattendLocal.Localization.UiText.Source("text.0dc569e8ed04"),
            global::UnattendLocal.Localization.UiText.Source("text.c6949a769239"), "NetFx3.ps1"),
        new ScriptTemplate("vcredist", global::UnattendLocal.Localization.UiText.Source("text.8697ae6ad89e"), "FirstLogon",
            global::UnattendLocal.Localization.UiText.Source("text.a16ba94e39a4"),
            global::UnattendLocal.Localization.UiText.Source("text.16db59f77bd6"), "VcRedist.ps1"),
        new ScriptTemplate("autorun", global::UnattendLocal.Localization.UiText.Source("text.6cb7e07cb7f0"), "System",
            global::UnattendLocal.Localization.UiText.Source("text.5ba911814d27"),
            global::UnattendLocal.Localization.UiText.Source("text.961acec24684"), "DisableAutoRun.ps1"),
        new ScriptTemplate("local-ping", global::UnattendLocal.Localization.UiText.Source("text.91a6a63be8e6"), "System",
            global::UnattendLocal.Localization.UiText.Source("text.4bdbe007d38b"),
            global::UnattendLocal.Localization.UiText.Source("text.2fd708bc054f"), "AllowLocalPing.ps1"),
        new ScriptTemplate("remote-assistance", global::UnattendLocal.Localization.UiText.Source("text.057957ef54fc"), "System",
            global::UnattendLocal.Localization.UiText.Source("text.0123d2f8ba3d"),
            global::UnattendLocal.Localization.UiText.Source("text.e2fb125bdb64"), "DisableRemoteAssistance.ps1"),
        new ScriptTemplate("chrome-winget", global::UnattendLocal.Localization.UiText.Source("text.be1e6cb6c137"), "FirstLogon",
            global::UnattendLocal.Localization.UiText.Source("text.ac1d90411e51"),
            global::UnattendLocal.Localization.UiText.Source("text.1a5e930cdb10"), "InstallChromeWinGet.ps1"),
        new ScriptTemplate("public-shortcuts", global::UnattendLocal.Localization.UiText.Source("text.219b7835f98e"), "System",
            global::UnattendLocal.Localization.UiText.Source("text.4e8d3acf240c"),
            global::UnattendLocal.Localization.UiText.Source("text.adfdb7dcbea3"), "PublicDesktopShortcuts.ps1")
    });

    public static ScriptEntry Add(InstallPlan plan, string id)
    {
        var template = All.SingleOrDefault(t => t.Id == id) ?? throw new ArgumentException(global::UnattendLocal.Localization.UiText.Source("text.8d54cc82c917"));
        if (plan.Scripts.Count >= 60) throw new ArgumentException(global::UnattendLocal.Localization.UiText.Source("text.bca97a516f08"));
        var content = template.Content;
        if (plan.Scripts.Any(s => s.Phase == template.Phase && s.Type == "Ps1" && s.Content.Replace("\r\n", "\n") == content.Replace("\r\n", "\n")))
            throw new ArgumentException(global::UnattendLocal.Localization.UiText.Source("text.84cb3157c63f"));
        var row = new ScriptEntry { Name = template.Name, Enabled = false, Phase = template.Phase, Type = "Ps1", Content = content };
        plan.Scripts.Add(row);
        return row;
    }
}
