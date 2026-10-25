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
        _ => throw new ArgumentException(global::UnattendLocal.Localization.UiText.Format("message.6b3886fd3683", new string[] { global::UnattendLocal.Localization.UiText.Arg((label), 0, null) }))
    };
    public static IColorSettings Build(InstallPlan plan)
    {
        if (plan.ColorSettingsMode == Default) return new DefaultColorSettings();
        if (!IsCustom(plan)) throw new ArgumentException(global::UnattendLocal.Localization.UiText.Source("text.5c1ae03ec732"));
        var system = Theme(plan.WindowsColorTheme, global::UnattendLocal.Localization.UiText.Source("text.fac85ce99e72"));
        var apps = Theme(plan.AppsColorTheme, global::UnattendLocal.Localization.UiText.Source("text.68357d2d3318"));
        if (!TryNormalize(plan.AccentColorHex, out var hex)) throw new ArgumentException(global::UnattendLocal.Localization.UiText.Source("text.c2556d8fe330"));
        int rgb = int.Parse(hex.AsSpan(1), NumberStyles.HexNumber, CultureInfo.InvariantCulture);
        return new CustomColorSettings(system, apps, plan.EnableColorTransparency, StartAccentEnabled(plan), plan.AccentColorOnBorders, Color.FromArgb(rgb >> 16, (rgb >> 8) & 255, rgb & 255));
    }
    private static string State(bool value) => value ? global::UnattendLocal.Localization.UiText.Source("text.8da97ddda990") : global::UnattendLocal.Localization.UiText.Source("text.3fd47edce45b");
    public static string ModeSummary(InstallPlan plan)
    {
        if (plan.ColorSettingsMode == Default) return global::UnattendLocal.Localization.UiText.Source("text.928f624718f7");
        if (!IsCustom(plan)) return global::UnattendLocal.Localization.UiText.Source("text.e63e1066f987");
        var accent = TryNormalize(plan.AccentColorHex, out var hex) ? hex : global::UnattendLocal.Localization.UiText.Source("text.2f54f0e00f68");
        var start = plan.WindowsColorTheme == Light ? global::UnattendLocal.Localization.UiText.Source("text.6936ed43f47e") : State(StartAccentEnabled(plan));
        return global::UnattendLocal.Localization.UiText.Format("message.0311e2a6d91b", new string[] { global::UnattendLocal.Localization.UiText.Arg((plan.WindowsColorTheme), 0, null), global::UnattendLocal.Localization.UiText.Arg((plan.AppsColorTheme), 0, null), global::UnattendLocal.Localization.UiText.Arg((accent), 0, null), global::UnattendLocal.Localization.UiText.Arg((State(plan.EnableColorTransparency)), 0, null), global::UnattendLocal.Localization.UiText.Arg((start), 0, null), global::UnattendLocal.Localization.UiText.Arg((State(plan.AccentColorOnBorders)), 0, null) });
    }
    public static string Summary(InstallPlan plan) => global::UnattendLocal.Localization.UiText.Concat(new object?[] { global::UnattendLocal.Localization.UiText.Source("text.22630e2b80b4"), ModeSummary(plan) },new bool[]{false,false});
}
