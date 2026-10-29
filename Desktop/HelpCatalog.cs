using UnattendLocal.Core;

namespace UnattendLocal.Desktop;

public record HelpSection(string Text, bool IsError = false);
public record HelpTopic(string Id, string Title, string Body)
{
    public HelpSection[]? Parts { get; init; }
    public IReadOnlyList<HelpSection> Sections => Parts ?? [new(Body)];
}

// 中文整理：上游网站操作说明 + 当前固定版本的生成器行为。
// 提示只描述设置，不读取或显示账户 / Wi-Fi 密码。
public static class HelpCatalog
{
    public const string SourceUrl = "https://schneegans.de/windows/unattend-generator/";
    private static readonly HelpTopic[][] Pages =
    [
        [
            new("lockkeys", global::UnattendLocal.Localization.UiText.Source("text.d1e1632c4257"), global::UnattendLocal.Localization.UiText.Source("text.2e964a9e252e")),
            new("numlock", global::UnattendLocal.Localization.UiText.Source("text.532261fe8fec"), global::UnattendLocal.Localization.UiText.Source("text.e7f4ad59bc68")),
            new("capslock", global::UnattendLocal.Localization.UiText.Source("text.66eab4b8966a"), global::UnattendLocal.Localization.UiText.Source("text.3fd520c515d6")),
            new("scrolllock", global::UnattendLocal.Localization.UiText.Source("text.39eab180560a"), global::UnattendLocal.Localization.UiText.Source("text.8f4d174f08e8")),
            new("lockbehavior", global::UnattendLocal.Localization.UiText.Source("text.91e89eef3156"), global::UnattendLocal.Localization.UiText.Source("text.9b3bc43015ec")),
            new("pointerprecision", global::UnattendLocal.Localization.UiText.Source("text.304f774461c8"), global::UnattendLocal.Localization.UiText.Source("text.99296b5186a7")),
            new("stickykeys", global::UnattendLocal.Localization.UiText.Source("text.89c3759ef74f"), global::UnattendLocal.Localization.UiText.Source("text.61c972d350db")),
            new("stickyhotkey", global::UnattendLocal.Localization.UiText.Source("text.8acf9b351153"), global::UnattendLocal.Localization.UiText.Source("text.66c6ab5a8fd7")),
            new("stickyhotkeysound", global::UnattendLocal.Localization.UiText.Source("text.99c64bfd2faa"), global::UnattendLocal.Localization.UiText.Source("text.ff6a00f9b6e5")),
            new("stickyindicator", global::UnattendLocal.Localization.UiText.Source("text.7be829547dd4"), global::UnattendLocal.Localization.UiText.Source("text.a427f5cff4e8")),
            new("stickyaudible", global::UnattendLocal.Localization.UiText.Source("text.77d497330cad"), global::UnattendLocal.Localization.UiText.Source("text.f711bbdfbac2")),
            new("stickytristate", global::UnattendLocal.Localization.UiText.Source("text.37be147529e9"), global::UnattendLocal.Localization.UiText.Source("text.2868c461e3d0")),
            new("stickytwokeysoff", global::UnattendLocal.Localization.UiText.Source("text.434b4e7fe0f3"), global::UnattendLocal.Localization.UiText.Source("text.19abb573843d")),
            new("registeredinformation",global::UnattendLocal.Localization.UiText.Source("text.5d922bf0452e"),RegisteredInformationSettings.Description),
            new("registeredowner",global::UnattendLocal.Localization.UiText.Source("text.b36d05aba66e"),RegisteredInformationSettings.Description),
            new("registeredorganization",global::UnattendLocal.Localization.UiText.Source("text.3f38e493103b"),RegisteredInformationSettings.Description),
            new("computer", global::UnattendLocal.Localization.UiText.Source("text.582424e3c3fb"), ComputerNamingSettings.Description),
            new("extralanguages",global::UnattendLocal.Localization.UiText.Source("text.81a3c4669e25"),LanguagePreferences.Description),
            new("architecture", global::UnattendLocal.Localization.UiText.Source("text.07d058a0bde0"), global::UnattendLocal.Localization.UiText.Source("text.4aa5c564c42e")),
            new("language", global::UnattendLocal.Localization.UiText.Source("text.b0609c5fc7e6"), global::UnattendLocal.Localization.UiText.Source("text.1903dcd11951")),
            new("region", global::UnattendLocal.Localization.UiText.Source("text.f706f5fccd83"), global::UnattendLocal.Localization.UiText.Source("text.a97f15dfb167"))
        ],
        [
            new("accountlockout", global::UnattendLocal.Localization.UiText.Source("text.fa67952632ed"), global::UnattendLocal.Localization.UiText.Source("text.d4c0b7c42b3c")),
            new("lockoutthreshold", global::UnattendLocal.Localization.UiText.Source("text.d8c67d877a42"), global::UnattendLocal.Localization.UiText.Source("text.748c397d1d4b")),
            new("lockoutreset", global::UnattendLocal.Localization.UiText.Source("text.3a1a38579b7c"), global::UnattendLocal.Localization.UiText.Source("text.0f48fae83001")),
            new("lockoutduration", global::UnattendLocal.Localization.UiText.Source("text.4d6a1e5831c7"), global::UnattendLocal.Localization.UiText.Source("text.f61151a6e067")),
            new("account", global::UnattendLocal.Localization.UiText.Source("text.d8ee97da527d"), global::UnattendLocal.Localization.UiText.Source("text.a9369cde71bf")),
            new("user", global::UnattendLocal.Localization.UiText.Source("text.359de92d0d9f"), global::UnattendLocal.Localization.UiText.Source("text.933fd5e5d6a5")),
            new("empty", global::UnattendLocal.Localization.UiText.Source("text.591b628eb807"), global::UnattendLocal.Localization.UiText.Source("text.e7f56e03cd83")),
            new("logon", global::UnattendLocal.Localization.UiText.Source("text.e7e34d265eb4"), FirstLogonSettings.Description),
            new("builtinlogon", global::UnattendLocal.Localization.UiText.Source("text.318767e623fc"), FirstLogonSettings.Description),
            new("expiration", global::UnattendLocal.Localization.UiText.Source("text.444f1003027d"), global::UnattendLocal.Localization.UiText.Source("text.06670e4edb97")),
            new("secrets", global::UnattendLocal.Localization.UiText.Source("text.943befbfbf85"), global::UnattendLocal.Localization.UiText.Source("text.4fb8d85fce2f"))
        ],
        [
            new("workgroup",global::UnattendLocal.Localization.UiText.Source("text.aba99991e57d"),WorkgroupSettings.Description),
            new("staticipv6", global::UnattendLocal.Localization.UiText.Source("text.c8799a9ed706"), StaticIPv4Settings.Description),
            new("staticv6prefix", global::UnattendLocal.Localization.UiText.Source("text.46081930dc9f"), global::UnattendLocal.Localization.UiText.Source("text.5551f2ceaa87")),
            new("staticv6gateway", global::UnattendLocal.Localization.UiText.Source("text.98a14cd61cbb"), global::UnattendLocal.Localization.UiText.Source("text.d93318a3105d")),
            new("staticv6dns", "IPv6 DNS", global::UnattendLocal.Localization.UiText.Source("text.c5a40dd844db")),
            new("staticprefix", global::UnattendLocal.Localization.UiText.Source("text.cbe92cd83d4a"), global::UnattendLocal.Localization.UiText.Source("text.8a2cb962f804")),
            new("staticipv4", global::UnattendLocal.Localization.UiText.Source("text.135a18a0d21a"), StaticIPv4Settings.Description),
            new("staticmask", global::UnattendLocal.Localization.UiText.Source("text.433752fe2112"), global::UnattendLocal.Localization.UiText.Source("text.c11c12664fe6")),
            new("staticmac", global::UnattendLocal.Localization.UiText.Source("text.4bc08fc87160"), global::UnattendLocal.Localization.UiText.Source("text.3a4e6d4a1f74")),
            new("staticgateway", global::UnattendLocal.Localization.UiText.Source("text.9c64973c4dbe"), global::UnattendLocal.Localization.UiText.Source("text.1ae380aef784")),
            new("staticdns", global::UnattendLocal.Localization.UiText.Source("text.4baea824392f"), global::UnattendLocal.Localization.UiText.Source("text.5edb8e349286")),
            new("wifiprofile",global::UnattendLocal.Localization.UiText.Source("text.dcaf8219f22d"),global::UnattendLocal.Localization.UiText.Source("text.bed01b8350d0")),
            new("wifi", global::UnattendLocal.Localization.UiText.Source("text.7585399b0923"), global::UnattendLocal.Localization.UiText.Source("text.a4896f48cb73")),
            new("ssid", global::UnattendLocal.Localization.UiText.Source("text.8a64860a50b7"), global::UnattendLocal.Localization.UiText.Source("text.384ab9cbbf31")),
            new("hidden", global::UnattendLocal.Localization.UiText.Source("text.26c897391a13"), global::UnattendLocal.Localization.UiText.Source("text.aa8f76a64c13")),
            new("offline", global::UnattendLocal.Localization.UiText.Source("text.f9c3ad867cb7"), global::UnattendLocal.Localization.UiText.Source("text.cbfe38df8f66")),
            new("driver", global::UnattendLocal.Localization.UiText.Source("text.e8196a61686d"), global::UnattendLocal.Localization.UiText.Source("text.42d405f63e9f"))
        ],
        [
            new("privacy", global::UnattendLocal.Localization.UiText.Source("text.7ee415979a64"), global::UnattendLocal.Localization.UiText.Source("text.b1a7c1c94abb")),
            new("online", global::UnattendLocal.Localization.UiText.Source("text.1a027612c20c"), global::UnattendLocal.Localization.UiText.Source("text.96121ae5965d")),
            new("eula", global::UnattendLocal.Localization.UiText.Source("text.020338934a26"), global::UnattendLocal.Localization.UiText.Source("text.a895505cedf1")),
            new("oem", global::UnattendLocal.Localization.UiText.Source("text.33043b017b23"), global::UnattendLocal.Localization.UiText.Source("text.ef713239f1c4")),
            new("appsuggestions", global::UnattendLocal.Localization.UiText.Source("text.1d8dadc1b1d5"), global::UnattendLocal.Localization.UiText.Source("text.650d455f9603")),
            new("windowsupdate", global::UnattendLocal.Localization.UiText.Source("text.1225202a13e3"), global::UnattendLocal.Localization.UiText.Source("text.65702b1dbf39")),
            new("edgefre", global::UnattendLocal.Localization.UiText.Source("text.6aadf835a13b"), global::UnattendLocal.Localization.UiText.Source("text.769b676dcb3a")),
            new("edgebackground", global::UnattendLocal.Localization.UiText.Source("text.d18749fcc693"), global::UnattendLocal.Localization.UiText.Source("text.65bfadaf9b4b")),
            new("longpaths", global::UnattendLocal.Localization.UiText.Source("text.38b14c74389b"), global::UnattendLocal.Localization.UiText.Source("text.34374f403b4f")),
            new("systemsounds", global::UnattendLocal.Localization.UiText.Source("text.aaa9a8f072bd"), global::UnattendLocal.Localization.UiText.Source("text.df56ec6a9c7d")),
            new("narrator",global::UnattendLocal.Localization.UiText.Source("text.d45604280001"),AdditionalSetupSettings.Description("UseNarrator")),
            new("edgeuninstall",global::UnattendLocal.Localization.UiText.Source("text.3c2010a271a7"),AdditionalSetupSettings.Description("MakeEdgeUninstallable")),
            new("systemrestore",global::UnattendLocal.Localization.UiText.Source("text.63e5e50b2b64"),AdditionalSetupSettings.Description("DisableSystemRestore")),
            new("vmwaretools","VMware Tools",VmGuestToolsSettings.Description("VMwareTools")),
            new("vboxtools",global::UnattendLocal.Localization.UiText.Source("text.791aeb981543"),VmGuestToolsSettings.Description("VBoxGuestAdditions")),
            new("virtiotools","VirtIO Guest Tools",VmGuestToolsSettings.Description("VirtIoGuestTools")),
            new("parallelstools","Parallels Tools",VmGuestToolsSettings.Description("ParallelsTools")),
            new("applocker",global::UnattendLocal.Localization.UiText.Source("text.fb672eaf5972"),AppControlSettings.Description),
            new("pelog",global::UnattendLocal.Localization.UiText.Source("text.9278b2501e7b"),PeLogSettings.Description),
            new("setupdynamicupdate",global::UnattendLocal.Localization.UiText.Source("text.1ee43d4aa992"),SetupDynamicUpdateSettings.Description),
            new("setupdynamicui",global::UnattendLocal.Localization.UiText.Source("text.55fc38ab0f29"),global::UnattendLocal.Localization.UiText.Source("text.92ad512eb48f")),
            new("setupdrivers",global::UnattendLocal.Localization.UiText.Source("text.ea435b7a0464"),SetupDriverSettings.Description),
            new("setupdriverpaths",global::UnattendLocal.Localization.UiText.Source("text.c8b39163bc55"),global::UnattendLocal.Localization.UiText.Source("text.278aba32d56c")),
            new("configset",global::UnattendLocal.Localization.UiText.Source("text.a0c25e67053c"),ConfigurationSetSettings.Description),
            new("sac",global::UnattendLocal.Localization.UiText.Source("text.bc695532793b"),SecurityOptions.Description(nameof(InstallPlan.DisableSac))),
            new("smartscreen",global::UnattendLocal.Localization.UiText.Source("text.ff473b49a6a6"),SecurityOptions.Description(nameof(InstallPlan.DisableSmartScreen))),
            new("coreisolation",global::UnattendLocal.Localization.UiText.Source("text.7b2b6db9d9af"),SecurityOptions.Description(nameof(InstallPlan.DisableCoreIsolation))),
            new("rootacl",global::UnattendLocal.Localization.UiText.Source("text.e875cabd1080"),SecurityOptions.Description(nameof(InstallPlan.HardenSystemDriveAcl))),
            new("uac",global::UnattendLocal.Localization.UiText.Source("text.b2771e6e82de"),ProtectionSettings.UacDescription),
            new("wpbt",global::UnattendLocal.Localization.UiText.Source("text.83cd4c6e5228"),ProtectionSettings.WpbtDescription),
            new("processaudit",global::UnattendLocal.Localization.UiText.Source("text.4d21ee8328ab"),ProcessAuditSettings.Description),
            new("updaterestart",global::UnattendLocal.Localization.UiText.Source("text.5c0d4ac1e048"),SystemMaintenanceSettings.RestartDescription),
            new("junctions", global::UnattendLocal.Localization.UiText.Source("text.32d71cdd1bb0"), JunctionSettings.Description),
            new("lastaccess",global::UnattendLocal.Localization.UiText.Source("text.ad9c574533ea"),SystemMaintenanceSettings.LastAccessDescription),
            new("remotedesktop",global::UnattendLocal.Localization.UiText.Source("text.0b35607d3abf"),AdvancedSystemSettings.RdpDescription),
            new("pspolicy",global::UnattendLocal.Localization.UiText.Source("text.c33ffb14d26a"),AdvancedSystemSettings.ScriptDescription),
            new("powerplan", global::UnattendLocal.Localization.UiText.Source("text.8190df8a26b0"), global::UnattendLocal.Localization.UiText.Source("text.e8e06ae00697")),
            new("errorreporting", global::UnattendLocal.Localization.UiText.Source("text.cc1c0c4c9120"), global::UnattendLocal.Localization.UiText.Source("text.e29e6d26542e")),
            new("faststartup", global::UnattendLocal.Localization.UiText.Source("text.479947bd2aab"), global::UnattendLocal.Localization.UiText.Source("text.4c98e8572fd3")),
            new("deviceencryption", global::UnattendLocal.Localization.UiText.Source("text.3dd6efa9eecd"), global::UnattendLocal.Localization.UiText.Source("text.18357c59ad8a")),
            new("hardware", global::UnattendLocal.Localization.UiText.Source("text.98bee575873a"), global::UnattendLocal.Localization.UiText.Source("text.0c6f149791b5")),
            new("legacyupgrade", global::UnattendLocal.Localization.UiText.Source("text.4e152ba8ef23"), LegacyUpgradeSettings.Description),
            new("hardwaretpm", global::UnattendLocal.Localization.UiText.Source("text.03d4d9f03919"), global::UnattendLocal.Localization.UiText.Source("text.92b2ba89d86f")),
            new("hardwareboot", global::UnattendLocal.Localization.UiText.Source("text.443cc44b8f86"), global::UnattendLocal.Localization.UiText.Source("text.0528b2706650")),
            new("hardwareram", global::UnattendLocal.Localization.UiText.Source("text.3d712e0edce3"), global::UnattendLocal.Localization.UiText.Source("text.dba9b9edd157")),
            new("powershell", global::UnattendLocal.Localization.UiText.Source("text.7c2eae94d517"), global::UnattendLocal.Localization.UiText.Source("text.610040dfabc3")),
            new("sensitivefiles", global::UnattendLocal.Localization.UiText.Source("text.0b8cc964217a"), global::UnattendLocal.Localization.UiText.Source("text.6f06bff4e5be"))
        ],
        [
            new("preview", global::UnattendLocal.Localization.UiText.Source("text.b10faf01051d"), global::UnattendLocal.Localization.UiText.Source("text.3aa04546b05d")),
            new("exportfilename", global::UnattendLocal.Localization.UiText.Source("text.a78cc04ad5db"), global::UnattendLocal.Localization.UiText.Concat(new object?[] { AnswerFileExport.Usage(true), global::UnattendLocal.Localization.UiText.Source("text.ffe37b9f0a87") },new bool[]{false,false})),
            new("exportiso", global::UnattendLocal.Localization.UiText.Source("text.c6c90fe1aedd"), global::UnattendLocal.Localization.UiText.Source("text.4f318d67142c")),
            new("export", global::UnattendLocal.Localization.UiText.Source("text.6e37987f9baa"), global::UnattendLocal.Localization.UiText.Source("text.1c582b61e6ae")),
            new("save", global::UnattendLocal.Localization.UiText.Source("text.f7a618418a03"), global::UnattendLocal.Localization.UiText.Source("text.e741b8574710")),
            new("verify", global::UnattendLocal.Localization.UiText.Source("text.0a922eb8e993"), global::UnattendLocal.Localization.UiText.Source("text.3e8e91db5a2b"))
        ],
        [
            new("custom", global::UnattendLocal.Localization.UiText.Source("text.d726fe88e3a1"), global::UnattendLocal.Localization.UiText.Source("text.6d9a09a786e7")),
            new("timezone", global::UnattendLocal.Localization.UiText.Source("text.0756abdb17d0"), global::UnattendLocal.Localization.UiText.Source("text.a9427f33e641")),
            new("restartsignon", global::UnattendLocal.Localization.UiText.Source("text.29d265ce3beb"), global::UnattendLocal.Localization.UiText.Source("text.8fc2e88a5fb8")),
            new("deviceapps", global::UnattendLocal.Localization.UiText.Source("text.4d0d421c643c"), global::UnattendLocal.Localization.UiText.Source("text.8d107e8c6306")),
            new("windowsold", global::UnattendLocal.Localization.UiText.Source("text.f47435507d77"), global::UnattendLocal.Localization.UiText.Source("text.facca3c52f99")),
            new("compactos", global::UnattendLocal.Localization.UiText.Source("text.a019e8f52463"), global::UnattendLocal.Localization.UiText.Source("text.66679d954ec9")),
            new("image", global::UnattendLocal.Localization.UiText.Source("text.e0458a35f06e"), global::UnattendLocal.Localization.UiText.Source("text.85dea54e4351")),
            new("additionalxml", global::UnattendLocal.Localization.UiText.Source("text.998f70399011"), global::UnattendLocal.Localization.UiText.Source("text.5ff0989f0a03")),
            new("customscripts", global::UnattendLocal.Localization.UiText.Source("text.d0c97feab173"), global::UnattendLocal.Localization.UiText.Source("text.9d3f89786949")),
            new("scriptfiles", global::UnattendLocal.Localization.UiText.Source("text.e3cb1a0f6bfc"), global::UnattendLocal.Localization.UiText.Source("text.bf47550d25e2")),
            new("scriptcontent", global::UnattendLocal.Localization.UiText.Source("text.8f25b1aa5d53"), global::UnattendLocal.Localization.UiText.Source("text.56eb3a9ec59f")),
            new("diskscript", global::UnattendLocal.Localization.UiText.Source("text.c74c19056e32"), global::UnattendLocal.Localization.UiText.Source("text.5dfa72edebbe")),
            new("diskscriptexample", global::UnattendLocal.Localization.UiText.Source("text.79226b2f898e"), global::UnattendLocal.Localization.UiText.Source("text.ca6af0da67dc")),
            new("diskmode", global::UnattendLocal.Localization.UiText.Source("text.33092d9064a3"), global::UnattendLocal.Localization.UiText.Source("text.c58b1cfb4573")),
            new("disktarget", global::UnattendLocal.Localization.UiText.Source("text.c21d416d9112"), global::UnattendLocal.Localization.UiText.Source("text.7b7fb1fb5280")),
            new("diskformat", global::UnattendLocal.Localization.UiText.Source("text.0964255ac929"), global::UnattendLocal.Localization.UiText.Source("text.4b454963e223")),
            new("disklayout", global::UnattendLocal.Localization.UiText.Source("text.b8de49b69294"), global::UnattendLocal.Localization.UiText.Source("text.15dd831f9384")),
            new("systempartition", global::UnattendLocal.Localization.UiText.Source("text.de1165bef1bf"), global::UnattendLocal.Localization.UiText.Source("text.1343e1c51325")),
            new("partitionpause", global::UnattendLocal.Localization.UiText.Source("text.88ac2e924024"), global::UnattendLocal.Localization.UiText.Source("text.796a68711269")),
            new("diskconfirm", global::UnattendLocal.Localization.UiText.Source("text.1d836e4f54eb"), global::UnattendLocal.Localization.UiText.Source("text.4ad86ab27e7f")),
            new("edition", global::UnattendLocal.Localization.UiText.Source("text.4362abcaae1d"), global::UnattendLocal.Localization.UiText.Source("text.f948cb389136")),
            new("sku", global::UnattendLocal.Localization.UiText.Source("text.58dadad54b8b"), global::UnattendLocal.Localization.UiText.Source("text.a883c6233dd4")),
            new("installkey", global::UnattendLocal.Localization.UiText.Source("text.d98e871fb09b"), global::UnattendLocal.Localization.UiText.Source("text.8576dec5ab41")),
            new("activation", global::UnattendLocal.Localization.UiText.Source("text.ea71bb462122"), global::UnattendLocal.Localization.UiText.Source("text.4915b11ea19e"))
        ],
        [
            new("taskbarsearch", global::UnattendLocal.Localization.UiText.Source("text.c5b4794cc702"), global::UnattendLocal.Localization.UiText.Source("text.a24addb2277d")),
            new("taskbaricons", global::UnattendLocal.Localization.UiText.Source("text.27dfcbb03c53"), global::UnattendLocal.Localization.UiText.Source("text.68a6115733c2")),
            new("starttiles", global::UnattendLocal.Localization.UiText.Source("text.5d713274fde0"), global::UnattendLocal.Localization.UiText.Source("text.188833532294")),
            new("startpins", global::UnattendLocal.Localization.UiText.Source("text.8252e535891f"), global::UnattendLocal.Localization.UiText.Source("text.ad65473f6c93")),
            new("widgets", global::UnattendLocal.Localization.UiText.Source("text.49902cfaa3a7"), global::UnattendLocal.Localization.UiText.Source("text.6013f1987fd7")),
            new("taskbaralignment", global::UnattendLocal.Localization.UiText.Source("text.bfd564c34b91"), global::UnattendLocal.Localization.UiText.Source("text.ea93ef26f43a")),
            new("taskview", global::UnattendLocal.Localization.UiText.Source("text.22f7bf9d8880"), global::UnattendLocal.Localization.UiText.Source("text.7625c91cc0c1")),
            new("trayicons", global::UnattendLocal.Localization.UiText.Source("text.3e29ab494a7a"), global::UnattendLocal.Localization.UiText.Source("text.a4583531feef")),
            new("bingsearch", global::UnattendLocal.Localization.UiText.Source("text.a2f58bc02d0a"), global::UnattendLocal.Localization.UiText.Source("text.0f823d0e69ea"))
        ],
        [
            new("explorerhidden", global::UnattendLocal.Localization.UiText.Source("text.8641baa2e90a"), global::UnattendLocal.Localization.UiText.Source("text.0d84f5971c39")),
            new("explorerextensions", global::UnattendLocal.Localization.UiText.Source("text.ba2e8c9e5c9f"), global::UnattendLocal.Localization.UiText.Source("text.b07f68eeaeba")),
            new("explorercontext", global::UnattendLocal.Localization.UiText.Source("text.394d87c545a0"), global::UnattendLocal.Localization.UiText.Source("text.d77b4ce8344e")),
            new("explorertips", global::UnattendLocal.Localization.UiText.Source("text.61f23ba07fe9"), global::UnattendLocal.Localization.UiText.Source("text.cad50ac4f430")),
            new("explorerlaunch", global::UnattendLocal.Localization.UiText.Source("text.2fa6807a1930"), global::UnattendLocal.Localization.UiText.Source("text.e5759a52b98c")),
            new("taskbarendtask", global::UnattendLocal.Localization.UiText.Source("text.bd100ac26d3f"), global::UnattendLocal.Localization.UiText.Source("text.9688df6f4738"))
        ],
        [
            new("desktopicons", global::UnattendLocal.Localization.UiText.Source("text.d49ecddf530b"), global::UnattendLocal.Localization.UiText.Source("text.a57fddba9941")),
            new("edgeicon", global::UnattendLocal.Localization.UiText.Source("text.d694db6e8c6a"), global::UnattendLocal.Localization.UiText.Source("text.3c565f124abe")),
            new("startfolders", global::UnattendLocal.Localization.UiText.Source("text.99d2a9e7cac0"), global::UnattendLocal.Localization.UiText.Source("text.ebad06a1b79d"))
        ]
        ,[
            new("visualeffects", global::UnattendLocal.Localization.UiText.Source("text.48b160969d7c"), global::UnattendLocal.Localization.UiText.Source("text.bbe94973214e")),
            ..VisualEffectsSettings.Items.Select(item=>new HelpTopic(item.HelpId,item.Label,global::UnattendLocal.Localization.UiText.Concat(new object?[] { item.Description, global::UnattendLocal.Localization.UiText.Source("text.9ebc0bbec0e2") },new bool[]{false,false})))
        ]
        ,[
            new("wallpaper", global::UnattendLocal.Localization.UiText.Source("text.96fb8a19eb9b"), global::UnattendLocal.Localization.UiText.Source("text.1c804179fe14")),
            new("wallpaperimage", global::UnattendLocal.Localization.UiText.Source("text.71e39362571e"), global::UnattendLocal.Localization.UiText.Source("text.a1e0554987b1")),
            new("lockscreen", global::UnattendLocal.Localization.UiText.Source("text.c7a90b8fdf58"), global::UnattendLocal.Localization.UiText.Source("text.e9459f517139")),
            new("lockscreenimage", global::UnattendLocal.Localization.UiText.Source("text.02418e0cdcf3"), global::UnattendLocal.Localization.UiText.Source("text.8b59c0a0bb6a")),
            new("imageexample", global::UnattendLocal.Localization.UiText.Source("text.72393e0b5126"), global::UnattendLocal.Localization.UiText.Source("text.5d2aa6c4a2f5")),
            new("wallpapercolor", global::UnattendLocal.Localization.UiText.Source("text.8efdfa24f05f"), global::UnattendLocal.Localization.UiText.Source("text.cc5ab3f3f442")),
            new("windowscolors", global::UnattendLocal.Localization.UiText.Source("text.2a2fc47e77a2"), global::UnattendLocal.Localization.UiText.Source("text.a40d808f5727")),
            new("systemcolortheme", global::UnattendLocal.Localization.UiText.Source("text.fa52d383c4ae"), global::UnattendLocal.Localization.UiText.Source("text.3a33ce5010a3")),
            new("appscolortheme", global::UnattendLocal.Localization.UiText.Source("text.753c05f80981"), global::UnattendLocal.Localization.UiText.Source("text.fffb2cf2f43d")),
            new("accentcolor", global::UnattendLocal.Localization.UiText.Source("text.6dc739b4e978"), global::UnattendLocal.Localization.UiText.Source("text.9902e803f06f")),
            new("colortransparency", global::UnattendLocal.Localization.UiText.Source("text.3327fde37265"), global::UnattendLocal.Localization.UiText.Source("text.74bab704d538")),
            new("accentstart", global::UnattendLocal.Localization.UiText.Source("text.3a615e28c70b"), global::UnattendLocal.Localization.UiText.Source("text.df1ec53a8af4")),
            new("accentborders", global::UnattendLocal.Localization.UiText.Source("text.fe3692777d67"), global::UnattendLocal.Localization.UiText.Source("text.94d4b4ee53f6"))
        ]
    ];

