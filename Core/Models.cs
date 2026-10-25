using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Collections.ObjectModel;
namespace UnattendLocal.Core;
public class Observable : INotifyPropertyChanged {
 public event PropertyChangedEventHandler? PropertyChanged;
 protected void Notify(string name)=>PropertyChanged?.Invoke(this,new(name));
 protected void Set<T>(ref T field,T value,[CallerMemberName]string? name=null) { if(EqualityComparer<T>.Default.Equals(field,value))return;field=value;PropertyChanged?.Invoke(this,new(name)); }
}
public class UserEntry : Observable {
 private string _Name = "User"; public string Name {get=>_Name;set=>Set(ref _Name,value);}
 private string _DisplayName = ""; public string DisplayName {get=>_DisplayName;set=>Set(ref _DisplayName,value);}
 private string _Password = ""; public string Password {get=>_Password;set=>Set(ref _Password,value);}
 private bool _Administrator = true; public bool Administrator {get=>_Administrator;set=>Set(ref _Administrator,value);}
 private bool _AllowEmptyPassword = false; public bool AllowEmptyPassword {get=>_AllowEmptyPassword;set=>Set(ref _AllowEmptyPassword,value);}
 private bool _NeedsPassword = false; public bool NeedsPassword {get=>_NeedsPassword;set=>Set(ref _NeedsPassword,value);}
}
public class InstallPlan : Observable {
 private bool _DisableNewNtfsShortNames;
 public bool DisableNewNtfsShortNames { get=>_DisableNewNtfsShortNames; set=>Set(ref _DisableNewNtfsShortNames,value); }
 private bool _UseCustomPageFile;
 public bool UseCustomPageFile { get=>_UseCustomPageFile; set=>Set(ref _UseCustomPageFile,value); }
 private int _PageFileInitialMiB=2048;
 public int PageFileInitialMiB { get=>_PageFileInitialMiB; set=>Set(ref _PageFileInitialMiB,value); }
 private int _PageFileMaximumMiB=4096;
 public int PageFileMaximumMiB { get=>_PageFileMaximumMiB; set=>Set(ref _PageFileMaximumMiB,value); }

 private string _PowerPlanMode=NativePreferences.Default;public string PowerPlanMode {get=>_PowerPlanMode;set=>Set(ref _PowerPlanMode,value);}
 private string _CustomPowerPlanGuid="";public string CustomPowerPlanGuid {get=>_CustomPowerPlanGuid;set=>Set(ref _CustomPowerPlanGuid,value);}
 private string _ErrorReportingMode=NativePreferences.Default;public string ErrorReportingMode {get=>_ErrorReportingMode;set=>Set(ref _ErrorReportingMode,value);}

 public ObservableCollection<XmlComponentEntry> XmlComponents {get;set;}=new();
 private bool _ConfirmAdditionalXml;public bool ConfirmAdditionalXml {get=>_ConfirmAdditionalXml;set=>Set(ref _ConfirmAdditionalXml,value);}

 private bool _ExportNonAutomaticFilename;public bool ExportNonAutomaticFilename {get=>_ExportNonAutomaticFilename;set=>Set(ref _ExportNonAutomaticFilename,value);}
 private bool _UseWorkgroup;public bool UseWorkgroup {get=>_UseWorkgroup;set=>Set(ref _UseWorkgroup,value);}
 private string _WorkgroupName="WORKGROUP";public string WorkgroupName {get=>_WorkgroupName;set=>Set(ref _WorkgroupName,value);}
 private bool _UsePeLogPath;public bool UsePeLogPath {get=>_UsePeLogPath;set{if(_UsePeLogPath==value)return;ClearDiskConsent();Set(ref _UsePeLogPath,value);}}
 private string _PeLogPath="";public string PeLogPath {get=>_PeLogPath;set{if(_PeLogPath==value)return;ClearDiskConsent();Set(ref _PeLogPath,value);}}

 private bool _UseRegisteredOwner;public bool UseRegisteredOwner {get=>_UseRegisteredOwner;set=>Set(ref _UseRegisteredOwner,value);}
 private string _RegisteredOwner="";public string RegisteredOwner {get=>_RegisteredOwner;set=>Set(ref _RegisteredOwner,value);}
 private string _RegisteredOwnerPass="specialize";public string RegisteredOwnerPass {get=>_RegisteredOwnerPass;set=>Set(ref _RegisteredOwnerPass,value);}
 private bool _UseRegisteredOrganization;public bool UseRegisteredOrganization {get=>_UseRegisteredOrganization;set=>Set(ref _UseRegisteredOrganization,value);}
 private string _RegisteredOrganization="";public string RegisteredOrganization {get=>_RegisteredOrganization;set=>Set(ref _RegisteredOrganization,value);}
 private string _RegisteredOrganizationPass="specialize";public string RegisteredOrganizationPass {get=>_RegisteredOrganizationPass;set=>Set(ref _RegisteredOrganizationPass,value);}

 private string _SetupDynamicUpdateMode=SetupDynamicUpdateSettings.Default;public string SetupDynamicUpdateMode {get=>_SetupDynamicUpdateMode;set{if(_SetupDynamicUpdateMode==value)return;ClearDiskConsent();Set(ref _SetupDynamicUpdateMode,value);}}
 private string _SetupDynamicUpdateUi=SetupDynamicUpdateSettings.UiDefault;public string SetupDynamicUpdateUi {get=>_SetupDynamicUpdateUi;set=>Set(ref _SetupDynamicUpdateUi,value);}

