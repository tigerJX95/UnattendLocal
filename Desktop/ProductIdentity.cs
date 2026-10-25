using System.Reflection;

namespace UnattendLocal.Desktop;

public static class ProductIdentity
{
    public static string Version { get; } = typeof(ProductIdentity).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion.Split('+')[0]
        ?? typeof(ProductIdentity).Assembly.GetName().Version?.ToString(2) ?? global::UnattendLocal.Localization.UiText.Source("text.4d8c1c5b4283");
    public static string VersionLabel => "V" + Version;
    public static string WindowTitle => global::UnattendLocal.Localization.UiText.Concat(new object?[] { global::UnattendLocal.Localization.UiText.Source("text.79ac728c2960"), VersionLabel },new bool[]{false,false});
    public static string ReadyStatus => global::UnattendLocal.Localization.UiText.Concat(new object?[] { global::UnattendLocal.Localization.UiText.Source("text.a7779e475609"), VersionLabel },new bool[]{false,false});
}
