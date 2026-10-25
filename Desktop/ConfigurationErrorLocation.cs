using UnattendLocal.Core;

namespace UnattendLocal.Desktop;

public sealed record ConfigurationErrorLocation(SettingSearchResult Setting, int? UserIndex = null)
{
    public string Path => Setting.Page + " → " + Setting.Title;
    public string ActionText => Setting.Target == "SettingAnchor000" ? global::UnattendLocal.Localization.UiText.Source("text.555ac7a39a71") : global::UnattendLocal.Localization.UiText.Source("text.90e01acb5c9e");

    public static ConfigurationErrorLocation? FromException(Exception error) => error is ConfigurationValidationException known
        ? FromField(known.Field, known.UserIndex) : null;

    public static ConfigurationErrorLocation? FromField(string field, int? userIndex = null)
    {
        if (field is "Users.Name" or "Users.Password" or nameof(InstallPlan.Users)) {
            string target = field == "Users.Name" ? "UserNameEditor" : field == "Users.Password" ? "UserPasswordEditor" : "AddUserButton";
            string title = field == "Users.Name" ? global::UnattendLocal.Localization.UiText.Source("text.077ee596fe81") : field == "Users.Password" ? global::UnattendLocal.Localization.UiText.Source("text.6d1b4fc22349") : global::UnattendLocal.Localization.UiText.Source("text.2115004781b1");
            if (userIndex.HasValue) title += global::UnattendLocal.Localization.UiText.Format("message.cada219637ac", new string[] { global::UnattendLocal.Localization.UiText.Arg((userIndex.Value + 1), 0, null) });
            return new(new(title, global::UnattendLocal.Localization.UiText.Source("text.4e21977cff0f"), 4, target, "user", ""), userIndex);
        }
        string targetName = field switch {
            nameof(InstallPlan.PageFileInitialMiB) => "PageFileInitialEditor",
            nameof(InstallPlan.PageFileMaximumMiB) => "PageFileMaximumEditor",
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