 private bool _UseSetupDrivers;public bool UseSetupDrivers {get=>_UseSetupDrivers;set{if(_UseSetupDrivers==value)return;ClearDiskConsent();Set(ref _UseSetupDrivers,value);}}
 private string _SetupDriverPaths="";public string SetupDriverPaths {get=>_SetupDriverPaths;set{if(_SetupDriverPaths==value)return;ClearDiskConsent();Set(ref _SetupDriverPaths,value);}}
 private bool _UseStaticIPv6;public bool UseStaticIPv6 {get=>_UseStaticIPv6;set=>Set(ref _UseStaticIPv6,value);}
 private string _StaticIPv6Address="";public string StaticIPv6Address {get=>_StaticIPv6Address;set=>Set(ref _StaticIPv6Address,value);}
 private int _StaticIPv6Prefix=64;public int StaticIPv6Prefix {get=>_StaticIPv6Prefix;set=>Set(ref _StaticIPv6Prefix,value);}
 private string _StaticIPv6Gateway="";public string StaticIPv6Gateway {get=>_StaticIPv6Gateway;set=>Set(ref _StaticIPv6Gateway,value);}
 private string _StaticIPv6Dns="";public string StaticIPv6Dns {get=>_StaticIPv6Dns;set=>Set(ref _StaticIPv6Dns,value);}
 private bool _UseStaticIPv4;public bool UseStaticIPv4 {get=>_UseStaticIPv4;set=>Set(ref _UseStaticIPv4,value);}
 private string _StaticIPv4Mac="";public string StaticIPv4Mac {get=>_StaticIPv4Mac;set=>Set(ref _StaticIPv4Mac,value);}
 private string _StaticIPv4Address="";public string StaticIPv4Address {get=>_StaticIPv4Address;set=>Set(ref _StaticIPv4Address,value);}
 private bool _UseStaticIPv4SubnetMask;public bool UseStaticIPv4SubnetMask {get=>_UseStaticIPv4SubnetMask;set=>Set(ref _UseStaticIPv4SubnetMask,value);}
 private string _StaticIPv4SubnetMask="";public string StaticIPv4SubnetMask {get=>_StaticIPv4SubnetMask;set=>Set(ref _StaticIPv4SubnetMask,value);}
 private int _StaticIPv4Prefix=24;public int StaticIPv4Prefix {get=>_StaticIPv4Prefix;set=>Set(ref _StaticIPv4Prefix,value);}
 private string _StaticIPv4Gateway="";public string StaticIPv4Gateway {get=>_StaticIPv4Gateway;set=>Set(ref _StaticIPv4Gateway,value);}
 private string _StaticIPv4Dns="";public string StaticIPv4Dns {get=>_StaticIPv4Dns;set=>Set(ref _StaticIPv4Dns,value);}
 private bool _UseBuiltinAdministrator;public bool UseBuiltinAdministrator {get=>_UseBuiltinAdministrator;set=>Set(ref _UseBuiltinAdministrator,value);}
 private string _BuiltinAdministratorPassword="";public string BuiltinAdministratorPassword {get=>_BuiltinAdministratorPassword;set=>Set(ref _BuiltinAdministratorPassword,value);}
 private bool _AllowEmptyBuiltinAdministratorPassword;public bool AllowEmptyBuiltinAdministratorPassword {get=>_AllowEmptyBuiltinAdministratorPassword;set=>Set(ref _AllowEmptyBuiltinAdministratorPassword,value);}
 private bool _NeedsBuiltinAdministratorPassword;public bool NeedsBuiltinAdministratorPassword {get=>_NeedsBuiltinAdministratorPassword;set=>Set(ref _NeedsBuiltinAdministratorPassword,value);}

 private string _ComputerNameMode=ComputerNamingSettings.Existing;public string ComputerNameMode {get=>_ComputerNameMode;set=>Set(ref _ComputerNameMode,value);}
 private string _ComputerNameScript="";public string ComputerNameScript {get=>_ComputerNameScript;set=>Set(ref _ComputerNameScript,value);}
 private bool _UseSecondLanguage=false;public bool UseSecondLanguage {get=>_UseSecondLanguage;set=>Set(ref _UseSecondLanguage,value);}
 private bool _UseThirdLanguage=false;public bool UseThirdLanguage {get=>_UseThirdLanguage;set=>Set(ref _UseThirdLanguage,value);}
 private string _SecondLocale="";public string SecondLocale {get=>_SecondLocale;set=>Set(ref _SecondLocale,value);}
 private string _SecondKeyboard="";public string SecondKeyboard {get=>_SecondKeyboard;set=>Set(ref _SecondKeyboard,value);}
 private string _ThirdLocale="";public string ThirdLocale {get=>_ThirdLocale;set=>Set(ref _ThirdLocale,value);}
 private string _ThirdKeyboard="";public string ThirdKeyboard {get=>_ThirdKeyboard;set=>Set(ref _ThirdKeyboard,value);}

 private bool _UseNarrator;public bool UseNarrator {get=>_UseNarrator;set=>Set(ref _UseNarrator,value);}
 private bool _MakeEdgeUninstallable;public bool MakeEdgeUninstallable {get=>_MakeEdgeUninstallable;set=>Set(ref _MakeEdgeUninstallable,value);}
 private bool _DisableSystemRestore;public bool DisableSystemRestore {get=>_DisableSystemRestore;set=>Set(ref _DisableSystemRestore,value);}

