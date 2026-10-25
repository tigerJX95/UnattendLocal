using System.Drawing;
using System.Globalization;
using Schneegans.Unattend;

namespace UnattendLocal.Core;

public static class WallpaperSettings
{
    public const string Default = "使用 Windows 默认背景", Solid = "使用纯色背景", Script = "使用图片背景（脚本加载）";
    public static bool IsSolid(InstallPlan plan) => plan.WallpaperMode == Solid;
    public static bool IsScript(InstallPlan plan) => plan.WallpaperMode == Script;
    public static IWallpaperSettings Build(InstallPlan plan)
    {
        if (plan.WallpaperMode == Default) return new DefaultWallpaperSettings();
        if (IsScript(plan)) return new ScriptWallpaperSettings(ImageBackgroundSettings.ValidateScript(plan.WallpaperImageScript, global::UnattendLocal.Localization.UiText.Source("text.e8af3df2fae6")));
        if (!IsSolid(plan)) throw new ArgumentException(global::UnattendLocal.Localization.UiText.Source("text.c54c28634450"));
        if (!ColorSettings.TryNormalize(plan.WallpaperColorHex, out var hex)) throw new ArgumentException(global::UnattendLocal.Localization.UiText.Source("text.4ba3adbcb03b"));
        int rgb = int.Parse(hex.AsSpan(1), NumberStyles.HexNumber, CultureInfo.InvariantCulture);
        return new SolidWallpaperSettings(Color.FromArgb(rgb >> 16, (rgb >> 8) & 255, rgb & 255));
    }
    public static string ModeSummary(InstallPlan plan) => plan.WallpaperMode switch
    {
        Default => global::UnattendLocal.Localization.UiText.Source("text.0989e32d9ac3"),
        Solid => ColorSettings.TryNormalize(plan.WallpaperColorHex, out var hex) ? global::UnattendLocal.Localization.UiText.Concat(new object?[] { global::UnattendLocal.Localization.UiText.Source("text.d7b543eff71f"), hex },new bool[]{false,false}) : global::UnattendLocal.Localization.UiText.Source("text.773edb91fc04"),
        Script => string.IsNullOrWhiteSpace(plan.WallpaperImageScript) ? global::UnattendLocal.Localization.UiText.Source("text.94f354712d0f") : global::UnattendLocal.Localization.UiText.Source("text.11c57a9b45fc"),
        _ => global::UnattendLocal.Localization.UiText.Source("text.058a6ecea59c")
    };
    public static string Summary(InstallPlan plan) => global::UnattendLocal.Localization.UiText.Concat(new object?[] { global::UnattendLocal.Localization.UiText.Source("text.ff8ec58d5c4d"), ModeSummary(plan) },new bool[]{false,false});
}
