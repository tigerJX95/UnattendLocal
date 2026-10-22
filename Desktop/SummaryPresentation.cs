namespace UnattendLocal.Desktop;

// 只重排现有摘要，值仍由原有配置联动生成；未知字段继续显示。
public record SummaryRow(string SourceLabel, string Label, string Value)
{
    public string DisplayValue => Value;
    public bool IsError => DiagnosticText.IsErrorSummary(SourceLabel + "：" + Value);
}
public record SummarySection(string Kind, string Title, IReadOnlyList<SummaryRow> Rows);
public record SummaryReview(string Title, string Value, bool NeedsAttention)
{
    public bool IsError => Title == "生成前待填" && NeedsAttention;
}
public record SummaryPresentation(IReadOnlyList<SummarySection> Sections, IReadOnlyList<SummaryReview> Reviews, IReadOnlyList<string> Notices)
{
    private static readonly (string Kind, string Title, string[] Labels)[] Categories =
    [
        ("basic", "基本设置与账户", ["Windows 注册信息", "适用系统", "版本与密钥", "区域与时区", "账户", "键盘初始状态", "粘滞键", "鼠标指针精确度", "账户锁定策略", "重启后自动登录"]),
        ("protection", "安装与设备保护", ["Windows 11 安装检查", "旧版 MoSetup 升级兼容", "安装镜像", "安装驱动目录", "安装阶段动态更新", "PE 日志目录", "CompactOS", "磁盘分区", "设备自动加密", "智能应用控制", "SmartScreen", "核心隔离", "C 盘根目录权限", "AppLocker", "$OEM$ 配置集", "硬件关联应用", "空 Windows.old 目录", "兼容联接点"]),
        ("network", "网络、隐私与脚本", ["网络", "工作组", "静态 IPv4", "静态 IPv6", "隐私", "脚本", "自定义脚本", "额外 XML", "应用移除", "远程桌面", "PowerShell 执行策略"]),
        ("shell", "开始菜单与任务栏", ["任务栏", "任务栏开关", "搜索与托盘", "开始菜单", "开始菜单电源按钮旁文件夹", "任务栏结束任务"]),
        ("explorer", "资源管理器与桌面", ["资源管理器", "资源管理器操作", "系统桌面图标", "Edge 桌面快捷方式"]),
        ("appearance", "颜色与视觉效果", ["视觉效果", "Windows 配色", "桌面背景", "锁屏图片"])
    ];
    private static readonly Dictionary<string, string> ShortLabels = new()
    {
        ["Windows 11 安装检查"] = "安装检查", ["设备自动加密"] = "自动加密",
        ["开始菜单电源按钮旁文件夹"] = "电源旁文件夹", ["任务栏结束任务"] = "结束任务入口",
        ["资源管理器操作"] = "菜单与打开位置", ["系统桌面图标"] = "系统图标", ["Edge 桌面快捷方式"] = "Edge 快捷方式"
    };
    public static SummaryPresentation FromText(string? summary)
    {
        var buckets = Categories.ToDictionary(c => c.Kind, _ => new List<SummaryRow>());
        var extra = new List<SummaryRow>(); var reviews = new List<SummaryReview>(); var notices = new List<string>();
        foreach (var line in (summary ?? "").Split('\n').Select(l => l.Trim()).Where(l => l.Length > 0))
        {
            if (line.StartsWith("请核对镜像、", StringComparison.Ordinal)) { notices.Add(line); continue; }
            int colon = line.IndexOf('：');
            string label = colon >= 0 ? line[..colon] : "补充说明", value = colon >= 0 ? line[(colon + 1)..].Trim() : line;
            if (label is "生成前待填" or "安装时仍需操作" or "安装时可能需要操作")
            {
                bool needsAttention = label == "生成前待填" ? !value.StartsWith("未发现空缺必填项", StringComparison.Ordinal) : !value.StartsWith("所列步骤已预设", StringComparison.Ordinal);
                reviews.Add(new(label, value, needsAttention)); continue;
            }
            var category = Categories.FirstOrDefault(c => c.Labels.Contains(label));
            var row = new SummaryRow(label, ShortLabels.GetValueOrDefault(label, label), value);
            if (category.Kind != null) buckets[category.Kind].Add(row); else extra.Add(row);
        }
        var sections = Categories.Where(c => buckets[c.Kind].Count > 0).Select(c => new SummarySection(c.Kind, c.Title, buckets[c.Kind])).ToList();
        if (extra.Count > 0) sections.Add(new("other", "其他信息", extra));
        return new(sections, reviews, notices);
    }
}