 private bool _VMwareTools;public bool VMwareTools {get=>_VMwareTools;set=>Set(ref _VMwareTools,value);}
 private bool _VBoxGuestAdditions;public bool VBoxGuestAdditions {get=>_VBoxGuestAdditions;set=>Set(ref _VBoxGuestAdditions,value);}
 private bool _VirtIoGuestTools;public bool VirtIoGuestTools {get=>_VirtIoGuestTools;set=>Set(ref _VirtIoGuestTools,value);}
 private bool _ParallelsTools;public bool ParallelsTools {get=>_ParallelsTools;set=>Set(ref _ParallelsTools,value);}
 private bool _DisableUac;public bool DisableUac {get=>_DisableUac;set=>Set(ref _DisableUac,value);}
 private bool _DeleteJunctions;public bool DeleteJunctions {get=>_DeleteJunctions;set=>Set(ref _DeleteJunctions,value);}
 private bool _UseConfigurationSet;public bool UseConfigurationSet {get=>_UseConfigurationSet;set=>Set(ref _UseConfigurationSet,value);}
 private bool _UseAppLocker;public bool UseAppLocker {get=>_UseAppLocker;set=>Set(ref _UseAppLocker,value);}
 private string _AppLockerPolicyXml="";public string AppLockerPolicyXml {get=>_AppLockerPolicyXml;set=>Set(ref _AppLockerPolicyXml,value);}
 private bool _DisableSac;public bool DisableSac {get=>_DisableSac;set=>Set(ref _DisableSac,value);}
 private bool _DisableSmartScreen;public bool DisableSmartScreen {get=>_DisableSmartScreen;set=>Set(ref _DisableSmartScreen,value);}
 private bool _DisableCoreIsolation;public bool DisableCoreIsolation {get=>_DisableCoreIsolation;set=>Set(ref _DisableCoreIsolation,value);}
 private bool _HardenSystemDriveAcl;public bool HardenSystemDriveAcl {get=>_HardenSystemDriveAcl;set=>Set(ref _HardenSystemDriveAcl,value);}
 private bool _DisableWpbt;public bool DisableWpbt {get=>_DisableWpbt;set=>Set(ref _DisableWpbt,value);}
 private string _ProcessAuditMode=ProcessAuditSettings.Default;public string ProcessAuditMode {get=>_ProcessAuditMode;set=>Set(ref _ProcessAuditMode,value);}
 private bool _PreventAutomaticReboot;public bool PreventAutomaticReboot {get=>_PreventAutomaticReboot;set=>Set(ref _PreventAutomaticReboot,value);}
 private bool _DisableLastAccess;public bool DisableLastAccess {get=>_DisableLastAccess;set=>Set(ref _DisableLastAccess,value);}
 private bool _EnableRemoteDesktop;public bool EnableRemoteDesktop {get=>_EnableRemoteDesktop;set=>Set(ref _EnableRemoteDesktop,value);}
 private bool _AllowPowerShellScripts;public bool AllowPowerShellScripts {get=>_AllowPowerShellScripts;set=>Set(ref _AllowPowerShellScripts,value);}
 private ObservableCollection<string> _RemovedApps=new();public ObservableCollection<string> RemovedApps {get=>_RemovedApps;set=>Set(ref _RemovedApps,value);}
 private string _WallpaperImageScript="";public string WallpaperImageScript {get=>_WallpaperImageScript;set=>Set(ref _WallpaperImageScript,value);}
 private string _LockScreenMode=ImageBackgroundSettings.LockDefault;public string LockScreenMode {get=>_LockScreenMode;set=>Set(ref _LockScreenMode,value);}
 private string _LockScreenImageScript="";public string LockScreenImageScript {get=>_LockScreenImageScript;set=>Set(ref _LockScreenImageScript,value);}
 private bool _DisableAutomaticRestartSignOn; public bool DisableAutomaticRestartSignOn {get=>_DisableAutomaticRestartSignOn;set=>Set(ref _DisableAutomaticRestartSignOn,value);}
 private bool _PreventDeviceApps; public bool PreventDeviceApps {get=>_PreventDeviceApps;set=>Set(ref _PreventDeviceApps,value);}
 private bool _DeleteWindowsOld; public bool DeleteWindowsOld {get=>_DeleteWindowsOld;set=>Set(ref _DeleteWindowsOld,value);}
 private string _CompactOsMode=CompactOsSettings.Default; public string CompactOsMode {get=>_CompactOsMode;set=>Set(ref _CompactOsMode,value);}
 private ImportedXml? _ImportedXml; public ImportedXml? ImportedXml {get=>_ImportedXml;set=>Set(ref _ImportedXml,value);}
 private bool _ConfirmImportedXmlRisk; public bool ConfirmImportedXmlRisk {get=>_ConfirmImportedXmlRisk;set=>Set(ref _ConfirmImportedXmlRisk,value);}
 public ObservableCollection<ScriptEntry> Scripts {get;set;}=new();
 private bool _RestartExplorerAfterScripts;public bool RestartExplorerAfterScripts {get=>_RestartExplorerAfterScripts;set=>Set(ref _RestartExplorerAfterScripts,value);}
 private bool _DisablePointerPrecision; public bool DisablePointerPrecision {get=>_DisablePointerPrecision;set=>Set(ref _DisablePointerPrecision,value);}
 private string _StickyKeysMode=StickyKeySettings.Default; public string StickyKeysMode {get=>_StickyKeysMode;set=>Set(ref _StickyKeysMode,value);}
 private bool _StickyHotKeyActive; public bool StickyHotKeyActive {get=>_StickyHotKeyActive;set=>Set(ref _StickyHotKeyActive,value);}
 private bool _StickyHotKeySound; public bool StickyHotKeySound {get=>_StickyHotKeySound;set=>Set(ref _StickyHotKeySound,value);}
 private bool _StickyIndicator; public bool StickyIndicator {get=>_StickyIndicator;set=>Set(ref _StickyIndicator,value);}
 private bool _StickyAudibleFeedback; public bool StickyAudibleFeedback {get=>_StickyAudibleFeedback;set=>Set(ref _StickyAudibleFeedback,value);}
 private bool _StickyTriState; public bool StickyTriState {get=>_StickyTriState;set=>Set(ref _StickyTriState,value);}
 private bool _StickyTwoKeysOff; public bool StickyTwoKeysOff {get=>_StickyTwoKeysOff;set=>Set(ref _StickyTwoKeysOff,value);}
 private bool _IgnoreNumLock; public bool IgnoreNumLock {get=>_IgnoreNumLock;set=>Set(ref _IgnoreNumLock,value);}
 private bool _IgnoreCapsLock; public bool IgnoreCapsLock {get=>_IgnoreCapsLock;set=>Set(ref _IgnoreCapsLock,value);}
 private bool _IgnoreScrollLock; public bool IgnoreScrollLock {get=>_IgnoreScrollLock;set=>Set(ref _IgnoreScrollLock,value);}
 private bool _DisableAppSuggestions; public bool DisableAppSuggestions {get=>_DisableAppSuggestions;set=>Set(ref _DisableAppSuggestions,value);}
 private bool _DisableWindowsUpdate; public bool DisableWindowsUpdate {get=>_DisableWindowsUpdate;set=>Set(ref _DisableWindowsUpdate,value);}
 private bool _HideEdgeFre; public bool HideEdgeFre {get=>_HideEdgeFre;set=>Set(ref _HideEdgeFre,value);}
 private bool _DisableEdgeStartupBoost; public bool DisableEdgeStartupBoost {get=>_DisableEdgeStartupBoost;set=>Set(ref _DisableEdgeStartupBoost,value);}
 private bool _EnableLongPaths; public bool EnableLongPaths {get=>_EnableLongPaths;set=>Set(ref _EnableLongPaths,value);}
 private bool _TurnOffSystemSounds; public bool TurnOffSystemSounds {get=>_TurnOffSystemSounds;set=>Set(ref _TurnOffSystemSounds,value);}
 private bool _DisableFastStartup; public bool DisableFastStartup {get=>_DisableFastStartup;set=>Set(ref _DisableFastStartup,value);}
 private string _AccountLockoutMode=AccountLockoutPolicy.Default; public string AccountLockoutMode {get=>_AccountLockoutMode;set=>Set(ref _AccountLockoutMode,value);}
 private int? _LockoutThreshold=10; public int? LockoutThreshold {get=>_LockoutThreshold;set=>Set(ref _LockoutThreshold,value);}
 private int? _LockoutDurationMinutes=10; public int? LockoutDurationMinutes {get=>_LockoutDurationMinutes;set=>Set(ref _LockoutDurationMinutes,value);}
 private int? _LockoutResetMinutes=10; public int? LockoutResetMinutes {get=>_LockoutResetMinutes;set=>Set(ref _LockoutResetMinutes,value);}
 private string _LockKeyMode=KeyboardLockSettings.Default; public string LockKeyMode {get=>_LockKeyMode;set=>Set(ref _LockKeyMode,value);}
 private bool _InitialNumLockOn=true; public bool InitialNumLockOn {get=>_InitialNumLockOn;set=>Set(ref _InitialNumLockOn,value);}
 private bool _InitialCapsLockOn=false; public bool InitialCapsLockOn {get=>_InitialCapsLockOn;set=>Set(ref _InitialCapsLockOn,value);}
 private bool _InitialScrollLockOn=false; public bool InitialScrollLockOn {get=>_InitialScrollLockOn;set=>Set(ref _InitialScrollLockOn,value);}
 private string _WallpaperMode=WallpaperSettings.Default; public string WallpaperMode {get=>_WallpaperMode;set=>Set(ref _WallpaperMode,value);}
 private string _WallpaperColorHex="#000000"; public string WallpaperColorHex {get=>_WallpaperColorHex;set=>Set(ref _WallpaperColorHex,value);}
 private string _ColorSettingsMode=ColorSettings.Default; public string ColorSettingsMode {get=>_ColorSettingsMode;set=>Set(ref _ColorSettingsMode,value);}
 private string _WindowsColorTheme=ColorSettings.Dark; public string WindowsColorTheme {get=>_WindowsColorTheme;set=>Set(ref _WindowsColorTheme,value);}
 private string _AppsColorTheme=ColorSettings.Light; public string AppsColorTheme {get=>_AppsColorTheme;set=>Set(ref _AppsColorTheme,value);}
 private string _AccentColorHex="#0078D4"; public string AccentColorHex {get=>_AccentColorHex;set=>Set(ref _AccentColorHex,value);}
 private bool _EnableColorTransparency=true; public bool EnableColorTransparency {get=>_EnableColorTransparency;set=>Set(ref _EnableColorTransparency,value);}
 private bool _AccentColorOnStart; public bool AccentColorOnStart {get=>_AccentColorOnStart;set=>Set(ref _AccentColorOnStart,value);}
 private bool _AccentColorOnBorders; public bool AccentColorOnBorders {get=>_AccentColorOnBorders;set=>Set(ref _AccentColorOnBorders,value);}
 private bool _AllowUnsupportedUpgrade; public bool AllowUnsupportedUpgrade {get=>_AllowUnsupportedUpgrade;set=>Set(ref _AllowUnsupportedUpgrade,value);}
 private bool _BypassTPMCheck; public bool BypassTPMCheck {get=>_BypassTPMCheck;set=>Set(ref _BypassTPMCheck,value);}
 private bool _BypassSecureBootCheck; public bool BypassSecureBootCheck {get=>_BypassSecureBootCheck;set=>Set(ref _BypassSecureBootCheck,value);}
 private bool _BypassRAMCheck; public bool BypassRAMCheck {get=>_BypassRAMCheck;set=>Set(ref _BypassRAMCheck,value);}
 private string _VisualEffectsMode=global::UnattendLocal.Localization.UiText.Source("text.54a8207458f7"); public string VisualEffectsMode {get=>_VisualEffectsMode;set=>Set(ref _VisualEffectsMode,value);}
 private bool _EffectControlAnimations; public bool EffectControlAnimations {get=>_EffectControlAnimations;set=>Set(ref _EffectControlAnimations,value);}
 private bool _EffectAnimateMinMax; public bool EffectAnimateMinMax {get=>_EffectAnimateMinMax;set=>Set(ref _EffectAnimateMinMax,value);}
 private bool _EffectTaskbarAnimations; public bool EffectTaskbarAnimations {get=>_EffectTaskbarAnimations;set=>Set(ref _EffectTaskbarAnimations,value);}
 private bool _EffectDWMAeroPeekEnabled; public bool EffectDWMAeroPeekEnabled {get=>_EffectDWMAeroPeekEnabled;set=>Set(ref _EffectDWMAeroPeekEnabled,value);}
 private bool _EffectMenuAnimation; public bool EffectMenuAnimation {get=>_EffectMenuAnimation;set=>Set(ref _EffectMenuAnimation,value);}
 private bool _EffectTooltipAnimation; public bool EffectTooltipAnimation {get=>_EffectTooltipAnimation;set=>Set(ref _EffectTooltipAnimation,value);}
 private bool _EffectSelectionFade; public bool EffectSelectionFade {get=>_EffectSelectionFade;set=>Set(ref _EffectSelectionFade,value);}
 private bool _EffectDWMSaveThumbnailEnabled; public bool EffectDWMSaveThumbnailEnabled {get=>_EffectDWMSaveThumbnailEnabled;set=>Set(ref _EffectDWMSaveThumbnailEnabled,value);}
 private bool _EffectCursorShadow; public bool EffectCursorShadow {get=>_EffectCursorShadow;set=>Set(ref _EffectCursorShadow,value);}
 private bool _EffectListviewShadow; public bool EffectListviewShadow {get=>_EffectListviewShadow;set=>Set(ref _EffectListviewShadow,value);}
 private bool _EffectThumbnailsOrIcon; public bool EffectThumbnailsOrIcon {get=>_EffectThumbnailsOrIcon;set=>Set(ref _EffectThumbnailsOrIcon,value);}
 private bool _EffectListviewAlphaSelect; public bool EffectListviewAlphaSelect {get=>_EffectListviewAlphaSelect;set=>Set(ref _EffectListviewAlphaSelect,value);}
 private bool _EffectDragFullWindows; public bool EffectDragFullWindows {get=>_EffectDragFullWindows;set=>Set(ref _EffectDragFullWindows,value);}
 private bool _EffectComboBoxAnimation; public bool EffectComboBoxAnimation {get=>_EffectComboBoxAnimation;set=>Set(ref _EffectComboBoxAnimation,value);}
 private bool _EffectFontSmoothing; public bool EffectFontSmoothing {get=>_EffectFontSmoothing;set=>Set(ref _EffectFontSmoothing,value);}
 private bool _EffectListBoxSmoothScrolling; public bool EffectListBoxSmoothScrolling {get=>_EffectListBoxSmoothScrolling;set=>Set(ref _EffectListBoxSmoothScrolling,value);}
 private bool _EffectDropShadow; public bool EffectDropShadow {get=>_EffectDropShadow;set=>Set(ref _EffectDropShadow,value);}
 private bool _DesktopControlPanel; public bool DesktopControlPanel {get=>_DesktopControlPanel;set=>Set(ref _DesktopControlPanel,value);}
 private bool _DesktopDesktop; public bool DesktopDesktop {get=>_DesktopDesktop;set=>Set(ref _DesktopDesktop,value);}
 private bool _DesktopDocuments; public bool DesktopDocuments {get=>_DesktopDocuments;set=>Set(ref _DesktopDocuments,value);}
 private bool _DesktopDownloads; public bool DesktopDownloads {get=>_DesktopDownloads;set=>Set(ref _DesktopDownloads,value);}
 private bool _DesktopGallery; public bool DesktopGallery {get=>_DesktopGallery;set=>Set(ref _DesktopGallery,value);}
 private bool _DesktopHome; public bool DesktopHome {get=>_DesktopHome;set=>Set(ref _DesktopHome,value);}
 private bool _DesktopMusic; public bool DesktopMusic {get=>_DesktopMusic;set=>Set(ref _DesktopMusic,value);}
 private bool _DesktopNetwork; public bool DesktopNetwork {get=>_DesktopNetwork;set=>Set(ref _DesktopNetwork,value);}
 private bool _DesktopPictures; public bool DesktopPictures {get=>_DesktopPictures;set=>Set(ref _DesktopPictures,value);}
 private bool _DesktopRecycleBin; public bool DesktopRecycleBin {get=>_DesktopRecycleBin;set=>Set(ref _DesktopRecycleBin,value);}
 private bool _DesktopThisPC; public bool DesktopThisPC {get=>_DesktopThisPC;set=>Set(ref _DesktopThisPC,value);}
 private bool _DesktopUserFiles; public bool DesktopUserFiles {get=>_DesktopUserFiles;set=>Set(ref _DesktopUserFiles,value);}
 private bool _DesktopVideos; public bool DesktopVideos {get=>_DesktopVideos;set=>Set(ref _DesktopVideos,value);}
 private bool _StartFolderDocuments; public bool StartFolderDocuments {get=>_StartFolderDocuments;set=>Set(ref _StartFolderDocuments,value);}
 private bool _StartFolderDownloads; public bool StartFolderDownloads {get=>_StartFolderDownloads;set=>Set(ref _StartFolderDownloads,value);}
 private bool _StartFolderFileExplorer; public bool StartFolderFileExplorer {get=>_StartFolderFileExplorer;set=>Set(ref _StartFolderFileExplorer,value);}
 private bool _StartFolderMusic; public bool StartFolderMusic {get=>_StartFolderMusic;set=>Set(ref _StartFolderMusic,value);}
 private bool _StartFolderNetwork; public bool StartFolderNetwork {get=>_StartFolderNetwork;set=>Set(ref _StartFolderNetwork,value);}
 private bool _StartFolderPersonalFolder; public bool StartFolderPersonalFolder {get=>_StartFolderPersonalFolder;set=>Set(ref _StartFolderPersonalFolder,value);}
 private bool _StartFolderPictures; public bool StartFolderPictures {get=>_StartFolderPictures;set=>Set(ref _StartFolderPictures,value);}
 private bool _StartFolderSettings; public bool StartFolderSettings {get=>_StartFolderSettings;set=>Set(ref _StartFolderSettings,value);}
 private bool _StartFolderVideos; public bool StartFolderVideos {get=>_StartFolderVideos;set=>Set(ref _StartFolderVideos,value);}
 private string _DesktopIconsMode=global::UnattendLocal.Localization.UiText.Source("text.86db4171d7fb"); public string DesktopIconsMode {get=>_DesktopIconsMode;set=>Set(ref _DesktopIconsMode,value);}
 private string _StartFoldersMode=global::UnattendLocal.Localization.UiText.Source("text.0b8312769b1d"); public string StartFoldersMode {get=>_StartFoldersMode;set=>Set(ref _StartFoldersMode,value);}
 private bool _DeleteEdgeDesktopIcon; public bool DeleteEdgeDesktopIcon {get=>_DeleteEdgeDesktopIcon;set=>Set(ref _DeleteEdgeDesktopIcon,value);}
 private string _HideFilesMode=global::UnattendLocal.Localization.UiText.Source("text.54a8207458f7"); public string HideFilesMode {get=>_HideFilesMode;set=>Set(ref _HideFilesMode,value);}
 private bool _ShowFileExtensions; public bool ShowFileExtensions {get=>_ShowFileExtensions;set=>Set(ref _ShowFileExtensions,value);}
 private bool _ClassicContextMenu; public bool ClassicContextMenu {get=>_ClassicContextMenu;set=>Set(ref _ClassicContextMenu,value);}
 private bool _HideInfoTip; public bool HideInfoTip {get=>_HideInfoTip;set=>Set(ref _HideInfoTip,value);}
 private bool _LaunchToThisPC; public bool LaunchToThisPC {get=>_LaunchToThisPC;set=>Set(ref _LaunchToThisPC,value);}
 private bool _ShowEndTask; public bool ShowEndTask {get=>_ShowEndTask;set=>Set(ref _ShowEndTask,value);}
 private string _TaskbarSearch=global::UnattendLocal.Localization.UiText.Source("text.f2e4f338f5b2"); public string TaskbarSearch {get=>_TaskbarSearch;set=>Set(ref _TaskbarSearch,value);}
 private string _TaskbarIconsMode=global::UnattendLocal.Localization.UiText.Source("text.b71c73b95540"); public string TaskbarIconsMode {get=>_TaskbarIconsMode;set=>Set(ref _TaskbarIconsMode,value);}
 private string _TaskbarIconsXml=""; public string TaskbarIconsXml {get=>_TaskbarIconsXml;set=>Set(ref _TaskbarIconsXml,value);}
 private string _StartTilesMode=global::UnattendLocal.Localization.UiText.Source("text.ea371476026f"); public string StartTilesMode {get=>_StartTilesMode;set=>Set(ref _StartTilesMode,value);}
 private string _StartTilesXml=""; public string StartTilesXml {get=>_StartTilesXml;set=>Set(ref _StartTilesXml,value);}
 private string _StartPinsMode=global::UnattendLocal.Localization.UiText.Source("text.0b6ea014b2f6"); public string StartPinsMode {get=>_StartPinsMode;set=>Set(ref _StartPinsMode,value);}
 private string _StartPinsJson=""; public string StartPinsJson {get=>_StartPinsJson;set=>Set(ref _StartPinsJson,value);}
 private bool _DisableWidgets; public bool DisableWidgets {get=>_DisableWidgets;set=>Set(ref _DisableWidgets,value);}
 private bool _LeftTaskbar; public bool LeftTaskbar {get=>_LeftTaskbar;set=>Set(ref _LeftTaskbar,value);}
 private bool _HideTaskViewButton; public bool HideTaskViewButton {get=>_HideTaskViewButton;set=>Set(ref _HideTaskViewButton,value);}
 private bool _ShowAllTrayIcons; public bool ShowAllTrayIcons {get=>_ShowAllTrayIcons;set=>Set(ref _ShowAllTrayIcons,value);}
 private bool _DisableBingResults; public bool DisableBingResults {get=>_DisableBingResults;set=>Set(ref _DisableBingResults,value);}
 private string _ImageSelection=global::UnattendLocal.Localization.UiText.Source("text.cc65b6582544"); public string ImageSelection {get=>_ImageSelection;set=>Set(ref _ImageSelection,value);}
 private string _ImageName=""; public string ImageName {get=>_ImageName;set=>Set(ref _ImageName,value);}
 private int? _ImageIndex; public int? ImageIndex {get=>_ImageIndex;set=>Set(ref _ImageIndex,value);}
 private string _DiskMode=global::UnattendLocal.Localization.UiText.Source("text.4a9df1058984"); public string DiskMode {get=>_DiskMode;set{if(_DiskMode==value)return;ClearDiskConsent();Set(ref _DiskMode,value);}}
 private int? _TargetDiskId; public int? TargetDiskId {get=>_TargetDiskId;set{if(_TargetDiskId==value)return;ClearDiskConsent();Set(ref _TargetDiskId,value);}}
 private int? _TargetPartitionId; public int? TargetPartitionId {get=>_TargetPartitionId;set{if(_TargetPartitionId==value)return;ClearDiskConsent();Set(ref _TargetPartitionId,value);}}
 private string _DiskLayout="UEFI / GPT"; public string DiskLayout {get=>_DiskLayout;set{if(_DiskLayout==value)return;ClearDiskConsent();Set(ref _DiskLayout,value);}}
 private int? _SystemPartitionSizeMB; public int? SystemPartitionSizeMB {get=>_SystemPartitionSizeMB;set{if(_SystemPartitionSizeMB==value)return;ClearDiskConsent();Set(ref _SystemPartitionSizeMB,value);}}
 private bool _PauseBeforeFormatting; public bool PauseBeforeFormatting {get=>_PauseBeforeFormatting;set{if(_PauseBeforeFormatting==value)return;ClearDiskConsent();Set(ref _PauseBeforeFormatting,value);}}
 private int _RecoverySizeMB=1536; public int RecoverySizeMB {get=>_RecoverySizeMB;set{if(_RecoverySizeMB==value)return;ClearDiskConsent();Set(ref _RecoverySizeMB,value);}}
 private bool _FormatExistingPartition; public bool FormatExistingPartition {get=>_FormatExistingPartition;set{if(_FormatExistingPartition==value)return;ConfirmFormatPartition=false;Set(ref _FormatExistingPartition,value);}}
 private bool _ConfirmWipeDisk; public bool ConfirmWipeDisk {get=>_ConfirmWipeDisk;set=>Set(ref _ConfirmWipeDisk,value);}
 private bool _ConfirmFormatPartition; public bool ConfirmFormatPartition {get=>_ConfirmFormatPartition;set=>Set(ref _ConfirmFormatPartition,value);}
 private string _DiskPartScript=""; public string DiskPartScript {get=>_DiskPartScript;set{if(_DiskPartScript==value)return;ClearDiskConsent();Set(ref _DiskPartScript,value);}}
 private bool _ConfirmDiskScript; public bool ConfirmDiskScript {get=>_ConfirmDiskScript;set=>Set(ref _ConfirmDiskScript,value);}
 public void ClearDiskConsent(){ConfirmAdditionalXml=false;ConfirmImportedXmlRisk=false;ConfirmDiskScript=false;ConfirmWipeDisk=false;ConfirmFormatPartition=false;}
 private string _CustomStartingPoint=global::UnattendLocal.Localization.UiText.Source("text.f0866be5e5a3"); public string CustomStartingPoint {get=>_CustomStartingPoint;set=>Set(ref _CustomStartingPoint,value);}
 private string _TimeZoneMode=global::UnattendLocal.Localization.UiText.Source("text.980f34c4425d"); public string TimeZoneMode {get=>_TimeZoneMode;set=>Set(ref _TimeZoneMode,value);}
 private string _ConfigurationMode=global::UnattendLocal.Localization.UiText.Source("text.cb1e0e22c7de"); public string ConfigurationMode {get=>_ConfigurationMode;set=>Set(ref _ConfigurationMode,value);}
 private string _TargetWindows="Windows 11"; public string TargetWindows {get=>_TargetWindows;set=>Set(ref _TargetWindows,value);}
 private string _RecommendationId=""; public string RecommendationId {get=>_RecommendationId;set=>Set(ref _RecommendationId,value);}
 private string _RecommendationWindows=""; public string RecommendationWindows {get=>_RecommendationWindows;set=>Set(ref _RecommendationWindows,value);}
 private int _RecommendationVersion; public int RecommendationVersion {get=>_RecommendationVersion;set=>Set(ref _RecommendationVersion,value);}

