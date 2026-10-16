using System.Reflection;
namespace UnattendLocal.Core;
public static class Recommendations {
 public record Difference(string Property,string Label,string Current,string Recommended);
 // Only policy fields are reset. Machine identity, locale, SKU and all credentials remain user-owned.
 private static readonly Dictionary<string,string> Fields=new(){
  [nameof(InstallPlan.DeleteJunctions)]="删除旧版兼容联接点",
  [nameof(InstallPlan.UseAppLocker)]="应用 AppLocker 策略",[nameof(InstallPlan.UseConfigurationSet)]="使用 $OEM$ 配置集",
  [nameof(InstallPlan.DisableSac)]="关闭智能应用控制（SAC）",
  [nameof(InstallPlan.DisableSmartScreen)]="关闭 Windows 与 Edge 的 SmartScreen",
  [nameof(InstallPlan.DisableCoreIsolation)]="关闭核心隔离（VBS / 内存完整性）",
  [nameof(InstallPlan.HardenSystemDriveAcl)]="收紧 C 盘根目录写入权限",

  [nameof(InstallPlan.UseSecondLanguage)]="启用第二组语言与键盘",[nameof(InstallPlan.UseThirdLanguage)]="启用第三组语言与键盘",
  [nameof(InstallPlan.UseNarrator)]="讲述人辅助安装",[nameof(InstallPlan.MakeEdgeUninstallable)]="允许卸载 Edge",[nameof(InstallPlan.DisableSystemRestore)]="关闭 C 盘系统还原",
  [nameof(InstallPlan.VMwareTools)]="安装 VMware Tools",[nameof(InstallPlan.VBoxGuestAdditions)]="安装 VirtualBox 增强功能",[nameof(InstallPlan.VirtIoGuestTools)]="安装 VirtIO Guest Tools",[nameof(InstallPlan.ParallelsTools)]="安装 Parallels Tools",
  [nameof(InstallPlan.DisableUac)]="关闭用户账户控制（UAC）",[nameof(InstallPlan.DisableWpbt)]="阻止 WPBT 固件程序执行",
  [nameof(InstallPlan.ProcessAuditMode)]="进程创建审计",
  [nameof(InstallPlan.PreventAutomaticReboot)]="减少 Windows 更新自动重启",[nameof(InstallPlan.DisableLastAccess)]="禁用 NTFS 最后访问时间更新",
  [nameof(InstallPlan.EnableRemoteDesktop)]="启用远程桌面",[nameof(InstallPlan.AllowPowerShellScripts)]="Windows PowerShell 执行策略",
  [nameof(InstallPlan.RemovedApps)]="应用移除",
  [nameof(InstallPlan.DisableAutomaticRestartSignOn)]="禁用重启后的最后用户自动登录",
  [nameof(InstallPlan.PreventDeviceApps)]="阻止硬件关联应用自动安装",
  [nameof(InstallPlan.DeleteWindowsOld)]="清理空 Windows.old 文件夹",
  [nameof(InstallPlan.CompactOsMode)]="CompactOS 系统文件压缩",
  [nameof(InstallPlan.DisablePointerPrecision)]="关闭鼠标提高指针精确度",
  [nameof(InstallPlan.StickyKeysMode)]="粘滞键设置",
  [nameof(InstallPlan.DisableAppSuggestions)]="禁用应用建议和自动推广",
  [nameof(InstallPlan.DisableWindowsUpdate)]="循环暂停 Windows 更新",
  [nameof(InstallPlan.HideEdgeFre)]="跳过 Edge 首次运行引导",
  [nameof(InstallPlan.DisableEdgeStartupBoost)]="关闭 Edge 启动加速及后台运行",
  [nameof(InstallPlan.EnableLongPaths)]="启用长路径支持",
  [nameof(InstallPlan.TurnOffSystemSounds)]="关闭系统声音",
  [nameof(InstallPlan.DisableFastStartup)]="禁用快速启动",
  [nameof(InstallPlan.AccountLockoutMode)]="账户锁定策略",
  [nameof(InstallPlan.LockKeyMode)]="键盘初始状态",
  [nameof(InstallPlan.WallpaperMode)]="桌面背景",
  [nameof(InstallPlan.LockScreenMode)]="锁屏图片",
  [nameof(InstallPlan.ColorSettingsMode)]="Windows 配色",
  [nameof(InstallPlan.VisualEffectsMode)]="视觉效果",
  [nameof(InstallPlan.BypassTPMCheck)]="绕过 TPM 2.0 检查",[nameof(InstallPlan.BypassSecureBootCheck)]="绕过安全启动检查",[nameof(InstallPlan.BypassRAMCheck)]="绕过内存容量检查",
  [nameof(InstallPlan.EditionMode)]="安装密钥方式",[nameof(InstallPlan.AccountMode)]="账户创建方式",
  [nameof(InstallPlan.AutoLogon)]="首次自动登录",[nameof(InstallPlan.PasswordExpiration)]="密码到期策略",
  [nameof(InstallPlan.WifiMode)]="网络配置方式",[nameof(InstallPlan.HiddenWifi)]="隐藏无线网络",
  [nameof(InstallPlan.ConnectWifi)]="自动连接预设 Wi-Fi",[nameof(InstallPlan.BypassNetwork)]="允许无互联网安装",
  [nameof(InstallPlan.PrivacyMode)]="OOBE 快捷隐私",[nameof(InstallPlan.HideEula)]="隐藏许可页面",
  [nameof(InstallPlan.HideOnlineAccount)]="隐藏在线账户页面",[nameof(InstallPlan.HideOEMRegistration)]="隐藏 OEM 注册页面",
  [nameof(InstallPlan.HidePowerShellWindows)]="隐藏 PowerShell 窗口",[nameof(InstallPlan.KeepSensitiveFiles)]="保留安装隐私文件",
  [nameof(InstallPlan.ObscurePasswords)]="编码 XML 中的密码",[nameof(InstallPlan.BypassHardware)]="绕过硬件检查",
  [nameof(InstallPlan.PreventDeviceEncryption)]="禁止 BitLocker 自动设备加密",
  [nameof(InstallPlan.TaskbarSearch)]="任务栏搜索样式",[nameof(InstallPlan.TaskbarIconsMode)]="任务栏固定图标",
  [nameof(InstallPlan.StartTilesMode)]="Windows 10 开始菜单磁贴",[nameof(InstallPlan.StartPinsMode)]="Windows 11 开始菜单固定应用",
  [nameof(InstallPlan.DisableWidgets)]="禁用小组件",[nameof(InstallPlan.LeftTaskbar)]="任务栏左对齐",
  [nameof(InstallPlan.HideTaskViewButton)]="隐藏任务视图",[nameof(InstallPlan.ShowAllTrayIcons)]="显示全部托盘图标",[nameof(InstallPlan.DisableBingResults)]="不显示 Bing 搜索结果",
  [nameof(InstallPlan.HideFilesMode)]="隐藏文件显示方式",[nameof(InstallPlan.ShowFileExtensions)]="始终显示文件扩展名",
  [nameof(InstallPlan.ClassicContextMenu)]="使用经典右键菜单",[nameof(InstallPlan.HideInfoTip)]="隐藏悬停说明",
  [nameof(InstallPlan.LaunchToThisPC)]="打开资源管理器时进入此电脑",[nameof(InstallPlan.ShowEndTask)]="任务栏显示结束任务",
  [nameof(InstallPlan.DesktopIconsMode)]="系统桌面图标",[nameof(InstallPlan.StartFoldersMode)]="开始菜单电源按钮旁文件夹",[nameof(InstallPlan.DeleteEdgeDesktopIcon)]="删除 Edge 桌面快捷方式",
  [nameof(InstallPlan.TimeZoneMode)]="时区设置方式",[nameof(InstallPlan.InteractiveLanguage)]="手动选择语言与区域"};
 public static bool Known(string id)=>id is "wired" or "wifi";
 public static string Name(string id)=>id=="wifi"?"预设 Wi-Fi":"有线局域网";
 private static InstallPlan Baseline(string id,string windows,bool preventDeviceEncryption=false){
  if(!Known(id))throw new ArgumentException("未知推荐方案。");
  if(windows is not ("Windows 10" or "Windows 11" or "common"))throw new ArgumentException("推荐基准系统无效。");
  return new(){PreventDeviceEncryption=preventDeviceEncryption,EditionMode="按 SKU 使用通用安装密钥",AccountMode="自动创建本地账户",AutoLogon=true,PasswordExpiration="永不过期",WifiMode=id=="wifi"?"预设 WPA2 Wi-Fi":"隐藏无线网络选择页",HiddenWifi=false,ConnectWifi=id=="wifi",BypassNetwork=windows!="Windows 10",PrivacyMode="关闭快捷隐私设置",HideEula=true,HideOnlineAccount=true,HideOEMRegistration=true,HidePowerShellWindows=true,KeepSensitiveFiles=false,ObscurePasswords=true,BypassHardware=false,InteractiveLanguage=false};
 }
 private static PropertyInfo Property(string name)=>typeof(InstallPlan).GetProperty(name)!;
 // Historical version-1 Windows-specific policy; the current UI uses the shared version-3 overload.
 public static void Apply(InstallPlan p,string id,string windows){var b=Baseline(id,windows);foreach(var name in Fields.Keys){if(name==nameof(InstallPlan.CompactOsMode)&&!CompactOsSettings.CanEdit(p))continue;Property(name).SetValue(p,Property(name).GetValue(b));}p.TargetWindows=windows;p.RecommendationId=id;p.RecommendationWindows=windows;p.RecommendationVersion=1;p.ConfigurationMode="推荐配置";}
 public static void Apply(InstallPlan p,string id){var b=Baseline(id,"common",true);foreach(var name in Fields.Keys){if(name==nameof(InstallPlan.CompactOsMode)&&!CompactOsSettings.CanEdit(p))continue;Property(name).SetValue(p,Property(name).GetValue(b));}p.DiskMode="手动选择磁盘与分区";p.FormatExistingPartition=false;p.ClearDiskConsent();p.RecommendationId=id;p.RecommendationWindows="";p.RecommendationVersion=3;p.ConfigurationMode="推荐配置";}
 // Scene selection updates only its dependent network fields. Full policy restoration remains explicit.
 public static void SelectNetworkScene(InstallPlan p,string id){
  var b=Baseline(id,"common");p.WifiMode=b.WifiMode;p.HiddenWifi=b.HiddenWifi;p.ConnectWifi=b.ConnectWifi;
  p.RecommendationId=id;p.RecommendationWindows="";p.RecommendationVersion=3;
 }
 public static bool HasBaseline(InstallPlan p)=>Known(p.RecommendationId)&&(p.RecommendationVersion is 2 or 3||(p.RecommendationVersion==1&&p.RecommendationWindows is "Windows 10" or "Windows 11"));
 private static string Display(object? value)=>value switch{bool b=>b?"开启":"关闭","隐藏无线网络选择页"=>"有线局域网（跳过 Wi-Fi 配置）",WifiSettings.Wpa2 or WifiSettings.Wpa3 or WifiSettings.Open=>WifiSettings.ModeLabel((string)value),"关闭快捷隐私设置"=>"全部关闭",_=>value?.ToString()??""};
 private static string DisplayField(InstallPlan p,string name)=>name switch {nameof(InstallPlan.RemovedApps)=>AppRemovalSettings.Selection(p),nameof(InstallPlan.CompactOsMode)=>CompactOsSettings.ModeSummary(p),nameof(InstallPlan.StickyKeysMode)=>StickyKeySettings.ModeSummary(p),nameof(InstallPlan.AccountLockoutMode)=>AccountLockoutPolicy.ModeSummary(p),nameof(InstallPlan.LockKeyMode)=>KeyboardLockSettings.ModeSummary(p),nameof(InstallPlan.WallpaperMode)=>WallpaperSettings.ModeSummary(p),nameof(InstallPlan.LockScreenMode)=>ImageBackgroundSettings.LockSummary(p),nameof(InstallPlan.ColorSettingsMode)=>ColorSettings.ModeSummary(p),nameof(InstallPlan.VisualEffectsMode)=>VisualEffectsSettings.ModeSummary(p),nameof(InstallPlan.DesktopIconsMode)=>DesktopSettings.IconSummary(p),nameof(InstallPlan.StartFoldersMode)=>DesktopSettings.FolderSummary(p),_=>Display(Property(name).GetValue(p))};
 public static List<Difference> Differences(InstallPlan p){
  if(!HasBaseline(p))return new();var b=Baseline(p.RecommendationId,p.RecommendationVersion>=2?"common":p.RecommendationWindows,p.RecommendationVersion>=3);
  var result=Fields.Where(f=>!(f.Key==nameof(InstallPlan.RemovedApps)?p.RemovedApps.Order().SequenceEqual(b.RemovedApps.Order()):Equals(Property(f.Key).GetValue(p),Property(f.Key).GetValue(b)))).Select(f=>new Difference(f.Key,f.Value,DisplayField(p,f.Key),DisplayField(b,f.Key))).ToList();
  if(p.DiskMode!="手动选择磁盘与分区")result.Add(new(nameof(InstallPlan.DiskMode),"磁盘与分区",p.DiskMode,"手动选择磁盘与分区"));return result;
 }
 public static void Restore(InstallPlan p,string name){if(!HasBaseline(p))throw new ArgumentException("此方案没有可用的推荐基准。");if(name==nameof(InstallPlan.DiskMode)){p.DiskMode="手动选择磁盘与分区";p.FormatExistingPartition=false;p.ClearDiskConsent();return;}if(name==nameof(InstallPlan.CompactOsMode)&&!CompactOsSettings.CanEdit(p))throw new ArgumentException("原 XML 的 CompactOS 由自定义安装/压缩脚本控制，请使用原工具修改参数后重新导入。");if(!Fields.ContainsKey(name))throw new ArgumentException("此项不属于推荐设置。");Property(name).SetValue(p,Property(name).GetValue(Baseline(p.RecommendationId,p.RecommendationVersion>=2?"common":p.RecommendationWindows,p.RecommendationVersion>=3)));}
 public static string State(InstallPlan p)=>p.ConfigurationMode!="推荐配置"?(p.CustomStartingPoint=="基础配置"?"自定义配置 · 从基础值开始，请逐项选择或填写":"自定义配置 · 保留当前设置，所有选项均可自主调整"):!HasBaseline(p)?"推荐基准不可用，请重新应用推荐方案":$"基于 {Name(p.RecommendationId)}推荐 · {(Differences(p).Count==0?"推荐值已应用":"已修改")}";
 public static string InteractiveSteps(InstallPlan p){
  var steps=new List<string>();
  if(p.InteractiveLanguage)steps.Add("语言与区域");
  if(p.ComputerNameMode==ComputerNamingSettings.Prompt)steps.Add("计算机名输入");
  if(p.EditionMode=="安装时输入密钥")steps.Add("产品密钥与版本");
  if(p.AccountMode is "安装时创建本地账户" or "安装时添加微软账户")steps.Add("账户创建");
  if(p.WifiMode=="安装时手动选择")steps.Add("无线网络");
  if(p.PrivacyMode=="安装时手动选择")steps.Add("快捷隐私");
  if(!p.HideEula)steps.Add("许可协议");
  if(!p.HideOEMRegistration)steps.Add("OEM 注册（若适用）");
  if(p.AccountMode!="安装时添加微软账户"&&!p.HideOnlineAccount)steps.Add("在线账户页面（若适用）");
  if(p.DiskMode==InstallationSettings.Manual)steps.Add("磁盘与分区");
  return steps.Count==0?"所列步骤已预设；仍须验证镜像、驱动及目标环境。":string.Join("、",steps);
 }
 public static string MissingInputs(InstallPlan p){
  var items=new List<string>();
  if(p.UseAppLocker&&string.IsNullOrWhiteSpace(p.AppLockerPolicyXml))items.Add("AppLocker 策略 XML");
  if(p.ComputerNameMode==ComputerNamingSettings.Script&&string.IsNullOrWhiteSpace(p.ComputerNameScript))items.Add("计算机命名脚本");
  if(!p.InteractiveLanguage){if(p.UseSecondLanguage&&(string.IsNullOrWhiteSpace(p.SecondLocale)||string.IsNullOrWhiteSpace(p.SecondKeyboard)))items.Add("第二组语言与键盘");if(p.UseThirdLanguage&&(string.IsNullOrWhiteSpace(p.ThirdLocale)||string.IsNullOrWhiteSpace(p.ThirdKeyboard)))items.Add("第三组语言与键盘");}
  if(!p.InteractiveLanguage){if(string.IsNullOrWhiteSpace(p.ImageLanguage))items.Add("显示语言");if(string.IsNullOrWhiteSpace(p.Locale))items.Add("区域格式");if(string.IsNullOrWhiteSpace(p.Keyboard))items.Add("键盘 / 输入法");if(string.IsNullOrWhiteSpace(p.GeoId))items.Add("国家或地区");}
  if(p.TimeZoneMode=="指定时区"&&string.IsNullOrWhiteSpace(p.TimeZone))items.Add("时区");
  if(p.EditionMode=="按 SKU 使用通用安装密钥"&&string.IsNullOrWhiteSpace(p.EditionId))items.Add("SKU");
  if(p.EditionMode=="指定自己的产品密钥"&&string.IsNullOrWhiteSpace(p.CustomProductKey))items.Add("安装密钥");
  if(p.UseActivationKey&&string.IsNullOrWhiteSpace(p.ActivationProductKey))items.Add("激活密钥");
  if(p.AccountMode=="自动创建本地账户"){
   if(p.Users.Count==0)items.Add("本地账户");
   else{if(p.Users.Any(u=>string.IsNullOrWhiteSpace(u.Name)))items.Add("账户名称");if(!p.Users.Any(u=>u.Administrator))items.Add("管理员账户");if(p.Users.Any(u=>!u.AllowEmptyPassword&&string.IsNullOrEmpty(u.Password)))items.Add("账户密码或明确允许空密码");}
  }
  if(WifiSettings.IsPreset(p)){if(string.IsNullOrWhiteSpace(p.Ssid))items.Add("Wi-Fi 名称");if(WifiSettings.NeedsPassword(p)&&string.IsNullOrEmpty(p.WifiPassword))items.Add("Wi-Fi 密码");}
  if(p.WifiMode==WifiSettings.Profile&&(p.NeedsWifiProfile||string.IsNullOrWhiteSpace(p.WifiProfileXml)))items.Add("无线配置 XML");
  if(p.DiskMode!=InstallationSettings.Manual&&p.TargetDiskId==null)items.Add("目标磁盘编号");
  if((p.DiskMode==InstallationSettings.Existing||p.DiskMode==DiskScriptSettings.Mode)&&p.TargetPartitionId==null)items.Add("目标分区编号");
  if(p.DiskMode==DiskScriptSettings.Mode&&string.IsNullOrWhiteSpace(p.DiskPartScript))items.Add("DiskPart 脚本");
  if(p.Scripts?.Any(s=>s.Enabled&&string.IsNullOrWhiteSpace(s.Content))==true)items.Add("启用的自定义脚本内容");
  if(p.TaskbarIconsMode==ShellSettings.CustomXml&&string.IsNullOrWhiteSpace(p.TaskbarIconsXml))items.Add("任务栏图标 XML");
  if(p.StartTilesMode==ShellSettings.CustomXml&&string.IsNullOrWhiteSpace(p.StartTilesXml))items.Add("Windows 10 磁贴 XML");
  if(p.StartPinsMode==ShellSettings.CustomJson&&string.IsNullOrWhiteSpace(p.StartPinsJson))items.Add("Windows 11 固定应用 JSON");
  if(ColorSettings.IsCustom(p)&&!ColorSettings.TryNormalize(p.AccentColorHex,out _))items.Add("有效的 Windows 强调色（#RRGGBB）");
  if(WallpaperSettings.IsSolid(p)&&!ColorSettings.TryNormalize(p.WallpaperColorHex,out _))items.Add("有效的桌面背景颜色（#RRGGBB）");
  if(WallpaperSettings.IsScript(p)&&string.IsNullOrWhiteSpace(p.WallpaperImageScript))items.Add("桌面图片加载脚本");
  if(ImageBackgroundSettings.IsLockScript(p)&&string.IsNullOrWhiteSpace(p.LockScreenImageScript))items.Add("锁屏图片加载脚本");
  if(StickyKeySettings.ValidationError(p) is {} stickyError)items.Add(stickyError);
  if(AccountLockoutPolicy.ValidationError(p) is {} lockoutError)items.Add(lockoutError);
  return items.Count==0?"未发现空缺必填项；仍需检查配置与 XML。":string.Join("、",items);
 }
 public static string Summary(InstallPlan p)=>$"适用系统：Windows 10 / 11（由安装介质决定）\n版本与密钥：{p.EditionMode}；SKU：{p.EditionId}\n区域与时区：{(p.InteractiveLanguage?"安装时选择语言区域":"预设语言区域")}；{(p.TimeZoneMode=="由 Windows 决定"?p.TimeZoneMode:ChoiceLocalization.Get("TimeOffset",p.TimeZone,p.TimeZone))}\n账户：{p.AccountMode}；预设 {p.Users.Count} 个账户；首次自动登录：{Display(p.AutoLogon)}\n网络：{Display(p.WifiMode)}；允许无互联网安装：{Display(p.BypassNetwork)}\n隐私：{Display(p.PrivacyMode)}；隐藏许可 / 在线账户 / OEM：{Display(p.HideEula)} / {Display(p.HideOnlineAccount)} / {Display(p.HideOEMRegistration)}\n脚本：隐藏 PowerShell {Display(p.HidePowerShellWindows)}；安装隐私文件：{(p.KeepSensitiveFiles?"保留":"首次管理员登录时清理")}\n{ComputerNamingSettings.Summary(p)}\n{LanguagePreferences.Summary(p)}\n{WifiSettings.Summary(p)}\n{CustomScripts.Summary(p)}\n设备自动加密：{(p.PreventDeviceEncryption?"禁止自动启用":"遵循 Windows 默认行为")}\n{ShellSettings.Summary(p)}\n{ExplorerSettings.Summary(p)}\n{DesktopSettings.Summary(p)}\n{VisualEffectsSettings.Summary(p)}\n{ColorSettings.Summary(p)}\n{WallpaperSettings.Summary(p)}\n锁屏图片：{ImageBackgroundSettings.LockSummary(p)}\n{KeyboardLockSettings.Summary(p)}\n{StickyKeySettings.Summary(p)}\n鼠标指针精确度：{(p.DisablePointerPrecision?"关闭提高指针精确度":"遵循 Windows 默认行为")}\n{AccountLockoutPolicy.Summary(p)}\n快速启动：{(p.DisableFastStartup?"禁用":"遵循 Windows 默认行为")}\n长路径：{(p.EnableLongPaths?"启用（应用须支持）":"遵循 Windows 默认行为")}；系统声音：{(p.TurnOffSystemSounds?"关闭系统提示音方案":"遵循 Windows 默认行为")}\n{CommonSystemSettings.Summary(p)}\n{AppRemovalSettings.Summary(p)}\n{AdvancedSystemSettings.Summary(p)}\n{SystemMaintenanceSettings.Summary(p)}\n{ProcessAuditSettings.Summary(p)}\n{ProtectionSettings.Summary(p)}\n{SecurityOptions.Summary(p)}\n{JunctionSettings.Summary(p)}\n{AppControlSettings.Summary(p)}\n{ConfigurationSetSettings.Summary(p)}\n{VmGuestToolsSettings.Summary(p)}\n{AdditionalSetupSettings.Summary(p)}\n应用建议：{(p.DisableAppSuggestions?"禁用建议与自动推广（移动设备管理可能受影响）":"遵循 Windows 默认行为")}\nWindows 更新：{(p.DisableWindowsUpdate?"循环暂停 7 天（PauseWindowsUpdate 任务）":"遵循 Windows 默认行为")}\nEdge：首次引导{(p.HideEdgeFre?"跳过":"遵循默认")}；启动加速/后台运行{(p.DisableEdgeStartupBoost?"默认关闭（用户可调整）":"遵循默认")}\nXML 导入：{XmlPlanImport.Summary(p)}\n{HardwareSettings.Summary(p)}\n安装镜像：{InstallationSettings.ImageSummary(p)}\n{CompactOsSettings.Summary(p)}\n磁盘分区：{InstallationSettings.DiskSummary(p)}{(p.ImportedXml?.RequiresDiskReview==true?"；原 XML 另含磁盘配置或 Windows PE 命令，须核对完整原文":"")}\n生成前待填：{MissingInputs(p)}\n安装时仍需操作：{InteractiveSteps(p)}\n请核对镜像、SKU、驱动及目标磁盘；错误配置可能中止或要求人工处理。";
}
