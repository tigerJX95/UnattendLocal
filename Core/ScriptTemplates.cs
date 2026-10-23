namespace UnattendLocal.Core;

public record ScriptTemplate(string Id, string Name, string Phase, string Purpose, string Requirements, string Resource)
{
    public string Stage => "执行阶段：" + CustomScripts.PhaseLabel(Phase) + "（" + Phase + "）。";
    public string Content
    {
        get
        {
            using var stream = typeof(ScriptTemplates).Assembly.GetManifestResourceStream("UnattendLocal.Core.Resources.Templates." + Resource)
                ?? throw new InvalidOperationException("找不到内置脚本模板。");
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
        new ScriptTemplate("netfx3", "离线安装 .NET Framework 3.5", "FirstLogon",
            "从安装介质的 sources\\sxs 安装旧应用需要的 .NET Framework 3.5。",
            "需要管理员首次登录，并保留与目标 Windows 版本、语言及架构匹配的安装介质。\n\n仅使用本地源；找不到源或安装失败会记录错误，不转向 Windows Update。Windows 内部版本 28000 及以上需使用独立安装程序，此模板会停止。", "NetFx3.ps1"),
        new ScriptTemplate("vcredist", "安装微软 Visual C++ v14 运行库", "FirstLogon",
            "从微软下载并静默安装 VC++ v14 运行库，供使用 MSVC 的应用运行。",
            "需要管理员首次登录及联网。按目标系统架构选择 x86 / x64 / ARM64，64 位系统同时安装 x86 运行库。\n\n执行前检查有效的微软 Authenticode 签名；接受成功、需要重启及已安装较新版本的返回码，不主动重启。", "VcRedist.ps1"),
        new ScriptTemplate("autorun", "禁用所有驱动器的 AutoRun", "System",
            "禁止驱动器中的 autorun.inf 自动运行命令，减少插入介质时的自动执行。",
            "以 SYSTEM 身份写入全机策略，影响所有用户。\n\n此设置控制 AutoRun；不替代病毒扫描，也不保证隐藏所有自动播放提示。组织策略可能覆盖该设置。", "DisableAutoRun.ps1")
    });

    public static ScriptEntry Add(InstallPlan plan, string id)
    {
        var template = All.SingleOrDefault(t => t.Id == id) ?? throw new ArgumentException("请选择有效的脚本模板。");
        if (plan.Scripts.Count >= 60) throw new ArgumentException("最多 60 条脚本。");
        var content = template.Content;
        if (plan.Scripts.Any(s => s.Phase == template.Phase && s.Type == "Ps1" && s.Content.Replace("\r\n", "\n") == content.Replace("\r\n", "\n")))
            throw new ArgumentException("相同模板已在脚本列表中，请选择已有条目核对并启用。");
        var row = new ScriptEntry { Enabled = false, Phase = template.Phase, Type = "Ps1", Content = content };
        plan.Scripts.Add(row);
        return row;
    }
}