 private bool _PreventDeviceEncryption; public bool PreventDeviceEncryption {get=>_PreventDeviceEncryption;set=>Set(ref _PreventDeviceEncryption,value);}
 private bool _HidePowerShellWindows; public bool HidePowerShellWindows {get=>_HidePowerShellWindows;set=>Set(ref _HidePowerShellWindows,value);}
 private string _EditionMode = global::UnattendLocal.Localization.UiText.Source("text.5056d73b13ec"); public string EditionMode {get=>_EditionMode;set=>Set(ref _EditionMode,value);}
 private string _EditionId = "pro"; public string EditionId {get=>_EditionId;set=>Set(ref _EditionId,value);}
 private string _CustomProductKey = ""; public string CustomProductKey {get=>_CustomProductKey;set{Set(ref _CustomProductKey,value);if(!string.IsNullOrWhiteSpace(value))NeedsInstallationKey=false;}}
 private bool _UseActivationKey; public bool UseActivationKey {get=>_UseActivationKey;set=>Set(ref _UseActivationKey,value);}
 private string _ActivationProductKey = ""; public string ActivationProductKey {get=>_ActivationProductKey;set{Set(ref _ActivationProductKey,value);if(!string.IsNullOrWhiteSpace(value))NeedsActivationKey=false;}}
 private bool _NeedsInstallationKey; public bool NeedsInstallationKey {get=>_NeedsInstallationKey;set=>Set(ref _NeedsInstallationKey,value);}
 private bool _NeedsActivationKey; public bool NeedsActivationKey {get=>_NeedsActivationKey;set=>Set(ref _NeedsActivationKey,value);}
 private bool _KeepSensitiveFiles; public bool KeepSensitiveFiles {get=>_KeepSensitiveFiles;set=>Set(ref _KeepSensitiveFiles,value);}
 private int _SchemaVersion = 1; public int SchemaVersion {get=>_SchemaVersion;set=>Set(ref _SchemaVersion,value);}
 private string _ComputerName = ""; public string ComputerName {get=>_ComputerName;set=>Set(ref _ComputerName,value);}
 private string _ImageLanguage = "zh-CN"; public string ImageLanguage {get=>_ImageLanguage;set=>Set(ref _ImageLanguage,value);}
 private string _Locale = "zh-Hans-CN"; public string Locale {get=>_Locale;set=>Set(ref _Locale,value);}
 private string _Keyboard = "0804:{81d4e9c9-1d3b-41bc-9e6c-4b40bf79e35e}{fa550b04-5ad7-411f-a5ac-ca038ec515d7}"; public string Keyboard {get=>_Keyboard;set=>Set(ref _Keyboard,value);}
 private string _GeoId = "45"; public string GeoId {get=>_GeoId;set=>Set(ref _GeoId,value);}
 private string _TimeZone = "China Standard Time"; public string TimeZone {get=>_TimeZone;set=>Set(ref _TimeZone,value);}
 private bool _InteractiveLanguage = false; public bool InteractiveLanguage {get=>_InteractiveLanguage;set=>Set(ref _InteractiveLanguage,value);}
 private string _AccountMode = global::UnattendLocal.Localization.UiText.Source("text.cdaa9a6700f6"); public string AccountMode {get=>_AccountMode;set=>Set(ref _AccountMode,value);}
 private bool _AutoLogon = true; public bool AutoLogon {get=>_AutoLogon;set=>Set(ref _AutoLogon,value);}
 private string _PasswordExpiration = global::UnattendLocal.Localization.UiText.Source("text.2c60316d5ea6"); public string PasswordExpiration {get=>_PasswordExpiration;set=>Set(ref _PasswordExpiration,value);}
 private int _PasswordDays = 42; public int PasswordDays {get=>_PasswordDays;set=>Set(ref _PasswordDays,value);}
 private string _WifiProfileXml=""; public string WifiProfileXml {get=>_WifiProfileXml;set{Set(ref _WifiProfileXml,value);if(!string.IsNullOrWhiteSpace(value))NeedsWifiProfile=false;}}
 private bool _NeedsWifiProfile;public bool NeedsWifiProfile {get=>_NeedsWifiProfile;set=>Set(ref _NeedsWifiProfile,value);}
 private string _WifiMode = global::UnattendLocal.Localization.UiText.Source("text.fd17c35ec3cc"); public string WifiMode {get=>_WifiMode;set=>Set(ref _WifiMode,value);}
 private string _Ssid = ""; public string Ssid {get=>_Ssid;set=>Set(ref _Ssid,value);}
 private string _WifiPassword = ""; public string WifiPassword {get=>_WifiPassword;set=>Set(ref _WifiPassword,value);}
 private bool _NeedsWifiPassword = false; public bool NeedsWifiPassword {get=>_NeedsWifiPassword;set=>Set(ref _NeedsWifiPassword,value);}
 private bool _HiddenWifi = false; public bool HiddenWifi {get=>_HiddenWifi;set=>Set(ref _HiddenWifi,value);}
 private bool _ConnectWifi = true; public bool ConnectWifi {get=>_ConnectWifi;set=>Set(ref _ConnectWifi,value);}
 private bool _BypassNetwork = false; public bool BypassNetwork {get=>_BypassNetwork;set=>Set(ref _BypassNetwork,value);}
 private string _PrivacyMode = global::UnattendLocal.Localization.UiText.Source("text.fd17c35ec3cc"); public string PrivacyMode {get=>_PrivacyMode;set=>Set(ref _PrivacyMode,value);}
 private bool _HideOEMRegistration; public bool HideOEMRegistration {get=>_HideOEMRegistration;set=>Set(ref _HideOEMRegistration,value);}
 private bool _HideEula = false; public bool HideEula {get=>_HideEula;set=>Set(ref _HideEula,value);}
 private bool _HideOnlineAccount = true; public bool HideOnlineAccount {get=>_HideOnlineAccount;set=>Set(ref _HideOnlineAccount,value);}
 private bool _ObscurePasswords = true; public bool ObscurePasswords {get=>_ObscurePasswords;set=>Set(ref _ObscurePasswords,value);}
 private bool _BypassHardware = false; public bool BypassHardware {get=>_BypassHardware;set=>Set(ref _BypassHardware,value);}
 private string _Architecture = "amd64"; public string Architecture {get=>_Architecture;set{if(_Architecture==value)return;ClearDiskConsent();Set(ref _Architecture,value);}}
 public ObservableCollection<UserEntry> Users {get;set;} = new();
 // Operational defaults avoid installation prompts; identity and environment values must be filled in the tool.
 // Do not change field initializers: older saved plans rely on their compatibility defaults.
 public static InstallPlan NewCustom()=>new(){PreventDeviceEncryption=true,ConfigurationMode=global::UnattendLocal.Localization.UiText.Source("text.cb1e0e22c7de"),CustomStartingPoint=global::UnattendLocal.Localization.UiText.Source("text.12a6c9d91dc8"),AccountMode=global::UnattendLocal.Localization.UiText.Source("text.cdaa9a6700f6"),AutoLogon=true,PasswordExpiration=global::UnattendLocal.Localization.UiText.Source("text.1823400b7251"),EditionMode=global::UnattendLocal.Localization.UiText.Source("text.cc706914a2ad"),EditionId="",WifiMode=global::UnattendLocal.Localization.UiText.Source("text.307f498ede07"),ConnectWifi=false,BypassNetwork=true,PrivacyMode=global::UnattendLocal.Localization.UiText.Source("text.9d9c1d7fb13c"),HideOnlineAccount=true,HideEula=true,HideOEMRegistration=true,InteractiveLanguage=false,ImageLanguage="",Locale="",Keyboard="",GeoId="",TimeZoneMode=global::UnattendLocal.Localization.UiText.Source("text.499ff33c1078"),TimeZone="",HidePowerShellWindows=true,ObscurePasswords=true};
 public static InstallPlan New() {var p=new InstallPlan();p.Users.Add(new());Recommendations.Apply(p,"wired");return p;}
}
