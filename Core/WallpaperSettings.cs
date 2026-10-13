using System.Drawing;
using System.Globalization;
using Schneegans.Unattend;

namespace UnattendLocal.Core;

public static class WallpaperSettings
{
    public const string Default = "使用 Windows 默认背景", Solid = "使用纯色背景";
    public static bool IsSolid(InstallPlan plan) => plan.WallpaperMode == Solid;
    public static IWallpaperSettings Build(InstallPlan plan)
    {
        if (plan.WallpaperMode == Default) return new DefaultWallpaperSettings();
        if (!IsSolid(plan)) throw new ArgumentException("请选择有效的桌面背景设置方式。");
        if (!ColorSettings.TryNormalize(plan.WallpaperColorHex, out var hex)) throw new ArgumentException("桌面背景颜色应为 #RRGGBB，例如 #003366；只接受六位十六进制颜色。");
        int rgb = int.Parse(hex.AsSpan(1), NumberStyles.HexNumber, CultureInfo.InvariantCulture);
        return new SolidWallpaperSettings(Color.FromArgb(rgb >> 16, (rgb >> 8) & 255, rgb & 255));
    }
    public static string ModeSummary(InstallPlan plan) => plan.WallpaperMode switch
    {
        Default => "Windows 默认背景（不写入背景覆盖）",
        Solid => ColorSettings.TryNormalize(plan.WallpaperColorHex, out var hex) ? "纯色背景 " + hex : "纯色背景：颜色无效，请输入 #RRGGBB",
        _ => "背景设置方式无效，请重新选择"
    };
    public static string Summary(InstallPlan plan) => "桌面背景：" + ModeSummary(plan);
}
