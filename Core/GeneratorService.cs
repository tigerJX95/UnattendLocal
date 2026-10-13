using System.Collections.Immutable;
using System.Text;
using System.Xml;
using System.Text.RegularExpressions;
using Schneegans.Unattend;
namespace UnattendLocal.Core;
public class GeneratorService {
 private readonly UnattendGenerator generator=new();
 public record Choice(string Id,string Label);
 public List<Choice> Languages=>generator.ImageLanguages.Values.Select(x=>new Choice(x.Id,ChoiceLocalization.Get("ImageLanguage",x.Id,x.DisplayName))).ToList();
 public List<Choice> Locales=>generator.UserLocales.Values.Select(x=>new Choice(x.Id,ChoiceLocalization.Get("UserLocale",x.Id,x.DisplayName))).ToList();
 public List<Choice> Keyboards=>generator.KeyboardIdentifiers.Values.Select(x=>new Choice(x.Id,ChoiceLocalization.Get("KeyboardIdentifier",x.Id,x.DisplayName))).ToList();
 public List<Choice> Regions=>generator.GeoLocations.Values.Select(x=>new Choice(x.Id,ChoiceLocalization.Get("GeoId",x.Id,x.DisplayName))).ToList();
 public List<Choice> TimeZones=>generator.TimeOffsets.Values.Select(x=>new Choice(x.Id,ChoiceLocalization.Get("TimeOffset",x.Id,x.DisplayName))).ToList();
 public List<Choice> Editions=>generator.WindowsEditions.Values.Where(x=>x.Visible).Select(x=>new Choice(x.Id,EditionLabel(x.Id,x.DisplayName))).OrderBy(x=>x.Id).ToList();
 public string GenericInstallationKey(string id)=>id!=null&&generator.WindowsEditions.TryGetValue(id,out var edition)?edition.ProductKey:"";
 private static string EditionLabel(string id,string label)=>id switch {
  "home"=>"家庭版（Home）","home_n"=>"家庭版 N（Home N）","home_single"=>"家庭单语言版（Home Single Language）",
  "pro"=>"专业版（Pro）","pro_n"=>"专业版 N（Pro N）","pro_education"=>"专业教育版（Pro Education）","pro_education_n"=>"专业教育版 N（Pro Education N）",
  "pro_workstations"=>"专业工作站版（Pro for Workstations）","pro_workstations_n"=>"专业工作站版 N（Pro N for Workstations）",
  "education"=>"教育版（Education）","education_n"=>"教育版 N（Education N）","enterprise"=>"企业版（Enterprise）","enterprise_n"=>"企业版 N（Enterprise N）",_=>label
 };
 public byte[] Generate(InstallPlan p) {
  InstallationSettings.Validate(p);
  ShellSettings.Validate(p);
  if(!p.InteractiveLanguage&&new[]{p.ImageLanguage,p.Locale,p.Keyboard,p.GeoId}.Any(string.IsNullOrWhiteSpace))throw new ArgumentException("请在基础设置中填写显示语言、区域格式、键盘和国家地区，或选择安装时手动设置语言与区域。");
  if(p.TimeZoneMode=="指定时区"&&string.IsNullOrWhiteSpace(p.TimeZone))throw new ArgumentException("请选择要指定的时区，或改为由 Windows 决定。");
  if(p.Architecture is not ("amd64" or "arm64" or "x86"))throw new ArgumentException("请选择有效架构。");
  IAccountSettings accounts=p.AccountMode switch {
   "安装时创建本地账户"=>new InteractiveLocalAccountSettings(),
   "安装时添加微软账户"=>new InteractiveMicrosoftAccountSettings(),
   "自动创建本地账户"=>CreateAccounts(p),
   _=>throw new ArgumentException("请在用户与密码页明确选择账户创建方式。")
  };
  IWifiSettings wifi=p.WifiMode switch {
   "安装时手动选择"=>new InteractiveWifiSettings(),
   "隐藏无线网络选择页"=>new SkipWifiSettings(),
   "预设 WPA2 Wi-Fi"=>CreateWifi(p),
   _=>throw new ArgumentException("网络模式无效。")
  };
  if(p.BypassNetwork&&p.AccountMode=="安装时添加微软账户")throw new ArgumentException("微软账户模式需要联网，请关闭允许离线安装。");
  if(p.AccountMode=="自动创建本地账户"&&p.PasswordExpiration=="指定天数"&&(p.PasswordDays<1||p.PasswordDays>999))throw new ArgumentException("密码到期天数应为 1–999。");
  if(p.AccountMode=="自动创建本地账户"&&!p.AutoLogon&&!p.KeepSensitiveFiles)throw new ArgumentException("关闭首次自动登录时，无法自动清理安装隐私文件。请启用首次自动登录，或在 OOBE 页的“安装脚本与隐私文件”中明确勾选“保留安装隐私文件”。");
  var config=Configuration.Default with {
   ActivationKey=p.UseActivationKey?ValidateProductKey(p.ActivationProductKey,"单独指定的激活密钥"):null,
   KeepSensitiveFiles=p.KeepSensitiveFiles,
   HidePowerShellWindows=p.HidePowerShellWindows,
   PreventDeviceEncryption=p.PreventDeviceEncryption,
   DisableFastStartup=p.DisableFastStartup,
   EnableLongPaths=p.EnableLongPaths,TurnOffSystemSounds=p.TurnOffSystemSounds,
   HideEdgeFre=p.HideEdgeFre,DisableEdgeStartupBoost=p.DisableEdgeStartupBoost,
   DisableWindowsUpdate=p.DisableWindowsUpdate,
   Effects=VisualEffectsSettings.Build(p),
   ColorSettings=ColorSettings.Build(p),
   WallpaperSettings=WallpaperSettings.Build(p),
   LockKeySettings=KeyboardLockSettings.Build(p),
   LockoutSettings=AccountLockoutPolicy.Build(p),
   ScriptSettings=CustomScripts.Build(p),
   TaskbarSearch=ShellSettings.Search(p.TaskbarSearch),
   TaskbarIcons=ShellSettings.Taskbar(p),StartTilesSettings=ShellSettings.Tiles(p),StartPinsSettings=ShellSettings.Pins(p),
   DisableWidgets=p.DisableWidgets,LeftTaskbar=p.LeftTaskbar,HideTaskViewButton=p.HideTaskViewButton,
   ShowAllTrayIcons=p.ShowAllTrayIcons,DisableBingResults=p.DisableBingResults,
   HideFiles=ExplorerSettings.HiddenFiles(p.HideFilesMode),ShowFileExtensions=p.ShowFileExtensions,
   ClassicContextMenu=p.ClassicContextMenu,HideInfoTip=p.HideInfoTip,LaunchToThisPC=p.LaunchToThisPC,ShowEndTask=p.ShowEndTask,
   DesktopIcons=DesktopSettings.BuildIcons(generator,p),StartFolderSettings=DesktopSettings.BuildFolders(generator,p),DeleteEdgeDesktopIcon=p.DeleteEdgeDesktopIcon,
   AccountSettings=accounts,WifiSettings=wifi,
   ComputerNameSettings=string.IsNullOrEmpty(p.ComputerName)?new RandomComputerNameSettings():new CustomComputerNameSettings(p.ComputerName),
   LanguageSettings=p.InteractiveLanguage?new InteractiveLanguageSettings():new UnattendedLanguageSettings(generator.Lookup<ImageLanguage>(p.ImageLanguage),new LocaleAndKeyboard(generator.Lookup<UserLocale>(p.Locale),generator.Lookup<KeyboardIdentifier>(p.Keyboard)),null,null,generator.Lookup<GeoLocation>(p.GeoId)),
   TimeZoneSettings=p.TimeZoneMode switch {"由 Windows 决定"=>new ImplicitTimeZoneSettings(),"指定时区"=>generator.CreateExplicitTimeZoneSettings(p.TimeZone),_=>throw new ArgumentException("请选择有效的时区设置方式。")},
   PasswordExpirationSettings=p.AccountMode!="自动创建本地账户"?new DefaultPasswordExpirationSettings():p.PasswordExpiration switch {"永不过期"=>new UnlimitedPasswordExpirationSettings(),"Windows 默认"=>new DefaultPasswordExpirationSettings(),"指定天数"=>new CustomPasswordExpirationSettings(p.PasswordDays),_=>throw new ArgumentException("密码策略无效。")},
   ExpressSettings=p.PrivacyMode switch {"安装时手动选择"=>ExpressSettingsMode.Interactive,"关闭快捷隐私设置"=>ExpressSettingsMode.DisableAll,"启用快捷设置"=>ExpressSettingsMode.EnableAll,_=>throw new ArgumentException("隐私设置模式无效。")},
   BypassNetworkCheck=p.BypassNetwork,
   PESettings=new DefaultPESettings(CreateEdition(p),HardwareSettings.AllEnabled(p)),
   ProcessorArchitectures=ImmutableHashSet.Create(Enum.Parse<ProcessorArchitecture>(p.Architecture))
  };
  var doc=generator.GenerateXml(config);
  var ns=new XmlNamespaceManager(doc.NameTable);ns.AddNamespace("u","urn:schemas-microsoft-com:unattend");
  foreach(XmlElement oobe in doc.SelectNodes("//u:OOBE",ns)!) {
   SetOobe(doc,ns,oobe,"HideEULAPage",p.HideEula);
   SetOobe(doc,ns,oobe,"HideOEMRegistrationScreen",p.HideOEMRegistration);
   SetOobe(doc,ns,oobe,"HideWirelessSetupInOOBE",p.WifiMode!="安装时手动选择");
   SetOobe(doc,ns,oobe,"HideOnlineAccountScreens",p.AccountMode=="安装时添加微软账户"?false:p.HideOnlineAccount);
  }
  HardwareSettings.Apply(doc,ns,p);
  InstallationSettings.Apply(doc,ns,p);
  using var schema=typeof(UnattendGenerator).Assembly.GetManifestResourceStream("Schneegans.Unattend.resource.autounattend.xsd")!;
  doc.Schemas.Add("urn:schemas-microsoft-com:unattend",XmlReader.Create(schema));
  doc.Validate((_,e)=>throw new ArgumentException("XML 结构验证失败："+e.Message));
  return UnattendGenerator.Serialize(doc);
 }
 public static string Preview(byte[] bytes) {
  var doc=new XmlDocument();doc.LoadXml(Encoding.UTF8.GetString(bytes));
  var ns=new XmlNamespaceManager(doc.NameTable);ns.AddNamespace("u","urn:schemas-microsoft-com:unattend");
  foreach(XmlNode n in doc.SelectNodes("//u:Password/u:Value",ns)!)n.InnerText="[密码已遮蔽]";
  foreach(XmlNode n in doc.SelectNodes("//u:ProductKey/u:Key | //u:component[@name='Microsoft-Windows-Shell-Setup']/u:ProductKey",ns)!)n.InnerText="[产品密钥已遮蔽]";
  foreach(XmlNode n in doc.SelectNodes("//*[local-name()='File']",ns)!)
   if(n.Attributes?["path"]?.Value.EndsWith("Wifi.xml",StringComparison.OrdinalIgnoreCase)==true)
    n.InnerText=Regex.Replace(n.InnerText,@"(<keyMaterial>).*?(</keyMaterial>)","$1[密码已遮蔽]$2",RegexOptions.Singleline);
  using var sw=new StringWriter();using(var w=XmlWriter.Create(sw,new XmlWriterSettings{Indent=true,OmitXmlDeclaration=true}))doc.Save(w);
  return "预览（密码和产品密钥已遮蔽，不能用于安装）\n"+sw.ToString();
 }
 private IEditionSettings CreateEdition(InstallPlan p)=>p.EditionMode switch {
  "安装时输入密钥"=>new InteractiveEditionSettings(),
  "按 SKU 使用通用安装密钥"=>new UnattendedEditionSettings(generator.Lookup<WindowsEdition>(!string.IsNullOrWhiteSpace(p.EditionId)?p.EditionId:throw new ArgumentException("请在版本与密钥页选择要安装的 SKU。"))),
  "指定自己的产品密钥"=>new CustomEditionSettings(ValidateProductKey(p.CustomProductKey,"安装产品密钥")),
  "使用 BIOS/UEFI 固件密钥"=>new FirmwareEditionSettings(),
  _=>throw new ArgumentException("请选择有效的安装版本与密钥方式。")
 };
 private static ProductKey ValidateProductKey(string? value,string label) {
  var key=value?.Trim().ToUpperInvariant()??"";
  if(!Regex.IsMatch(key,@"\A[A-Z0-9]{5}(?:-[A-Z0-9]{5}){4}\z"))throw new ArgumentException($"{label}应为五组、每组五位的字母或数字，用短横线分隔。请重新输入；格式检查不验证密钥授权或 SKU 匹配。");
  return new ProductKey(key);
 }
 private static void SetOobe(XmlDocument doc,XmlNamespaceManager ns,XmlElement parent,string name,bool value) {var node=parent.SelectSingleNode("u:"+name,ns) as XmlElement;if(node==null){node=doc.CreateElement(name,"urn:schemas-microsoft-com:unattend");parent.AppendChild(node);}node.InnerText=value?"true":"false";}
 private static IAccountSettings CreateAccounts(InstallPlan p) {
  if(p.Users.Count==0)throw new ArgumentException("请至少添加一个本地账户。");
  foreach(var u in p.Users){if(string.IsNullOrWhiteSpace(u.Name))throw new ArgumentException("请填写每个本地账户的名称。");if(u.NeedsPassword&&!(u.AllowEmptyPassword&&string.IsNullOrEmpty(u.Password)))throw new ArgumentException($"请重新输入账户 {u.Name} 的密码，或明确允许空密码。");if(string.IsNullOrEmpty(u.Password)&&!u.AllowEmptyPassword)throw new ArgumentException($"账户 {u.Name} 未设置密码；如需空密码，请明确勾选允许空密码。");if(u.AllowEmptyPassword&&!string.IsNullOrEmpty(u.Password))throw new ArgumentException($"账户 {u.Name} 同时设置了密码和允许空密码，请选择一种。");}
  return new UnattendedAccountSettings(p.Users.Select(u=>new Account(u.Name,u.DisplayName,u.Password,u.Administrator?Constants.AdministratorsGroup:Constants.UsersGroup)).ToImmutableList(),p.AutoLogon?new OwnAutoLogonSettings():new NoneAutoLogonSettings(),p.ObscurePasswords);
 }
 private static IWifiSettings CreateWifi(InstallPlan p) {
  if(p.NeedsWifiPassword)throw new ArgumentException("请重新输入 Wi-Fi 密码。");
  var len=Encoding.UTF8.GetByteCount(p.Ssid);
  if(len<1||len>32||p.Ssid.Any(char.IsControl)||p.Ssid.Contains('"')||p.Ssid.Contains('$')||p.Ssid.Contains((char)96))throw new ArgumentException("Wi-Fi 名称应为 1–32 字节，且不能包含控制字符、双引号、美元符号或反引号。");
  if(p.WifiPassword.Length<8||p.WifiPassword.Length>63||p.WifiPassword.Any(c=>c<32||c>126))throw new ArgumentException("WPA2 密码应为 8–63 位可打印 ASCII 字符。");
  return new ParameterizedWifiSettings(p.Ssid,p.WifiPassword,p.ConnectWifi,WifiAuthentications.WPA2PSK,p.HiddenWifi);
 }
}
