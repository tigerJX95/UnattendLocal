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
            "以 SYSTEM 身份写入全机策略，影响所有用户。\n\n此设置控制 AutoRun；不替代病毒扫描，也不保证隐藏所有自动播放提示。组织策略可能覆盖该设置。", "DisableAutoRun.ps1"),
        new ScriptTemplate("local-ping", "允许局域网 Ping（IPv4 / IPv6）", "System",
            "允许同一子网的设备通过 Ping 检查本机连通性。",
            "仅添加专用网络和域网络的入站回显请求规则，来源限制为本地子网；不新增公用网络放行。\n\n不关闭防火墙，不放行其他 ICMP 类型；现有规则、组织策略及网络设备仍可能影响实际连通性。", "AllowLocalPing.ps1"),
        new ScriptTemplate("remote-assistance", "关闭受邀 Windows 远程协助", "System",
            "关闭本机传统 Windows 远程协助的受邀连接开关。",
            "以 SYSTEM 身份设置全机 fAllowToGetHelp 开关，影响用户通过传统远程协助发起的求助连接。\n\n不关闭远程桌面（RDP）、快速助手或第三方远控软件；不删除服务或防火墙规则，组织策略可能覆盖此开关。", "DisableRemoteAssistance.ps1"),
        new ScriptTemplate("chrome-winget", "通过 WinGet 安装 Google Chrome", "FirstLogon",
            "通过 WinGet 社区源为整台电脑静默安装 Google Chrome 浏览器。",
            "需要 Windows 11 24H2（内部版本 26100）及以上、管理员首次登录和网络；最多等待 5 分钟让 WinGet 出现，安装完成时间不包含在此等待上限内。\n\n可修改脚本顶部的 PackageId 以安装其他软件。启用表示接受软件及源协议；默认不升级已有版本，不跳过安装包哈希验证。缺少应用安装程序、策略阻止或安装失败时会记录错误，不自动修复系统组件。", "InstallChromeWinGet.ps1"),
        new ScriptTemplate("public-shortcuts", "创建公共桌面快捷方式", "System",
            "为所有用户创建程序（.lnk）或网页（.url）快捷方式。",
            "默认创建系统属性与项目下载页两个示例，可在脚本顶部的 Shortcuts 列表修改名称和目标。程序链接需要绝对路径，网页链接使用 HTTP 或 HTTPS。\n\n在创建用户前写入系统解析出的公共桌面目录，不写死盘符；已有同名文件或文件夹会保留。此模板只创建链接，不下载或运行链接指向的内容。", "PublicDesktopShortcuts.ps1")
    });

    public static ScriptEntry Add(InstallPlan plan, string id)
    {
        var template = All.SingleOrDefault(t => t.Id == id) ?? throw new ArgumentException("请选择有效的脚本模板。");
        if (plan.Scripts.Count >= 60) throw new ArgumentException("最多 60 条脚本。");
        var content = template.Content;
        if (plan.Scripts.Any(s => s.Phase == template.Phase && s.Type == "Ps1" && s.Content.Replace("\r\n", "\n") == content.Replace("\r\n", "\n")))
            throw new ArgumentException("相同模板已在脚本列表中，请选择已有条目核对并启用。");
        var row = new ScriptEntry { Name = template.Name, Enabled = false, Phase = template.Phase, Type = "Ps1", Content = content };
        plan.Scripts.Add(row);
        return row;
    }
}
