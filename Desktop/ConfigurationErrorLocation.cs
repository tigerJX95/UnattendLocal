using UnattendLocal.Core;

namespace UnattendLocal.Desktop;

public sealed record ConfigurationErrorLocation(SettingSearchResult Setting, int? UserIndex = null)
{
    public string Path => Setting.Page + " → " + Setting.Title;
    public string ActionText => Setting.Target == "SettingAnchor000" ? "查看导入说明" : "前往修改";

    public static ConfigurationErrorLocation? FromException(Exception error) => error is ConfigurationValidationException known
        ? FromField(known.Field, known.UserIndex) : null;

    public static ConfigurationErrorLocation? FromField(string field, int? userIndex = null)
    {
        if (field is "Users.Name" or "Users.Password" or nameof(InstallPlan.Users)) {
            string target = field == "Users.Name" ? "UserNameEditor" : field == "Users.Password" ? "UserPasswordEditor" : "AddUserButton";
            string title = field == "Users.Name" ? "本地账户名称" : field == "Users.Password" ? "本地账户密码" : "添加本地账户";
            if (userIndex.HasValue) title += $"（第 {userIndex.Value + 1} 个账户）";
            return new(new(title, "用户与密码", 4, target, "user", ""), userIndex);
        }
        string targetName = field switch {
            nameof(InstallPlan.PowerPlanMode) => "PowerPlanModeEditor",
            nameof(InstallPlan.CustomPowerPlanGuid) => "PowerPlanGuidEditor",
            nameof(InstallPlan.ErrorReportingMode) => "ErrorReportingModeEditor",
            nameof(InstallPlan.XmlComponents) => "AdditionalXmlSection",
            nameof(InstallPlan.StaticIPv4Mac) => "StaticIPv4MacEditor",
            nameof(InstallPlan.StaticIPv4Address) => "StaticIPv4AddressEditor",
            nameof(InstallPlan.StaticIPv4SubnetMask) or nameof(InstallPlan.StaticIPv4Prefix) => "StaticIPv4SubnetEditor",
            nameof(InstallPlan.StaticIPv4Gateway) => "StaticIPv4GatewayEditor",
            nameof(InstallPlan.StaticIPv4Dns) => "StaticIPv4DnsEditor",
            nameof(InstallPlan.StaticIPv6Address) => "StaticIPv6AddressEditor",
            nameof(InstallPlan.StaticIPv6Prefix) => "StaticIPv6PrefixEditor",
            nameof(InstallPlan.StaticIPv6Gateway) => "StaticIPv6GatewayEditor",
            nameof(InstallPlan.StaticIPv6Dns) => "StaticIPv6DnsEditor",
            nameof(InstallPlan.SetupDriverPaths) => "SetupDriverPathsEditor",
            nameof(InstallPlan.ImportedXml) => "SettingAnchor000",
            nameof(InstallPlan.AccountMode) => "SettingAnchor070",
            nameof(InstallPlan.BuiltinAdministratorPassword) => "BuiltinAdministratorPassword",
            nameof(InstallPlan.ComputerName) => "SettingAnchor007",
            nameof(InstallPlan.CustomProductKey) => "SettingAnchor044",
            nameof(InstallPlan.ActivationProductKey) => "SettingAnchor047",
            nameof(InstallPlan.EditionId) => "SettingAnchor043",
            nameof(InstallPlan.EditionMode) => "SettingAnchor042",
            nameof(InstallPlan.PasswordDays) => "SettingAnchor075",
            nameof(InstallPlan.KeepSensitiveFiles) => "SettingAnchor140",
            nameof(InstallPlan.TimeZone) => "SettingAnchor022",
            nameof(InstallPlan.Architecture) => "SettingAnchor009",
            _ => field // WPF binding validation supplies the actual named control.
        };
        var setting = SettingsSearchCatalog.All.FirstOrDefault(candidate => candidate.Target == targetName && candidate.AppId.Length == 0);
        return setting is null ? null : new(setting, userIndex);
    }
}
