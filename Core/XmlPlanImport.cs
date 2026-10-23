using System.Collections.ObjectModel;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Xml;
using Schneegans.Unattend;
namespace UnattendLocal.Core;

public static class XmlPlanImport {
 private const string Marker="UnattendLocal:v1 ";
 private static Match Match(string s,string pattern)=>Regex.Match(s,pattern,RegexOptions.IgnoreCase|RegexOptions.Singleline,TimeSpan.FromSeconds(1));
 private static string? Text(XmlNode node,string path,XmlNamespaceManager ns)=>node.SelectSingleNode(path,ns)?.InnerText;
 private static bool IsTrue(string? text)=>text is "true" or "1";
 public static InstallPlan Import(string xml,GeneratorService service){
  var original=XmlMerge.Read(xml);var doc=XmlImportPaths.Inspection(original);var ns=new XmlNamespaceManager(doc.NameTable);ns.AddNamespace("u",XmlMerge.Namespace);
  if(doc.SelectNodes("//u:component[@name='Microsoft-Windows-UnattendedJoin']//u:Credentials/u:Password | //u:component[@name='Microsoft-Windows-UnattendedJoin']//u:MachinePassword | //u:component[@name='Microsoft-Windows-UnattendedJoin']//u:Provisioning/u:AccountData",ns)!.Cast<XmlElement>().Any(n=>n.InnerText==GeneratorService.DomainCredentialPlaceholder))throw new ArgumentException("此 XML 是遮蔽后的域加入预览，请导入实际导出的原始 XML。");
  var arches=doc.SelectNodes("//u:component/@processorArchitecture",ns)!.Cast<XmlAttribute>().Select(a=>a.Value).Distinct().ToArray();
  if(arches.Length>1)throw new ArgumentException("此 XML 含多个安装架构，当前界面仅支持单一架构；请先在原工具中导出单一架构文件。");
  if(arches.Length==1&&arches[0] is not ("amd64" or "arm64" or "x86"))throw new ArgumentException("此 XML 使用了尚不支持的安装架构。");
  var notes=new List<string>();var blockers=new List<string>();
  var p=ReadSnapshot(original,notes)??new InstallPlan();
  if(p.ImportedXml!=null)p.ImportedXml=null;
  bool snapshot=doc.SelectNodes("//comment()")!.Cast<XmlComment>().Any(c=>c.Value?.StartsWith(Marker)==true)&&notes.Count==0;
  if(!snapshot){
   ReadForm(doc,p,notes);ReadBasic(doc,ns,p,service,notes);ReadGeneratedSettings(original,ns,p,notes);
  }
  // Recheck native flags even when an older reversible note is present.
  BooleanScriptSettings.ReadIntoPlan(original,p,notes);
  // Credentials come from actual XML, never from form comments or the reversible settings note.
  ReadCredentials(doc,ns,p,notes);
  var firstLogonState=FirstLogonSettings.ReadIntoPlan(doc,p,notes);
  ComputerNamingSettings.ReadIntoPlan(doc,p,notes);
  LanguagePreferences.ReadIntoPlan(doc,p,notes);
  WifiSettings.ReadIntoPlan(doc,p,notes);
  CommonSystemSettings.ReadIntoPlan(doc,p,notes);
  ImageBackgroundSettings.ReadIntoPlan(doc,p,notes);
  var appRemovalState=AppRemovalSettings.ReadIntoPlan(doc,p,notes);
  AdvancedSystemSettings.ReadIntoPlan(doc,p,notes);
  SystemMaintenanceSettings.ReadIntoPlan(doc,p,notes);
  ProcessAuditSettings.ReadIntoPlan(doc,p,notes);
  ProtectionSettings.ReadIntoPlan(doc,p,notes);
  LegacyUpgradeSettings.ReadIntoPlan(doc,p,notes);
  VmGuestToolsSettings.ReadIntoPlan(doc,p,notes);
  AdditionalSetupSettings.ReadIntoPlan(doc,p,notes);
  SecurityOptions.ReadIntoPlan(doc,p,notes);
  JunctionSettings.ReadIntoPlan(doc,p,notes);
  AppControlSettings.ReadIntoPlan(doc,p,notes);
  ConfigurationSetSettings.ReadIntoPlan(doc,p,notes);
  var partitionState=AutoPartitionSettings.ReadIntoPlan(doc,p,notes);
  var compactState=CompactOsSettings.ReadIntoPlan(doc,p,notes);
  var networkState=StaticIPv4Settings.ReadIntoPlan(doc,p,notes);
  var driverState=SetupDriverSettings.ReadIntoPlan(doc,p,notes);
  var dynamicUpdateState=SetupDynamicUpdateSettings.ReadIntoPlan(doc,p,notes);
  var registeredInformationState=RegisteredInformationSettings.ReadIntoPlan(doc,p,notes);
  var workgroupState=WorkgroupSettings.ReadIntoPlan(doc,p,notes);
  var peLogState=PeLogSettings.ReadIntoPlan(doc,p,notes);
  var nativePreferenceStates=NativePreferences.ReadIntoPlan(doc,p,notes);
  p.Architecture=arches.FirstOrDefault()??"amd64";
  p.ConfigurationMode="自定义配置";p.CustomStartingPoint="保留当前设置";p.RecommendationId="";p.RecommendationVersion=0;p.RecommendationWindows="";
  p.ClearDiskConsent();
  var scripts=doc.SelectNodes("//*[local-name()='File']")!.Cast<XmlElement>();
  if(HasRemovedScript(doc)||p.Scripts.Any(s=>s.IsRemovedType))
   blockers.Add("检测到已移除支持的 JScript（.js）；请在原 XML 中移除或手动改写后重新导入，不能通过原文保留恢复启用。");
  bool disk=NeedsDiskReview(doc);
  // Generate an editable baseline without granting the user's risk consent.
  var clone=PlanStore.Deserialize(PlanStore.Serialize(p,true));clone.ImportedXml=null;
  clone.ConfirmWipeDisk=true;clone.ConfirmFormatPartition=true;clone.ConfirmDiskScript=true;
  string baseline="";
  try{baseline=Encoding.UTF8.GetString(service.GenerateImportBaseline(clone));var seed=XmlMerge.Read(baseline);NativePreferences.SeedBaseline(seed,doc,p);baseline=seed.OuterXml;}
  catch(Exception e)when(e is ArgumentException or ConfigurationException or XmlException){
   blockers.Add("部分设置无法建立可靠的编辑基准，暂不能再导出。请先使用原工具调整后重新导入；原因："+SafeReason(e.Message));
  }
  XmlImportParameters.Reconcile(doc,p,appRemovalState,notes);
  if(baseline.Length>0)DescribeRemainder(doc,XmlMerge.Read(baseline),notes);
  p.ImportedXml=new ImportedXml{AppRemovalImportVersion=2,AppRemovalReadOnly=appRemovalState.Ambiguous,SourceXml=original.OuterXml,BaselineXml=baseline,Notices=notes.Distinct().ToArray(),Blockers=blockers.Distinct().ToArray(),RequiresDiskReview=disk};
  p.ImportedXml.LegacyUpgradeReadOnly=LegacyUpgradeSettings.Inspect(doc).Ambiguous;
  p.ImportedXml.AutoPartitionReadOnly=partitionState.Mentioned&&!partitionState.Known;
  CompactOsSettings.UpdateImportState(p.ImportedXml,compactState);
  StaticIPv4Settings.UpdateImportState(p.ImportedXml,networkState);
  SetupDriverSettings.UpdateImportState(p.ImportedXml,driverState);
  SetupDynamicUpdateSettings.UpdateImportState(p.ImportedXml,dynamicUpdateState);
  RegisteredInformationSettings.UpdateImportState(p.ImportedXml,registeredInformationState);
  WorkgroupSettings.UpdateImportState(p.ImportedXml,workgroupState);
  PeLogSettings.UpdateImportState(p.ImportedXml,peLogState);
  NativePreferences.UpdateImportState(p.ImportedXml,nativePreferenceStates);
  FirstLogonSettings.UpdateImportState(p.ImportedXml,p,firstLogonState);
  p.ClearDiskConsent();return p;
 }
 private static string SafeReason(string message)=>message.Contains("'")?"原有模式或参数暂不受支持，请核对设置。":message;
 private static void ReadBasic(XmlDocument doc,XmlNamespaceManager ns,InstallPlan p,GeneratorService service,List<string> notes){
  var computer=Text(doc,"//u:settings[@pass='specialize']/u:component[@name='Microsoft-Windows-Shell-Setup']/u:ComputerName",ns);p.ComputerName=computer is null or "*"?"":computer;
  var language=Text(doc,"//u:component[@name='Microsoft-Windows-International-Core']/u:UILanguage",ns);p.InteractiveLanguage=language==null;
  if(language!=null){
   p.ImageLanguage=language;p.Locale=Text(doc,"//u:component[@name='Microsoft-Windows-International-Core']/u:UserLocale",ns)??"";
   string input=Text(doc,"//u:component[@name='Microsoft-Windows-International-Core']/u:InputLocale",ns)??"";var first=input.Split(';')[0];
   p.Keyboard=service.Keyboards.Any(k=>k.Id==first)?first:first.Contains(':')?first[(first.IndexOf(':')+1)..]:first;
   var generator=new UnattendGenerator();p.GeoId=generator.UserLocales.TryGetValue(p.Locale,out var locale)?locale.GeoLocation?.Id??"45":"45";
   var geo=Match(doc.InnerText,@"Set-WinHomeLocation -GeoId (\d+)");if(geo.Success)p.GeoId=geo.Groups[1].Value;
  }
  var timezone=Text(doc,"//u:component[@name='Microsoft-Windows-Shell-Setup']/u:TimeZone",ns);p.TimeZoneMode=timezone==null?"由 Windows 决定":"指定时区";p.TimeZone=timezone??"";
  p.HideEula=IsTrue(Text(doc,"//u:OOBE/u:HideEULAPage",ns));p.HideOEMRegistration=IsTrue(Text(doc,"//u:OOBE/u:HideOEMRegistrationScreen",ns));p.HideOnlineAccount=IsTrue(Text(doc,"//u:OOBE/u:HideOnlineAccountScreens",ns));
  p.AccountMode=doc.SelectNodes("//u:LocalAccount",ns)!.Count>0?"自动创建本地账户":p.HideOnlineAccount?"安装时创建本地账户":"安装时添加微软账户";
  p.AutoLogon=IsTrue(Text(doc,"//u:AutoLogon/u:Enabled",ns));
  p.PrivacyMode=Text(doc,"//u:OOBE/u:ProtectYourPC",ns) switch {"3"=>"关闭快捷隐私设置","1"=>"启用快捷设置",_=>"安装时手动选择"};
  var key=Text(doc,"//u:UserData/u:ProductKey/u:Key",ns);var show=Text(doc,"//u:UserData/u:ProductKey/u:WillShowUI",ns);
  if(key==null)p.EditionMode=show=="Never"?"使用 BIOS/UEFI 固件密钥":"安装时输入密钥";
  else{
   var edition=service.Editions.FirstOrDefault(e=>service.GenericInstallationKey(e.Id)==key);
   p.EditionMode=edition==null?"指定自己的产品密钥":"按 SKU 使用通用安装密钥";if(edition!=null)p.EditionId=edition.Id;p.CustomProductKey=edition==null?key:"";
  }
  var activation=Text(doc,"//u:component[@name='Microsoft-Windows-Shell-Setup']/u:ProductKey",ns);p.UseActivationKey=activation!=null&&activation!=key;p.ActivationProductKey=p.UseActivationKey?activation!:"";
  p.WifiMode=IsTrue(Text(doc,"//u:OOBE/u:HideWirelessSetupInOOBE",ns))?"隐藏无线网络选择页":"安装时手动选择";
  p.KeepSensitiveFiles=!doc.InnerText.Contains("unattend-original.xml");p.HidePowerShellWindows=Match(doc.InnerText,@"-WindowStyle ['""]?Hidden").Success;
  p.BypassNetwork=doc.InnerText.Contains("BypassNRO",StringComparison.OrdinalIgnoreCase);
  foreach(var item in HardwareSettings.Items)typeof(InstallPlan).GetProperty(item.Property)!.SetValue(p,doc.InnerText.Contains(item.RegistryValue));
  p.BypassHardware=false;
  var metadata=doc.SelectSingleNode("//u:ImageInstall/u:OSImage/u:InstallFrom/u:MetaData",ns);
  if(metadata!=null){p.ImageSelection=Text(metadata,"u:Key",ns)=="/IMAGE/NAME"?"按镜像名称":"按镜像索引";p.ImageName=Text(metadata,"u:Value",ns)??"";if(int.TryParse(p.ImageName,out int index))p.ImageIndex=index;}
  var target=doc.SelectSingleNode("//u:ImageInstall/u:OSImage/u:InstallTo",ns);p.DiskMode=InstallationSettings.Manual;
  if(target!=null&&int.TryParse(Text(target,"u:DiskID",ns),out int disk)&&int.TryParse(Text(target,"u:PartitionID",ns),out int part)){
   p.DiskMode=InstallationSettings.Existing;p.TargetDiskId=disk;p.TargetPartitionId=part;
   p.FormatExistingPartition=doc.SelectNodes("//u:DiskConfiguration//u:Format",ns)!.Count>0;
  }
  var wipe=doc.SelectNodes("//u:RunSynchronousCommand/u:Path",ns)!.Cast<XmlNode>().FirstOrDefault(n=>n.InnerText.Contains(@"X:\UnattendLocal-diskpart.txt")&&n.InnerText.Contains("echo clean"));
  if(wipe!=null){
   p.DiskMode=InstallationSettings.Wipe;p.DiskLayout=wipe.InnerText.Contains("echo convert gpt")?"UEFI / GPT":"BIOS / MBR";
   var recovery=Match(wipe.InnerText,@"shrink minimum=(\d+)");if(recovery.Success)p.RecoverySizeMB=int.Parse(recovery.Groups[1].Value);
  }
 }
 private static void ReadCredentials(XmlDocument doc,XmlNamespaceManager ns,InstallPlan p,List<string> notes){
  if(p.AccountMode=="自动创建本地账户"){
   var users=new ObservableCollection<UserEntry>();
   foreach(XmlElement user in doc.SelectNodes("//u:LocalAccount",ns)!) {
    string name=Text(user,"u:Name",ns)??"";string value=Text(user,"u:Password/u:Value",ns)??"";bool plain=IsTrue(Text(user,"u:Password/u:PlainText",ns));
    if(!plain&&value.Length>0){
     try{value=new UnicodeEncoding(false,false,true).GetString(Convert.FromBase64String(value));if(!value.EndsWith("Password",StringComparison.Ordinal))throw new FormatException();value=value[..^8];}
     catch(Exception e)when(e is FormatException or DecoderFallbackException){throw new ArgumentException("账户密码编码无法还原，未导入；请使用原始安装 XML。");}
    }
    if(value.Contains("密码已遮蔽"))throw new ArgumentException("此 XML 是遮蔽后的预览，不能恢复密码；请导入实际导出的文件。");
    string group=Text(user,"u:Group",ns)??"";
    if(group is not ("Administrators" or "Users"))notes.Add("账户分组含界面尚不支持的组；原分组保留，修改账户组时可能产生冲突。");
    users.Add(new UserEntry{Name=name,DisplayName=Text(user,"u:DisplayName",ns)??"",Administrator=group=="Administrators",Password=value,AllowEmptyPassword=value.Length==0,NeedsPassword=false});
    p.ObscurePasswords=!plain;
   }
   p.Users=users;
  }else p.Users.Clear();
  var install=Text(doc,"//u:UserData/u:ProductKey/u:Key",ns);if(p.EditionMode=="指定自己的产品密钥"&&install!=null)p.CustomProductKey=install;
  if(p.UseActivationKey)p.ActivationProductKey=Text(doc,"//u:component[@name='Microsoft-Windows-Shell-Setup']/u:ProductKey",ns)??"";
  p.NeedsInstallationKey=false;p.NeedsActivationKey=false;
 }
 private static void ReadGeneratedSettings(XmlDocument doc,XmlNamespaceManager ns,InstallPlan p,List<string> notes){
  // A website URL can be stale after hand editing. Modes must be backed by actual payloads.
  p.VisualEffectsMode=VisualEffectsSettings.Default;p.DesktopIconsMode=DesktopSettings.DefaultIcons;p.StartFoldersMode=DesktopSettings.DefaultFolders;
  p.LockKeyMode=KeyboardLockSettings.Default;p.StickyKeysMode=StickyKeySettings.Default;p.ColorSettingsMode=ColorSettings.Default;p.WallpaperMode=WallpaperSettings.Default;
  var files=doc.SelectNodes("//*[local-name()='File']")!.Cast<XmlElement>().ToArray();string body=doc.InnerText;
  string File(string name)=>files.FirstOrDefault(f=>f.GetAttribute("path").EndsWith("\\"+name,StringComparison.OrdinalIgnoreCase))?.InnerText??"";
  var hidden=Match(File("DefaultUser.ps1"),@"/v ""?ShowSuperHidden""?.*?/d 1");
  if(!hidden.Success)hidden=Match(File("DefaultUser.ps1"),@"/v ""?ShowSuperHidden""?");
  p.HideFilesMode=body.Contains("ShowSuperHidden")?ExplorerSettings.ShowAll:Match(body,@"/v ""?Hidden""? /t REG_DWORD /d 1").Success?ExplorerSettings.ProtectedOnly:ExplorerSettings.Default;
  var search=Match(body,@"-Name 'SearchboxTaskbarMode' -Type 'DWord' -Value (\d+)");if(search.Success)p.TaskbarSearch=search.Groups[1].Value switch {"0"=>"隐藏","1"=>"仅图标","3"=>"图标和文字",_=>"完整搜索框"};
  var age=Match(File("Specialize.ps1"),@"/maxpwage:(UNLIMITED|\d+)");p.PasswordExpiration=age.Success?age.Groups[1].Value.Equals("UNLIMITED",StringComparison.OrdinalIgnoreCase)?"永不过期":"指定天数":"Windows 默认";if(age.Success&&int.TryParse(age.Groups[1].Value,out int days))p.PasswordDays=days;
  var lockout=Match(File("Specialize.ps1"),@"/lockoutthreshold:(\d+)(?: /lockoutduration:(\d+) /lockoutwindow:(\d+))?");
  if(lockout.Success){p.AccountLockoutMode=lockout.Groups[2].Success?AccountLockoutPolicy.Custom:AccountLockoutPolicy.Disabled;p.LockoutThreshold=int.Parse(lockout.Groups[1].Value);if(lockout.Groups[2].Success){p.LockoutDurationMinutes=int.Parse(lockout.Groups[2].Value);p.LockoutResetMinutes=int.Parse(lockout.Groups[3].Value);}}
  var indicators=Match(File("DefaultUser.ps1"),@"-Name 'InitialKeyboardIndicators' -Type 'String' -Value (\d+)");
  if(indicators.Success){p.LockKeyMode=KeyboardLockSettings.Custom;int bits=int.Parse(indicators.Groups[1].Value);p.InitialCapsLockOn=(bits&1)!=0;p.InitialNumLockOn=(bits&2)!=0;p.InitialScrollLockOn=(bits&4)!=0;}
  var scan=Match(File("Specialize.ps1"),@"Scancode Map'.*?FromBase64String\('([^']+)'\)");
  if(scan.Success){
   try{var bytes=Convert.FromBase64String(scan.Groups[1].Value);if(bytes.Length<16||(bytes.Length-12)%4!=0)throw new FormatException();
    for(int i=12;i<bytes.Length-4;i+=4){if(bytes[i]!=0||bytes[i+1]!=0||bytes[i+3]!=0){notes.Add("键盘扫描码含其他重映射，原文保留，修改锁定键映射可能冲突。");continue;}switch(bytes[i+2]){case 0x3a:p.IgnoreCapsLock=true;break;case 0x45:p.IgnoreNumLock=true;break;case 0x46:p.IgnoreScrollLock=true;break;default:notes.Add("键盘扫描码含其他按键映射，原文保留。");break;}}
   }catch(FormatException){notes.Add("无法还原键盘扫描码映射，原文保留。");}
  }
  var sticky=Match(File("DefaultUser.ps1"),@"Accessibility\\StickyKeys"" /v Flags /t REG_SZ /d (\d+)");
  if(sticky.Success){int bits=int.Parse(sticky.Groups[1].Value);p.StickyKeysMode=bits==10?StickyKeySettings.Disabled:StickyKeySettings.Custom;p.StickyHotKeyActive=(bits&4)!=0;p.StickyHotKeySound=(bits&16)!=0;p.StickyIndicator=(bits&32)!=0;p.StickyAudibleFeedback=(bits&64)!=0;p.StickyTriState=(bits&128)!=0;p.StickyTwoKeysOff=(bits&256)!=0;}
  var color=File("SetColorTheme.ps1");if(Match(color,@"\$lightThemeSystem = [01];").Success&&Match(color,@"\$lightThemeApps = [01];").Success&&Match(color,@"\$htmlAccentColor = '(#[0-9a-f]{6})'").Success){
   p.ColorSettingsMode=ColorSettings.Custom;p.WindowsColorTheme=Match(color,@"\$lightThemeSystem = 1;").Success?ColorSettings.Light:ColorSettings.Dark;p.AppsColorTheme=Match(color,@"\$lightThemeApps = 1;").Success?ColorSettings.Light:ColorSettings.Dark;
   p.EnableColorTransparency=Match(color,@"\$enableTransparency = 1;").Success;p.AccentColorOnStart=Match(color,@"\$accentColorOnStart = 1;").Success;
   var accent=Match(color,@"\$htmlAccentColor = '(#[0-9a-f]{6})'");if(accent.Success)p.AccentColorHex=accent.Groups[1].Value;
   p.AccentColorOnBorders=Match(File("DefaultUser.ps1"),@"\\DWM"" /v ColorPrevalence /t REG_DWORD /d 1").Success;
  }
  var effects=Match(File("UserOnce.ps1"),@"-Name 'VisualFXSetting' -Type 'DWord' -Value ([123])");
  if(effects.Success){
   p.VisualEffectsMode=effects.Groups[1].Value switch {"1"=>VisualEffectsSettings.Appearance,"2"=>VisualEffectsSettings.Performance,_=>VisualEffectsSettings.Custom};
   foreach(var item in VisualEffectsSettings.Items)typeof(InstallPlan).GetProperty(item.Property)!.SetValue(p,Match(File("Specialize.ps1"),@"VisualEffects\\"+Regex.Escape(item.Effect.ToString())+@""" -Name 'DefaultValue' -Value 1").Success);
  }
  var generator=new UnattendGenerator();
  if(File("UserOnce.ps1").Contains("HideDesktopIcons")){
   p.DesktopIconsMode=DesktopSettings.CustomIcons;
   foreach(var item in DesktopSettings.Icons)typeof(InstallPlan).GetProperty(item.Property)!.SetValue(p,Match(File("UserOnce.ps1"),@"-Name '"+Regex.Escape(generator.DesktopIcons[item.Id].Guid)+@"' -Value 0").Success);
  }
  var places=Match(File("UserOnce.ps1"),@"-Name 'VisiblePlaces'.*?FromBase64String\('([^']*)'\)");
  if(places.Success){p.StartFoldersMode=DesktopSettings.CustomFolders;var bytes=Convert.FromBase64String(places.Groups[1].Value);
   foreach(var item in DesktopSettings.Folders){var id=generator.StartFolders[item.Id].Bytes;bool found=false;for(int i=0;i+id.Length<=bytes.Length;i++)if(bytes.AsSpan(i,id.Length).SequenceEqual(id)){found=true;break;}typeof(InstallPlan).GetProperty(item.Property)!.SetValue(p,found);}
  }
  var wallpaper=Match(File("SetWallpaper.ps1"),@"Set-WallpaperColor -HtmlColor '(#[0-9a-f]{6})'");if(wallpaper.Success){p.WallpaperMode=WallpaperSettings.Solid;p.WallpaperColorHex=wallpaper.Groups[1].Value;}
  if(p.Scripts.Count==0)foreach(var file in files.Where(f=>Match(f.GetAttribute("path"),@"\\unattend-[0-9a-f]{2}\.(cmd|ps1|reg|vbs|js)$").Success)){
   string path=file.GetAttribute("path");string[] phases={"System","DefaultUser","FirstLogon","UserOnce"};string[] names={"Specialize.ps1","DefaultUser.ps1","FirstLogon.ps1","UserOnce.ps1"};
   var found=phases.Where((phase,i)=>File(names[i]).Contains(path,StringComparison.OrdinalIgnoreCase)).ToArray();
   if(found.Length!=1){notes.Add("有自定义脚本无法确定执行阶段，保留原文及调用，未放入脚本编辑列表。");continue;}
   string ext=Path.GetExtension(path)[1..];p.Scripts.Add(new ScriptEntry{Phase=found[0],Type=char.ToUpperInvariant(ext[0])+ext[1..],Enabled=ext!="js",Content=file.InnerText.Trim()});
  }
  ReadLayout("TaskbarLayoutModification.xml","taskbar");ReadLayout("LayoutModification.xml","tiles");
  var pins=Match(File("SetStartPins.ps1"),@"^\s*\$json = '((?:''|[^'])*)';");if(pins.Success){p.StartPinsJson=pins.Groups[1].Value.Replace("''","'");p.StartPinsMode=p.StartPinsJson.Replace(" ","").Replace("\n","").Replace("\r","")=="{\"pinnedList\":[]}"?"移除全部固定应用":ShellSettings.CustomJson;}
  void ReadLayout(string name,string kind){string value=File(name);if(value.Length==0)return;try{ShellSettings.ValidateDocument(kind,value);if(kind=="taskbar"){p.TaskbarIconsXml=value;p.TaskbarIconsMode=value.Contains("#leaveempty")?"移除全部固定图标":ShellSettings.CustomXml;}else{p.StartTilesXml=value;p.StartTilesMode=ShellSettings.CustomXml;}}catch(ArgumentException){if(kind=="tiles"&&!value.Contains("Tile "))p.StartTilesMode="移除全部磁贴";else notes.Add("部分开始菜单/任务栏布局无法在界面还原，原布局保留。");}}
 }
 private static void DescribeRemainder(XmlDocument source,XmlDocument baseline,List<string> notes){
  var comparison=AppRemovalSettings.ComparisonDocument(source);
  var generated=baseline.SelectNodes("//*[local-name()='File']")!.Cast<XmlElement>().ToDictionary(f=>f.GetAttribute("path"),StringComparer.OrdinalIgnoreCase);
  foreach(XmlElement file in comparison.SelectNodes("//*[local-name()='File']")!) {
   string path=file.GetAttribute("path");
   if(generated.TryGetValue(path,out var other)){
    if(XmlMerge.Equal(file,other))continue;
    if(ScriptImportDetails.Describe(comparison,file,other,notes)){file.InnerText=other.InnerText;continue;}
   }
   string prefix=path.EndsWith(".ps1",StringComparison.OrdinalIgnoreCase)?"脚本原文已保留，内容与界面生成版本存在差异：":"文件原文已保留，内容未完整映射为界面设置：";
   notes.Add(prefix+PathName(path));
  }
  var known=baseline.SelectNodes("//*[local-name()='component']")!.Cast<XmlElement>().Select(XmlMerge.Identity).ToHashSet();
  foreach(XmlElement component in source.SelectNodes("//u:component",NamespaceManager(source))!)if(!known.Contains(XmlMerge.Identity(component))&&component.GetAttribute("name") is not (StaticIPv4Settings.Tcp or StaticIPv4Settings.Dns or SetupDriverSettings.ComponentName))notes.Add("原文保留，未实现的组件："+component.GetAttribute("name"));
  if(!XmlMerge.Equal(comparison.DocumentElement,baseline.DocumentElement)&&notes.Count==0)notes.Add("原 XML 含无法完整还原的参数或组件内容；原文保留，修改发生冲突时将阻止导出。");
 }
 internal static void RefreshRemainder(InstallPlan p){
  if(p.ImportedXml is not {OriginalOmitted:false} data||data.SourceXml.Length==0||data.BaselineXml.Length==0)return;
  var prefixes=new[]{"原文保留，未完全还原到界面：","脚本原文已保留，","文件原文已保留，","脚本原文已保留：","脚本中的设置已识别：","原文保留，未实现的组件：","原 XML 含无法完整还原的参数或组件内容；"};
  var notes=data.Notices.Where(n=>!prefixes.Any(prefix=>n.StartsWith(prefix,StringComparison.Ordinal))).ToList();
  DescribeRemainder(XmlMerge.Read(data.SourceXml),XmlMerge.Read(data.BaselineXml),notes);
  data.Notices=notes.Distinct().ToArray();
 }
 private static void ReadForm(XmlDocument doc,InstallPlan p,List<string> notes){
  var values=XmlImportParameters.ReadValues(doc,notes);
  if(values.Count==0)return;
  string Value(string key,string fallback="")=>values.GetValueOrDefault(key,fallback);
  bool Checked(string key)=>Value(key)=="true";
  var known=new HashSet<string>();
  foreach(var prop in typeof(InstallPlan).GetProperties().Where(prop=>prop.PropertyType==typeof(bool)&&prop.CanWrite))if(values.ContainsKey(prop.Name)){known.Add(prop.Name);prop.SetValue(p,Checked(prop.Name));}
  void Mode(string key,string property,params string[] pairs){
   known.Add(key);var value=Value(key);for(int i=0;i<pairs.Length;i+=2)if(value==pairs[i]){typeof(InstallPlan).GetProperty(property)!.SetValue(p,pairs[i+1]);return;}
   if(value.Length>0&&value is not ("Default" or "Implicit"))notes.Add("生成参数暂不能完全还原："+key+"；相关 XML 原文保留。");
  }
  Mode("EffectsMode",nameof(p.VisualEffectsMode),"Default",VisualEffectsSettings.Default,"Appearance",VisualEffectsSettings.Appearance,"Performance",VisualEffectsSettings.Performance,"Custom",VisualEffectsSettings.Custom);
  Mode("DesktopIconsMode",nameof(p.DesktopIconsMode),"Default",DesktopSettings.DefaultIcons,"Custom",DesktopSettings.CustomIcons);
  Mode("StartFoldersMode",nameof(p.StartFoldersMode),"Default",DesktopSettings.DefaultFolders,"Custom",DesktopSettings.CustomFolders);
  Mode("LockKeysMode",nameof(p.LockKeyMode),"Skip",KeyboardLockSettings.Default,"Default",KeyboardLockSettings.Default,"Custom",KeyboardLockSettings.Custom);
  Mode("StickyKeysMode",nameof(p.StickyKeysMode),"Default",StickyKeySettings.Default,"Disabled",StickyKeySettings.Disabled,"Custom",StickyKeySettings.Custom);
  foreach(var item in VisualEffectsSettings.Items){known.Add(item.Effect.ToString());typeof(InstallPlan).GetProperty(item.Property)!.SetValue(p,Checked(item.Effect.ToString()));}
  foreach(var item in DesktopSettings.Icons){known.Add("Icon"+item.Id);typeof(InstallPlan).GetProperty(item.Property)!.SetValue(p,Checked("Icon"+item.Id));}
  foreach(var item in DesktopSettings.Folders){known.Add("StartFolder"+item.Id);typeof(InstallPlan).GetProperty(item.Property)!.SetValue(p,Checked("StartFolder"+item.Id));}
  foreach(var key in new[]{"NumLock","CapsLock","ScrollLock"}){
   known.Add(key+"Initial");known.Add(key+"Behavior");typeof(InstallPlan).GetProperty("Initial"+key+"On")!.SetValue(p,Value(key+"Initial")=="On");typeof(InstallPlan).GetProperty("Ignore"+key)!.SetValue(p,Value(key+"Behavior")=="Ignore");
  }
  p.RestartExplorerAfterScripts=Checked("RestartExplorer");known.Add("RestartExplorer");
  known.Add("ProcessAudit");known.Add("IncludeCommandLine");known.Add("AppLockerMode");known.Add("AppLockerPolicyXml");
  // Native XML and payload readers handle these; credentials are never accepted from comments.
  foreach(var key in new[]{"LanguageMode","UILanguage","Locale","Keyboard","GeoLocation","ComputerNameMode","ComputerName","ComputerNameScript","Locale2","Keyboard2","Locale3","Keyboard3","TimeZoneMode","TimeZone","WindowsEditionMode","WindowsEdition","ProductKey","UseActivationKey","ActivationKey","UserAccountMode","AutoLogonMode","BuiltinAdministratorPassword","ObscurePasswords","PasswordExpirationMode","PasswordMaxAge","LockoutMode","LockoutThreshold","LockoutWindow","LockoutDuration","WifiMode","WifiName","WifiPassword","WifiAuthentication","WifiNonBroadcast","ExpressSettings","TaskbarSearch","HideFiles","TaskbarIconsMode","TaskbarIconsXml","StartTilesMode","StartTilesXml","StartPinsMode","StartPinsJson","ColorMode","SystemColorTheme","AppsColorTheme","AccentColor","AccentColorOnStart","AccentColorOnBorders","EnableTransparency","WallpaperMode","WallpaperColor","WallpaperScript","LockScreenMode","LockScreenScript","LockKeysMode","ProcessorArchitecture","BypassRequirementsCheck","BypassNetworkCheck","StickyKeysHotKeyActive","StickyKeysHotKeySound","StickyKeysIndicator","StickyKeysAudibleFeedback","StickyKeysTriState","StickyKeysTwoKeysOff","NotFilename","CompactOs"})known.Add(key);
  foreach(var item in AppRemovalSettings.Items)known.Add(item.Id);
  foreach(var pair in values){
   if(XmlImportParameters.IsHandled(pair.Key,pair.Value)||known.Contains(pair.Key)||Match(pair.Key,@"^(Account(Name|DisplayName|Password|Group)|(?:System|DefaultUser|FirstLogon|UserOnce)Script(?:Type)?)[0-9]+$").Success)continue;
   if(pair.Value is "false" or "Default" or "" or "Implicit")continue;
   // Report names only: parameters can contain passwords, keys or whole scripts.
   if(pair.Value=="true"||pair.Key.EndsWith("Mode")||pair.Key.StartsWith("Remove"))notes.Add("暂未识别的生成参数："+pair.Key+"；相关 XML 原文保留，未自动套用此参数。");
  }
 }
 private static string PathName(string path)=>path[(path.LastIndexOf('\\')+1)..];
 private static XmlNamespaceManager NamespaceManager(XmlDocument d){var n=new XmlNamespaceManager(d.NameTable);n.AddNamespace("u",XmlMerge.Namespace);return n;}
 private static InstallPlan? ReadSnapshot(XmlDocument doc,List<string> notes){
  var comments=doc.SelectNodes("//comment()")!.Cast<XmlComment>().Where(c=>c.Value?.StartsWith(Marker)==true).ToArray();if(comments.Length==0)return null;
  if(comments.Length!=1){notes.Add("配置还原记录重复，已忽略记录并读取实际 XML。");return null;}
  string[] parts=comments[0].Value![Marker.Length..].Split(' ',2);
  if(parts.Length!=2||parts[0]!=Digest(doc)){notes.Add("XML 已在导出后修改，配置还原记录已失效；以实际 XML 可识别内容为准，其余原文保留。");return null;}
  try{return PlanStore.Deserialize(Encoding.UTF8.GetString(Convert.FromBase64String(parts[1])));}catch(Exception e)when(e is FormatException or JsonException or ArgumentException){throw new ArgumentException("XML 配置还原记录无效，未导入。");}
 }
 private static string Digest(XmlDocument doc)=>Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(XmlMerge.Canonical(doc.DocumentElement!))));
 public static void AddSnapshot(XmlDocument doc,InstallPlan plan){
  foreach(XmlComment c in doc.SelectNodes("//comment()")!.Cast<XmlComment>().ToArray())if(c.Value?.StartsWith(Marker)==true||c.Value?.TrimStart().StartsWith("https://schneegans.de/windows/unattend-generator/?")==true)c.ParentNode!.RemoveChild(c);
  var copy=PlanStore.Deserialize(PlanStore.Serialize(plan,true));copy.ImportedXml=null;
  if(copy.PowerPlanMode!=NativePreferences.Custom)copy.CustomPowerPlanGuid=NativePreferences.Value(copy,NativePreferences.Power)??"";
  copy.ExportNonAutomaticFilename=false; // Output preference belongs to JSON, not installation settings.
  copy.XmlComponents=new(copy.XmlComponents.Where(row=>row.Enabled));
  copy.Scripts=new(copy.Scripts.Where(s=>s.Enabled&&!s.IsRemovedType));
  if(copy.AccountMode!="自动创建本地账户")copy.Users.Clear();
  if(!WifiSettings.IsPreset(copy)){copy.Ssid="";copy.WifiPassword="";}
  if(!copy.UseStaticIPv4){copy.UseStaticIPv4SubnetMask=false;copy.StaticIPv4SubnetMask="";}
  else {copy.StaticIPv4Prefix=StaticIPv4Settings.EffectivePrefix(copy);if(!copy.UseStaticIPv4SubnetMask)copy.StaticIPv4SubnetMask="";}
  if(!copy.UseSetupDrivers)copy.SetupDriverPaths="";
  if(!copy.UseWorkgroup)copy.WorkgroupName="";
  if(!copy.UsePeLogPath)copy.PeLogPath="";
  if(!copy.UseRegisteredOwner)copy.RegisteredOwner="";
  if(!copy.UseRegisteredOrganization)copy.RegisteredOrganization="";
  if(!copy.UseAppLocker)copy.AppLockerPolicyXml="";
  if(copy.ComputerNameMode!=ComputerNamingSettings.Script)copy.ComputerNameScript="";
  if(copy.TaskbarIconsMode!=ShellSettings.CustomXml)copy.TaskbarIconsXml="";
  if(copy.StartTilesMode!=ShellSettings.CustomXml)copy.StartTilesXml="";
  if(copy.StartPinsMode!=ShellSettings.CustomJson)copy.StartPinsJson="";
  if(copy.DiskMode!=DiskScriptSettings.Mode)copy.DiskPartScript="";
  if(!WallpaperSettings.IsScript(copy))copy.WallpaperImageScript="";
  if(!ImageBackgroundSettings.IsLockScript(copy))copy.LockScreenImageScript="";
  // Active fragment elements already exist in the export. Retain only those elements
  // in its editable snapshot; omit disabled drafts and comments outside the elements.
  var snapshot=System.Text.Json.Nodes.JsonNode.Parse(PlanStore.Serialize(copy,false))!;
  for(int i=0;i<copy.XmlComponents.Count;i++)snapshot[nameof(InstallPlan.XmlComponents)]![i]![nameof(XmlComponentEntry.Content)]=string.Concat(AdditionalXmlSettings.Parse(copy.XmlComponents[i]).DocumentElement!.ChildNodes.OfType<XmlElement>().Select(element=>element.OuterXml));
  var payload=Convert.ToBase64String(Encoding.UTF8.GetBytes(snapshot.ToJsonString()));
  doc.DocumentElement!.PrependChild(doc.CreateComment(Marker+Digest(doc)+" "+payload));
 }
 public static string Summary(InstallPlan p)=>p.ImportedXml is not {} data?"未导入 XML":data.OriginalOmitted?"XML 原文未随方案保存，请重新导入原 XML 后导出":data.Blockers.Length>0?"已导入；存在阻止导出的项目":data.Notices.Length>0?"已导入；部分内容原文保留，修改冲突时停止导出":"已导入；可修改已还原的配置";
 public static void ValidateImported(InstallPlan p){
  if(p.ImportedXml is not {} data)return;
  if(data.OriginalOmitted||data.SourceXml.Length==0||data.BaselineXml.Length==0)throw new ArgumentException("缺少完整 XML 原文/编辑基准，请重新导入原 XML；不会使用界面默认值代替丢失内容。");
  if(data.Blockers.Length>0)throw new ArgumentException(string.Join("\n",data.Blockers));
  var source=XmlImportPaths.Inspection(XmlMerge.Read(data.SourceXml));
  AutoPartitionSettings.ValidateImported(p,source);
  CompactOsSettings.ValidateImported(p,source);
  StaticIPv4Settings.ValidateImported(p,source);
  SetupDriverSettings.ValidateImported(p,source);
  SetupDynamicUpdateSettings.ValidateImported(p,source);
  RegisteredInformationSettings.ValidateImported(p,source);
  WorkgroupSettings.ValidateImported(p,source);
  PeLogSettings.ValidateImported(p,source);
  NativePreferences.ValidateImported(p,source);
  ComputerNamingSettings.ValidateImported(p,source);
  LanguagePreferences.ValidateImported(p,source);
  WifiSettings.ValidateImported(p,source);
  CommonSystemSettings.ValidateImported(p,source);
  ImageBackgroundSettings.ValidateImported(p,source);
  AppRemovalSettings.ValidateImported(p,source);
  FirstLogonSettings.ValidateImported(p,source);
  AdvancedSystemSettings.ValidateImported(p,source);
  SystemMaintenanceSettings.ValidateImported(p,source);
  ProcessAuditSettings.ValidateImported(p,source);
  ProtectionSettings.ValidateImported(p,source);
  LegacyUpgradeSettings.ValidateImported(p,source);
  VmGuestToolsSettings.ValidateImported(p,source);
  AdditionalSetupSettings.ValidateImported(p,source);
  SecurityOptions.ValidateImported(p,source);
  JunctionSettings.ValidateImported(p,source);
  AppControlSettings.ValidateImported(p,source);
  ConfigurationSetSettings.ValidateImported(p,source);
  if(HasRemovedScript(source))throw new ArgumentException("原 XML 仍含已移除支持的 JScript，不能再导出。");
  if((data.RequiresDiskReview||NeedsDiskReview(source))&&!p.ConfirmImportedXmlRisk)throw new ArgumentException("导入的 XML 含磁盘配置或 Windows PE 命令，请在配置方案页重新核对原文操作范围并勾选风险确认。");
 }
 private static bool NeedsDiskReview(XmlDocument doc){
  var ns=NamespaceManager(doc);
  bool knownNarrator=AdditionalSetupSettings.Inspect(doc)[nameof(InstallPlan.UseNarrator)].Enabled;
  return doc.SelectNodes("//u:DiskConfiguration",ns)!.Count>0||Match(doc.InnerText,@"\b(diskpart(?:\.exe)?|Clear-Disk|Initialize-Disk|Format-Volume|Remove-Partition|New-Partition)\b").Success||doc.SelectNodes("//u:settings[@pass='windowsPE']//u:Path",ns)!.Cast<XmlNode>().Any(n=>!(knownNarrator&&AdditionalSetupSettings.IsNarratorPeCommand(n.InnerText))&&!Match(n.InnerText,@"^reg\.exe add .*LabConfig.*Bypass(?:TPM|RAM|SecureBoot)Check").Success);
 }
 private static bool HasRemovedScript(XmlDocument doc)=>doc.SelectNodes("//*[local-name()='File']")!.Cast<XmlElement>().Any(f=>f.GetAttribute("path").EndsWith(".js",StringComparison.OrdinalIgnoreCase))||Match(doc.InnerText,@"//e:jscript|\bjscript\b|\b(?:cscript|wscript)(?:\.exe)?[^\r\n]*\.js\b").Success;
}