    private static readonly HelpTopic[] RemovalTopics=[new("appremoval",global::UnattendLocal.Localization.UiText.Source("text.91ebd1a7c347"),global::UnattendLocal.Localization.UiText.Source("text.6fba7e0c8188"))];
    private static readonly HelpTopic[][] DisplayPages = Pages.Append(RemovalTopics)
        .Select(page => page.Select(topic => {
            var body = HelpText.Paragraphs(topic.Body);
            UnattendLocal.Localization.UiText.PinAuthoredText(body);
            return topic with { Body = body };
        }).ToArray()).ToArray();
    public static int PageCount => DisplayPages.Length;
    public static IReadOnlyList<HelpTopic> ForPage(int page) => DisplayPages[Math.Clamp(page, 0, DisplayPages.Length - 1)];

    public static HelpTopic Current(int page, string? id, InstallPlan plan, UserEntry? user)
    {
        var topics = ForPage(page);
        var topic = topics.FirstOrDefault(t => t.Id == id) ?? topics[0];
        string state = topic.Id switch
        {
            "narrator" or "edgeuninstall" or "systemrestore"=>AdditionalSetupSettings.HelpState(plan,topic.Id),
            "vmwaretools" or "vboxtools" or "virtiotools" or "parallelstools"=>VmGuestToolsSettings.HelpState(plan,topic.Id),
            "uac"=>plan.DisableUac?global::UnattendLocal.Localization.UiText.Source("text.12e4633b39dd"):global::UnattendLocal.Localization.UiText.Source("text.93fee6ea4014"),
            "applocker"=>AppControlSettings.Summary(plan),
            "configset"=>ConfigurationSetSettings.Summary(plan),
            "registeredinformation" or "registeredowner" or "registeredorganization"=>RegisteredInformationSettings.Summary(plan),
            "workgroup"=>WorkgroupSettings.Summary(plan),
            "pelog"=>PeLogSettings.Summary(plan),
            "setupdynamicupdate" or "setupdynamicui"=>SetupDynamicUpdateSettings.Summary(plan),
            "setupdrivers" or "setupdriverpaths"=>SetupDriverSettings.Summary(plan),
            "sac" or "smartscreen" or "coreisolation" or "rootacl"=>SecurityOptions.HelpState(plan,topic.Id),
            "wpbt"=>plan.DisableWpbt?global::UnattendLocal.Localization.UiText.Source("text.38760d81e09d"):global::UnattendLocal.Localization.UiText.Source("text.56521605359b"),
            "processaudit"=>global::UnattendLocal.Localization.UiText.Concat(new object?[] { global::UnattendLocal.Localization.UiText.Source("text.660648805666"), ProcessAuditSettings.ModeSummary(plan), "。" },new bool[]{false,false,false}),
            "updaterestart"=>plan.PreventAutomaticReboot?global::UnattendLocal.Localization.UiText.Source("text.89c92ff2f95c"):global::UnattendLocal.Localization.UiText.Source("text.613d5581bc26"),
            "junctions"=>JunctionSettings.Summary(plan),
            "lastaccess"=>plan.DisableLastAccess?global::UnattendLocal.Localization.UiText.Source("text.e3f845134c8f"):global::UnattendLocal.Localization.UiText.Source("text.56521605359b"),
            "remotedesktop"=>plan.EnableRemoteDesktop?global::UnattendLocal.Localization.UiText.Source("text.d9f09f53ec52"):global::UnattendLocal.Localization.UiText.Source("text.56521605359b"),
            "pspolicy"=>plan.AllowPowerShellScripts?global::UnattendLocal.Localization.UiText.Source("text.aa5204c0d642"):global::UnattendLocal.Localization.UiText.Source("text.4bd02b0917c4"),
            "appremoval"=>AppRemovalSettings.Selection(plan),
            "accountlockout" => global::UnattendLocal.Localization.UiText.Format("message.6f9f1bf897f7", new string[] { global::UnattendLocal.Localization.UiText.Arg((AccountLockoutPolicy.ModeSummary(plan)), 0, null) }),
            "lockoutthreshold" or "lockoutreset" or "lockoutduration" => plan.AccountLockoutMode!=AccountLockoutPolicy.Custom?global::UnattendLocal.Localization.UiText.Source("text.40ae862dc79c"):global::UnattendLocal.Localization.UiText.Format("message.6f9f1bf897f7", new string[] { global::UnattendLocal.Localization.UiText.Arg((AccountLockoutPolicy.ModeSummary(plan)), 0, null) }),
            "pointerprecision" => plan.DisablePointerPrecision?global::UnattendLocal.Localization.UiText.Source("text.e70bd1d47be8"):global::UnattendLocal.Localization.UiText.Source("text.56521605359b"),
            "stickykeys" => global::UnattendLocal.Localization.UiText.Format("message.6f9f1bf897f7", new string[] { global::UnattendLocal.Localization.UiText.Arg((StickyKeySettings.ModeSummary(plan)), 0, null) }),
            "stickyhotkey" or "stickyhotkeysound" or "stickyindicator" or "stickyaudible" or "stickytristate" or "stickytwokeysoff" => plan.StickyKeysMode!=StickyKeySettings.Custom?global::UnattendLocal.Localization.UiText.Source("text.2f8d9c3a0320"):global::UnattendLocal.Localization.UiText.Format("message.6f9f1bf897f7", new string[] { global::UnattendLocal.Localization.UiText.Arg((StickyKeySettings.ModeSummary(plan)), 0, null) }),
            "lockbehavior" => plan.LockKeyMode==KeyboardLockSettings.Default?global::UnattendLocal.Localization.UiText.Source("text.499db6e80a16"):global::UnattendLocal.Localization.UiText.Format("message.6f9f1bf897f7", new string[] { global::UnattendLocal.Localization.UiText.Arg((KeyboardLockSettings.ModeSummary(plan)), 0, null) }),
            "lockkeys" => global::UnattendLocal.Localization.UiText.Format("message.6f9f1bf897f7", new string[] { global::UnattendLocal.Localization.UiText.Arg((KeyboardLockSettings.ModeSummary(plan)), 0, null) }),
            "numlock" => KeyboardLockSettings.CurrentKey(plan, plan.InitialNumLockOn,plan.IgnoreNumLock),
            "capslock" => KeyboardLockSettings.CurrentKey(plan, plan.InitialCapsLockOn,plan.IgnoreCapsLock),
            "scrolllock" => KeyboardLockSettings.CurrentKey(plan, plan.InitialScrollLockOn,plan.IgnoreScrollLock),
            "wallpaper" => global::UnattendLocal.Localization.UiText.Format("message.6f9f1bf897f7", new string[] { global::UnattendLocal.Localization.UiText.Arg((WallpaperSettings.ModeSummary(plan)), 0, null) }),
            "wallpaperimage" => WallpaperSettings.IsScript(plan)?global::UnattendLocal.Localization.UiText.Format("message.6f9f1bf897f7", new string[] { global::UnattendLocal.Localization.UiText.Arg((WallpaperSettings.ModeSummary(plan)), 0, null) }):global::UnattendLocal.Localization.UiText.Source("text.42956b1630cb"),
            "lockscreen" or "lockscreenimage" => global::UnattendLocal.Localization.UiText.Format("message.6f9f1bf897f7", new string[] { global::UnattendLocal.Localization.UiText.Arg((ImageBackgroundSettings.LockSummary(plan)), 0, null) }),
            "wallpapercolor" => !WallpaperSettings.IsSolid(plan)?global::UnattendLocal.Localization.UiText.Source("text.3bdd7bad46eb"):ColorSettings.TryNormalize(plan.WallpaperColorHex,out var backgroundColor)?global::UnattendLocal.Localization.UiText.Format("message.e5f2fc276f5f", new string[] { global::UnattendLocal.Localization.UiText.Arg((backgroundColor), 0, null) }):global::UnattendLocal.Localization.UiText.Source("text.ae1c94a024df"),
            "windowscolors" => global::UnattendLocal.Localization.UiText.Format("message.6f9f1bf897f7", new string[] { global::UnattendLocal.Localization.UiText.Arg((ColorSettings.ModeSummary(plan)), 0, null) }),
            "systemcolortheme" => ColorSettings.IsCustom(plan)?global::UnattendLocal.Localization.UiText.Format("message.4e7c3e002a3f", new string[] { global::UnattendLocal.Localization.UiText.Arg((plan.WindowsColorTheme), 0, null) }):global::UnattendLocal.Localization.UiText.Source("text.eda04095bfb4"),
            "appscolortheme" => ColorSettings.IsCustom(plan)?global::UnattendLocal.Localization.UiText.Format("message.cf699b590299", new string[] { global::UnattendLocal.Localization.UiText.Arg((plan.AppsColorTheme), 0, null) }):global::UnattendLocal.Localization.UiText.Source("text.88dfc6d77fde"),
            "accentcolor" => !ColorSettings.IsCustom(plan)?global::UnattendLocal.Localization.UiText.Source("text.13a5ce91a08e"):ColorSettings.TryNormalize(plan.AccentColorHex,out var color)?global::UnattendLocal.Localization.UiText.Format("message.6f9f1bf897f7", new string[] { global::UnattendLocal.Localization.UiText.Arg((color), 0, null) }):global::UnattendLocal.Localization.UiText.Source("text.78e22d1bc9e3"),
            "colortransparency" => !ColorSettings.IsCustom(plan)?global::UnattendLocal.Localization.UiText.Source("text.a44fa11e24d8"):global::UnattendLocal.Localization.UiText.Format("message.3933362531ed", new string[] { global::UnattendLocal.Localization.UiText.Arg(((plan.EnableColorTransparency?global::UnattendLocal.Localization.UiText.Source("text.8da97ddda990"):global::UnattendLocal.Localization.UiText.Source("text.3fd47edce45b"))), 0, null) }),
            "accentstart" => !ColorSettings.IsCustom(plan)?global::UnattendLocal.Localization.UiText.Source("text.a44fa11e24d8"):plan.WindowsColorTheme==ColorSettings.Light?global::UnattendLocal.Localization.UiText.Source("text.8726d91e6e80"):global::UnattendLocal.Localization.UiText.Format("message.97f4a2e69f81", new string[] { global::UnattendLocal.Localization.UiText.Arg(((ColorSettings.StartAccentEnabled(plan)?global::UnattendLocal.Localization.UiText.Source("text.8da97ddda990"):global::UnattendLocal.Localization.UiText.Source("text.3fd47edce45b"))), 0, null) }),
            "accentborders" => !ColorSettings.IsCustom(plan)?global::UnattendLocal.Localization.UiText.Source("text.a44fa11e24d8"):global::UnattendLocal.Localization.UiText.Format("message.8bb4b9dd5630", new string[] { global::UnattendLocal.Localization.UiText.Arg(((plan.AccentColorOnBorders?global::UnattendLocal.Localization.UiText.Source("text.8da97ddda990"):global::UnattendLocal.Localization.UiText.Source("text.3fd47edce45b"))), 0, null) }),
            "visualeffects" => global::UnattendLocal.Localization.UiText.Format("message.6f9f1bf897f7", new string[] { global::UnattendLocal.Localization.UiText.Arg((VisualEffectsSettings.ModeSummary(plan)), 0, null) }),
            _ when VisualEffectsSettings.Items.Any(item=>item.HelpId==topic.Id) => global::UnattendLocal.Localization.UiText.Format("message.6f9f1bf897f7", new string[] { global::UnattendLocal.Localization.UiText.Arg((VisualEffectsSettings.ItemState(plan,VisualEffectsSettings.Items.First(item=>item.HelpId==topic.Id))), 0, null) }),
            "desktopicons" => global::UnattendLocal.Localization.UiText.Format("message.6f9f1bf897f7", new string[] { global::UnattendLocal.Localization.UiText.Arg((DesktopSettings.IconSummary(plan)), 0, null) }),
            "edgeicon" => global::UnattendLocal.Localization.UiText.Format("message.5ae652b92ee5", new string[] { global::UnattendLocal.Localization.UiText.Arg(((plan.DeleteEdgeDesktopIcon?global::UnattendLocal.Localization.UiText.Source("text.8da97ddda990"):global::UnattendLocal.Localization.UiText.Source("text.3fd47edce45b"))), 0, null) }),
            "startfolders" => global::UnattendLocal.Localization.UiText.Format("message.730d289bfbcc", new string[] { global::UnattendLocal.Localization.UiText.Arg((DesktopSettings.FolderSummary(plan)), 0, null) }),
            "explorerhidden" => global::UnattendLocal.Localization.UiText.Format("message.6f9f1bf897f7", new string[] { global::UnattendLocal.Localization.UiText.Arg((plan.HideFilesMode), 0, null) }),
            "explorerextensions" => global::UnattendLocal.Localization.UiText.Format("message.8fda2578fe4c", new string[] { global::UnattendLocal.Localization.UiText.Arg(((plan.ShowFileExtensions?global::UnattendLocal.Localization.UiText.Source("text.8da97ddda990"):global::UnattendLocal.Localization.UiText.Source("text.3fd47edce45b"))), 0, null) }),
            "explorercontext" => global::UnattendLocal.Localization.UiText.Format("message.2686e522012a", new string[] { global::UnattendLocal.Localization.UiText.Arg(((plan.ClassicContextMenu?global::UnattendLocal.Localization.UiText.Source("text.8da97ddda990"):global::UnattendLocal.Localization.UiText.Source("text.3fd47edce45b"))), 0, null) }),
            "explorertips" => global::UnattendLocal.Localization.UiText.Format("message.d00a914d8442", new string[] { global::UnattendLocal.Localization.UiText.Arg(((plan.HideInfoTip?global::UnattendLocal.Localization.UiText.Source("text.8da97ddda990"):global::UnattendLocal.Localization.UiText.Source("text.3fd47edce45b"))), 0, null) }),
            "explorerlaunch" => global::UnattendLocal.Localization.UiText.Format("message.0fdebaa433d8", new string[] { global::UnattendLocal.Localization.UiText.Arg(((plan.LaunchToThisPC?global::UnattendLocal.Localization.UiText.Source("text.479f86df2c34"):global::UnattendLocal.Localization.UiText.Source("text.0ebbbcbe3e78"))), 0, null) }),
            "taskbarendtask" => global::UnattendLocal.Localization.UiText.Format("message.95bd84e69ed0", new string[] { global::UnattendLocal.Localization.UiText.Arg(((plan.ShowEndTask?global::UnattendLocal.Localization.UiText.Source("text.8da97ddda990"):global::UnattendLocal.Localization.UiText.Source("text.3fd47edce45b"))), 0, null) }),
            "taskbarsearch" => global::UnattendLocal.Localization.UiText.Format("message.6f9f1bf897f7", new string[] { global::UnattendLocal.Localization.UiText.Arg((plan.TaskbarSearch), 0, null) }),
            "taskbaricons" => global::UnattendLocal.Localization.UiText.Format("message.dd8a7755a66a", new string[] { global::UnattendLocal.Localization.UiText.Arg((plan.TaskbarIconsMode), 0, null), global::UnattendLocal.Localization.UiText.Arg(((plan.TaskbarIconsMode==ShellSettings.CustomXml?global::UnattendLocal.Localization.UiText.Source("text.2e94e4663cad"):global::UnattendLocal.Localization.UiText.Source("text.69dd3178697a"))), 0, null) }),
            "starttiles" => global::UnattendLocal.Localization.UiText.Format("message.ff19e471ce04", new string[] { global::UnattendLocal.Localization.UiText.Arg((plan.StartTilesMode), 0, null) }),
            "startpins" => global::UnattendLocal.Localization.UiText.Format("message.730d289bfbcc", new string[] { global::UnattendLocal.Localization.UiText.Arg((plan.StartPinsMode), 0, null) }),
            "widgets" => global::UnattendLocal.Localization.UiText.Format("message.2575b11c3a71", new string[] { global::UnattendLocal.Localization.UiText.Arg(((plan.DisableWidgets?global::UnattendLocal.Localization.UiText.Source("text.8da97ddda990"):global::UnattendLocal.Localization.UiText.Source("text.3fd47edce45b"))), 0, null) }),
            "taskbaralignment" => global::UnattendLocal.Localization.UiText.Format("message.8c0319cf3c01", new string[] { global::UnattendLocal.Localization.UiText.Arg(((plan.LeftTaskbar?global::UnattendLocal.Localization.UiText.Source("text.8da97ddda990"):global::UnattendLocal.Localization.UiText.Source("text.3fd47edce45b"))), 0, null) }),
            "taskview" => global::UnattendLocal.Localization.UiText.Format("message.71a190cb461c", new string[] { global::UnattendLocal.Localization.UiText.Arg(((plan.HideTaskViewButton?global::UnattendLocal.Localization.UiText.Source("text.8da97ddda990"):global::UnattendLocal.Localization.UiText.Source("text.3fd47edce45b"))), 0, null) }),
            "trayicons" => global::UnattendLocal.Localization.UiText.Format("message.202426460e16", new string[] { global::UnattendLocal.Localization.UiText.Arg(((plan.ShowAllTrayIcons?global::UnattendLocal.Localization.UiText.Source("text.8da97ddda990"):global::UnattendLocal.Localization.UiText.Source("text.3fd47edce45b"))), 0, null) }),
            "bingsearch" => global::UnattendLocal.Localization.UiText.Format("message.c537d97ebe15", new string[] { global::UnattendLocal.Localization.UiText.Arg(((plan.DisableBingResults?global::UnattendLocal.Localization.UiText.Source("text.8da97ddda990"):global::UnattendLocal.Localization.UiText.Source("text.3fd47edce45b"))), 0, null) }),
            "account" => global::UnattendLocal.Localization.UiText.Format("message.6f9f1bf897f7", new string[] { global::UnattendLocal.Localization.UiText.Arg((plan.AccountMode), 0, null) }),
            "empty" => user is null ? global::UnattendLocal.Localization.UiText.Source("text.1aa2d0c0d957") : user.AllowEmptyPassword
                ? global::UnattendLocal.Localization.UiText.Source("text.40386f84762a")
                : global::UnattendLocal.Localization.UiText.Source("text.103515b1b047"),
            "expiration" => plan.PasswordExpiration switch
            {
                "Windows 默认" => global::UnattendLocal.Localization.UiText.Source("text.0e22b095cce3"),
                "指定天数" => global::UnattendLocal.Localization.UiText.Format("message.6f83a4e5f6ec", new string[] { global::UnattendLocal.Localization.UiText.Arg((plan.PasswordDays), 0, null) }),
                _ => global::UnattendLocal.Localization.UiText.Source("text.39f14243d650")
            },
            "logon" or "builtinlogon" => global::UnattendLocal.Localization.UiText.Concat(new object?[] { global::UnattendLocal.Localization.UiText.Source("text.660648805666"), FirstLogonSettings.Summary(plan), "。" },new bool[]{false,false,false}),
            "staticipv6" or "staticv6prefix" or "staticv6gateway" or "staticv6dns" => StaticIPv4Settings.SummaryIPv6(plan),
            "staticmac" => StaticIPv4Settings.Summary(plan)+"\n"+StaticIPv4Settings.SummaryIPv6(plan),
            "staticprefix" or "staticipv4" or "staticgateway" or "staticdns" or "staticmask" => StaticIPv4Settings.Summary(plan),
            "wifi" => plan.WifiMode==global::UnattendLocal.Localization.UiText.Source("text.307f498ede07") ? global::UnattendLocal.Localization.UiText.Source("text.627fe6934172") : global::UnattendLocal.Localization.UiText.Format("message.6f9f1bf897f7", new string[] { global::UnattendLocal.Localization.UiText.Arg((WifiSettings.ModeLabel(plan.WifiMode)), 0, null) }),
            "offline" => plan.BypassNetwork ? global::UnattendLocal.Localization.UiText.Source("text.5b9dfc38663e") : global::UnattendLocal.Localization.UiText.Source("text.4717cc0d0189"),
            "privacy" => global::UnattendLocal.Localization.UiText.Format("message.6f9f1bf897f7", new string[] { global::UnattendLocal.Localization.UiText.Arg((plan.PrivacyMode), 0, null) }),
            "online" => plan.AccountMode == global::UnattendLocal.Localization.UiText.Source("text.5f78e95b6bd9") ? global::UnattendLocal.Localization.UiText.Source("text.81abe9433387") : global::UnattendLocal.Localization.UiText.Format("message.3bb3260d22b6", new string[] { global::UnattendLocal.Localization.UiText.Arg(((plan.HideOnlineAccount?global::UnattendLocal.Localization.UiText.Source("text.8da97ddda990"):global::UnattendLocal.Localization.UiText.Source("text.3fd47edce45b"))), 0, null) }),
            "language" => plan.InteractiveLanguage ? global::UnattendLocal.Localization.UiText.Source("text.ed15fcf10052") : global::UnattendLocal.Localization.UiText.Format("message.1f3b725ef9d0", new string[] { global::UnattendLocal.Localization.UiText.Arg(((new[]{plan.ImageLanguage,plan.Locale,plan.Keyboard,plan.GeoId}.Any(string.IsNullOrWhiteSpace)?global::UnattendLocal.Localization.UiText.Source("text.8445aad151d8"):global::UnattendLocal.Localization.UiText.Source("text.0672437fd668"))), 0, null) }),
            "region" or "timezone" => plan.TimeZoneMode==global::UnattendLocal.Localization.UiText.Source("text.499ff33c1078") ? global::UnattendLocal.Localization.UiText.Source("text.892224ee4430") : global::UnattendLocal.Localization.UiText.Format("message.df31d78a7708", new string[] { global::UnattendLocal.Localization.UiText.Arg(((string.IsNullOrWhiteSpace(plan.TimeZone)?global::UnattendLocal.Localization.UiText.Source("text.c5768ca9726c"):ChoiceLocalization.Get("TimeOffset",plan.TimeZone,plan.TimeZone))), 0, null) }),
            "wifiprofile"=>global::UnattendLocal.Localization.UiText.Concat(new object?[] { global::UnattendLocal.Localization.UiText.Source("text.660648805666"), WifiSettings.Summary(plan), "。" },new bool[]{false,false,false}),
            "hidden" => !WifiSettings.IsPreset(plan) ? global::UnattendLocal.Localization.UiText.Source("text.3ab19f7458b6") : global::UnattendLocal.Localization.UiText.Format("message.db3113da12dd", new string[] { global::UnattendLocal.Localization.UiText.Arg(((plan.HiddenWifi?global::UnattendLocal.Localization.UiText.Source("text.8da97ddda990"):global::UnattendLocal.Localization.UiText.Source("text.3fd47edce45b"))), 0, null), global::UnattendLocal.Localization.UiText.Arg(((plan.ConnectWifi?global::UnattendLocal.Localization.UiText.Source("text.8da97ddda990"):global::UnattendLocal.Localization.UiText.Source("text.3fd47edce45b"))), 0, null) }),
            "eula" => global::UnattendLocal.Localization.UiText.Format("message.f159f33b04ad", new string[] { global::UnattendLocal.Localization.UiText.Arg(((plan.HideEula?global::UnattendLocal.Localization.UiText.Source("text.145219e726b8"):global::UnattendLocal.Localization.UiText.Source("text.4e1449e7d5e5"))), 0, null) }),
            "oem" => global::UnattendLocal.Localization.UiText.Format("message.e06694c62566", new string[] { global::UnattendLocal.Localization.UiText.Arg(((plan.HideOEMRegistration?global::UnattendLocal.Localization.UiText.Source("text.145219e726b8"):global::UnattendLocal.Localization.UiText.Source("text.4e1449e7d5e5"))), 0, null) }),
            "restartsignon" => plan.DisableAutomaticRestartSignOn?global::UnattendLocal.Localization.UiText.Source("text.d33cd0c78bb6"):global::UnattendLocal.Localization.UiText.Source("text.56521605359b"),
            "deviceapps" => plan.PreventDeviceApps?global::UnattendLocal.Localization.UiText.Source("text.b7b9aae23990"):global::UnattendLocal.Localization.UiText.Source("text.56521605359b"),
            "windowsold" => plan.DeleteWindowsOld?global::UnattendLocal.Localization.UiText.Source("text.4a179d152cb9"):global::UnattendLocal.Localization.UiText.Source("text.0590b95cb860"),
            "appsuggestions" => plan.DisableAppSuggestions?global::UnattendLocal.Localization.UiText.Source("text.1196b57206f7"):global::UnattendLocal.Localization.UiText.Source("text.56521605359b"),
            "windowsupdate" => plan.DisableWindowsUpdate?global::UnattendLocal.Localization.UiText.Source("text.5e2ad3f07581"):global::UnattendLocal.Localization.UiText.Source("text.56521605359b"),
            "edgefre" => plan.HideEdgeFre?global::UnattendLocal.Localization.UiText.Source("text.273d64d10e7d"):global::UnattendLocal.Localization.UiText.Source("text.6bdd4ae0a0f1"),
            "edgebackground" => plan.DisableEdgeStartupBoost?global::UnattendLocal.Localization.UiText.Source("text.6fe1109588ea"):global::UnattendLocal.Localization.UiText.Source("text.6bdd4ae0a0f1"),
            "longpaths" => plan.EnableLongPaths?global::UnattendLocal.Localization.UiText.Source("text.eb6e27cd7f88"):global::UnattendLocal.Localization.UiText.Source("text.56521605359b"),
            "systemsounds" => plan.TurnOffSystemSounds?global::UnattendLocal.Localization.UiText.Source("text.391f659989af"):global::UnattendLocal.Localization.UiText.Source("text.56521605359b"),
            "faststartup" => plan.DisableFastStartup?global::UnattendLocal.Localization.UiText.Source("text.75d78575f38d"):global::UnattendLocal.Localization.UiText.Source("text.56521605359b"),
            "deviceencryption" => plan.PreventDeviceEncryption?global::UnattendLocal.Localization.UiText.Source("text.c2a90735bd65"):global::UnattendLocal.Localization.UiText.Source("text.d766ea6154ae"),
            "hardware" => global::UnattendLocal.Localization.UiText.Format("message.6f9f1bf897f7", new string[] { global::UnattendLocal.Localization.UiText.Arg((HardwareSettings.Summary(plan)), 0, null) }),
            "legacyupgrade" => global::UnattendLocal.Localization.UiText.Concat(new object?[] { global::UnattendLocal.Localization.UiText.Source("text.660648805666"), (plan.AllowUnsupportedUpgrade?global::UnattendLocal.Localization.UiText.Source("text.f84337215f53"):global::UnattendLocal.Localization.UiText.Source("text.328903dedf7c")) },new bool[]{false,false}),
            "hardwaretpm" => global::UnattendLocal.Localization.UiText.Format("message.0a194e7ea11a", new string[] { global::UnattendLocal.Localization.UiText.Arg(((plan.BypassHardware||plan.BypassTPMCheck?global::UnattendLocal.Localization.UiText.Source("text.8da97ddda990"):global::UnattendLocal.Localization.UiText.Source("text.3fd47edce45b"))), 0, null) }),
            "hardwareboot" => global::UnattendLocal.Localization.UiText.Format("message.21a05bb55eea", new string[] { global::UnattendLocal.Localization.UiText.Arg(((plan.BypassHardware||plan.BypassSecureBootCheck?global::UnattendLocal.Localization.UiText.Source("text.8da97ddda990"):global::UnattendLocal.Localization.UiText.Source("text.3fd47edce45b"))), 0, null) }),
            "hardwareram" => global::UnattendLocal.Localization.UiText.Format("message.f9b1a62fe7fe", new string[] { global::UnattendLocal.Localization.UiText.Arg(((plan.BypassHardware||plan.BypassRAMCheck?global::UnattendLocal.Localization.UiText.Source("text.8da97ddda990"):global::UnattendLocal.Localization.UiText.Source("text.3fd47edce45b"))), 0, null) }),
            "architecture" => global::UnattendLocal.Localization.UiText.Format("message.269e496506e4", new string[] { global::UnattendLocal.Localization.UiText.Arg((ChoiceLocalization.Architecture(plan.Architecture)), 0, null) }),
            "computer" => global::UnattendLocal.Localization.UiText.Concat(new object?[] { global::UnattendLocal.Localization.UiText.Source("text.660648805666"), ComputerNamingSettings.Summary(plan), "。" },new bool[]{false,false,false}),
            "extralanguages"=>global::UnattendLocal.Localization.UiText.Concat(new object?[] { global::UnattendLocal.Localization.UiText.Source("text.660648805666"), LanguagePreferences.Summary(plan), "。" },new bool[]{false,false,false}),
            "compactos" => global::UnattendLocal.Localization.UiText.Format("message.6f9f1bf897f7", new string[] { global::UnattendLocal.Localization.UiText.Arg((CompactOsSettings.ModeSummary(plan)), 0, null) }),
            "image" => global::UnattendLocal.Localization.UiText.Format("message.6f9f1bf897f7", new string[] { global::UnattendLocal.Localization.UiText.Arg((InstallationSettings.ImageSummary(plan)), 0, null) }),
            "diskmode" or "disktarget" or "diskformat" or "disklayout" => global::UnattendLocal.Localization.UiText.Format("message.6f9f1bf897f7", new string[] { global::UnattendLocal.Localization.UiText.Arg((InstallationSettings.DiskSummary(plan)), 0, null) }),
            "edition" or "sku" => global::UnattendLocal.Localization.UiText.Format("message.8e577c814421", new string[] { global::UnattendLocal.Localization.UiText.Arg((plan.EditionMode), 0, null), global::UnattendLocal.Localization.UiText.Arg(((string.IsNullOrWhiteSpace(plan.EditionId)?global::UnattendLocal.Localization.UiText.Source("text.c5768ca9726c"):plan.EditionId)), 0, null) }),
            "activation" => plan.UseActivationKey?global::UnattendLocal.Localization.UiText.Source("text.f6f0e8d43dfd"):global::UnattendLocal.Localization.UiText.Source("text.8c56477b2732"),
            "powershell" => plan.HidePowerShellWindows ? global::UnattendLocal.Localization.UiText.Source("text.254ef4a28cb2") : global::UnattendLocal.Localization.UiText.Source("text.6621ae90381b"),
            "sensitivefiles" => plan.KeepSensitiveFiles ? global::UnattendLocal.Localization.UiText.Source("text.3e28197bd2d9") : global::UnattendLocal.Localization.UiText.Source("text.586619a6dcc5"),
            _ => ""
        };
        bool network = topic.Id is "staticmac" or "staticprefix" or "staticipv4" or "staticgateway" or "staticdns" or "staticmask"
            or "staticipv6" or "staticv6prefix" or "staticv6gateway" or "staticv6dns";
        // Keep protocol states separate so an incomplete IPv4 configuration does not color normal IPv6 text red.
        string[] states = topic.Id == "staticmac" ? [StaticIPv4Settings.Summary(plan), StaticIPv4Settings.SummaryIPv6(plan)]
            : string.IsNullOrEmpty(state) ? [] : [state];
        var sections = states.Select(value => new HelpSection(network ? HelpText.Paragraphs(value) : value, DiagnosticText.IsErrorSummary(value)))
            .Append(new HelpSection(topic.Body)).ToArray();
        if(plan.UseGeneratedPe && (topic.Id is "edition" or "sku" or "installkey"))
            sections=[new HelpSection(global::UnattendLocal.Localization.UiText.Source("v129.pe.editionDescription")),new HelpSection(global::UnattendLocal.Localization.UiText.Source(plan.EditionMode==global::UnattendLocal.Localization.UiText.Source("text.5056d73b13ec")?"v129.pe.ready":"v125.pe.edition")),..sections];
        if(plan.UseGeneratedPe && topic.Id=="activation")
            sections=[new HelpSection(global::UnattendLocal.Localization.UiText.Source("v129.pe.activation")),..sections];
        return topic with { Body = global::UnattendLocal.Localization.UiText.Join("\n\n", sections.Select(section => section.Text), false), Parts = sections };
    }
}
