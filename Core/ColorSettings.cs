using System.Drawing;
using System.Globalization;
using System.Text.RegularExpressions;
using Schneegans.Unattend;

namespace UnattendLocal.Core;

public static class ColorSettings
{
    public const string Default = "使用 Windows 默认配色", Custom = "自定义 Windows 配色", Light = "浅色", Dark = "深色";
    public static bool IsCustom(InstallPlan plan) => plan.ColorSettingsMode == Custom;
    public static bool StartAccentEnabled(InstallPlan plan) => IsCustom(plan) && plan.WindowsColorTheme == Dark && plan.AccentColorOnStart;
    public static bool TryNormalize(string? value, out string normalized)
    {
        normalized = value?.Trim().ToUpperInvariant() ?? "";
        return Regex.IsMatch(normalized, @"\A#[0-9A-F]{6}\z");
    }
    private static ColorTheme Theme(string value, string label) => value switch
    {
        Light => ColorTheme.Light, Dark => ColorTheme.Dark,
        _ => throw new ArgumentException($"请在 配色与背景页选择有效的{label}：浅色或深色。")
    };
    public static IColorSettings Build(InstallPlan plan)
    {
        if (plan.ColorSettingsMode == Default) return new DefaultColorSettings();
        if (!IsCustom(plan)) throw new ArgumentException("请选择有效的 Windows 配色设置方式。");
        var system = Theme(plan.WindowsColorTheme, "系统模式");
        var apps = Theme(plan.AppsColorTheme, "应用模式");
        if (!TryNormalize(plan.AccentColorHex, out var hex)) throw new ArgumentException("Windows 强调色应为 #RRGGBB，例如 #0078D4；只接受六位十六进制颜色。");
        int rgb = int.Parse(hex.AsSpan(1), NumberStyles.HexNumber, CultureInfo.InvariantCulture);
        return new CustomColorSettings(system, apps, plan.EnableColorTransparency, StartAccentEnabled(plan), plan.AccentColorOnBorders, Color.FromArgb(rgb >> 16, (rgb >> 8) & 255, rgb & 255));
    }
    private static string State(bool value) => value ? "开启" : "关闭";
    public static string ModeSummary(InstallPlan plan)
    {
        if (plan.ColorSettingsMode == Default) return "Windows 默认配色（不写入颜色主题覆盖）";
        if (!IsCustom(plan)) return "配色设置方式无效，请重新选择";
        var accent = TryNormalize(plan.AccentColorHex, out var hex) ? hex : "无效，请输入 #RRGGBB";
        var start = plan.WindowsColorTheme == Light ? "不生效（系统浅色模式）" : State(StartAccentEnabled(plan));
        return $"系统{plan.WindowsColorTheme}；应用{plan.AppsColorTheme}；强调色 {accent}；透明效果{State(plan.EnableColorTransparency)}；开始 / 任务栏强调色{start}；标题栏 / 边框强调色{State(plan.AccentColorOnBorders)}";
    }
    public static string Summary(InstallPlan plan) => "Windows 配色：" + ModeSummary(plan);
}
