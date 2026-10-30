using System.Collections.Immutable;
using System.Text;
using System.Xml;
using System.Text.RegularExpressions;
using Schneegans.Unattend;
namespace UnattendLocal.Core;
public class GeneratorService {
 internal const string DomainCredentialPlaceholder="[域加入凭据已遮蔽]";
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
  "home"=>global::UnattendLocal.Localization.UiText.Source("text.3526a284af49"),"home_n"=>global::UnattendLocal.Localization.UiText.Source("text.f216a6a04ef4"),"home_single"=>global::UnattendLocal.Localization.UiText.Source("text.17a792eeb486"),
  "pro"=>global::UnattendLocal.Localization.UiText.Source("text.992fa5f756cc"),"pro_n"=>global::UnattendLocal.Localization.UiText.Source("text.3dafdc542848"),"pro_education"=>global::UnattendLocal.Localization.UiText.Source("text.40c0f02f152c"),"pro_education_n"=>global::UnattendLocal.Localization.UiText.Source("text.786a6590a69c"),
  "pro_workstations"=>global::UnattendLocal.Localization.UiText.Source("text.fcdc3ac6ed32"),"pro_workstations_n"=>global::UnattendLocal.Localization.UiText.Source("text.02fe99d2ba75"),
  "education"=>global::UnattendLocal.Localization.UiText.Source("text.7e33df4dcd51"),"education_n"=>global::UnattendLocal.Localization.UiText.Source("text.93f0cda66aa7"),"enterprise"=>global::UnattendLocal.Localization.UiText.Source("text.e8ff4f278b28"),"enterprise_n"=>global::UnattendLocal.Localization.UiText.Source("text.0ae9ce5bcefe"),_=>label
 };
 public byte[] GenerateStandalone(InstallPlan p)=>GenerateDocument(p,false);
 public byte[] Generate(InstallPlan p)=>GenerateDocument(p,true);
 // Import baselines must retain editable source values, including a /0 awaiting correction.
 // This internal path never writes a user export; public generation always checks deployment.
 internal byte[] GenerateImportBaseline(InstallPlan p)=>GenerateDocument(p,false,false);
 // Recognition only: never exposed through the user export entry points.
 internal byte[] GenerateLegacyPeBaseline(InstallPlan p)=>GenerateDocument(p,false,false,true);
 internal byte[] GeneratePreviousPeBaseline(InstallPlan p)=>GenerateDocument(p,false,false,false,true);
 internal byte[] GeneratePrePagingPeBaseline(InstallPlan p)=>GenerateDocument(p,false,false,false,false,true);
 private byte[] GenerateDocument(InstallPlan p,bool includeImport,bool checkDeployment=true,bool legacyTargetChain=false,bool unguardedSelector=false,bool legacyPePaging=false) {
  GeneratedPeSettings.Validate(p);
  NativePreferences.Validate(p);
  AdditionalXmlSettings.Validate(p,checkDeployment);
  if(checkDeployment)StaticIPv4Settings.ValidateDeployment(p);
  AppRemovalSettings.Validate(p);
  if(includeImport)ConfigurationValidationException.Check(nameof(InstallPlan.ImportedXml),()=>XmlPlanImport.ValidateImported(p));
  CompactOsSettings.Validate(p);
  DeploymentTuningSettings.Validate(p);
  StaticIPv4Settings.Validate(p);
  SetupDriverSettings.Validate(p);
  SetupDynamicUpdateSettings.Validate(p);
  RegisteredInformationSettings.Validate(p);
  WorkgroupSettings.Validate(p);
  PeLogSettings.Validate(p);
  InstallationSettings.Validate(p);
  ShellSettings.Validate(p);
  if(!p.InteractiveLanguage&&new[]{p.ImageLanguage,p.Locale,p.Keyboard,p.GeoId}.Any(string.IsNullOrWhiteSpace))throw new ArgumentException(global::UnattendLocal.Localization.UiText.Source("text.dd5498e8054d"));
  if(p.TimeZoneMode==global::UnattendLocal.Localization.UiText.Source("text.980f34c4425d")&&string.IsNullOrWhiteSpace(p.TimeZone))throw new ConfigurationValidationException(nameof(InstallPlan.TimeZone), global::UnattendLocal.Localization.UiText.Source("text.826962f4d5ab"));
  if(p.Architecture is not ("amd64" or "arm64" or "x86"))throw new ConfigurationValidationException(nameof(InstallPlan.Architecture), global::UnattendLocal.Localization.UiText.Source("text.ec7600c951f5"));
  IAccountSettings accounts=p.AccountMode switch {
   "安装时创建本地账户"=>new InteractiveLocalAccountSettings(),
   "安装时添加微软账户"=>new InteractiveMicrosoftAccountSettings(),
   "自动创建本地账户"=>CreateAccounts(p),
   _=>throw new ConfigurationValidationException(nameof(InstallPlan.AccountMode), global::UnattendLocal.Localization.UiText.Source("text.7258f2371efc"))
  };
  IWifiSettings wifi=WifiSettings.Build(p);
  if(p.BypassNetwork&&p.AccountMode==global::UnattendLocal.Localization.UiText.Source("text.5f78e95b6bd9"))throw new ArgumentException(global::UnattendLocal.Localization.UiText.Source("text.83e750e69158"));
  if(p.AccountMode==global::UnattendLocal.Localization.UiText.Source("text.cdaa9a6700f6")&&p.PasswordExpiration==global::UnattendLocal.Localization.UiText.Source("text.5fb01e314f6b")&&(p.PasswordDays<1||p.PasswordDays>999))throw new ConfigurationValidationException(nameof(InstallPlan.PasswordDays), global::UnattendLocal.Localization.UiText.Source("text.d3f7ce11167b"));
  if(p.AccountMode==global::UnattendLocal.Localization.UiText.Source("text.cdaa9a6700f6")&&!p.AutoLogon&&!p.KeepSensitiveFiles)throw new ConfigurationValidationException(nameof(InstallPlan.KeepSensitiveFiles), global::UnattendLocal.Localization.UiText.Source("text.548f2416d7e2"));
  var config=Configuration.Default with {
   Bloatwares=AppRemovalSettings.Build(generator,p),
   DeleteJunctions=p.DeleteJunctions,
   UseConfigurationSet=p.UseConfigurationSet,AppLockerSettings=AppControlSettings.Build(p),
   ProcessAuditSettings=ProcessAuditSettings.Build(p),
   UseNarrator=p.UseNarrator,MakeEdgeUninstallable=p.MakeEdgeUninstallable,DisableSystemRestore=p.DisableSystemRestore,
   DisableUac=p.DisableUac,DisableWpbt=p.DisableWpbt,
   DisableSac=p.DisableSac,DisableSmartScreen=p.DisableSmartScreen,DisableCoreIsolation=p.DisableCoreIsolation,HardenSystemDriveAcl=p.HardenSystemDriveAcl,
   VMwareTools=p.VMwareTools,VBoxGuestAdditions=p.VBoxGuestAdditions,VirtIoGuestTools=p.VirtIoGuestTools,ParallelsTools=p.ParallelsTools,
   EnableRemoteDesktop=p.EnableRemoteDesktop,AllowPowerShellScripts=p.AllowPowerShellScripts,
   PreventAutomaticReboot=p.PreventAutomaticReboot,DisableLastAccess=p.DisableLastAccess,
   ActivationKey=p.UseActivationKey?ConfigurationValidationException.Read(nameof(InstallPlan.ActivationProductKey),()=>ValidateProductKey(p.ActivationProductKey,global::UnattendLocal.Localization.UiText.Source("text.889ccdc9646d"))):null,
   KeepSensitiveFiles=p.KeepSensitiveFiles,
   HidePowerShellWindows=p.HidePowerShellWindows,
   PreventDeviceEncryption=p.PreventDeviceEncryption,
   DisableFastStartup=p.DisableFastStartup,
   EnableLongPaths=p.EnableLongPaths,TurnOffSystemSounds=p.TurnOffSystemSounds,
   HideEdgeFre=p.HideEdgeFre,DisableEdgeStartupBoost=p.DisableEdgeStartupBoost,
   DisableWindowsUpdate=p.DisableWindowsUpdate,
   DisableAppSuggestions=p.DisableAppSuggestions,
   DisableAutomaticRestartSignOn=p.DisableAutomaticRestartSignOn,PreventDeviceApps=p.PreventDeviceApps,DeleteWindowsOld=p.DeleteWindowsOld,
   Effects=VisualEffectsSettings.Build(p),
   ColorSettings=ColorSettings.Build(p),
   WallpaperSettings=WallpaperSettings.Build(p),
   LockScreenSettings=ImageBackgroundSettings.BuildLockScreen(p),
   LockKeySettings=KeyboardLockSettings.Build(p),
   StickyKeysSettings=StickyKeySettings.Build(p),
   DisablePointerPrecision=p.DisablePointerPrecision,
   LockoutSettings=AccountLockoutPolicy.Build(p),
   ScriptSettings=DeploymentTuningSettings.Build(p,legacyPePaging||legacyTargetChain||unguardedSelector),
   TaskbarSearch=ShellSettings.Search(p.TaskbarSearch),
   TaskbarIcons=ShellSettings.Taskbar(p),StartTilesSettings=ShellSettings.Tiles(p),StartPinsSettings=ShellSettings.Pins(p),
   DisableWidgets=p.DisableWidgets,LeftTaskbar=p.LeftTaskbar,HideTaskViewButton=p.HideTaskViewButton,
   ShowAllTrayIcons=p.ShowAllTrayIcons,DisableBingResults=p.DisableBingResults,
   HideFiles=ExplorerSettings.HiddenFiles(p.HideFilesMode),ShowFileExtensions=p.ShowFileExtensions,
   ClassicContextMenu=p.ClassicContextMenu,HideInfoTip=p.HideInfoTip,LaunchToThisPC=p.LaunchToThisPC,ShowEndTask=p.ShowEndTask,
   DesktopIcons=DesktopSettings.BuildIcons(generator,p),StartFolderSettings=DesktopSettings.BuildFolders(generator,p),DeleteEdgeDesktopIcon=p.DeleteEdgeDesktopIcon,
   AccountSettings=accounts,WifiSettings=wifi,
   ComputerNameSettings=ComputerNamingSettings.Build(p),
   LanguageSettings=LanguagePreferences.Build(generator,p),
   TimeZoneSettings=p.TimeZoneMode switch {"由 Windows 决定"=>new ImplicitTimeZoneSettings(),"指定时区"=>generator.CreateExplicitTimeZoneSettings(p.TimeZone),_=>throw new ArgumentException(global::UnattendLocal.Localization.UiText.Source("text.dc0e2226d004"))},
   PasswordExpirationSettings=p.AccountMode!=global::UnattendLocal.Localization.UiText.Source("text.cdaa9a6700f6")?new DefaultPasswordExpirationSettings():p.PasswordExpiration switch {"永不过期"=>new UnlimitedPasswordExpirationSettings(),"Windows 默认"=>new DefaultPasswordExpirationSettings(),"指定天数"=>new CustomPasswordExpirationSettings(p.PasswordDays),_=>throw new ArgumentException(global::UnattendLocal.Localization.UiText.Source("text.ef0cd5829f6b"))},
   ExpressSettings=p.PrivacyMode switch {"安装时手动选择"=>ExpressSettingsMode.Interactive,"关闭快捷隐私设置"=>ExpressSettingsMode.DisableAll,"启用快捷设置"=>ExpressSettingsMode.EnableAll,_=>throw new ArgumentException(global::UnattendLocal.Localization.UiText.Source("text.f46ca367fb9b"))},
   BypassNetworkCheck=p.BypassNetwork,
   PESettings=GeneratedPeSettings.Build(p,CreateEdition(p),legacyTargetChain,unguardedSelector,legacyPePaging),
   ProcessorArchitectures=ImmutableHashSet.Create(Enum.Parse<ProcessorArchitecture>(p.Architecture))
  };
  var doc=GeneratedPeSettings.Generate(generator,config,p,legacyTargetChain);
  var ns=new XmlNamespaceManager(doc.NameTable);ns.AddNamespace("u","urn:schemas-microsoft-com:unattend");
  foreach(XmlElement oobe in doc.SelectNodes("//u:OOBE",ns)!) {
   SetOobe(doc,ns,oobe,"HideEULAPage",p.HideEula);
   SetOobe(doc,ns,oobe,"HideOEMRegistrationScreen",p.HideOEMRegistration);
   SetOobe(doc,ns,oobe,"HideWirelessSetupInOOBE",p.WifiMode!=global::UnattendLocal.Localization.UiText.Source("text.fd17c35ec3cc"));
   SetOobe(doc,ns,oobe,"HideOnlineAccountScreens",p.AccountMode==global::UnattendLocal.Localization.UiText.Source("text.5f78e95b6bd9")?false:p.HideOnlineAccount);
  }
  if(!p.UseGeneratedPe)HardwareSettings.Apply(doc,ns,p);
  LegacyUpgradeSettings.Apply(doc,p);
  if(!p.UseGeneratedPe)InstallationSettings.Apply(doc,ns,p);
  CompactOsSettings.Apply(doc,ns,p);
  StaticIPv4Settings.Apply(doc,ns,p);
  SetupDriverSettings.Apply(doc,ns,p);
  SetupDynamicUpdateSettings.Apply(doc,ns,p);
  RegisteredInformationSettings.Apply(doc,p);
  WorkgroupSettings.Apply(doc,p);
  PeLogSettings.Apply(doc,p);
  NativePreferences.Apply(doc,p);
  AdditionalXmlSettings.Apply(doc,p);
  if(includeImport&&p.ImportedXml is {} imported){FirstLogonSettings.ValidateGenerated(p,doc);doc=AppRemovalSettings.MergeImported(p,doc);WorkgroupSettings.FinishImported(doc,p);NativePreferences.FinishImported(doc,p);}
  using var schema=typeof(UnattendGenerator).Assembly.GetManifestResourceStream("Schneegans.Unattend.resource.autounattend.xsd")!;
  doc.Schemas.Add("urn:schemas-microsoft-com:unattend",XmlReader.Create(schema));
  doc.Validate((_,e)=>throw new ArgumentException(global::UnattendLocal.Localization.UiText.Concat(new object?[] { global::UnattendLocal.Localization.UiText.Source("text.f0a035a733c2"), e.Message },new bool[]{false,false})));
  if(checkDeployment)StaticIPv4Settings.ValidateDeployment(doc);
  if(includeImport)XmlPlanImport.AddSnapshot(doc,p);
  return UnattendGenerator.Serialize(doc);
 }
 public static string Preview(byte[] bytes,InstallPlan? plan=null,bool english=false) {
  string Label(string key)=>english?global::UnattendLocal.Localization.UiText.EnglishText(global::UnattendLocal.Localization.UiText.Source(key)):global::UnattendLocal.Localization.UiText.Source(key);
  var doc=new XmlDocument();doc.LoadXml(Encoding.UTF8.GetString(bytes));
  if(plan!=null)AdditionalXmlSettings.RedactPreview(doc,plan,english);
  foreach(XmlComment comment in doc.SelectNodes("//comment()")!.Cast<XmlComment>().ToArray())comment.ParentNode!.RemoveChild(comment);
  var ns=new XmlNamespaceManager(doc.NameTable);ns.AddNamespace("u","urn:schemas-microsoft-com:unattend");
  foreach(XmlNode n in doc.SelectNodes("//u:Password/u:Value | //u:AdministratorPassword/u:Value | //u:Credentials/u:Password",ns)!)n.InnerText=Label("text.a87e466de86e");
  foreach(XmlNode n in doc.SelectNodes("//u:component[@name='Microsoft-Windows-PnpCustomizationsWinPE']//u:Credentials/u:Password",ns)!)n.InnerText=Label("text.ce9d3225eb28");
  foreach(XmlNode n in doc.SelectNodes("//u:component[@name='Microsoft-Windows-UnattendedJoin']//u:Credentials/u:Password | //u:component[@name='Microsoft-Windows-UnattendedJoin']//u:MachinePassword | //u:component[@name='Microsoft-Windows-UnattendedJoin']//u:Provisioning/u:AccountData",ns)!)n.InnerText=Label("text.b8de0a664760");
  foreach(XmlNode n in doc.SelectNodes("//u:ProductKey/u:Key | //u:component[@name='Microsoft-Windows-Shell-Setup']/u:ProductKey",ns)!)n.InnerText=Label("text.abf82a99491a");
  foreach(XmlNode n in doc.SelectNodes("//*[local-name()='File']",ns)!)
   if(n.Attributes?["path"]?.Value.EndsWith("Wifi.xml",StringComparison.OrdinalIgnoreCase)==true)
    n.InnerText=Label("text.838cc513a5e2");
   else if(n.Attributes?["path"]?.Value.EndsWith("AppLockerPolicy.xml",StringComparison.OrdinalIgnoreCase)==true)
    n.InnerText=Label("text.c67ed423c4cf");
  using var sw=new StringWriter();using(var w=XmlWriter.Create(sw,new XmlWriterSettings{Indent=true,OmitXmlDeclaration=true}))doc.Save(w);
  return Label("text.991932c052b6").TrimEnd() + "\n" + sw.ToString();
 }
 private IEditionSettings CreateEdition(InstallPlan p)=>p.EditionMode switch {
  "安装时输入密钥"=>new InteractiveEditionSettings(),
  "按 SKU 使用通用安装密钥"=>new UnattendedEditionSettings(generator.Lookup<WindowsEdition>(!string.IsNullOrWhiteSpace(p.EditionId)?p.EditionId:throw new ConfigurationValidationException(nameof(InstallPlan.EditionId), global::UnattendLocal.Localization.UiText.Source("text.26bd7a690ba8")))),
  "指定自己的产品密钥"=>new CustomEditionSettings(ConfigurationValidationException.Read(nameof(InstallPlan.CustomProductKey),()=>ValidateProductKey(p.CustomProductKey,global::UnattendLocal.Localization.UiText.Source("text.fecd385fb644")))),
  "使用 BIOS/UEFI 固件密钥"=>new FirmwareEditionSettings(),
  _=>throw new ConfigurationValidationException(nameof(InstallPlan.EditionMode), global::UnattendLocal.Localization.UiText.Source("text.7f9ef38f9bf2"))
 };
 private static ProductKey ValidateProductKey(string? value,string label) {
  var key=value?.Trim().ToUpperInvariant()??"";
  if(!Regex.IsMatch(key,@"\A[A-Z0-9]{5}(?:-[A-Z0-9]{5}){4}\z"))throw new ArgumentException(global::UnattendLocal.Localization.UiText.Format("message.2351af24006b", new string[] { global::UnattendLocal.Localization.UiText.Arg((label), 0, null) }));
  return new ProductKey(key);
 }
 private static void SetOobe(XmlDocument doc,XmlNamespaceManager ns,XmlElement parent,string name,bool value) {var node=parent.SelectSingleNode("u:"+name,ns) as XmlElement;if(node==null){node=doc.CreateElement(name,"urn:schemas-microsoft-com:unattend");parent.AppendChild(node);}node.InnerText=value?"true":"false";}
 private static IAccountSettings CreateAccounts(InstallPlan p) {
  FirstLogonSettings.Validate(p);
  if(p.Users.Count==0&&!FirstLogonSettings.IsBuiltin(p))throw new ConfigurationValidationException(nameof(InstallPlan.Users), global::UnattendLocal.Localization.UiText.Source("text.1205a4d8c79e"));
  for(int i=0;i<p.Users.Count;i++) {
   var u=p.Users[i];
   if(string.IsNullOrWhiteSpace(u.Name))throw new ConfigurationValidationException("Users.Name", global::UnattendLocal.Localization.UiText.Source("text.7721c8d84613"), userIndex:i);
   if(u.Name.Any(char.IsControl))throw new ConfigurationValidationException("Users.Name", global::UnattendLocal.Localization.UiText.Source("text.a5f5cfc58972"), userIndex:i);
   if(u.NeedsPassword&&!(u.AllowEmptyPassword&&string.IsNullOrEmpty(u.Password)))throw new ConfigurationValidationException("Users.Password", global::UnattendLocal.Localization.UiText.Format("message.281ae7fd2c2b", new string[] { global::UnattendLocal.Localization.UiText.Arg((u.Name), 0, null) }), userIndex:i);
   if(string.IsNullOrEmpty(u.Password)&&!u.AllowEmptyPassword)throw new ConfigurationValidationException("Users.Password", global::UnattendLocal.Localization.UiText.Format("message.d0e561967ca1", new string[] { global::UnattendLocal.Localization.UiText.Arg((u.Name), 0, null) }), userIndex:i);
   if(u.AllowEmptyPassword&&!string.IsNullOrEmpty(u.Password))throw new ConfigurationValidationException("Users.Password", global::UnattendLocal.Localization.UiText.Format("message.38acde427f2b", new string[] { global::UnattendLocal.Localization.UiText.Arg((u.Name), 0, null) }), userIndex:i);
  }
  return new UnattendedAccountSettings(p.Users.Select(u=>new Account(u.Name,u.DisplayName,u.Password,u.Administrator?Constants.AdministratorsGroup:Constants.UsersGroup)).ToImmutableList(),FirstLogonSettings.Build(p),p.ObscurePasswords);
 }
}
