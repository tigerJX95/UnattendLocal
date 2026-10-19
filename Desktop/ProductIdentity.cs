using System.Reflection;

namespace UnattendLocal.Desktop;

public static class ProductIdentity
{
    public static string Version { get; } = typeof(ProductIdentity).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion.Split('+')[0]
        ?? typeof(ProductIdentity).Assembly.GetName().Version?.ToString(2) ?? "未知";
    public static string VersionLabel => "V" + Version;
    public static string WindowTitle => "Windows 无人值守配置工具 · " + VersionLabel;
    public static string ReadyStatus => "就绪 · " + VersionLabel;
}
