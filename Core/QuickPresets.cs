namespace UnattendLocal.Core;

// Mirrors the website's Minimal and LocalUser starting points using the existing model.
public static class QuickPresets
{
    public const string Minimal = "最小配置";
    public const string LocalAccount = "仅创建本地账户";

    public static InstallPlan Create(string kind)
    {
        if (kind is not (Minimal or LocalAccount)) throw new ArgumentException(global::UnattendLocal.Localization.UiText.Source("text.1d76f3abbde6"));
        var plan = new InstallPlan
        {
            ConfigurationMode = global::UnattendLocal.Localization.UiText.Source("text.cb1e0e22c7de"),
            CustomStartingPoint = kind,
            InteractiveLanguage = true,
            TimeZoneMode = global::UnattendLocal.Localization.UiText.Source("text.499ff33c1078"),
            AccountMode = global::UnattendLocal.Localization.UiText.Source("text.5f78e95b6bd9"),
            AutoLogon = false,
            PasswordExpiration = global::UnattendLocal.Localization.UiText.Source("text.1823400b7251"),
            PrivacyMode = global::UnattendLocal.Localization.UiText.Source("text.fd17c35ec3cc"),
            HideOnlineAccount = false,
            KeepSensitiveFiles = true,
            ObscurePasswords = false
        };
        if (kind == LocalAccount)
        {
            plan.AccountMode = global::UnattendLocal.Localization.UiText.Source("text.cdaa9a6700f6");
            plan.AutoLogon = true;
            plan.KeepSensitiveFiles = false;
            plan.Users.Add(new UserEntry { Name = "Example", Administrator = true, AllowEmptyPassword = true });
        }
        return plan;
    }
}
