namespace UnattendLocal.Desktop;

// Match application-produced diagnostic states, never generic words in user content.
public static class DiagnosticText
{
    public static bool IsErrorSummary(string? text)
    {
        string value = (text ?? "").TrimStart();
        return new[] {
            "首选电源计划：待配置；", "首选电源计划：与启用的额外 XML 组件重复", "Windows 错误报告：与启用的额外 XML 组件重复",
            "静态 IPv4：待配置；", "静态 IPv6：待配置；", "网段预览：",
            "安装驱动目录：已启用，目录待配置", "AppLocker：已启用，策略 XML 待补充或校验",
            "XML 导入：XML 原文未随方案保存", "XML 导入：已导入；存在阻止导出的项目"
        }.Any(prefix => value.StartsWith(prefix, StringComparison.Ordinal));
    }
}
