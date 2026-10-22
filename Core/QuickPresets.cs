namespace UnattendLocal.Core;

// Mirrors the website's Minimal and LocalUser starting points using the existing model.
public static class QuickPresets
{
    public const string Minimal = "最小配置";
    public const string LocalAccount = "仅创建本地账户";

    public static InstallPlan Create(string kind)
    {
        if (kind is not (Minimal or LocalAccount)) throw new ArgumentException("请选择有效的快捷预设。");
        var plan = new InstallPlan
        {
            ConfigurationMode = "自定义配置",
            CustomStartingPoint = kind,
            InteractiveLanguage = true,
            TimeZoneMode = "由 Windows 决定",
            AccountMode = "安装时添加微软账户",
            AutoLogon = false,
            PasswordExpiration = "Windows 默认",
            PrivacyMode = "安装时手动选择",
            HideOnlineAccount = false,
            KeepSensitiveFiles = true,
            ObscurePasswords = false
        };
        if (kind == LocalAccount)
        {
            plan.AccountMode = "自动创建本地账户";
            plan.AutoLogon = true;
            plan.KeepSensitiveFiles = false;
            plan.Users.Add(new UserEntry { Name = "Example", Administrator = true, AllowEmptyPassword = true });
        }
        return plan;
    }
}
