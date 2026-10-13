using System.Reflection;
namespace UnattendLocal.Core;
public static class Recommendations {
 public record Difference(string Property,string Label,string Current,string Recommended);
 // Only policy fields are reset. Machine identity, locale, SKU and all credentials remain user-owned.
 private static readonly Dictionary<string,string> Fields=new(){
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
 public static void Apply(InstallPlan p,string id,string windows){var b=Baseline(id,windows);foreach(var name in Fields.Keys)Property(name).SetValue(p,Property(name).GetValue(b));p.TargetWindows=windows;p.RecommendationId=id;p.RecommendationWindows=windows;p.RecommendationVersion=1;p.ConfigurationMode="推荐配置";}
 public static void Apply(InstallPlan p,string id){var b=Baseline(id,"common",true);foreach(var name in Fields.Keys)Property(name).SetValue(p,Property(name).GetValue(b));p.DiskMode="手动选择磁盘与分区";p.FormatExistingPartition=false;p.ClearDiskConsent();p.RecommendationId=id;p.RecommendationWindows="";p.RecommendationVersion=3;p.ConfigurationMode="推荐配置";}
 // Scene selection updates only its dependent network fields. Full policy restoration remains explicit.
 public static void SelectNetworkScene(InstallPlan p,string id){
  var b=Baseline(id,"common");p.WifiMode=b.WifiMode;p.HiddenWifi=b.HiddenWifi;p.ConnectWifi=b.ConnectWifi;
  p.RecommendationId=id;p.RecommendationWindows="";p.RecommendationVersion=3;
 }
 public static bool HasBaseline(InstallPlan p)=>Known(p.RecommendationId)&&(p.RecommendationVersion is 2 or 3||(p.RecommendationVersion==1&&p.RecommendationWindows is "Windows 10" or "Windows 11"));
 private static string Display(object? value)=>value switch{bool b=>b?"开启":"关闭","隐藏无线网络选择页"=>"有线局域网（跳过 Wi-Fi 配置）","关闭快捷隐私设置"=>"全部关闭",_=>value?.ToString()??""};
 private static string DisplayField(InstallPlan p,string name)=>name switch {nameof(InstallPlan.StickyKeysMode)=>StickyKeySettings.ModeSummary(p),nameof(InstallPlan.AccountLockoutMode)=>AccountLockoutPolicy.ModeSummary(p),nameof(InstallPlan.LockKeyMode)=>KeyboardLockSettings.ModeSummary(p),nameof(InstallPlan.WallpaperMode)=>WallpaperSettings.ModeSummary(p),nameof(InstallPlan.ColorSettingsMode)=>ColorSettings.ModeSummary(p),nameof(InstallPlan.VisualEffectsMode)=>VisualEffectsSettings.ModeSummary(p),nameof(InstallPlan.DesktopIconsMode)=>DesktopSettings.IconSummary(p),nameof(InstallPlan.StartFoldersMode)=>DesktopSettings.FolderSummary(p),_=>Display(Property(name).GetValue(p))};
 public static List<Difference> Differences(InstallPlan p){
  if(!HasBaseline(p))return new();var b=Baseline(p.RecommendationId,p.RecommendationVersion>=2?"common":p.RecommendationWindows,p.RecommendationVersion>=3);
  var result=Fields.Where(f=>!Equals(Property(f.Key).GetValue(p),Property(f.Key).GetValue(b))).Select(f=>new Difference(f.Key,f.Value,DisplayField(p,f.Key),DisplayField(b,f.Key))).ToList();
  if(p.DiskMode!="手动选择磁盘与分区")result.Add(new(nameof(InstallPlan.DiskMode),"磁盘与分区",p.DiskMode,"手动选择磁盘与分区"));return result;
 }
 public static void Restore(InstallPlan p,string name){if(!HasBaseline(p))throw new ArgumentException("此方案没有可用的推荐基准。");if(name==nameof(InstallPlan.DiskMode)){p.DiskMode="手动选择磁盘与分区";p.FormatExistingPartition=false;p.ClearDiskConsent();return;}if(!Fields.ContainsKey(name))throw new ArgumentException("此项不属于推荐设置。");Property(name).SetValue(p,Property(name).GetValue(Baseline(p.RecommendationId,p.RecommendationVersion>=2?"common":p.RecommendationWindows,p.RecommendationVersion>=3)));}
 public static string State(InstallPlan p)=>p.ConfigurationMode!="推荐配置"?(p.CustomStartingPoint=="基础配置"?"自定义配置 · 从基础值开始，请逐项选择或填写":"自定义配置 · 保留当前设置，所有选项均可自主调整"):!HasBaseline(p)?"推荐基准不可用，请重新应用推荐方案":$"基于 {Name(p.RecommendationId)}推荐 · {(Differences(p).Count==0?"推荐值已应用":"已修改")}";
 public static string InteractiveSteps(InstallPlan p){
  var steps=new List<string>();
  if(p.InteractiveLanguage)steps.Add("语言与区域");
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
  if(!p.InteractiveLanguage){if(string.IsNullOrWhiteSpace(p.ImageLanguage))items.Add("显示语言");if(string.IsNullOrWhiteSpace(p.Locale))items.Add("区域格式");if(string.IsNullOrWhiteSpace(p.Keyboard))items.Add("键盘 / 输入法");if(string.IsNullOrWhiteSpace(p.GeoId))items.Add("国家或地区");}
  if(p.TimeZoneMode=="指定时区"&&string.IsNullOrWhiteSpace(p.TimeZone))items.Add("时区");
  if(p.EditionMode=="按 SKU 使用通用安装密钥"&&string.IsNullOrWhiteSpace(p.EditionId))items.Add("SKU");
  if(p.EditionMode=="指定自己的产品密钥"&&string.IsNullOrWhiteSpace(p.CustomProductKey))items.Add("安装密钥");
  if(p.UseActivationKey&&string.IsNullOrWhiteSpace(p.ActivationProductKey))items.Add("激活密钥");
  if(p.AccountMode=="自动创建本地账户"){
   if(p.Users.Count==0)items.Add("本地账户");
   else{if(p.Users.Any(u=>string.IsNullOrWhiteSpace(u.Name)))items.Add("账户名称");if(!p.Users.Any(u=>u.Administrator))items.Add("管理员账户");if(p.Users.Any(u=>!u.AllowEmptyPassword&&string.IsNullOrEmpty(u.Password)))items.Add("账户密码或明确允许空密码");}
  }
  if(p.WifiMode=="预设 WPA2 Wi-Fi"){if(string.IsNullOrWhiteSpace(p.Ssid))items.Add("Wi-Fi 名称");if(string.IsNullOrEmpty(p.WifiPassword))items.Add("Wi-Fi 密码");}
  if(p.DiskMode!=InstallationSettings.Manual&&p.TargetDiskId==null)items.Add("目标磁盘编号");
  if((p.DiskMode==InstallationSettings.Existing||p.DiskMode==DiskScriptSettings.Mode)&&p.TargetPartitionId==null)items.Add("目标分区编号");
  if(p.DiskMode==DiskScriptSettings.Mode&&string.IsNullOrWhiteSpace(p.DiskPartScript))items.Add("DiskPart 脚本");
  if(p.Scripts?.Any(s=>s.Enabled&&string.IsNullOrWhiteSpace(s.Content))==true)items.Add("启用的自定义脚本内容");
  if(p.TaskbarIconsMode==ShellSettings.CustomXml&&string.IsNullOrWhiteSpace(p.TaskbarIconsXml))items.Add("任务栏图标 XML");
  if(p.StartTilesMode==ShellSettings.CustomXml&&string.IsNullOrWhiteSpace(p.StartTilesXml))items.Add("Windows 10 磁贴 XML");
  if(p.StartPinsMode==ShellSettings.CustomJson&&string.IsNullOrWhiteSpace(p.StartPinsJson))items.Add("Windows 11 固定应用 JSON");
  if(ColorSettings.IsCustom(p)&&!ColorSettings.TryNormalize(p.AccentColorHex,out _))items.Add("有效的 Windows 强调色（#RRGGBB）");
  if(WallpaperSettings.IsSolid(p)&&!ColorSettings.TryNormalize(p.WallpaperColorHex,out _))items.Add("有效的桌面背景颜色（#RRGGBB）");
  if(StickyKeySettings.ValidationError(p) is {} stickyError)items.Add(stickyError);
  if(AccountLockoutPolicy.ValidationError(p) is {} lockoutError)items.Add(lockoutError);
  return items.Count==0?"未发现空缺必填项；仍需检查配置与 XML。":string.Join("、",items);
 }
 public static string Summary(InstallPlan p)=>$"适用系统：Windows 10 / 11（由安装介质决定）\n版本与密钥：{p.EditionMode}；SKU：{p.EditionId}\n区域与时区：{(p.InteractiveLanguage?"安装时选择语言区域":"预设语言区域")}；{(p.TimeZoneMode=="由 Windows 决定"?p.TimeZoneMode:ChoiceLocalization.Get("TimeOffset",p.TimeZone,p.TimeZone))}\n账户：{p.AccountMode}；预设 {p.Users.Count} 个账户；首次自动登录：{Display(p.AutoLogon)}\n网络：{Display(p.WifiMode)}；允许无互联网安装：{Display(p.BypassNetwork)}\n隐私：{Display(p.PrivacyMode)}；隐藏许可 / 在线账户 / OEM：{Display(p.HideEula)} / {Display(p.HideOnlineAccount)} / {Display(p.HideOEMRegistration)}\n脚本：隐藏 PowerShell {Display(p.HidePowerShellWindows)}；安装隐私文件：{(p.KeepSensitiveFiles?"保留":"首次管理员登录时清理")}\n{CustomScripts.Summary(p)}\n设备自动加密：{(p.PreventDeviceEncryption?"禁止自动启用":"遵循 Windows 默认行为")}\n{ShellSettings.Summary(p)}\n{ExplorerSettings.Summary(p)}\n{DesktopSettings.Summary(p)}\n{VisualEffectsSettings.Summary(p)}\n{ColorSettings.Summary(p)}\n{WallpaperSettings.Summary(p)}\n{KeyboardLockSettings.Summary(p)}\n{StickyKeySettings.Summary(p)}\n鼠标指针精确度：{(p.DisablePointerPrecision?"关闭提高指针精确度":"遵循 Windows 默认行为")}\n{AccountLockoutPolicy.Summary(p)}\n快速启动：{(p.DisableFastStartup?"禁用":"遵循 Windows 默认行为")}\n长路径：{(p.EnableLongPaths?"启用（应用须支持）":"遵循 Windows 默认行为")}；系统声音：{(p.TurnOffSystemSounds?"关闭系统提示音方案":"遵循 Windows 默认行为")}\n应用建议：{(p.DisableAppSuggestions?"禁用建议与自动推广（移动设备管理可能受影响）":"遵循 Windows 默认行为")}\nWindows 更新：{(p.DisableWindowsUpdate?"循环暂停 7 天（PauseWindowsUpdate 任务）":"遵循 Windows 默认行为")}\nEdge：首次引导{(p.HideEdgeFre?"跳过":"遵循默认")}；启动加速/后台运行{(p.DisableEdgeStartupBoost?"默认关闭（用户可调整）":"遵循默认")}\nXML 导入：{XmlPlanImport.Summary(p)}\n{HardwareSettings.Summary(p)}\n安装镜像：{InstallationSettings.ImageSummary(p)}\n磁盘分区：{InstallationSettings.DiskSummary(p)}{(p.ImportedXml?.RequiresDiskReview==true?"；原 XML 另含磁盘配置或 Windows PE 命令，须核对完整原文":"")}\n生成前待填：{MissingInputs(p)}\n安装时仍需操作：{InteractiveSteps(p)}\n请核对镜像、SKU、驱动及目标磁盘；错误配置可能中止或要求人工处理。";
}
