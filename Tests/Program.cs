using System.Text;using System.Xml;using UnattendLocal.Core;
int count=0;var g=new GeneratorService();
if(args.Contains("--pe-import-compatibility-only")){Console.WriteLine($"PE import compatibility: {PeImportCompatibilityTests.Run(g)} checks passed.");return;}
if(args.Length==2 && args[0]=="--export-pe-chain-fixtures"){PeSafetyTests.ExportChainFixtures(g,args[1]);return;}
if(args.Contains("--key-mode-compatibility-only")){Console.WriteLine($"Key mode compatibility: {KeyModeCompatibilityTests.Run(g)} checks passed.");return;}
if(args.Contains("--pe-preflight-only")){Console.WriteLine($"PE preflight: {PePreflightTests.Run(g)} checks passed.");return;}
if(args.Contains("--v129-guidance-only")){Console.WriteLine($"V1.29 guidance: {V129GuidanceTests.Run(g)} checks passed.");return;}
if(args.Contains("--preview-localization-only")){Console.WriteLine($"Preview localization: {PreviewLocalizationTests.Run(g)} checks passed.");return;}
if(args.Contains("--v128-security-only")){Console.WriteLine($"V1.28 security: {V128SecurityTests.Run(g)} checks passed.");return;}
if(args.Contains("--pe-safety-only")){Console.WriteLine($"V1.27 PE safety: {PeSafetyTests.Run(g)} checks passed.");return;}
if(args.Contains("--generated-pe-only")){Console.WriteLine($"V1.25 PE: {GeneratedPeTests.Run(g)} checks passed.");return;}
if(args.Contains("--v124-only")){Console.WriteLine($"V1.24 targeted: {V124SecurityAndNamingTests.Run(g)} checks passed.");return;}
if(args.Contains("--deployment-tuning-only")){Console.WriteLine($"V1.21 deployment tuning: {DeploymentTuningTests.Run(g)} checks passed.");return;}
if(args.Contains("--localization-only") || args.Contains("--localization-stress-only")){
 if(args.Contains("--localization-stress-only")){
  _ = new UnattendLocal.Desktop.MainViewModel().Status;
  for(int i=0;i<12005;i++)UnattendLocal.Localization.UiText.Concat(["cache probe ", i], [false, true]);
 }
 try{LocalizationTests.Run(g);}catch(Exception failure){Console.Error.WriteLine(failure.GetType().FullName);Console.Error.WriteLine(failure.Message);Environment.ExitCode=1;}return;
}
if(args.Contains("--script-enhancements-only")){ScriptEnhancementTests.Run(g);return;}
if(args.Contains("--script-templates-only")){ScriptTemplateTests.Run(g);return;}
if(args.Contains("--file-reliability-only")){FileReliabilityTests.Run();return;}
if(args.Contains("--native-preferences-only")){Console.WriteLine($"V1.15 native preferences: {NativePreferencesTests.Run(g)} checks passed.");return;}
if(args.Contains("--advanced-xml-only")){Console.WriteLine($"V1.14 presets and advanced XML: {AdvancedXmlTests.Run(g)} checks passed.");return;}
if(args.Contains("--answer-export-only")){var i=Array.IndexOf(args,"--export-samples");Console.WriteLine($"V1.13 export: {AnswerFileExportTests.Run(g,i>=0?args[i+1]:null)} checks passed.");return;}
if(args.Contains("--security-audit-only")){Console.WriteLine($"V1.12.1 security audit: {SecurityAuditFixTests.Run(g)} checks passed.");return;}
if(args.Contains("--import-enhancements-only")){Console.WriteLine($"V1.12 import enhancements: {ImportEnhancementTests.Run(g)} checks passed.");return;}
if(args.Contains("--unified-network-only")){Console.WriteLine($"V1.11 unified network: {UnifiedSubnetTests.Run(g)} checks passed.");return;}
if(args.Contains("--error-location-only")){Console.WriteLine($"V1.11 error location: {ErrorLocationTests.Run(g)} checks passed.");return;}
if(args.Contains("--help-layout-only")){Console.WriteLine($"V1.10 help layout: {HelpLayoutTests.Run(g)} checks passed.");return;}
if(args.Contains("--network-deployment-only")){Console.WriteLine($"Network deployment: {NetworkDeploymentTests.Run(g)} checks passed.");return;}
if(args.Contains("--dual-stack-only")){Console.WriteLine($"V1.9 dual stack: {DualStackNetworkTests.Run(g)} checks passed.");return;}
if(args.Contains("--ui-hotfix-only")){Console.WriteLine($"V1.8.1 UI hotfix: {ColorPickerHotfixTests.Run()} checks passed.");return;}
if(args.Contains("--ui-improvements-only")){Console.WriteLine($"V1.8 UI improvements: {UiImprovementTests.Run(g)} checks passed.");return;}
if(args.Contains("--network-enhancement-only")){Console.WriteLine($"V1.9 network enhancements: {NetworkEnhancementTests.Run(g)} checks passed.");return;}
if(args.Contains("--version-display-only")){VersionDisplayTests.Run();Console.WriteLine("Version display check passed.");return;}
if(args.Contains("--native-deployment-only")){Console.WriteLine($"V1.7 native deployment: {NativeDeploymentTests.Run(g)} checks passed.");return;}
if(args.Contains("--registered-info-only")){Console.WriteLine($"V1.5 registered information: {RegisteredInformationTests.Run(g)} checks passed.");return;}
if(args.Contains("--dynamic-update-only")){Console.WriteLine($"V1.4 dynamic updates: {SetupDynamicUpdateTests.Run(g)} checks passed.");return;}
if(args.Contains("--setup-drivers-only")){Console.WriteLine($"V1.3 drivers: {SetupDriverTests.Run(g)} checks passed.");return;}
if(args.Contains("--script-files-only")){Console.WriteLine($"V1.2 script files: {ScriptFileTests.Run(g)} checks passed.");return;}
if(args.Contains("--partition-only")){Console.WriteLine($"V1.1 partition: {AutoPartitionTests.Run(g)} checks passed.");return;}
if(args.Contains("--import-only")){Console.WriteLine($"V1.0.1 import: {ImportCompatibilityTests.Run(g)} checks passed.");return;}
if(args.Contains("--network-only")){Console.WriteLine($"V1.0 network: {StaticNetworkTests.Run(g)} checks passed.");return;}
void Check(bool ok,string name){if(!ok)throw new Exception(name);Console.WriteLine("PASS "+name);count++;}
void Reject(InstallPlan p,string name){try{g.Generate(p);}catch(Exception){Check(true,name);return;}throw new Exception("Expected rejection: "+name);}
InstallPlan Plan(){var p=InstallPlan.New();p.Users[0].Password="A<&'12345";return p;}
string Xml(InstallPlan p)=>Encoding.UTF8.GetString(g.Generate(p));
var p=Plan();var xml=Xml(p);var doc=new XmlDocument();doc.LoadXml(xml);Check(xml.Contains("LocalAccount"),"local account");Check(xml.Contains("China Standard Time"),"time zone");Check(!xml.Contains("A&lt;"),"obscured password");Check(!xml.Contains("DiskConfiguration"),"no automatic disk formatting");
p.AutoLogon=false;Reject(p,"no auto logon requires explicit retention");p.KeepSensitiveFiles=true;Check(!Xml(p).Contains("<AutoLogon>"),"disable auto logon");p=Plan();p.Users.Add(new(){Name="USER",Password="12345678"});Reject(p,"case insensitive duplicate");p=Plan();p.Users[0].Administrator=false;Reject(p,"administrator required");p=Plan();p.Users[0].Password="";Reject(p,"explicit empty consent");p.Users[0].AllowEmptyPassword=true;Check(Xml(p).Contains("LocalAccount"),"explicit empty password");
p=Plan();p.WifiMode="预设 WPA2 Wi-Fi";p.Ssid="测试 & Network";p.WifiPassword="testpass123";p.HiddenWifi=true;Check(Xml(p).Contains("nonBroadcast"),"hidden wifi embedded");var saved=PlanStore.Serialize(p,false);Check(!saved.Contains("testpass123"),"wifi secret omitted");var restored=PlanStore.Deserialize(saved);Reject(restored,"missing secrets prevent export");Check(PlanStore.Deserialize(PlanStore.Serialize(p,true)).WifiPassword==p.WifiPassword,"secret opt in roundtrip");p.Ssid="bad\\\"ssid";Reject(p,"unsafe wifi name");p=Plan();p.ComputerName="User";Reject(p,"computer account collision");p=Plan();p.AccountMode="安装时创建本地账户";Check(Xml(p).Contains("<HideOnlineAccountScreens>true"),"interactive local OOBE");p.AccountMode="安装时添加微软账户";p.BypassNetwork=true;Reject(p,"online offline conflict");p=Plan();p.PasswordExpiration="指定天数";p.PasswordDays=1000;Reject(p,"password days range");foreach(var arch in new[]{"amd64","arm64","x86"}){p=Plan();p.Architecture=arch;Check(Xml(p).Contains("processorArchitecture=\""+arch+"\""),arch);}
p=Plan();p.ObscurePasswords=false;p.WifiMode="预设 WPA2 Wi-Fi";p.Ssid="Demo";p.WifiPassword="supersecret123";var preview=GeneratorService.Preview(g.Generate(p));Check(!preview.Contains("supersecret123")&&!preview.Contains("A&lt;"),"preview redacts account and wifi");p.Ssid="bad$()ssid";Reject(p,"PowerShell wifi interpolation rejected");
p=Plan();p.Users[0].DisplayName="中文 & 测试";Check(Xml(p).Contains("&#"),"non ASCII serialization");p=Plan();p.WifiMode="隐藏无线网络选择页";Check(Xml(p).Contains("<HideWirelessSetupInOOBE>true"),"skip wifi OOBE");p=Plan();p.HideEula=true;Check(Xml(p).Contains("<HideEULAPage>true"),"hide EULA");
Console.WriteLine($"{count} tests passed.");
// 产品密钥配置验证：检查不同安装阶段的 XML，不进行实际授权验证。
string? Node(string text,string path){var d=new XmlDocument();d.LoadXml(text);var n=new XmlNamespaceManager(d.NameTable);n.AddNamespace("u","urn:schemas-microsoft-com:unattend");return d.SelectSingleNode(path,n)?.InnerText;}
string? InstallKey(string text)=>Node(text,"//u:UserData/u:ProductKey/u:Key");
string? ActivationKey(string text)=>Node(text,"//u:component[@name='Microsoft-Windows-Shell-Setup']/u:ProductKey");
var genericKeys=new Dictionary<string,string>{
["home"]="YTMG3-N6DKC-DKB77-7M9GH-8HVX7",["home_n"]="4CPRK-NM3K3-X6XXQ-RXX86-WXCHW",["home_single"]="BT79Q-G7N6G-PGBYW-4YWX6-6F4BT",
["pro"]="VK7JG-NPHTM-C97JM-9MPGT-3V66T",["pro_n"]="2B87N-8KFHP-DKV6R-Y2C8J-PKCKT",["pro_education"]="8PTT6-RNW4C-6V7J2-C2D3X-MHBPB",["pro_education_n"]="GJTYN-HDMQY-FRR76-HVGC7-QPF8P",
["pro_workstations"]="DXG7C-N36C4-C4HTG-X4T3X-2YV77",["pro_workstations_n"]="WYPNQ-8C467-V2W6J-TX4WX-WT2RQ",["education"]="YNMGQ-8RYV3-4PGQ3-C8XTP-7CFBY",["education_n"]="84NGF-MHBT6-FXBX8-QWJK7-DRR8H",
["enterprise"]="XGVPP-NMH47-7TTHJ-W3FW7-8HV2C",["enterprise_n"]="WGGHN-J84D6-QYCPR-T7PJ7-X766F"};
Check(g.Editions.Count==13,"13 upstream SKU choices");
foreach(var pair in genericKeys){p=Plan();p.EditionMode="按 SKU 使用通用安装密钥";p.EditionId=pair.Key;xml=Xml(p);Check(InstallKey(xml)==pair.Value&&ActivationKey(xml)==null,"generic SKU "+pair.Key);}
p=Plan();p.EditionMode="安装时输入密钥";xml=Xml(p);Check(Node(xml,"//u:UserData/u:ProductKey/u:WillShowUI")=="Always","default interactive product key");
p.EditionMode="使用 BIOS/UEFI 固件密钥";xml=Xml(p);Check(InstallKey(xml)==null&&Node(xml,"//u:UserData/u:ProductKey/u:WillShowUI")=="Never","firmware key delegated to installer");
p=Plan();p.EditionMode="指定自己的产品密钥";p.CustomProductKey="  vk7jg-nphtm-c97jm-9mpgt-3v66t  ";xml=Xml(p);Check(InstallKey(xml)==genericKeys["pro"]&&ActivationKey(xml)==genericKeys["pro"],"custom install and activation normalized");
// 格式有效的假密钥，仅用于验证安装和激活设置彼此独立。
const string fixtureKey="AAAAA-BBBBB-CCCCC-DDDDD-EEEEE";
p.UseActivationKey=true;p.ActivationProductKey=fixtureKey;xml=Xml(p);Check(InstallKey(xml)==genericKeys["pro"]&&ActivationKey(xml)==fixtureKey,"separate activation overrides only activation pass");
preview=GeneratorService.Preview(g.Generate(p));Check(!preview.Contains(fixtureKey)&&!preview.Contains(genericKeys["pro"])&&preview.Contains("产品密钥已遮蔽"),"preview redacts both product key stages");
p.EditionMode="按 SKU 使用通用安装密钥";p.EditionId="enterprise";xml=Xml(p);Check(InstallKey(xml)==genericKeys["enterprise"]&&ActivationKey(xml)==fixtureKey,"generic install with separate activation");
p.ActivationProductKey="";Reject(p,"enabled activation key required");p.UseActivationKey=false;p.CustomProductKey="bad";Check(ActivationKey(Xml(p))==null,"inactive custom and activation values ignored");
p.EditionId="unknown";Reject(p,"unknown generic SKU rejected");p=Plan();p.EditionMode="unknown";Reject(p,"unknown installation key mode rejected");
p.EditionMode="指定自己的产品密钥";p.CustomProductKey="bad";Reject(p,"malformed installation key rejected");p.CustomProductKey="";Reject(p,"empty custom key rejected");
p.CustomProductKey=fixtureKey;p.UseActivationKey=true;p.ActivationProductKey=genericKeys["pro"];saved=PlanStore.Serialize(p,false);Check(!saved.Contains(fixtureKey)&&!saved.Contains(genericKeys["pro"]),"custom installation and activation keys omitted by default");restored=PlanStore.Deserialize(saved);Check(restored.NeedsInstallationKey&&restored.NeedsActivationKey,"omitted key reminders restored");restored.Users[0].Password="12345678";restored.Users[0].NeedsPassword=false;Reject(restored,"omitted active keys block export");restored.CustomProductKey=fixtureKey;restored.ActivationProductKey=genericKeys["pro"];Check(!restored.NeedsInstallationKey&&!restored.NeedsActivationKey&&InstallKey(Xml(restored))==fixtureKey,"reentered keys restore export");restored=PlanStore.Deserialize(PlanStore.Serialize(p,true));Check(restored.CustomProductKey==fixtureKey&&restored.ActivationProductKey==genericKeys["pro"],"explicit key opt-in roundtrip");
restored=PlanStore.Deserialize("{\"SchemaVersion\":1,\"Users\":[]}");Check(restored.EditionMode=="安装时输入密钥"&&!restored.UseActivationKey,"pre2 plans retain interactive default");
p=Plan();p.EditionMode="指定自己的产品密钥";p.CustomProductKey=fixtureKey;Check(!Xml(p).Contains("DiskConfiguration"),"key selection retains manual partitioning");
Console.WriteLine($"Total: {count} tests passed.");
// 安装脚本窗口与隐私文件清理分别配置。
string DecodedXml(string text){var d=new XmlDocument();d.LoadXml(text);return d.InnerText;}
Check(InstallPlan.New().HidePowerShellWindows&&!InstallPlan.New().KeepSensitiveFiles&&InstallPlan.New().EditionMode=="按 SKU 使用通用安装密钥"&&InstallPlan.New().EditionId=="pro"&&InstallPlan.New().WifiMode=="隐藏无线网络选择页"&&InstallPlan.New().BypassNetwork&&!InstallPlan.New().HiddenWifi&&!InstallPlan.New().ConnectWifi,"new plan defaults: generic Pro, wired LAN, offline allowed, hidden PowerShell, cleanup");
restored=PlanStore.Deserialize("{\"SchemaVersion\":1,\"Users\":[]}");Check(!restored.HidePowerShellWindows&&!restored.KeepSensitiveFiles,"legacy plans preserve visible PowerShell default");
foreach(var hide in new[]{false,true})foreach(var keep in new[]{false,true}){
 p=Plan();p.HidePowerShellWindows=hide;p.KeepSensitiveFiles=keep;p.WifiMode="预设 WPA2 Wi-Fi";p.Ssid="WindowTest";p.WifiPassword="testpass123";
 var decoded=DecodedXml(Xml(p));var selectedStyle=hide?"Hidden":"Normal";var otherStyle=hide?"Normal":"Hidden";
 Check(decoded.Contains("powershell.exe -WindowStyle \""+selectedStyle+"\"")&&!decoded.Contains("powershell.exe -WindowStyle \""+otherStyle+"\""),"PowerShell style "+selectedStyle+" keep="+keep);
 var cleanFiles=new[]{@"'C:\Windows\Panther\unattend.xml';",@"'C:\Windows\Panther\unattend-original.xml';",@"'C:\Windows\Setup\Scripts\Wifi.xml';"};
 Check(cleanFiles.All(path=>decoded.Contains(path)==!keep),"all three sensitive files cleanup keep="+keep);
 var roundTrip=PlanStore.Deserialize(PlanStore.Serialize(p,true));Check(roundTrip.HidePowerShellWindows==hide&&roundTrip.KeepSensitiveFiles==keep,"independent settings roundtrip hide="+hide+" keep="+keep);
}
p=Plan();p.AutoLogon=false;p.HidePowerShellWindows=true;p.KeepSensitiveFiles=false;Reject(p,"hidden window does not bypass sensitive cleanup login requirement");p.KeepSensitiveFiles=true;Check(!DecodedXml(Xml(p)).Contains(@"'C:\Windows\Panther\unattend.xml';"),"no-auto-logon explicit retention still works with hidden windows");
Console.WriteLine($"Final total: {count} tests passed.");

// OOBE 默认组合与隐私设置的实际 XML 映射。
p=Plan();xml=Xml(p);Check(Node(xml,"//u:OOBE/u:ProtectYourPC")=="3"&&Node(xml,"//u:OOBE/u:HideEULAPage")=="true"&&Node(xml,"//u:OOBE/u:HideOEMRegistrationScreen")=="true"&&Node(xml,"//u:OOBE/u:HideOnlineAccountScreens")=="true"&&Node(xml,"//u:OOBE/u:HideWirelessSetupInOOBE")=="true"&&xml.Contains("LocalAccount"),"new plan OOBE automation with express privacy disabled");
p.PrivacyMode="安装时手动选择";Check(Node(Xml(p),"//u:OOBE/u:ProtectYourPC")==null,"interactive privacy keeps OOBE choice");p.PrivacyMode="启用快捷设置";Check(Node(Xml(p),"//u:OOBE/u:ProtectYourPC")=="1","express privacy enable opt-in");
p.HideOEMRegistration=false;Check(Node(Xml(p),"//u:OOBE/u:HideOEMRegistrationScreen")=="false","OEM registration visibility toggle");
restored=PlanStore.Deserialize("{\"SchemaVersion\":1,\"Users\":[]}");Check(restored.PrivacyMode=="安装时手动选择"&&!restored.HideEula&&!restored.HideOEMRegistration,"legacy OOBE defaults retained");
Console.WriteLine($"Final combined total: {count} tests passed.");

// Recommendation policies are editable; applying/restoring never touches user credentials.
foreach(var system in new[]{"Windows 10","Windows 11"})foreach(var scene in new[]{"wired","wifi"}){
 p=Plan();Recommendations.Apply(p,scene,system);
 Check(Recommendations.Differences(p).Count==0&&p.BypassNetwork==(system=="Windows 11"),"recommendation baseline "+system+" "+scene);
 if(scene=="wifi"){p.Ssid="TestNetwork";p.WifiPassword="testpass123";}xml=Xml(p);
 Check(Node(xml,"//u:OOBE/u:ProtectYourPC")=="3"&&Node(xml,"//u:OOBE/u:HideWirelessSetupInOOBE")=="true","recommendation generated OOBE "+system+" "+scene);
 p.HideEula=false;p.PrivacyMode="安装时手动选择";Check(Recommendations.Differences(p).Count==2&&Recommendations.State(p).Contains("已修改"),"editable recommendation "+system+" "+scene);
 Recommendations.Restore(p,nameof(InstallPlan.HideEula));Check(p.HideEula&&p.PrivacyMode=="安装时手动选择"&&Recommendations.Differences(p).Count==1,"single setting restore "+system+" "+scene);
}
p=Plan();p.ComputerName="DESKTOP-TEST";p.EditionId="home";p.Architecture="arm64";p.Locale="en-US";p.Users[0].Password="DoNotReset123";p.Ssid="SavedWifi";p.WifiPassword="SavedPassword";p.CustomProductKey=fixtureKey;p.ActivationProductKey=fixtureKey;
Recommendations.Apply(p,"wifi","Windows 10");Check(p.ComputerName=="DESKTOP-TEST"&&p.EditionId=="home"&&p.Architecture=="arm64"&&p.Locale=="en-US"&&p.Users[0].Password=="DoNotReset123"&&p.Ssid=="SavedWifi"&&p.WifiPassword=="SavedPassword"&&p.CustomProductKey==fixtureKey&&p.ActivationProductKey==fixtureKey,"apply preserves identity, SKU, locale, architecture and secrets");
p.PrivacyMode="启用快捷设置";p.ConfigurationMode="自定义配置";Check(p.PrivacyMode=="启用快捷设置"&&Recommendations.State(p).StartsWith("自定义配置"),"custom mode does not overwrite choices");
restored=PlanStore.Deserialize(PlanStore.Serialize(p,false));Check(restored.ConfigurationMode=="自定义配置"&&restored.RecommendationId=="wifi"&&restored.TargetWindows=="Windows 10"&&Recommendations.Differences(restored).Count==1&&!restored.WifiPassword.Any(),"metadata and modified settings survive secret omission");
Check(!Recommendations.Summary(p).Contains("SavedPassword")&&!Recommendations.Summary(p).Contains(fixtureKey)&&!Recommendations.Summary(p).Contains("DoNotReset123"),"summary contains no secrets");
p.TargetWindows="Windows 11";Check(!Recommendations.Differences(p).Any(d=>d.Property==nameof(InstallPlan.TargetWindows)),"system preference is not a recommendation deviation");Check(Recommendations.Differences(p).Count==1,"only actual policy changes are differences");
restored=PlanStore.Deserialize("{\"SchemaVersion\":1,\"Users\":[]}");Check(restored.ConfigurationMode=="自定义配置"&&!Recommendations.HasBaseline(restored),"legacy schemes open as custom without pretending recommendation");
p=Plan();p.RecommendationVersion=999;Check(!Recommendations.HasBaseline(p)&&Recommendations.Differences(p).Count==0,"unknown recommendation version is not silently replaced");
Console.WriteLine($"Release total: {count} tests passed.");

var viewModel=new UnattendLocal.Desktop.MainViewModel();Check(viewModel.IsRecommended&&viewModel.RecommendationDifferences.Count==0,"view model starts with recommendation");
viewModel.Plan.HideEula=false;Check(viewModel.RecommendationDifferences.Any(d=>d.Property==nameof(InstallPlan.HideEula))&&viewModel.RecommendationState.Contains("已修改"),"view model updates differences on edit");
viewModel.Plan.ConfigurationMode="自定义配置";Check(!viewModel.IsRecommended&&!viewModel.Plan.HideEula,"mode switch preserves edited values");
viewModel.Plan=PlanStore.Deserialize("{\"SchemaVersion\":1,\"Users\":[]}");Check(!viewModel.IsRecommended&&viewModel.RecommendationDifferences.Count==0,"loaded legacy scheme refreshes recommendation view");
Recommendations.Apply(viewModel.Plan,"wifi","Windows 10");Check(viewModel.IsRecommended&&viewModel.IsPresetWifi&&viewModel.ConfigurationSummary.Contains("Windows 10"),"apply refreshes policy-dependent controls and summary");
viewModel.Plan.Users.Add(new UserEntry());Check(viewModel.ConfigurationSummary.Contains("预设 1 个账户"),"summary updates after account collection edit");
Check(Enumerable.Range(0,UnattendLocal.Desktop.HelpCatalog.PageCount).SelectMany(UnattendLocal.Desktop.HelpCatalog.ForPage).All(t=>!t.Body.Contains("原作者")&&!t.Body.Contains("上游")),"display help has no author recommendation attribution");
Console.WriteLine($"Full release total: {count} tests passed.");

// Installation targets: generate only, never execute destructive commands during tests.
p=Plan();Check(p.DiskMode==InstallationSettings.Manual&&!p.FormatExistingPartition&&!p.ConfirmWipeDisk&&!p.ConfirmFormatPartition&&p.ImageSelection=="由安装程序决定","safe defaults: manual installation and no destructive consent");
p.TargetDiskId=0;p.TargetPartitionId=3;p.ConfirmWipeDisk=true;p.FormatExistingPartition=true;p.ConfirmFormatPartition=true;xml=Xml(p);Check(Node(xml,"//u:InstallTo")==null&&!xml.Contains("diskpart.exe")&&Node(xml,"//u:DiskConfiguration")==null,"inactive destructive values cannot leak into manual mode");
p=Plan();p.DiskMode=InstallationSettings.Existing;Reject(p,"existing mode requires explicit disk");p.TargetDiskId=1;Reject(p,"existing mode requires explicit partition");p.TargetPartitionId=4;xml=Xml(p);Check(Node(xml,"//u:InstallTo/u:DiskID")=="1"&&Node(xml,"//u:InstallTo/u:PartitionID")=="4"&&Node(xml,"//u:DiskConfiguration")==null&&!xml.Contains("diskpart.exe"),"existing partition install does not format or wipe");
p.FormatExistingPartition=true;Reject(p,"formatting requires separate consent");p.ConfirmFormatPartition=true;xml=Xml(p);Check(Node(xml,"//u:ModifyPartition/u:Format")=="NTFS"&&Node(xml,"//u:Disk/u:WillWipeDisk")=="false"&&Node(xml,"//u:ModifyPartition/u:PartitionID")=="4"&&!xml.Contains("diskpart.exe"),"explicit format affects only selected partition without disk wipe");
p.TargetPartitionId=5;Check(!p.ConfirmFormatPartition,"changing target partition invalidates format consent");p.ConfirmFormatPartition=true;p.TargetDiskId=2;Check(!p.ConfirmFormatPartition,"changing target disk invalidates format consent");
p.TargetPartitionId=0;Reject(p,"partition zero rejected");p.TargetPartitionId=1;p.TargetDiskId=-1;Reject(p,"negative disk rejected");
foreach(var layout in new[]{"UEFI / GPT","BIOS / MBR"})foreach(var architecture in new[]{"amd64","x86"}){
 p=Plan();p.Architecture=architecture;p.DiskMode=InstallationSettings.Wipe;p.DiskLayout=layout;p.TargetDiskId=2;
 Reject(p,"wipe consent required "+layout+" "+architecture);p.ConfirmWipeDisk=true;xml=Xml(p);var script=Node(xml,"//u:component[@name='Microsoft-Windows-Setup']/u:RunSynchronous/u:RunSynchronousCommand[u:Description='清空指定磁盘并创建系统、Windows 与恢复分区']/u:Path")!;
 Check(script.Contains("echo select disk 2 & echo clean")&&script.Contains("diskpart.exe /s")&&script.EndsWith("\""),"wipe bound to explicit disk and quoted command "+layout+" "+architecture);
 Check(Node(xml,"//u:InstallTo/u:DiskID")=="2"&&Node(xml,"//u:InstallTo/u:PartitionID")== (layout=="UEFI / GPT"?"3":"2")&&Node(xml,"//u:OSImage/u:WillShowUI")=="OnError","automatic destination "+layout+" "+architecture);
 Check(script.Contains(layout=="UEFI / GPT"?"echo convert gpt":"echo convert mbr")&&script.Contains("echo shrink minimum=1536 desired=1536")&&script.Contains("label=Recovery"),"layout includes recovery after Windows "+layout+" "+architecture);
 p.ConfirmWipeDisk=true;p.TargetDiskId=3;Check(!p.ConfirmWipeDisk,"target change resets wipe consent "+layout+" "+architecture);
}
p=Plan();p.DiskMode=InstallationSettings.Wipe;p.ConfirmWipeDisk=true;Reject(p,"wipe never defaults to disk zero");p.TargetDiskId=0;p.ConfirmWipeDisk=true;p.DiskLayout="BIOS / MBR";Check(!p.ConfirmWipeDisk,"layout change resets wipe consent");p.ConfirmWipeDisk=true;p.DiskMode=InstallationSettings.Existing;Check(!p.ConfirmWipeDisk,"mode change resets wipe consent");
p=Plan();p.ImageSelection="按镜像索引";Reject(p,"image index required");p.ImageIndex=0;Reject(p,"image index starts at one");p.ImageIndex=6;xml=Xml(p);Check(Node(xml,"//u:MetaData/u:Key")=="/IMAGE/INDEX"&&Node(xml,"//u:MetaData/u:Value")=="6"&&Node(xml,"//u:InstallTo")==null,"image index preserves manual disk selection");
p.ImageSelection="按镜像名称";Reject(p,"image name required");p.ImageName="  Windows 11 Pro & Test  ";xml=Xml(p);Check(Node(xml,"//u:MetaData/u:Value")=="Windows 11 Pro & Test"&&Node(xml,"//u:MetaData/u:Key")=="/IMAGE/NAME","image name safely XML escaped");p.ImageName="bad\nname";Reject(p,"image name control characters rejected");
p=Plan();p.ImageSelection="按镜像索引";p.ImageIndex=2;p.DiskMode=InstallationSettings.Wipe;p.TargetDiskId=1;p.ConfirmWipeDisk=true;
foreach(var secrets in new[]{false,true}){saved=PlanStore.Serialize(p,secrets);restored=PlanStore.Deserialize(saved);Check(restored.DiskMode==InstallationSettings.Wipe&&restored.TargetDiskId==1&&restored.ImageIndex==2&&!restored.ConfirmWipeDisk,"stored destructive intent requires renewed consent secrets="+secrets);restored.Users[0].Password="Password123";restored.Users[0].NeedsPassword=false;Reject(restored,"reloaded wipe blocked without acknowledgement secrets="+secrets);}
restored=PlanStore.Deserialize("{\"SchemaVersion\":1,\"DiskMode\":\"清空指定磁盘后自动分区\",\"TargetDiskId\":0,\"ConfirmWipeDisk\":true,\"ConfirmFormatPartition\":true}");Check(!restored.ConfirmWipeDisk&&!restored.ConfirmFormatPartition,"file supplied confirmations cannot authorize deletion");
p=Plan();p.DiskMode=InstallationSettings.Existing;p.TargetDiskId=0;p.TargetPartitionId=3;p.FormatExistingPartition=true;p.ConfirmFormatPartition=true;restored=PlanStore.Deserialize(PlanStore.Serialize(p,true));Check(restored.FormatExistingPartition&&!restored.ConfirmFormatPartition,"format intent persists but acknowledgement does not");
Recommendations.Apply(p,"wifi");Check(p.DiskMode==InstallationSettings.Manual&&!p.FormatExistingPartition&&!p.ConfirmFormatPartition&&p.RecommendationVersion==3,"recommendation restore always clears destructive mode and consent");p.TargetWindows="Windows 10";Check(Recommendations.Differences(p).Count==0&&!Recommendations.State(p).Contains("Windows 11"),"common recommendation has no system preference deviation");
p.DiskMode=InstallationSettings.Existing;Check(Recommendations.Differences(p).Any(d=>d.Property==nameof(InstallPlan.DiskMode)),"explicit automatic disk mode shown as actual adjustment");Recommendations.Restore(p,nameof(InstallPlan.DiskMode));Check(p.DiskMode==InstallationSettings.Manual,"single disk recommendation restore is safe");
viewModel.Plan=p;viewModel.Plan.DiskMode=InstallationSettings.Wipe;Check(viewModel.IsWipeDisk&&viewModel.IsDestructiveDisk&&viewModel.DiskRisk.Contains("危险"),"risk display follows destructive mode");viewModel.Plan.DiskMode=InstallationSettings.Existing;Check(viewModel.IsExistingDisk&&!viewModel.IsDestructiveDisk,"existing partition mode initially has no formatting risk");viewModel.Plan.FormatExistingPartition=true;Check(viewModel.IsFormatDisk&&viewModel.IsDestructiveDisk,"format risk immediately visible");
Console.WriteLine($"Pre7 release total: {count} tests passed.");

p=Plan();p.DiskMode=InstallationSettings.Wipe;p.TargetDiskId=0;p.ConfirmWipeDisk=true;p.RecoverySizeMB=2048;Check(!p.ConfirmWipeDisk,"recovery size change invalidates wipe confirmation");p.ConfirmWipeDisk=true;Check(Xml(p).Contains("shrink minimum=2048 desired=2048"),"custom recovery size mapped into fixed diskpart script");p.RecoverySizeMB=100;Reject(p,"undersized recovery partition rejected");p.RecoverySizeMB=1536;p.DiskLayout="BIOS / MBR";p.Architecture="arm64";p.ConfirmWipeDisk=true;Reject(p,"ARM64 legacy BIOS layout rejected");p.DiskLayout="UEFI / GPT";p.ConfirmWipeDisk=true;Check(Node(Xml(p),"//u:InstallTo/u:PartitionID")=="3","ARM64 GPT destination generated");
Console.WriteLine($"Final pre7 total: {count} tests passed.");

// Custom defaults require preparation in the tool, rather than interactive installation choices.
p=InstallPlan.NewCustom();
Check(p.ConfigurationMode=="自定义配置"&&p.CustomStartingPoint=="基础配置"&&p.RecommendationId==""&&p.RecommendationVersion==0&&p.Users.Count==0,"custom stays independent without a prefilled account or recommendation");
Check(p.EditionMode=="按 SKU 使用通用安装密钥"&&p.EditionId==""&&p.AccountMode=="自动创建本地账户"&&p.AutoLogon&&p.PasswordExpiration=="Windows 默认","custom automatic operation modes with explicit SKU and account preparation");
Check(!p.InteractiveLanguage&&p.TimeZoneMode=="由 Windows 决定"&&p.TimeZone==""&&p.ImageLanguage==""&&p.Locale==""&&p.Keyboard==""&&p.GeoId=="","custom fixed language is default but environment values stay unfilled");
Check(p.WifiMode=="隐藏无线网络选择页"&&!p.ConnectWifi&&p.BypassNetwork&&p.PrivacyMode=="关闭快捷隐私设置"&&p.HideEula&&p.HideOnlineAccount&&p.HideOEMRegistration,"custom OOBE and network defaults avoid prompts");
Check(p.DiskMode==InstallationSettings.Manual&&p.TargetDiskId==null&&!p.FormatExistingPartition&&!p.ConfirmWipeDisk&&!p.BypassHardware&&p.HidePowerShellWindows,"safe disk defaults preserved");
Check(Recommendations.MissingInputs(p).Contains("显示语言")&&Recommendations.MissingInputs(p).Contains("SKU")&&Recommendations.MissingInputs(p).Contains("本地账户"),"custom summary lists required preparation");
Check(Recommendations.InteractiveSteps(p)=="磁盘与分区（安装目标可能需要选择或核对）","custom summary accurately discloses remaining deliberate installation interaction");
Reject(p,"unfilled custom plan cannot silently generate interactive XML");
p.ImageLanguage="zh-CN";p.Locale="zh-Hans-CN";p.Keyboard=InstallPlan.New().Keyboard;p.GeoId="45";
Reject(p,"automatic account needs a real user");p.Users.Add(new(){Name="",Password="Password123"});Reject(p,"blank custom account name rejected");p.Users[0].Name="CustomUser";Reject(p,"custom generic key requires an actual SKU selection");p.EditionId="home";xml=Xml(p);
Check(Node(xml,"//u:LocalAccount/u:Name")=="CustomUser"&&Node(xml,"//u:OOBE/u:ProtectYourPC")=="3"&&Node(xml,"//u:UserData/u:ProductKey/u:WillShowUI")!="Always"&&Node(xml,"//u:OOBE/u:HideWirelessSetupInOOBE")=="true","filled custom plan writes actual automatic XML");
Check(Node(xml,"//u:TimeZone")==null&&Node(xml,"//u:InstallTo")==null&&!xml.Contains("diskpart.exe"),"implicit timezone and manual disk remain safe");
Check(!Recommendations.MissingInputs(p).Contains("账户密码")&&!Recommendations.Summary(p).Contains("Password123"),"readiness summary never includes credential values");
restored=PlanStore.Deserialize(PlanStore.Serialize(p,true));Check(restored.CustomStartingPoint=="基础配置"&&!restored.InteractiveLanguage&&restored.Users[0].Name=="CustomUser"&&restored.RecommendationId=="","new custom defaults survive save and load");
p.InteractiveLanguage=true;p.EditionMode="安装时输入密钥";p.AccountMode="安装时创建本地账户";p.WifiMode="安装时手动选择";p.PrivacyMode="安装时手动选择";p.HideEula=false;xml=Xml(p);
Check(Node(xml,"//u:OOBE/u:ProtectYourPC")==null&&Node(xml,"//u:UserData/u:ProductKey/u:WillShowUI")=="Always"&&Node(xml,"//u:OOBE/u:HideWirelessSetupInOOBE")=="false","manual installation choices still work when explicitly selected");
Check(new[]{"语言与区域","产品密钥与版本","账户创建","无线网络","快捷隐私","许可协议"}.All(Recommendations.InteractiveSteps(p).Contains),"summary enumerates explicitly selected interaction");
p.TimeZoneMode="指定时区";Reject(p,"fixed timezone requires choice");p.TimeZone="China Standard Time";Check(Node(Xml(p),"//u:TimeZone")=="China Standard Time","fixed timezone is written after selection");p.TimeZoneMode="bad";Reject(p,"unknown timezone mode rejected");
restored=PlanStore.Deserialize("{\"SchemaVersion\":1,\"Users\":[]}");Check(restored.TimeZoneMode=="指定时区"&&restored.CustomStartingPoint=="保留当前设置"&&restored.EditionMode=="安装时输入密钥"&&restored.WifiMode=="安装时手动选择","old saved plans retain compatibility choices");

// Scene selection, restoration, unrelated edits, and dependent UI states.
viewModel=new UnattendLocal.Desktop.MainViewModel();viewModel.Plan.Users[0].Password="AccountFixture123";viewModel.Plan.Ssid="SceneFixture";viewModel.Plan.WifiPassword="WifiFixture123";
viewModel.Plan.PrivacyMode="启用快捷设置";viewModel.Plan.HideEula=false;
viewModel.Plan.DiskMode=InstallationSettings.Existing;viewModel.Plan.TargetDiskId=2;viewModel.Plan.TargetPartitionId=3;
viewModel.PendingRecommendation="wifi";
Check(viewModel.Plan.WifiMode=="预设 WPA2 Wi-Fi"&&viewModel.Plan.ConnectWifi&&viewModel.IsPresetWifi&&viewModel.Plan.RecommendationId=="wifi","selecting Wi-Fi immediately changes actual network policy and UI");
Check(viewModel.Plan.PrivacyMode=="启用快捷设置"&&!viewModel.Plan.HideEula&&viewModel.Plan.DiskMode==InstallationSettings.Existing&&viewModel.Plan.TargetDiskId==2,"scene selection preserves unrelated user edits and disk target");
Check(viewModel.Plan.Ssid=="SceneFixture"&&viewModel.Plan.WifiPassword=="WifiFixture123"&&viewModel.Plan.Users[0].Password=="AccountFixture123","scene switching preserves all credentials");
xml=Xml(viewModel.Plan);Check(Node(xml,"//u:OOBE/u:HideWirelessSetupInOOBE")=="true"&&DecodedXml(xml).Contains("SceneFixture"),"scene selection reaches exported XML without a separate apply click");
viewModel.PendingRecommendation="wired";Check(!viewModel.IsPresetWifi&&viewModel.Plan.WifiMode=="隐藏无线网络选择页"&&!viewModel.Plan.ConnectWifi,"wired scene immediately disables preset Wi-Fi controls and connection");
viewModel.PendingRecommendation="wifi";viewModel.Plan.ConnectWifi=false;Check(!viewModel.Plan.ConnectWifi,"user can deliberately disable Wi-Fi autoconnection");Recommendations.Restore(viewModel.Plan,nameof(InstallPlan.ConnectWifi));Check(viewModel.Plan.ConnectWifi,"restoring network option uses selected scene baseline");
Recommendations.Apply(viewModel.Plan,"wired");Check(viewModel.PendingRecommendation=="wired"&&!viewModel.IsPresetWifi&&viewModel.Plan.DiskMode==InstallationSettings.Manual,"full restore synchronizes scene selector, network, and disk safety");
viewModel.Plan.ConfigurationMode="自定义配置";viewModel.PendingRecommendation="wifi";Check(viewModel.Plan.WifiMode=="隐藏无线网络选择页","hidden recommendation selection does not mutate custom configuration");
viewModel.Plan.WifiMode="预设 WPA2 Wi-Fi";Check(viewModel.Plan.ConnectWifi&&viewModel.IsPresetWifi,"manual switch into preset Wi-Fi defaults to automatic connection");
viewModel.Plan=InstallPlan.NewCustom();Check(viewModel.IsCustomConfiguration&&!viewModel.IsRecommended&&viewModel.IsFixedLanguage&&!viewModel.IsFixedTimeZone&&viewModel.IsAutomaticAccounts&&viewModel.IsGenericEdition&&!viewModel.IsPresetWifi,"new custom UI enablement matches actual defaults");
viewModel.Plan.InteractiveLanguage=true;Check(!viewModel.IsFixedLanguage&&viewModel.HelpTips["language"].Body.StartsWith("当前：安装时"),"language control and help refresh immediately");viewModel.Plan.InteractiveLanguage=false;viewModel.Plan.TimeZoneMode="指定时区";Check(viewModel.IsFixedLanguage&&viewModel.IsFixedTimeZone,"explicit language and timezone choices enable required fields");
viewModel.Plan.PasswordExpiration="指定天数";Check(viewModel.IsPasswordDays,"custom day control enabled only for explicit day policy");viewModel.Plan.PasswordExpiration="Windows 默认";Check(!viewModel.IsPasswordDays&&viewModel.HelpTips["expiration"].Body.Contains("当前：Windows 默认"),"default policy disables irrelevant numeric input and updates hint");
viewModel.Plan.PasswordExpiration="指定天数";viewModel.Plan.AccountMode="安装时添加微软账户";
Check(!viewModel.IsAutomaticAccounts&&!viewModel.IsPasswordDays&&!viewModel.CanBypassNetwork&&!viewModel.CanHideOnlineAccount&&!viewModel.Plan.BypassNetwork&&!viewModel.Plan.HideOnlineAccount,"Microsoft account synchronizes online and offline settings across pages");
viewModel.Plan.BypassNetwork=true;viewModel.Plan.HideOnlineAccount=true;Check(!viewModel.Plan.BypassNetwork&&!viewModel.Plan.HideOnlineAccount,"conflicting online settings cannot remain active after edit");
viewModel.Plan.AccountMode="自动创建本地账户";Check(viewModel.IsAutomaticAccounts&&viewModel.CanBypassNetwork&&viewModel.Plan.HideOnlineAccount&&viewModel.IsPasswordDays,"returning to local accounts restores applicable control states");
viewModel.Plan.EditionMode="指定自己的产品密钥";Check(viewModel.IsCustomEdition&&!viewModel.IsGenericEdition,"custom key panel follows edition selection");viewModel.Plan.EditionMode="按 SKU 使用通用安装密钥";viewModel.Plan.EditionId="pro";Check(viewModel.IsGenericEdition&&!viewModel.IsCustomEdition&&viewModel.SelectedEditionKey==genericKeys["pro"],"SKU key display synchronizes after switching back");
viewModel.Plan.ImageSelection="按镜像名称";Check(viewModel.IsImageName&&!viewModel.IsImageIndex,"image name field follows selection");viewModel.Plan.ImageSelection="按镜像索引";Check(!viewModel.IsImageName&&viewModel.IsImageIndex,"image index field follows selection");
viewModel.Plan.DiskMode=InstallationSettings.Wipe;viewModel.Plan.TargetDiskId=0;viewModel.Plan.ConfirmWipeDisk=true;viewModel.Plan.Architecture="arm64";Check(!viewModel.Plan.ConfirmWipeDisk,"changing architecture invalidates destructive disk consent");

// Changes to all users invalidate a previous preview, including unselected users; old subscriptions are detached.
viewModel.Plan=Plan();var second=new UserEntry{Name="Second",Password="SecondFixture123"};viewModel.Plan.Users.Add(second);viewModel.SelectedUser=viewModel.Plan.Users[0];
viewModel.Preview="OLD PREVIEW";second.Password="NewFixture123";Check(viewModel.Preview!="OLD PREVIEW"&&!viewModel.Preview.Contains("NewFixture123"),"unselected user edits invalidate preview without exposing secrets");
viewModel.Plan.Users.Remove(second);viewModel.Preview="KEEP";second.Name="Detached";Check(viewModel.Preview=="KEEP","removed users cannot update the current view model");
var previousPlan=viewModel.Plan;var previousUser=previousPlan.Users[0];viewModel.Plan=InstallPlan.NewCustom();viewModel.Preview="KEEP";previousPlan.HideEula=false;previousUser.Password="Detached123";Check(viewModel.Preview=="KEEP","replacing a plan detaches old plan and user observers");
viewModel.Plan.Users.Add(new(){Name="First",Password="Fixture123"});Check(viewModel.SelectedUser==viewModel.Plan.Users[0],"first added user automatically becomes selected");viewModel.Plan.Users.Clear();Check(viewModel.SelectedUser==null,"clearing users clears stale selection");
viewModel.Plan=PlanStore.Deserialize("{\"SchemaVersion\":1,\"Users\":[],\"AccountMode\":\"安装时添加微软账户\",\"BypassNetwork\":true,\"HideOnlineAccount\":true}");Check(viewModel.Plan.BypassNetwork&&viewModel.Plan.HideOnlineAccount,"loading old saved choices does not silently normalize them");Reject(viewModel.Plan,"loaded conflicting old values still fail generation checks");
foreach(var name in new[]{nameof(InstallPlan.PrivacyMode),nameof(InstallPlan.HideEula),nameof(InstallPlan.HideOEMRegistration),nameof(InstallPlan.HidePowerShellWindows),nameof(InstallPlan.KeepSensitiveFiles)}){
 viewModel.Plan=Plan();viewModel.Preview="OLD PREVIEW";var prop=typeof(InstallPlan).GetProperty(name)!;
 prop.SetValue(viewModel.Plan,prop.PropertyType==typeof(bool)?!(bool)prop.GetValue(viewModel.Plan)!:"启用快捷设置");
 Check(viewModel.Preview!="OLD PREVIEW","invalidate preview after "+name);
}
p=Plan();p.AccountMode="安装时创建本地账户";p.PasswordExpiration="指定天数";p.PasswordDays=1000;Check(!DecodedXml(Xml(p)).Contains("/maxpwage:1000"),"inactive automatic-account password day setting does not block or affect export");
viewModel.Plan=Plan();viewModel.Plan.AutoLogon=false;Check(viewModel.NeedsRetentionConsent&&!viewModel.Plan.KeepSensitiveFiles,"turning off auto logon requests retention consent without silently retaining secrets");viewModel.Plan.KeepSensitiveFiles=true;Check(!viewModel.NeedsRetentionConsent,"retention guidance follows OOBE setting");
// Chinese labels never change selection IDs or the installation configuration.
var upstream=new Schneegans.Unattend.UnattendGenerator();
var groups=new[]{
 ("显示语言",g.Languages,upstream.ImageLanguages.Values.Select(x=>x.Id).ToList()),
 ("区域格式",g.Locales,upstream.UserLocales.Values.Select(x=>x.Id).ToList()),
 ("键盘输入法",g.Keyboards,upstream.KeyboardIdentifiers.Values.Select(x=>x.Id).ToList()),
 ("国家地区",g.Regions,upstream.GeoLocations.Values.Select(x=>x.Id).ToList()),
 ("时区",g.TimeZones,upstream.TimeOffsets.Values.Select(x=>x.Id).ToList())};
foreach(var (label,choices,ids) in groups){
 Check(choices.Select(c=>c.Id).SequenceEqual(ids),label+" keeps upstream IDs and order");
 Check(choices.All(c=>c.Label.Any(ch=>ch>='\u3400'&&ch<='\u9fff')),label+" has Chinese display labels for every choice");
}
Check(g.Languages.Single(x=>x.Id=="zh-CN").Label=="简体中文","simplified Chinese display name");
Check(g.Locales.Single(x=>x.Id=="zh-Hans-CN").Label=="中文（简体，中国）","Chinese locale display name");
Check(g.Keyboards.Single(x=>x.Id==InstallPlan.New().Keyboard).Label=="中文（简体，中国） 微软拼音输入法","Microsoft Pinyin input method display name");
Check(g.Regions.Single(x=>x.Id=="45").Label=="中国","China region display name");
Check(g.TimeZones.Single(x=>x.Id=="China Standard Time").Label=="(UTC+08:00) 北京、重庆、香港、乌鲁木齐","Chinese timezone display name retains source offset");
Check(g.TimeZones.All(c=>c.Label.Split(')')[0]==upstream.TimeOffsets[c.Id].DisplayName.Split(')')[0]),"all timezone labels keep fixed upstream UTC offsets");
Check(ChoiceLocalization.Architecture("amd64")=="x64（Intel / AMD 64 位）"&&ChoiceLocalization.Architecture("arm64").Contains("64 位")&&ChoiceLocalization.Architecture("x86").Contains("32 位"),"architecture labels explain bitness in Chinese");
p=Plan();p.ImageLanguage=g.Languages.Single(x=>x.Id=="zh-CN").Id;p.Locale=g.Locales.Single(x=>x.Id=="zh-Hans-CN").Id;p.GeoId=g.Regions.Single(x=>x.Id=="45").Id;p.TimeZone=g.TimeZones.Single(x=>x.Id=="China Standard Time").Id;p.Keyboard=g.Keyboards.Single(x=>x.Id==p.Keyboard).Id;xml=Xml(p);
Check(Node(xml,"//u:UILanguage")=="zh-CN"&&Node(xml,"//u:UserLocale")=="zh-Hans-CN"&&Node(xml,"//u:TimeZone")=="China Standard Time","localized choices write the original Windows locale and timezone identifiers");
restored=PlanStore.Deserialize(PlanStore.Serialize(p,true));Check(restored.ImageLanguage==p.ImageLanguage&&restored.Locale==p.Locale&&restored.Keyboard==p.Keyboard&&restored.GeoId==p.GeoId&&restored.TimeZone==p.TimeZone,"saved selections remain compatible across Chinese display update");
viewModel.Plan=p;Check(viewModel.ConfigurationSummary.Contains("北京、重庆、香港、乌鲁木齐")&&!viewModel.ConfigurationSummary.Contains("China Standard Time")&&viewModel.HelpTips["timezone"].Body.Contains("北京"),"summary and timezone help use Chinese display labels too");
// Prevent automatic device encryption through the upstream specialize-stage setting.
const string encryptionCommand=@"reg.exe add ""HKLM\SYSTEM\CurrentControlSet\Control\BitLocker"" /v ""PreventDeviceEncryption"" /t REG_DWORD /d 1 /f;";
Check(InstallPlan.New().PreventDeviceEncryption&&InstallPlan.NewCustom().PreventDeviceEncryption,"recommended and independent custom plans prevent automatic device encryption by default");
foreach(var scene in new[]{"wired","wifi"}){
 p=Plan();Recommendations.Apply(p,scene);Check(p.PreventDeviceEncryption&&p.RecommendationVersion==3&&Recommendations.Differences(p).Count==0,"new recommendation baseline includes encryption prevention "+scene);
}
foreach(var arch in new[]{"amd64","arm64","x86"}){
 p=Plan();p.Architecture=arch;xml=Xml(p);var specialize=Node(xml,"//*[local-name()='File' and contains(@path,'Specialize.ps1')]")??"";
 Check(specialize.Contains(encryptionCommand),"device encryption registry command emitted for "+arch);
 Check(Node(xml,"//u:settings[@pass='specialize']//u:Path[contains(.,'Specialize.ps1')]")!=null,"encryption prevention script invoked during specialize for "+arch);
 p.PreventDeviceEncryption=false;Check(!DecodedXml(Xml(p)).Contains("PreventDeviceEncryption"),"unchecked option omits registry override for "+arch);
}
p=Plan();p.PreventDeviceEncryption=true;p.AutoLogon=false;p.KeepSensitiveFiles=true;Check(DecodedXml(Xml(p)).Contains(encryptionCommand),"prevention runs independently of first automatic logon");
p.HidePowerShellWindows=false;Check(DecodedXml(Xml(p)).Contains(encryptionCommand),"visible script windows do not alter encryption prevention");
foreach(var enabled in new[]{false,true}){
 p=Plan();p.PreventDeviceEncryption=enabled;
 foreach(var includeSecrets in new[]{false,true}){
  restored=PlanStore.Deserialize(PlanStore.Serialize(p,includeSecrets));Check(restored.PreventDeviceEncryption==enabled,"explicit encryption setting survives scheme save enabled="+enabled+" secrets="+includeSecrets);
 }
}
restored=PlanStore.Deserialize("{\"SchemaVersion\":1,\"Users\":[]}");Check(!restored.PreventDeviceEncryption,"older plans missing encryption setting keep their prior behavior");
p=Plan();p.RecommendationVersion=2;p.PreventDeviceEncryption=false;Check(Recommendations.HasBaseline(p)&&Recommendations.Differences(p).Count==0,"version 2 recommendation baseline remains historically accurate");
Recommendations.Restore(p,nameof(InstallPlan.PreventDeviceEncryption));Check(!p.PreventDeviceEncryption,"single restore respects older recommendation baseline");Recommendations.Apply(p,"wired");Check(p.PreventDeviceEncryption&&p.RecommendationVersion==3,"full restore explicitly upgrades old recommendation to prevention default");
viewModel=new UnattendLocal.Desktop.MainViewModel();viewModel.Preview="OLD PREVIEW";viewModel.Plan.PreventDeviceEncryption=false;
Check(viewModel.Preview!="OLD PREVIEW"&&viewModel.RecommendationDifferences.Any(d=>d.Property==nameof(InstallPlan.PreventDeviceEncryption)),"checkbox updates recommendation differences and invalidates preview");
Check(viewModel.ConfigurationSummary.Contains("设备自动加密：遵循 Windows 默认行为")&&viewModel.HelpTips["deviceencryption"].Body.StartsWith("当前：遵循"),"disabled checkbox updates summary and current tooltip");Recommendations.Restore(viewModel.Plan,nameof(InstallPlan.PreventDeviceEncryption));
Check(viewModel.Plan.PreventDeviceEncryption&&viewModel.ConfigurationSummary.Contains("设备自动加密：禁止自动启用")&&viewModel.HelpTips["deviceencryption"].Body.StartsWith("当前：禁止"),"single restore immediately refreshes prevention state everywhere");
viewModel.Plan.PreventDeviceEncryption=false;viewModel.PendingRecommendation="wifi";Check(!viewModel.Plan.PreventDeviceEncryption,"network-only scene switching preserves explicit encryption choice");Recommendations.Apply(viewModel.Plan,"wifi");Check(viewModel.Plan.PreventDeviceEncryption,"full Wi-Fi recommendation restore enables prevention");
viewModel.Plan=InstallPlan.NewCustom();viewModel.Plan.PreventDeviceEncryption=false;Check(!viewModel.IsRecommended&&viewModel.ConfigurationSummary.Contains("遵循 Windows 默认行为"),"custom checkbox remains independently adjustable");
p=Plan();var preventionXml=DecodedXml(Xml(p));Check(!preventionXml.Contains("manage-bde",StringComparison.OrdinalIgnoreCase)&&!preventionXml.Contains("Disable-BitLocker",StringComparison.OrdinalIgnoreCase)&&!preventionXml.Contains(encryptionCommand.Replace("/d 1","/d 0")),"prevention does not generate decryption commands or inverse encryption override");
Console.WriteLine($"Final pre12 total: {count} tests passed.");
// Shell layout generation, active-document validation and cross-page state.
Check(new[]{InstallPlan.New(),InstallPlan.NewCustom()}.All(q=>q.TaskbarSearch=="完整搜索框"&&q.TaskbarIconsMode=="使用默认图标"&&q.StartTilesMode=="使用默认磁贴"&&q.StartPinsMode=="使用默认固定应用"&&!q.DisableWidgets&&!q.LeftTaskbar&&!q.HideTaskViewButton&&!q.ShowAllTrayIcons&&!q.DisableBingResults),"new recommended and custom shell choices follow upstream defaults");
restored=PlanStore.Deserialize("{\"SchemaVersion\":1,\"Users\":[]}");Check(restored.TaskbarSearch=="完整搜索框"&&restored.TaskbarIconsMode=="使用默认图标"&&restored.StartTilesMode=="使用默认磁贴"&&restored.StartPinsMode=="使用默认固定应用","legacy plans missing shell fields keep prior default output");
foreach(var arch in new[]{"amd64","arm64","x86"}){
 foreach(var (mode,value) in new[]{("完整搜索框",2),("图标和文字",3),("仅图标",1),("隐藏",0)}){
  p=Plan();p.Architecture=arch;p.TaskbarSearch=mode;var output=DecodedXml(Xml(p));
  Check(value==2?!output.Contains("SearchboxTaskbarMode"):output.Contains($"-Name 'SearchboxTaskbarMode' -Type 'DWord' -Value {value};"),"search output retains upstream mode "+mode+" "+arch);
 }
 p=Plan();p.Architecture=arch;xml=Xml(p);
 Check(Node(xml,"//*[local-name()='File' and contains(@path,'TaskbarLayoutModification.xml')]")==null&&Node(xml,"//*[local-name()='File' and contains(@path,'SetStartPins.ps1')]")==null&&Node(xml,"//*[local-name()='File' and contains(@path,'Shell\\LayoutModification.xml')]")==null,"default shell layouts add no override "+arch);
 p.TaskbarIconsMode="移除全部固定图标";p.StartTilesMode="移除全部磁贴";p.StartPinsMode="移除全部固定应用";xml=Xml(p);
 var taskbarDoc=new XmlDocument();taskbarDoc.LoadXml(Node(xml,"//*[local-name()='File' and contains(@path,'TaskbarLayoutModification.xml')]" )!);
 Check(taskbarDoc.SelectSingleNode("//*[local-name()='TaskbarPinList']")!.ChildNodes.OfType<XmlElement>().All(e=>e.LocalName=="DesktopApp"&&e.GetAttribute("DesktopApplicationLinkPath")=="#leaveempty")&&DecodedXml(xml).Contains("Register-ScheduledTask -TaskName 'UnlockStartLayout'"),"empty taskbar uses upstream placeholder and unlock task "+arch);
 Check(Node(xml,"//*[local-name()='File' and contains(@path,'SetStartPins.ps1')]" )!.Contains("{\"pinnedList\":[]}")&&Node(xml,"//*[local-name()='File' and contains(@path,'SetStartPins.ps1')]" )!.Contains("Version.Build -lt 20000"),"empty Windows 11 pins retain Windows version guard "+arch);
 Check(Node(xml,"//*[local-name()='File' and contains(@path,'Shell\\LayoutModification.xml')]" )!.Contains("StartLayout")&&!Node(xml,"//*[local-name()='File' and contains(@path,'Shell\\LayoutModification.xml')]" )!.Contains("DesktopApplicationTile"),"empty Windows 10 tiles embedded "+arch);
 p.TaskbarIconsMode=ShellSettings.CustomXml;p.TaskbarIconsXml=ShellSettings.TaskbarExample;p.StartTilesMode=ShellSettings.CustomXml;p.StartTilesXml=ShellSettings.TilesExample;p.StartPinsMode=ShellSettings.CustomJson;p.StartPinsJson=ShellSettings.PinsExample;xml=Xml(p);
 Check(Node(xml,"//*[local-name()='File' and contains(@path,'TaskbarLayoutModification.xml')]" )!.Contains("Microsoft.Windows.Explorer")&&Node(xml,"//*[local-name()='File' and contains(@path,'Shell\\LayoutModification.xml')]" )!.Contains("DesktopApplicationTile")&&Node(xml,"//*[local-name()='File' and contains(@path,'SetStartPins.ps1')]" )!.Contains("desktopAppLink"),"all custom layout documents generated together "+arch);
 Check(Node(xml,"//u:InstallTo")==null&&!DecodedXml(xml).Contains("diskpart.exe"),"shell layout changes preserve safe manual disk mode "+arch);
}
foreach(var (property,marker) in new[]{(nameof(InstallPlan.DisableWidgets),"AllowNewsAndInterests"),(nameof(InstallPlan.LeftTaskbar),"TaskbarAl"),(nameof(InstallPlan.HideTaskViewButton),"ShowTaskViewButton"),(nameof(InstallPlan.ShowAllTrayIcons),"EnableAutoTray"),(nameof(InstallPlan.DisableBingResults),"DisableSearchBoxSuggestions")}){
 p=Plan();Check(!DecodedXml(Xml(p)).Contains(marker),"unchecked shell switch leaves Windows default "+property);
 typeof(InstallPlan).GetProperty(property)!.SetValue(p,true);Check(DecodedXml(Xml(p)).Contains(marker),"shell switch mapped to generated setting "+property);
}
p=Plan();p.ShowAllTrayIcons=true;Check(Node(Xml(p),"//*[local-name()='File' and contains(@path,'ShowAllTrayIcons.xml')]" )!=null,"show all tray icons embeds Windows 11 scheduled task definition");
p=Plan();p.TaskbarIconsMode=ShellSettings.CustomXml;p.TaskbarIconsXml=ShellSettings.TaskbarExample;p.StartTilesMode=ShellSettings.CustomXml;p.StartTilesXml=ShellSettings.TilesExample;p.StartPinsMode=ShellSettings.CustomJson;p.StartPinsJson=ShellSettings.PinsExample;p.TaskbarSearch="图标和文字";p.LeftTaskbar=true;p.DisableWidgets=true;p.HideTaskViewButton=true;p.ShowAllTrayIcons=true;p.DisableBingResults=true;
foreach(var includeSecrets in new[]{false,true}){
 restored=PlanStore.Deserialize(PlanStore.Serialize(p,includeSecrets));
 Check(restored.TaskbarIconsXml==p.TaskbarIconsXml&&restored.StartTilesXml==p.StartTilesXml&&restored.StartPinsJson==p.StartPinsJson&&restored.TaskbarSearch==p.TaskbarSearch&&restored.LeftTaskbar&&restored.DisableWidgets&&restored.HideTaskViewButton&&restored.ShowAllTrayIcons&&restored.DisableBingResults&&restored.TaskbarIconsMode==ShellSettings.CustomXml&&restored.StartTilesMode==ShellSettings.CustomXml&&restored.StartPinsMode==ShellSettings.CustomJson,"all shell choices and documents roundtrip secrets="+includeSecrets);
}
Recommendations.Apply(p,"wired");Check(p.TaskbarSearch=="完整搜索框"&&!p.LeftTaskbar&&!p.DisableWidgets&&!p.HideTaskViewButton&&!p.ShowAllTrayIcons&&!p.DisableBingResults&&p.TaskbarIconsMode=="使用默认图标"&&p.StartTilesMode=="使用默认磁贴"&&p.StartPinsMode=="使用默认固定应用"&&p.TaskbarIconsXml==ShellSettings.TaskbarExample&&p.StartTilesXml==ShellSettings.TilesExample&&p.StartPinsJson==ShellSettings.PinsExample,"full recommendation resets shell policies but preserves user layout documents");
void RejectLayout(string kind,string content,string name){try{ShellSettings.ValidateDocument(kind,content);}catch(ArgumentException e){Check(e.Message.Any(c=>c>='\u3400'&&c<='\u9fff'),name);return;}throw new Exception("Expected layout rejection: "+name);}
foreach(var kind in new[]{"taskbar","tiles","pins"})RejectLayout(kind,"","empty custom "+kind+" has Chinese error");
RejectLayout("taskbar","<wrong />","wrong taskbar root rejected");RejectLayout("tiles",ShellSettings.TaskbarExample,"taskbar document cannot stand in for tiles");RejectLayout("taskbar",ShellSettings.TilesExample,"tiles document cannot stand in for taskbar");
RejectLayout("taskbar",ShellSettings.TaskbarExample.Replace("Version=\"1\"","Version=\"2\""),"wrong XML layout version rejected");
RejectLayout("taskbar",ShellSettings.TaskbarExample.Replace("PinListPlacement=\"Replace\"","PinListPlacement=\"Wrong\""),"unknown pin placement rejected");
RejectLayout("taskbar",ShellSettings.TaskbarExample.Replace("DesktopApplicationID=\"Microsoft.Windows.Explorer\"","DesktopApplicationID=\"\""),"blank desktop app identifier rejected");
RejectLayout("taskbar",ShellSettings.TaskbarExample.Replace("<taskbar:DesktopApp DesktopApplicationID=\"Microsoft.Windows.Explorer\" />","<taskbar:Unexpected />"),"unknown taskbar pin type rejected");
RejectLayout("taskbar","<!DOCTYPE LayoutModificationTemplate [<!ENTITY x SYSTEM 'file:///etc/hostname'>]>"+ShellSettings.TaskbarExample,"external XML entities rejected before generation");
foreach(var content in new[]{"{","[]","{}","{\"pinnedList\":{}}","{\"pinnedList\":[\"app\"]}","{\"pinnedList\":[{}]}","{\"pinnedList\":[{\"packagedAppId\":\"\"}]}","{\"pinnedList\":[{\"desktopAppLink\":1}]}","{\"pinnedList\":[],\"other\":1} trailing"})RejectLayout("pins",content,"invalid pins JSON rejected "+content);
ShellSettings.ValidateDocument("pins","{\"pinnedList\":[]}");Check(true,"valid empty custom JSON accepted");
ShellSettings.ValidateDocument("pins","{\"pinnedList\":[{\"desktopAppId\":\"Microsoft.Windows.Explorer\"},{\"secondaryTile\":{\"tileId\":\"example\"}}]}");Check(true,"desktop app ID and secondary tile JSON accepted");
RejectLayout("pins",new string(' ',ShellSettings.MaxDocumentLength)+"{}","oversized layout rejected");
p=Plan();p.TaskbarIconsXml="invalid";p.StartTilesXml="invalid";p.StartPinsJson="invalid";Check(Xml(p).Contains("LocalAccount"),"inactive invalid editor content does not block generation");
p.TaskbarIconsMode=ShellSettings.CustomXml;Reject(p,"active invalid taskbar layout blocks export");p.TaskbarIconsMode="使用默认图标";p.StartTilesMode=ShellSettings.CustomXml;Reject(p,"active invalid tile layout blocks export");p.StartTilesMode="使用默认磁贴";p.StartPinsMode=ShellSettings.CustomJson;Reject(p,"active invalid pin layout blocks export");
p=Plan();p.TaskbarSearch="wrong";Reject(p,"invalid search selection rejected");p=Plan();p.TaskbarIconsMode="wrong";Reject(p,"invalid taskbar mode rejected");p=Plan();p.StartTilesMode="wrong";Reject(p,"invalid tiles mode rejected");p=Plan();p.StartPinsMode="wrong";Reject(p,"invalid pins mode rejected");
p=Plan();p.TaskbarIconsMode=ShellSettings.CustomXml;p.StartTilesMode=ShellSettings.CustomXml;p.StartPinsMode=ShellSettings.CustomJson;Check(Recommendations.MissingInputs(p).Contains("任务栏图标 XML")&&Recommendations.MissingInputs(p).Contains("Windows 10 磁贴 XML")&&Recommendations.MissingInputs(p).Contains("Windows 11 固定应用 JSON"),"summary lists missing active custom layouts");
p=Plan();p.StartPinsMode=ShellSettings.CustomJson;p.StartPinsJson="{\"pinnedList\":[{\"desktopAppLink\":\"C:\\\\O'Brien\\\\app.lnk\"}]}";
Check(Node(Xml(p),"//*[local-name()='File' and contains(@path,'SetStartPins.ps1')]" )!.Contains("O''Brien"),"apostrophes in JSON app paths safely quoted in generated PowerShell");
viewModel=new UnattendLocal.Desktop.MainViewModel();var shellNotifications=new HashSet<string>();viewModel.PropertyChanged+=(_,e)=>shellNotifications.Add(e.PropertyName!);
viewModel.Preview="OLD PREVIEW";viewModel.Plan.TaskbarIconsMode=ShellSettings.CustomXml;viewModel.Plan.StartTilesMode=ShellSettings.CustomXml;viewModel.Plan.StartPinsMode=ShellSettings.CustomJson;
Check(viewModel.IsCustomTaskbar&&viewModel.IsCustomTiles&&viewModel.IsCustomPins&&viewModel.Preview!="OLD PREVIEW"&&shellNotifications.Contains(nameof(viewModel.IsCustomTaskbar)),"shell modes enable editors and invalidate preview with property notifications");
Check(viewModel.HelpTips["taskbaricons"].Body.StartsWith("当前：自定义 XML")&&viewModel.HelpTips["starttiles"].Body.Contains("当前：自定义 XML")&&viewModel.HelpTips["startpins"].Body.Contains("当前：自定义 JSON"),"custom mode help follows selected layout modes");
foreach(var (mode,width,textVisible,visible) in new[]{("完整搜索框",260.0,true,true),("图标和文字",120.0,true,true),("仅图标",44.0,false,true),("隐藏",44.0,false,false)}){
 viewModel.Plan.TaskbarSearch=mode;Check(viewModel.SearchPreviewWidth==width&&viewModel.ShowSearchText==textVisible&&viewModel.ShowSearchPreview==visible&&viewModel.ConfigurationSummary.Contains("搜索 "+mode)&&viewModel.HelpTips["taskbarsearch"].Body.StartsWith("当前："+mode),"search visual preview and summary follow "+mode);
}
foreach(var prop in new[]{nameof(InstallPlan.TaskbarSearch),nameof(InstallPlan.TaskbarIconsMode),nameof(InstallPlan.StartTilesMode),nameof(InstallPlan.StartPinsMode),nameof(InstallPlan.DisableWidgets),nameof(InstallPlan.LeftTaskbar),nameof(InstallPlan.HideTaskViewButton),nameof(InstallPlan.ShowAllTrayIcons),nameof(InstallPlan.DisableBingResults)}){
 if(typeof(InstallPlan).GetProperty(prop)!.PropertyType==typeof(bool))typeof(InstallPlan).GetProperty(prop)!.SetValue(viewModel.Plan,true);
 Check(viewModel.RecommendationDifferences.Any(d=>d.Property==prop),"shell policy appears in recommendation differences "+prop);Recommendations.Restore(viewModel.Plan,prop);Check(!viewModel.RecommendationDifferences.Any(d=>d.Property==prop),"single shell policy restore refreshes differences "+prop);
}
viewModel.Plan.TaskbarIconsMode=ShellSettings.CustomXml;viewModel.Plan.TaskbarIconsXml=ShellSettings.TaskbarExample;viewModel.PendingRecommendation="wifi";Check(viewModel.IsCustomTaskbar&&viewModel.Plan.TaskbarIconsXml==ShellSettings.TaskbarExample,"network-only scene switch preserves shell selection and layout");
viewModel.Plan.TaskbarIconsMode="使用默认图标";Check(!viewModel.IsCustomTaskbar&&viewModel.Plan.TaskbarIconsXml==ShellSettings.TaskbarExample,"default mode disables editor without erasing content");
viewModel.Plan=InstallPlan.NewCustom();viewModel.Plan.TaskbarSearch="隐藏";viewModel.Plan.LeftTaskbar=true;Check(!viewModel.IsRecommended&&!viewModel.ShowSearchPreview&&viewModel.ConfigurationSummary.Contains("左对齐 开启"),"independent custom plan shell controls and summary work");
Console.WriteLine($"Final pre13 total: {count} tests passed.");
// Explorer preferences are generated for the target installation only.
var explorerFlags=new[]{nameof(InstallPlan.ShowFileExtensions),nameof(InstallPlan.ClassicContextMenu),nameof(InstallPlan.HideInfoTip),nameof(InstallPlan.LaunchToThisPC),nameof(InstallPlan.ShowEndTask)};
Check(new[]{InstallPlan.New(),InstallPlan.NewCustom()}.All(q=>q.HideFilesMode==ExplorerSettings.Default&&explorerFlags.All(name=>!(bool)typeof(InstallPlan).GetProperty(name)!.GetValue(q)!)),"recommended and independent custom explorer defaults match Windows defaults");
restored=PlanStore.Deserialize("{\"SchemaVersion\":1,\"Users\":[]}");Check(restored.HideFilesMode==ExplorerSettings.Default&&explorerFlags.All(name=>!(bool)typeof(InstallPlan).GetProperty(name)!.GetValue(restored)!),"legacy plans missing explorer fields retain prior output");
foreach(var arch in new[]{"amd64","arm64","x86"}){
 p=Plan();p.Architecture=arch;var defaultScript=Node(Xml(p),"//*[local-name()='File' and contains(@path,'DefaultUser.ps1')]" )??"";
 Check(!defaultScript.Contains("/v \"Hidden\"")&&!defaultScript.Contains("ShowSuperHidden")&&!defaultScript.Contains("HideFileExt")&&!defaultScript.Contains("TaskbarEndTask")&&!defaultScript.Contains("ShowInfoTip")&&!DecodedXml(Xml(p)).Contains("86ca1aa0-34aa-4e8b-a509-50c905bae2a2")&&!DecodedXml(Xml(p)).Contains("-Name 'LaunchTo'"),"default explorer choices emit no preference overrides "+arch);
 p.HideFilesMode=ExplorerSettings.ProtectedOnly;xml=Xml(p);var hiddenScript=Node(xml,"//*[local-name()='File' and contains(@path,'DefaultUser.ps1')]" )!;
 Check(hiddenScript.Contains("/v \"Hidden\" /t REG_DWORD /d 1 /f;")&&!hiddenScript.Contains("ShowSuperHidden"),"protected-only mode shows ordinary hidden files and preserves system protection "+arch);
 p.HideFilesMode=ExplorerSettings.ShowAll;xml=Xml(p);hiddenScript=Node(xml,"//*[local-name()='File' and contains(@path,'DefaultUser.ps1')]" )!;
 Check(hiddenScript.Contains("/v \"Hidden\" /t REG_DWORD /d 1 /f;")&&hiddenScript.Contains("/v \"ShowSuperHidden\" /t REG_DWORD /d 1 /f;"),"show all includes hidden and protected files "+arch);
 foreach(var name in explorerFlags)typeof(InstallPlan).GetProperty(name)!.SetValue(p,true);
 xml=Xml(p);var defaultUser=Node(xml,"//*[local-name()='File' and contains(@path,'DefaultUser.ps1')]" )!;var userOnce=Node(xml,"//*[local-name()='File' and contains(@path,'UserOnce.ps1')]" )!;
 Check(defaultUser.Contains("/v \"HideFileExt\" /t REG_DWORD /d 0 /f;")&&defaultUser.Contains("/v \"ShowInfoTip\" /d 0 /t REG_DWORD /f;"),"extensions and info tips set intended values in default user hive "+arch);
 Check(defaultUser.Contains("Explorer\\Advanced\\TaskbarDeveloperSettings\" /v TaskbarEndTask /t REG_DWORD /d 1 /f;"),"end task setting uses intended target registry key "+arch);
 Check(userOnce.Contains(@"reg.exe add ""HKCU\Software\Classes\CLSID\{86ca1aa0-34aa-4e8b-a509-50c905bae2a2}\InprocServer32"" /ve /f;")&&userOnce.Contains("Stop-Process -Force"),"classic menu applies once per user and restarts explorer "+arch);
 Check(userOnce.Contains("-Name 'LaunchTo' -Type 'DWord' -Value 1;")&&defaultUser.Contains("RunOnce")&&Node(xml,"//u:settings[@pass='specialize']//u:Path[contains(.,'DefaultUser.ps1')]" )!=null,"This PC startup preference and once-per-user setup are wired into installation "+arch);
 Check(Node(xml,"//u:InstallTo")==null&&!DecodedXml(xml).Contains("diskpart.exe")&&DecodedXml(xml).Contains("PreventDeviceEncryption"),"explorer changes preserve manual disk safety and encryption prevention "+arch);
 p.HideFilesMode=ExplorerSettings.Default;foreach(var name in explorerFlags)typeof(InstallPlan).GetProperty(name)!.SetValue(p,false);xml=Xml(p);
 Check(!DecodedXml(xml).Contains("ShowSuperHidden")&&!DecodedXml(xml).Contains("HideFileExt")&&!DecodedXml(xml).Contains("ShowInfoTip")&&!DecodedXml(xml).Contains("TaskbarEndTask")&&!DecodedXml(xml).Contains("86ca1aa0-34aa-4e8b-a509-50c905bae2a2")&&!DecodedXml(xml).Contains("-Name 'LaunchTo'"),"turning explorer preferences off omits overrides rather than generating inverse values "+arch);
}
p=Plan();p.HideFilesMode="invalid";Reject(p,"invalid hidden file mode blocks export");p.HideFilesMode=null!;Reject(p,"null hidden file mode blocks export");
p=Plan();p.HideFilesMode=ExplorerSettings.ShowAll;p.ShowFileExtensions=true;p.ClassicContextMenu=true;p.HideInfoTip=true;p.LaunchToThisPC=true;p.ShowEndTask=true;p.AutoLogon=false;p.KeepSensitiveFiles=true;
Check(Node(Xml(p),"//*[local-name()='File' and contains(@path,'UserOnce.ps1')]" )!.Contains("LaunchTo")&&!Xml(p).Contains("<AutoLogon>"),"explorer preferences do not force automatic logon");
foreach(var secrets in new[]{false,true}){
 restored=PlanStore.Deserialize(PlanStore.Serialize(p,secrets));Check(restored.HideFilesMode==ExplorerSettings.ShowAll&&explorerFlags.All(name=>(bool)typeof(InstallPlan).GetProperty(name)!.GetValue(restored)!),"all explorer preferences persist with credentials inclusion="+secrets);
}
p.TaskbarIconsXml=ShellSettings.TaskbarExample;p.StartPinsJson=ShellSettings.PinsExample;p.Ssid="SavedSSID";Recommendations.Apply(p,"wired");Check(p.HideFilesMode==ExplorerSettings.Default&&explorerFlags.All(name=>!(bool)typeof(InstallPlan).GetProperty(name)!.GetValue(p)!)&&p.TaskbarIconsXml==ShellSettings.TaskbarExample&&p.StartPinsJson==ShellSettings.PinsExample&&p.Ssid=="SavedSSID","full recommendation restores explorer defaults and preserves user documents and network input");
foreach(var version in new[]{1,2,3}){
 p=Plan();p.RecommendationVersion=version;p.RecommendationWindows=version==1?"Windows 11":"";p.PreventDeviceEncryption=version>=3;
 Check(Recommendations.Differences(p).Count==0,"explorer defaults agree with historical recommendation version "+version);
 p.HideFilesMode=ExplorerSettings.ShowAll;Recommendations.Restore(p,nameof(InstallPlan.HideFilesMode));Check(p.HideFilesMode==ExplorerSettings.Default,"single explorer restore respects historical baseline "+version);
}
var explorerHelp=new Dictionary<string,string>{[nameof(InstallPlan.HideFilesMode)]="explorerhidden",[nameof(InstallPlan.ShowFileExtensions)]="explorerextensions",[nameof(InstallPlan.ClassicContextMenu)]="explorercontext",[nameof(InstallPlan.HideInfoTip)]="explorertips",[nameof(InstallPlan.LaunchToThisPC)]="explorerlaunch",[nameof(InstallPlan.ShowEndTask)]="taskbarendtask"};
viewModel=new UnattendLocal.Desktop.MainViewModel();
foreach(var (name,helpId) in explorerHelp){
 viewModel.Preview="OLD PREVIEW";var prop=typeof(InstallPlan).GetProperty(name)!;prop.SetValue(viewModel.Plan,prop.PropertyType==typeof(bool)?(object)true:ExplorerSettings.ShowAll);
 Check(viewModel.Preview!="OLD PREVIEW"&&viewModel.RecommendationDifferences.Any(d=>d.Property==name)&&viewModel.HelpTips[helpId].Body.StartsWith("当前：")&&(name==nameof(InstallPlan.HideFilesMode)?viewModel.HelpTips[helpId].Body.Contains(ExplorerSettings.ShowAll):viewModel.HelpTips[helpId].Body.Split('\n')[0].Contains(name==nameof(InstallPlan.LaunchToThisPC)?"进入此电脑":"开启")),"explorer edit updates current help and recommendation differences "+name);
 Recommendations.Restore(viewModel.Plan,name);Check(!viewModel.RecommendationDifferences.Any(d=>d.Property==name)&&viewModel.HelpTips[helpId].Body.Split('\n')[0].Contains(name==nameof(InstallPlan.HideFilesMode)?ExplorerSettings.Default:name==nameof(InstallPlan.LaunchToThisPC)?"使用 Windows 默认入口":"关闭"),"single explorer restore refreshes help and differences "+name);
}
viewModel.Plan.HideFilesMode=ExplorerSettings.ProtectedOnly;viewModel.Plan.ShowFileExtensions=true;Check(viewModel.ConfigurationSummary.Contains(ExplorerSettings.ProtectedOnly)&&viewModel.ConfigurationSummary.Contains("显示扩展名 开启"),"hidden file mode and extensions are independent in summary");viewModel.Plan.HideFilesMode=ExplorerSettings.Default;Check(viewModel.Plan.ShowFileExtensions&&viewModel.ConfigurationSummary.Contains("显示扩展名 开启"),"changing hidden file display preserves extension preference");
viewModel.Plan.LaunchToThisPC=true;viewModel.Plan.ClassicContextMenu=true;viewModel.Plan.ShowEndTask=true;viewModel.PendingRecommendation="wifi";Check(viewModel.Plan.ShowFileExtensions&&viewModel.Plan.LaunchToThisPC&&viewModel.Plan.ClassicContextMenu&&viewModel.Plan.ShowEndTask,"network scene switch preserves edited explorer preferences");
viewModel.Plan=InstallPlan.NewCustom();viewModel.Plan.HideFilesMode=ExplorerSettings.ShowAll;viewModel.Plan.HideInfoTip=true;Check(!viewModel.IsRecommended&&viewModel.ConfigurationSummary.Contains("显示全部文件")&&viewModel.ConfigurationSummary.Contains("隐藏悬停说明 开启")&&viewModel.HelpTips["explorertips"].Body.Contains("不影响本工具"),"independent custom mode allows explorer edits and keeps tool hints distinct");
Check(UnattendLocal.Desktop.HelpCatalog.PageCount>=9&&viewModel.HelpTips.ContainsKey("taskbarendtask")&&viewModel.HelpTips.ContainsKey("explorerhidden"),"all new help topics registered across pages");
Console.WriteLine($"Final pre14 total: {count} tests passed.");
// System desktop icons and Windows 11 folders are distinct from ordinary desktop files and Start pins.
Check(DesktopSettings.Icons.Select(item=>item.Id).Order().SequenceEqual(upstream.DesktopIcons.Keys.Order())&&DesktopSettings.Icons.Count==13,"all 13 fixed upstream desktop icons are available");
Check(DesktopSettings.Folders.Select(item=>item.Id).Order().SequenceEqual(upstream.StartFolders.Keys.Order())&&DesktopSettings.Folders.Count==9,"all 9 fixed upstream start folders are available");
Check(DesktopSettings.Icons.Concat(DesktopSettings.Folders).All(item=>item.Label.Any(ch=>ch>='\u3400'&&ch<='\u9fff')&&typeof(InstallPlan).GetProperty(item.Property)?.PropertyType==typeof(bool)),"every icon and folder has a Chinese label and observable boolean selection");
Check(DesktopSettings.Icons.All(item=>item.Property=="Desktop"+item.Id)&&DesktopSettings.Folders.All(item=>item.Property=="StartFolder"+item.Id),"choice identifiers match independent model fields");
Check(new[]{InstallPlan.New(),InstallPlan.NewCustom()}.All(q=>q.DesktopIconsMode==DesktopSettings.DefaultIcons&&q.StartFoldersMode==DesktopSettings.DefaultFolders&&!q.DeleteEdgeDesktopIcon),"new recommended and independent custom plans retain Windows desktop and start-folder defaults");
restored=PlanStore.Deserialize("{\"SchemaVersion\":1,\"Users\":[]}");Check(restored.DesktopIconsMode==DesktopSettings.DefaultIcons&&restored.StartFoldersMode==DesktopSettings.DefaultFolders&&!restored.DeleteEdgeDesktopIcon,"old plans without desktop fields retain prior behavior");
byte[] FolderBytes(string output){var once=Node(output,"//*[local-name()='File' and contains(@path,'UserOnce.ps1')]" )??"";var match=System.Text.RegularExpressions.Regex.Match(once,@"-Name 'VisiblePlaces'.*?FromBase64String\('([^']*)'\)");if(!match.Success)throw new Exception("Missing VisiblePlaces");return Convert.FromBase64String(match.Groups[1].Value);}
foreach(var arch in new[]{"amd64","arm64","x86"}){
 p=Plan();p.Architecture=arch;xml=Xml(p);Check(!DecodedXml(xml).Contains("HideDesktopIcons")&&!DecodedXml(xml).Contains("VisiblePlaces")&&!DecodedXml(xml).Contains("Microsoft Edge.lnk"),"default desktop choices emit no overrides or deletions "+arch);
 p.DesktopIconsMode=DesktopSettings.CustomIcons;p.DesktopThisPC=true;p.DesktopRecycleBin=true;p.StartFoldersMode=DesktopSettings.CustomFolders;p.StartFolderFileExplorer=true;p.StartFolderSettings=true;xml=Xml(p);var once=Node(xml,"//*[local-name()='File' and contains(@path,'UserOnce.ps1')]" )!;
 Check(once.Contains("HideDesktopIcons\\ClassicStartMenu")&&once.Contains("HideDesktopIcons\\NewStartPanel")&&once.Contains("Stop-Process -Force"),"custom icons configure both desktop views and refresh explorer "+arch);
 Check(once.Contains("-Name '{20d04fe0-3aea-1069-a2d8-08002b30309d}' -Value 0 -Type 'DWord';")&&once.Contains("-Name '{645ff040-5081-101b-9f08-00aa002f954e}' -Value 0 -Type 'DWord';")&&once.Contains("-Name '{5399e694-6ce5-4d6c-8fce-1d8870fdcba0}' -Value 1 -Type 'DWord';"),"This PC and recycle bin show while unselected control panel hides "+arch);
 Check(upstream.DesktopIcons.Values.All(icon=>System.Text.RegularExpressions.Regex.Matches(once,System.Text.RegularExpressions.Regex.Escape($"-Name '{icon.Guid}' -Value {(icon.Id is "ThisPC" or "RecycleBin"?0:1)} -Type 'DWord';")).Count==2),"every desktop selection is written with correct visibility in both views "+arch);
 var expected=Convert.FromBase64String("vCSKFAzWiUKggG7Zu6JIgg==").Concat(Convert.FromBase64String("hghzUqpRQ0Kfeyd2WEZZ1A=="));Check(FolderBytes(xml).SequenceEqual(expected),"File Explorer and Settings produce exact expected VisiblePlaces binary "+arch);
 Check(!once.Contains("Remove-Item -LiteralPath")&&!DecodedXml(xml).Contains("Microsoft Edge.lnk")&&Node(xml,"//u:InstallTo")==null&&!DecodedXml(xml).Contains("diskpart.exe"),"custom icon visibility does not delete files or affect manual disk safety "+arch);
 p.DeleteEdgeDesktopIcon=true;xml=Xml(p);Check(Node(xml,"//*[local-name()='File' and contains(@path,'Specialize.ps1')]" )!.Contains(@"Remove-Item -LiteralPath 'C:\Users\Public\Desktop\Microsoft Edge.lnk'")&&Node(xml,"//*[local-name()='File' and contains(@path,'UserOnce.ps1')]" )!.Contains("${env:USERPROFILE}\\Desktop\\Microsoft Edge.lnk"),"Edge shortcut deletion covers intended public and user desktop paths "+arch);
 p.DesktopIconsMode=DesktopSettings.DefaultIcons;p.StartFoldersMode=DesktopSettings.DefaultFolders;xml=Xml(p);Check(!DecodedXml(xml).Contains("HideDesktopIcons")&&!DecodedXml(xml).Contains("VisiblePlaces")&&DecodedXml(xml).Contains("Microsoft Edge.lnk")&&p.DesktopThisPC&&p.StartFolderSettings,"default modes retain choices and keep Edge deletion independent "+arch);
 p.DeleteEdgeDesktopIcon=false;Check(!DecodedXml(Xml(p)).Contains("Microsoft Edge.lnk"),"disabling Edge deletion omits deletion command "+arch);
}
foreach(var item in DesktopSettings.Icons){
 p=Plan();p.DesktopIconsMode=DesktopSettings.CustomIcons;typeof(InstallPlan).GetProperty(item.Property)!.SetValue(p,true);var once=Node(Xml(p),"//*[local-name()='File' and contains(@path,'UserOnce.ps1')]" )!;
 Check(upstream.DesktopIcons.Values.All(icon=>once.Contains($"-Name '{icon.Guid}' -Value {(icon.Id==item.Id?0:1)} -Type 'DWord';")),"single desktop icon selection generates intended GUID only "+item.Id);
}
foreach(var item in DesktopSettings.Folders){
 p=Plan();p.StartFoldersMode=DesktopSettings.CustomFolders;typeof(InstallPlan).GetProperty(item.Property)!.SetValue(p,true);Check(FolderBytes(Xml(p)).SequenceEqual(upstream.StartFolders[item.Id].Bytes),"single start folder emits exact upstream bytes "+item.Id);
}
p=Plan();p.DesktopIconsMode=DesktopSettings.CustomIcons;p.StartFoldersMode=DesktopSettings.CustomFolders;xml=Xml(p);var emptyOnce=Node(xml,"//*[local-name()='File' and contains(@path,'UserOnce.ps1')]" )!;
Check(upstream.DesktopIcons.Values.All(icon=>emptyOnce.Contains($"-Name '{icon.Guid}' -Value 1 -Type 'DWord';"))&&FolderBytes(xml).Length==0,"empty custom selections explicitly hide system icons and folder entries");
DesktopSettings.SelectAll(p,true,true);DesktopSettings.SelectAll(p,false,true);xml=Xml(p);Check(DesktopSettings.Icons.All(item=>DesktopSettings.Selected(p,item))&&upstream.DesktopIcons.Values.All(icon=>Node(xml,"//*[local-name()='File' and contains(@path,'UserOnce.ps1')]" )!.Contains($"-Name '{icon.Guid}' -Value 0 -Type 'DWord';")),"select-all desktop helper shows all supported icon identifiers");
Check(FolderBytes(xml).SequenceEqual(upstream.StartFolders.Values.OrderBy(folder=>folder.Id).SelectMany(folder=>folder.Bytes)),"select-all folders produce exact sorted upstream binary");DesktopSettings.SelectAll(p,true,false);Check(DesktopSettings.Icons.All(item=>!DesktopSettings.Selected(p,item))&&DesktopSettings.Folders.All(item=>DesktopSettings.Selected(p,item)),"clear desktop selection does not change start folders");DesktopSettings.SelectAll(p,false,false);Check(DesktopSettings.Folders.All(item=>!DesktopSettings.Selected(p,item)),"clear folders removes all selected folder entries");
foreach(var mode in new[]{"unknown",null!}){p=Plan();p.DesktopIconsMode=mode;Reject(p,"invalid desktop mode rejected");p=Plan();p.StartFoldersMode=mode;Reject(p,"invalid start-folder mode rejected");}
p=Plan();p.DesktopIconsMode=DesktopSettings.CustomIcons;p.StartFoldersMode=DesktopSettings.CustomFolders;DesktopSettings.SelectAll(p,true,true);DesktopSettings.SelectAll(p,false,true);p.DeleteEdgeDesktopIcon=true;
foreach(var secrets in new[]{false,true}){restored=PlanStore.Deserialize(PlanStore.Serialize(p,secrets));Check(restored.DesktopIconsMode==p.DesktopIconsMode&&restored.StartFoldersMode==p.StartFoldersMode&&restored.DeleteEdgeDesktopIcon&&DesktopSettings.Icons.Concat(DesktopSettings.Folders).All(item=>DesktopSettings.Selected(restored,item)),"all modes and 22 selections roundtrip credentials="+secrets);}
p.AutoLogon=false;p.KeepSensitiveFiles=true;Check(Node(Xml(p),"//*[local-name()='File' and contains(@path,'UserOnce.ps1')]" )!.Contains("VisiblePlaces")&&!Xml(p).Contains("<AutoLogon>"),"desktop and folder settings do not force first automatic logon");
p.TaskbarIconsXml=ShellSettings.TaskbarExample;p.StartPinsJson=ShellSettings.PinsExample;Recommendations.Apply(p,"wired");Check(p.DesktopIconsMode==DesktopSettings.DefaultIcons&&p.StartFoldersMode==DesktopSettings.DefaultFolders&&!p.DeleteEdgeDesktopIcon&&DesktopSettings.Icons.Concat(DesktopSettings.Folders).All(item=>DesktopSettings.Selected(p,item))&&p.TaskbarIconsXml==ShellSettings.TaskbarExample&&p.StartPinsJson==ShellSettings.PinsExample,"full recommendation restores modes while preserving user selections and layout documents");
foreach(var version in new[]{1,2,3}){p=Plan();p.RecommendationVersion=version;p.RecommendationWindows=version==1?"Windows 11":"";p.PreventDeviceEncryption=version>=3;Check(Recommendations.Differences(p).Count==0,"historical baseline accepts default desktop modes "+version);}
viewModel=new UnattendLocal.Desktop.MainViewModel();var desktopNotices=new HashSet<string>();viewModel.PropertyChanged+=(_,e)=>desktopNotices.Add(e.PropertyName!);
viewModel.Plan.DesktopIconsMode=DesktopSettings.CustomIcons;viewModel.Plan.StartFoldersMode=DesktopSettings.CustomFolders;Check(viewModel.IsCustomDesktopIcons&&viewModel.IsCustomStartFolders&&viewModel.DesktopIconSelection.Contains("0 / 13")&&viewModel.StartFolderSelection.Contains("0 / 9"),"custom modes enable selectors and describe initially empty lists");
foreach(var item in DesktopSettings.Icons.Concat(DesktopSettings.Folders)){
 viewModel.Preview="OLD PREVIEW";typeof(InstallPlan).GetProperty(item.Property)!.SetValue(viewModel.Plan,true);Check(viewModel.Preview!="OLD PREVIEW"&&viewModel.ConfigurationSummary.Contains(item.Label)&&(item.Property.StartsWith("Desktop")?viewModel.DesktopIconSelection:viewModel.StartFolderSelection).Contains(item.Label),"individual checkbox updates selection summary and invalidates preview "+item.Property);
}
Check(viewModel.HelpTips["desktopicons"].Body.StartsWith("当前：自定义：")&&viewModel.HelpTips["startfolders"].Body.StartsWith("当前：自定义：")&&desktopNotices.Contains(nameof(viewModel.IsCustomDesktopIcons))&&desktopNotices.Contains(nameof(viewModel.DesktopIconSelection))&&desktopNotices.Contains(nameof(viewModel.StartFolderSelection)),"mode and selection change notifications refresh all current help");
Check(viewModel.RecommendationDifferences.Single(d=>d.Property==nameof(InstallPlan.DesktopIconsMode)).Current.Contains("此电脑")&&viewModel.RecommendationDifferences.Single(d=>d.Property==nameof(InstallPlan.StartFoldersMode)).Current.Contains("设置"),"recommendation mode differences show current selected entries");
Recommendations.Restore(viewModel.Plan,nameof(InstallPlan.DesktopIconsMode));Check(!viewModel.IsCustomDesktopIcons&&viewModel.Plan.DesktopThisPC&&viewModel.Plan.StartFolderSettings&&viewModel.ConfigurationSummary.Contains(DesktopSettings.DefaultIcons)&&!viewModel.RecommendationDifferences.Any(d=>d.Property==nameof(InstallPlan.DesktopIconsMode)),"single desktop restore disables selector without erasing selections");
Recommendations.Restore(viewModel.Plan,nameof(InstallPlan.StartFoldersMode));Check(!viewModel.IsCustomStartFolders&&viewModel.Plan.StartFolderSettings&&viewModel.HelpTips["startfolders"].Body.StartsWith("当前：使用 Windows 默认文件夹"),"single folder restore retains selection and updates current hint");
viewModel.Plan.DeleteEdgeDesktopIcon=true;Check(viewModel.RecommendationDifferences.Any(d=>d.Property==nameof(InstallPlan.DeleteEdgeDesktopIcon))&&viewModel.HelpTips["edgeicon"].Body.StartsWith("当前：删除 Edge 桌面快捷方式开启"),"Edge deletion independent switch updates difference and tooltip");Recommendations.Restore(viewModel.Plan,nameof(InstallPlan.DeleteEdgeDesktopIcon));Check(!viewModel.Plan.DeleteEdgeDesktopIcon,"single Edge shortcut setting restore disables deletion");
viewModel.Plan.DesktopIconsMode=DesktopSettings.CustomIcons;viewModel.Plan.StartFoldersMode=DesktopSettings.CustomFolders;viewModel.PendingRecommendation="wifi";Check(viewModel.IsCustomDesktopIcons&&viewModel.IsCustomStartFolders&&viewModel.Plan.DesktopThisPC&&viewModel.Plan.StartFolderSettings,"network-only recommendation switch preserves modes and individual selections");
viewModel.Plan=InstallPlan.NewCustom();viewModel.Plan.DesktopIconsMode=DesktopSettings.CustomIcons;viewModel.Plan.DesktopThisPC=true;viewModel.Plan.StartFoldersMode=DesktopSettings.CustomFolders;viewModel.Plan.StartFolderSettings=true;Check(!viewModel.IsRecommended&&viewModel.IsCustomDesktopIcons&&viewModel.IsCustomStartFolders&&viewModel.ConfigurationSummary.Contains("自定义：此电脑")&&viewModel.ConfigurationSummary.Contains("自定义：设置"),"independent custom plan supports desktop and folder preferences");
Check(viewModel.HelpTips.ContainsKey("desktopicons")&&viewModel.HelpTips.ContainsKey("edgeicon")&&viewModel.HelpTips.ContainsKey("startfolders")&&UnattendLocal.Desktop.HelpCatalog.PageCount>=9,"desktop and start-folder help topics registered");
Console.WriteLine($"Final pre15 total: {count} tests passed.");

// pre16: complete upstream visual-effects selection, script values and UI linkage.
Check(VisualEffectsSettings.Items.Count==17&&VisualEffectsSettings.Items.Select(item=>item.Effect).Order().SequenceEqual(Enum.GetValues<Schneegans.Unattend.Effect>().Order()),"all 17 fixed upstream effects covered exactly once");
Check(VisualEffectsSettings.Items.All(item=>item.Property=="Effect"+item.Effect&&typeof(InstallPlan).GetProperty(item.Property)?.PropertyType==typeof(bool)&&item.Label.Any(ch=>ch>='\u3400'&&ch<='\u9fff')&&item.Description.Length>0),"all effects have Chinese labels descriptions and independent boolean fields");
Check(VisualEffectsSettings.Items.Select(item=>item.HelpId).Distinct().Count()==17,"individual effect tooltip identifiers are unique");
Check(new[]{InstallPlan.New(),InstallPlan.NewCustom()}.All(q=>q.VisualEffectsMode==VisualEffectsSettings.Default&&VisualEffectsSettings.Items.All(item=>!VisualEffectsSettings.Selected(q,item))),"new recommended and custom plans use Windows effects default and initially unselected personal choices");
restored=PlanStore.Deserialize("{\"SchemaVersion\":1,\"Users\":[]}");Check(restored.VisualEffectsMode==VisualEffectsSettings.Default&&VisualEffectsSettings.Items.All(item=>!VisualEffectsSettings.Selected(restored,item)),"old plans missing effects fields retain native behavior");
string EffectSpecialize(string text)=>Node(text,"//*[local-name()='File' and contains(@path,'Specialize.ps1')]")??"";
string EffectUserOnce(string text)=>Node(text,"//*[local-name()='File' and contains(@path,'UserOnce.ps1')]")??"";
bool EffectValue(string script,Schneegans.Unattend.Effect effect,bool value)=>script.Contains($"Explorer\\VisualEffects\\{effect}\" -Name 'DefaultValue' -Value {(value?1:0)} -Type 'DWord' -Force;");
foreach(var arch in new[]{"amd64","arm64","x86"}){
 p=Plan();p.Architecture=arch;p.EffectFontSmoothing=true;xml=Xml(p);
 Check(!DecodedXml(xml).Contains("Explorer\\VisualEffects")&&p.EffectFontSmoothing,"default mode emits no visual effects overrides but retains selections "+arch);
 foreach(var mode in new[]{VisualEffectsSettings.Appearance,VisualEffectsSettings.Performance,VisualEffectsSettings.Custom}){
  p.VisualEffectsMode=mode;xml=Xml(p);var effectScript=EffectSpecialize(xml);var effectOnce=EffectUserOnce(xml);
  Check(VisualEffectsSettings.Items.All(item=>EffectValue(effectScript,item.Effect,mode==VisualEffectsSettings.Appearance||mode==VisualEffectsSettings.Custom&&VisualEffectsSettings.Selected(p,item))),"every active visual effect emits exact intended default value "+arch+" "+mode);
  var setting=mode==VisualEffectsSettings.Appearance?1:mode==VisualEffectsSettings.Performance?2:3;
  Check(effectOnce.Contains($"-Name 'VisualFXSetting' -Type 'DWord' -Value {setting} -Force;"),"visual FX mode written per user "+arch+" "+mode);
  Check(effectScript.Contains("HKEY_LOCAL_MACHINE")&&effectOnce.Contains("HKEY_CURRENT_USER")&&!effectOnce.Contains("DefaultValue"),"visual effects install defaults and per-user mode have correct scopes "+arch+" "+mode);
  Check(!DecodedXml(xml).Contains("diskpart.exe")&&Node(xml,"//u:InstallTo")==null&&p.PreventDeviceEncryption,"effects keep existing disk safety and encryption defaults "+arch+" "+mode);
 }
 p.VisualEffectsMode=VisualEffectsSettings.Default;Check(!DecodedXml(Xml(p)).Contains("VisualFXSetting")&&p.EffectFontSmoothing,"switch back to default removes generated override "+arch);
}
foreach(var item in VisualEffectsSettings.Items){
 p=Plan();p.VisualEffectsMode=VisualEffectsSettings.Custom;typeof(InstallPlan).GetProperty(item.Property)!.SetValue(p,true);
 var singleEffectScript=EffectSpecialize(Xml(p));Check(VisualEffectsSettings.Items.All(other=>EffectValue(singleEffectScript,other.Effect,other.Effect==item.Effect)),"single custom effect enables only correct enum key "+item.Effect);
}
p=Plan();p.VisualEffectsMode=VisualEffectsSettings.Custom;VisualEffectsSettings.SelectAll(p,true);xml=Xml(p);
Check(VisualEffectsSettings.Items.All(item=>EffectValue(EffectSpecialize(xml),item.Effect,true))&&VisualEffectsSettings.ModeSummary(p).Contains("17 / 17"),"custom all selected produces all enabled with custom mode summary");
VisualEffectsSettings.SelectAll(p,false);xml=Xml(p);Check(VisualEffectsSettings.Items.All(item=>EffectValue(EffectSpecialize(xml),item.Effect,false))&&EffectUserOnce(xml).Contains("-Value 3")&&VisualEffectsSettings.Selection(p)=="全部关闭","empty custom selection closes all effects while retaining custom mode");
p.EffectFontSmoothing=true;p.EffectThumbnailsOrIcon=true;p.DesktopThisPC=true;p.StartFolderSettings=true;p.ShowFileExtensions=true;
foreach(var secrets in new[]{false,true}){
 restored=PlanStore.Deserialize(PlanStore.Serialize(p,secrets));Check(restored.VisualEffectsMode==VisualEffectsSettings.Custom&&VisualEffectsSettings.Items.All(item=>VisualEffectsSettings.Selected(restored,item)==VisualEffectsSettings.Selected(p,item))&&restored.DesktopThisPC&&restored.StartFolderSettings&&restored.ShowFileExtensions,"effects mode and mixed selection roundtrip with other preferences "+secrets);
}
p.AutoLogon=false;p.KeepSensitiveFiles=true;xml=Xml(p);Check(EffectUserOnce(xml).Contains("VisualFXSetting")&&!xml.Contains("<AutoLogon>"),"effects do not force first automatic logon");
foreach(var invalid in new[]{"unknown",null!}){p=Plan();p.VisualEffectsMode=invalid;try{g.Generate(p);throw new Exception("invalid mode accepted");}catch(ArgumentException e){Check(e.Message.Contains("视觉效果"),"invalid effects mode rejected with Chinese explanation");}}
foreach(var version in new[]{1,2,3}){p=Plan();p.RecommendationVersion=version;p.RecommendationWindows=version==1?"Windows 11":"";p.PreventDeviceEncryption=version>=3;Check(Recommendations.Differences(p).Count==0,"historical recommendations accept default effects "+version);}
viewModel=new UnattendLocal.Desktop.MainViewModel();var effectsNotices=new HashSet<string>();viewModel.PropertyChanged+=(_,e)=>effectsNotices.Add(e.PropertyName!);
Check(!viewModel.IsCustomVisualEffects&&viewModel.VisualEffectsSummary.Contains("不写入覆盖")&&viewModel.HelpTips["effectFontSmoothing"].Body.StartsWith("当前：由 Windows 决定"),"default UI summary and each effect hint report system-chosen behavior");
viewModel.Plan.VisualEffectsMode=VisualEffectsSettings.Custom;Check(viewModel.IsCustomVisualEffects&&viewModel.VisualEffectsSummary.Contains("0 / 17")&&viewModel.VisualEffectsSummary.Contains("全部关闭"),"custom mode exposes initially empty choice list and exact count");
foreach(var item in VisualEffectsSettings.Items){
 viewModel.Preview="OLD PREVIEW";typeof(InstallPlan).GetProperty(item.Property)!.SetValue(viewModel.Plan,true);
 Check(viewModel.Preview!="OLD PREVIEW"&&viewModel.VisualEffectsSummary.Contains(item.Label)&&viewModel.ConfigurationSummary.Contains(item.Label)&&viewModel.HelpTips[item.HelpId].Body.StartsWith("当前：开启"),"each checkbox updates summary current hint and preview "+item.Effect);
}
Check(effectsNotices.Contains(nameof(viewModel.IsCustomVisualEffects))&&effectsNotices.Contains(nameof(viewModel.VisualEffectsSummary)),"mode and summary raise UI change notifications");
Check(viewModel.RecommendationDifferences.Single(d=>d.Property==nameof(InstallPlan.VisualEffectsMode)).Current.Contains("17 / 17"),"custom differences show effective count and selected choices");
foreach(var mode in new[]{VisualEffectsSettings.Appearance,VisualEffectsSettings.Performance}){
 viewModel.Preview="OLD PREVIEW";viewModel.Plan.VisualEffectsMode=mode;
 Check(!viewModel.IsCustomVisualEffects&&viewModel.VisualEffectsSummary.Contains(mode)&&viewModel.Preview!="OLD PREVIEW"&&VisualEffectsSettings.Items.All(item=>VisualEffectsSettings.Selected(viewModel.Plan,item)),"preset modes conceal custom controls and retain personal selection "+mode);
 Check(VisualEffectsSettings.Items.All(item=>viewModel.HelpTips[item.HelpId].Body.StartsWith("当前："+(mode==VisualEffectsSettings.Appearance?"开启":"关闭"))),"preset hints show active values rather than retained custom choice "+mode);
}
viewModel.Plan.VisualEffectsMode=VisualEffectsSettings.Custom;VisualEffectsSettings.SelectAll(viewModel.Plan,false);viewModel.Plan.EffectFontSmoothing=true;
Check(viewModel.VisualEffectsSummary.Contains("1 / 17")&&viewModel.RecommendationDifferences.Single(d=>d.Property==nameof(InstallPlan.VisualEffectsMode)).Current.Contains("平滑屏幕字体"),"bulk and individual edits refresh current difference details");
Recommendations.Restore(viewModel.Plan,nameof(InstallPlan.VisualEffectsMode));
Check(!viewModel.IsCustomVisualEffects&&viewModel.Plan.EffectFontSmoothing&&!viewModel.RecommendationDifferences.Any(d=>d.Property==nameof(InstallPlan.VisualEffectsMode))&&viewModel.HelpTips["effectFontSmoothing"].Body.StartsWith("当前：由 Windows 决定"),"single restore returns native default without erasing personal choices");
viewModel.Plan.VisualEffectsMode=VisualEffectsSettings.Custom;viewModel.PendingRecommendation="wifi";
Check(viewModel.IsCustomVisualEffects&&viewModel.Plan.EffectFontSmoothing,"network-only switch retains effects mode and personal selection");
viewModel.Plan.DesktopIconsMode=DesktopSettings.CustomIcons;viewModel.Plan.DesktopThisPC=true;Recommendations.Apply(viewModel.Plan,"wired");
Check(viewModel.Plan.VisualEffectsMode==VisualEffectsSettings.Default&&viewModel.Plan.EffectFontSmoothing&&viewModel.Plan.DesktopThisPC&&viewModel.Plan.RecommendationVersion==3&&viewModel.Plan.PreventDeviceEncryption,"full restoration keeps personal contents and existing baseline version");
viewModel.Plan=InstallPlan.NewCustom();viewModel.Plan.VisualEffectsMode=VisualEffectsSettings.Custom;viewModel.Plan.EffectFontSmoothing=true;
Check(!viewModel.IsRecommended&&viewModel.IsCustomVisualEffects&&viewModel.ConfigurationSummary.Contains("平滑屏幕字体"),"independent custom configuration supports complete visual effects");
Check(UnattendLocal.Desktop.HelpCatalog.PageCount>=10&&VisualEffectsSettings.Items.All(item=>viewModel.HelpTips.ContainsKey(item.HelpId))&&viewModel.HelpTips["visualeffects"].Body.Contains("全部 17 项"),"new general and 17 individual Chinese tips registered");
Console.WriteLine($"Final pre16 total: {count} tests passed.");


// pre17: independent Windows PE requirement checks, legacy migration and linked UI.
Check(HardwareSettings.Items.Select(i=>i.RegistryValue).SequenceEqual(new[]{"BypassTPMCheck","BypassSecureBootCheck","BypassRAMCheck"}),"hardware bypass is limited to exactly three native supported checks");
Check(new[]{InstallPlan.New(),InstallPlan.NewCustom()}.All(q=>HardwareSettings.Items.All(i=>!HardwareSettings.Enabled(q,i))),"recommended and custom hardware checks default to native checks");
List<(int Order,string Path)> PECommands(string text){var d=new XmlDocument();d.LoadXml(text);var n=new XmlNamespaceManager(d.NameTable);n.AddNamespace("u","urn:schemas-microsoft-com:unattend");return d.SelectNodes("//u:settings[@pass='windowsPE']/u:component[@name='Microsoft-Windows-Setup']/u:RunSynchronous/u:RunSynchronousCommand",n)!.Cast<XmlNode>().Select(cmd=>(int.Parse(cmd.SelectSingleNode("u:Order",n)!.InnerText),cmd.SelectSingleNode("u:Path",n)!.InnerText)).ToList();}
foreach(var arch in new[]{"amd64","arm64","x86"})foreach(var bits in Enumerable.Range(0,8)){
 p=Plan();p.Architecture=arch;p.BypassTPMCheck=(bits&1)!=0;p.BypassSecureBootCheck=(bits&2)!=0;p.BypassRAMCheck=(bits&4)!=0;xml=Xml(p);var cmds=PECommands(xml);var expected=HardwareSettings.Items.Where((_,index)=>(bits&(1<<index))!=0).ToList();
 Check(HardwareSettings.Items.All(item=>cmds.Count(cmd=>cmd.Path==$"reg.exe add \"HKLM\\SYSTEM\\Setup\\LabConfig\" /v {item.RegistryValue} /t REG_DWORD /d 1 /f")== (expected.Contains(item)?1:0)),"only chosen bypass keys appear once in windowsPE "+arch+" "+bits);
 Check(cmds.Select(c=>c.Order).Distinct().Count()==cmds.Count&&!Node(xml,"//u:settings[@pass='specialize']")!.Contains("LabConfig"),"bypass is scoped to Windows PE with unique command orders "+arch+" "+bits);
 Check(!DecodedXml(xml).Contains("BypassCPUCheck")&&!DecodedXml(xml).Contains("BypassStorageCheck")&&Node(xml,"//u:InstallTo")==null&&!DecodedXml(xml).Contains("diskpart.exe"),"bypass does not introduce unsupported checks or destructive disk defaults "+arch+" "+bits);
}
p=Plan();p.BypassTPMCheck=true;p.BypassRAMCheck=true;p.BypassSecureBootCheck=false;
foreach(var include in new[]{false,true}){
 restored=PlanStore.Deserialize(PlanStore.Serialize(p,include));Check(restored.BypassTPMCheck&&restored.BypassRAMCheck&&!restored.BypassSecureBootCheck&&!restored.BypassHardware,"partial bypass choice survives serialization "+include);
}
restored=PlanStore.Deserialize("{\"SchemaVersion\":1,\"BypassHardware\":true,\"Users\":[]}");Check(HardwareSettings.AllEnabled(restored)&&!restored.BypassHardware,"old global bypass true migrates to three visible individual selections");
restored=PlanStore.Deserialize("{\"SchemaVersion\":1,\"BypassHardware\":false,\"Users\":[]}");Check(HardwareSettings.Items.All(i=>!HardwareSettings.Enabled(restored,i)),"old global bypass false keeps default requirements");
restored=PlanStore.Deserialize("{\"SchemaVersion\":1,\"BypassTPMCheck\":true,\"BypassHardware\":false,\"Users\":[]}");Check(restored.BypassTPMCheck&&!restored.BypassSecureBootCheck&&!restored.BypassRAMCheck,"new individual selection is not erased by compatibility field false");
p=Plan();p.BypassHardware=true;xml=Xml(p);Check(HardwareSettings.Items.All(item=>PECommands(xml).Any(cmd=>cmd.Path.Contains("/v "+item.RegistryValue))),"legacy in-memory global switch retains original generated behavior");
restored=PlanStore.Deserialize(PlanStore.Serialize(p,true));Check(HardwareSettings.AllEnabled(restored)&&!restored.BypassHardware&&p.BypassHardware&&!p.BypassTPMCheck,"saving migrates a copy without mutating the caller");
viewModel=new UnattendLocal.Desktop.MainViewModel();viewModel.Plan=p;Check(!viewModel.Plan.BypassHardware&&viewModel.Plan.BypassTPMCheck&&viewModel.Plan.BypassSecureBootCheck&&viewModel.Plan.BypassRAMCheck,"view model shows all three values when receiving old global bypass plan");
foreach(var item in HardwareSettings.Items){
 Recommendations.Apply(viewModel.Plan,"wired");viewModel.Preview="OLD PREVIEW";typeof(InstallPlan).GetProperty(item.Property)!.SetValue(viewModel.Plan,true);
 var helpId=item.RegistryValue switch{"BypassTPMCheck"=>"hardwaretpm","BypassSecureBootCheck"=>"hardwareboot",_=>"hardwareram"};
 Check(viewModel.Preview!="OLD PREVIEW"&&viewModel.ConfigurationSummary.Contains(item.Label)&&viewModel.RecommendationDifferences.Any(d=>d.Property==item.Property)&&viewModel.HelpTips[helpId].Body.Contains("开启"),"individual bypass updates preview summary recommendation and current help "+item.Property);
 Recommendations.Restore(viewModel.Plan,item.Property);Check(!HardwareSettings.Enabled(viewModel.Plan,item)&&!viewModel.RecommendationDifferences.Any(d=>d.Property==item.Property)&&viewModel.HelpTips[helpId].Body.Contains("关闭"),"individual restore clears only selected bypass "+item.Property);
}
p=Plan();p.BypassTPMCheck=true;p.BypassSecureBootCheck=true;Recommendations.SelectNetworkScene(p,"wifi");Check(p.BypassTPMCheck&&p.BypassSecureBootCheck&&!p.BypassRAMCheck,"network scene retains independent requirement choices");
Recommendations.Apply(p,"wired");Check(HardwareSettings.Items.All(item=>!HardwareSettings.Enabled(p,item))&&p.PreventDeviceEncryption&&p.RecommendationVersion==3,"full recommendation clears checks without changing other defaults or baseline version");
foreach(var version in new[]{1,2,3}){p=Plan();p.RecommendationVersion=version;p.RecommendationWindows=version==1?"Windows 11":"";p.PreventDeviceEncryption=version>=3;Check(Recommendations.Differences(p).Count==0,"historic default recommendations retain native requirement checks "+version);}
p=Plan();p.BypassTPMCheck=true;p.BypassRAMCheck=true;p.DiskMode=InstallationSettings.Wipe;p.TargetDiskId=2;p.DiskLayout="UEFI / GPT";p.ConfirmWipeDisk=true;xml=Xml(p);var pe=PECommands(xml);Check(pe.Where(cmd=>cmd.Path.Contains("LabConfig")).Max(c=>c.Order)<pe.Single(cmd=>cmd.Path.Contains("diskpart.exe")).Order,"partial checks execute before confirmed automatic disk partition command");
p=Plan();p.BypassHardware=true;p.DiskMode=InstallationSettings.Wipe;p.TargetDiskId=2;p.ConfirmWipeDisk=true;xml=Xml(p);pe=PECommands(xml);Check(pe.Count(c=>c.Path.Contains("LabConfig"))==3&&pe.Where(c=>c.Path.Contains("LabConfig")).Max(c=>c.Order)<pe.Single(c=>c.Path.Contains("diskpart.exe")).Order,"native all-checks path precedes confirmed disk partitioning without duplicates");
p=Plan();p.BypassTPMCheck=true;p.AccountMode="安装时添加微软账户";p.BypassNetwork=false;xml=Xml(p);Check(PECommands(xml).Any(c=>c.Path.Contains("BypassTPMCheck"))&&!DecodedXml(xml).Contains("BypassNRO"),"hardware bypass stays independent from online account and network bypass");
viewModel.Plan=InstallPlan.NewCustom();viewModel.Plan.BypassRAMCheck=true;Check(!viewModel.IsRecommended&&viewModel.ConfigurationSummary.Contains("绕过内存容量检查")&&viewModel.HelpTips["hardware"].Body.Contains("绕过内存容量检查"),"independent custom plan exposes current requirement selection");
Console.WriteLine($"Final pre17 total: {count} tests passed.");

// 工具主题偏好单独保存；损坏或不可写文件不会阻断配置工作。
var preferencesDirectory=Path.Combine(Path.GetTempPath(),"UnattendLocal-preferences-"+Guid.NewGuid().ToString("N"));
Directory.CreateDirectory(preferencesDirectory);
try{
 var preferencesPath=Path.Combine(preferencesDirectory,"ui-preferences.json");
 Check(UnattendLocal.Desktop.UiPreferences.Load(preferencesPath)=="Light","missing UI preference uses light theme");
 Check(UnattendLocal.Desktop.UiPreferences.Save("Dark",preferencesPath)&&UnattendLocal.Desktop.UiPreferences.Load(preferencesPath)=="Dark","dark theme persists independently");
 Check(UnattendLocal.Desktop.UiPreferences.Save("Light",preferencesPath)&&UnattendLocal.Desktop.UiPreferences.Load(preferencesPath)=="Light","light theme replaces previous preference");
 Check(!UnattendLocal.Desktop.UiPreferences.Save("Unsupported",preferencesPath)&&UnattendLocal.Desktop.UiPreferences.Load(preferencesPath)=="Light","invalid UI theme rejected without changing preference");
 foreach(var invalid in new[]{"{broken", "null", "[]", "{\"theme\":22}", "{\"theme\":\"Unknown\"}", new string('x',4097)}){File.WriteAllText(preferencesPath,invalid);Check(UnattendLocal.Desktop.UiPreferences.Load(preferencesPath)=="Light","invalid UI preferences safely fall back "+invalid.Length);}
 File.WriteAllText(preferencesPath,"block directory creation");
 Check(!UnattendLocal.Desktop.UiPreferences.Save("Dark",Path.Combine(preferencesPath,"settings.json")),"unwritable preference reports failure without throwing");
 Check(Directory.GetFiles(preferencesDirectory,"*.tmp").Length==0,"UI preference writes leave no temporary files");
}finally{Directory.Delete(preferencesDirectory,true);}
Console.WriteLine($"Final pre18 total: {count} tests passed.");


// pre19: target Windows colors, effective settings and preserved inactive choices.
Check(InstallPlan.New().ColorSettingsMode==ColorSettings.Default&&InstallPlan.NewCustom().ColorSettingsMode==ColorSettings.Default,"both new configuration modes retain native Windows colors");
p=PlanStore.Deserialize("{\"SchemaVersion\":1}");Check(p.ColorSettingsMode==ColorSettings.Default&&ColorSettings.Build(p) is Schneegans.Unattend.DefaultColorSettings,"older plans without color fields use default output");
p=Plan();p.WindowsColorTheme="invalid inactive";p.AppsColorTheme="invalid inactive";p.AccentColorHex="bad';command";p.AccentColorOnStart=true;p.AccentColorOnBorders=true;
Check(!DecodedXml(Xml(p)).Contains("SetColorTheme.ps1")&&!Recommendations.MissingInputs(p).Contains("强调色"),"default mode ignores inactive custom colors and emits no color script");
foreach(var architecture in new[]{"amd64","arm64","x86"})foreach(var system in new[]{ColorSettings.Dark,ColorSettings.Light})foreach(var apps in new[]{ColorSettings.Dark,ColorSettings.Light}){
 p=Plan();p.Architecture=architecture;p.ColorSettingsMode=ColorSettings.Custom;p.WindowsColorTheme=system;p.AppsColorTheme=apps;p.AccentColorHex=" #12aB34 ";p.EnableColorTransparency=apps==ColorSettings.Light;p.AccentColorOnStart=true;p.AccentColorOnBorders=system==ColorSettings.Dark;
 xml=Xml(p);var decoded=DecodedXml(xml);var colorConfig=(Schneegans.Unattend.CustomColorSettings)ColorSettings.Build(p);
 Check(colorConfig.AccentColor.A==255&&colorConfig.AccentColor.R==0x12&&colorConfig.AccentColor.G==0xAB&&colorConfig.AccentColor.B==0x34,"strict RGB mapping with opaque alpha "+architecture+system+apps);
 Check(decoded.Contains("$lightThemeSystem = "+(system==ColorSettings.Light?1:0)+";")&&decoded.Contains("$lightThemeApps = "+(apps==ColorSettings.Light?1:0)+";"),"independent system and app color modes "+architecture+system+apps);
 Check(decoded.Contains("$htmlAccentColor = '#12AB34';")&&decoded.Contains("$enableTransparency = "+(p.EnableColorTransparency?1:0)+";")&&decoded.Contains("$accentColorOnStart = "+(system==ColorSettings.Dark?1:0)+";"),"normalized color and effective transparency/start flags "+architecture+system+apps);
 Check(decoded.Contains(@"reg.exe add ""HKU\DefaultUser\Software\Microsoft\Windows\DWM"" /v ColorPrevalence /t REG_DWORD /d "+(p.AccentColorOnBorders?1:0)+" /f;")&&decoded.Contains("SetColorTheme.ps1")&&decoded.Contains("SystemUsesLightTheme")&&decoded.Contains("AppsUseLightTheme"),"native default-user border flag and per-user script "+architecture+system+apps);
 Check(p.AccentColorOnStart&&p.AccentColorHex==" #12aB34 "&&!xml.Contains("<DiskConfiguration>"),"effective mapping preserves caller values and disk protection "+architecture+system+apps);
}
p=Plan();p.ColorSettingsMode=ColorSettings.Custom;p.WindowsColorTheme=ColorSettings.Dark;p.AppsColorTheme=ColorSettings.Dark;p.EnableColorTransparency=false;p.AccentColorOnStart=false;p.AccentColorOnBorders=false;
var offColors=DecodedXml(Xml(p));Check(offColors.Contains("$enableTransparency = 0;")&&offColors.Contains("$accentColorOnStart = 0;"),"unchecked custom switches explicitly disabled");
foreach(var color in new[]{"#000000","#FFFFFF","#010203"}){p.AccentColorHex=color;Check(DecodedXml(Xml(p)).Contains("$htmlAccentColor = '"+color+"';"),"RGB boundaries remain exact hex "+color);}
foreach(var invalid in new[]{"", "red", "#FFF", "#11223344", "#12345G", "123456", "#12 3456", "#112233\n#445566", "#112233';Write-Host x", null}){p.AccentColorHex=invalid!;Reject(p,"invalid active accent color "+(invalid??"null"));}
p.AccentColorHex="#0078D4";p.WindowsColorTheme="Default";Reject(p,"unsupported active system color mode");p.WindowsColorTheme=ColorSettings.Dark;p.AppsColorTheme="";Reject(p,"missing active apps color mode");p.AppsColorTheme=ColorSettings.Light;p.ColorSettingsMode="Unknown";Reject(p,"unknown color settings mode");
p=Plan();p.ColorSettingsMode=ColorSettings.Custom;p.WindowsColorTheme=ColorSettings.Light;p.AppsColorTheme=ColorSettings.Dark;p.AccentColorHex="#010203";p.EnableColorTransparency=false;p.AccentColorOnStart=true;p.AccentColorOnBorders=true;
var colorsRoundTrip=PlanStore.Deserialize(PlanStore.Serialize(p,true));Check(colorsRoundTrip.ColorSettingsMode==p.ColorSettingsMode&&colorsRoundTrip.WindowsColorTheme==p.WindowsColorTheme&&colorsRoundTrip.AppsColorTheme==p.AppsColorTheme&&colorsRoundTrip.AccentColorHex==p.AccentColorHex&&!colorsRoundTrip.EnableColorTransparency&&colorsRoundTrip.AccentColorOnStart&&colorsRoundTrip.AccentColorOnBorders,"all seven color settings roundtrip with retained light-mode selection");
Check(!ColorSettings.StartAccentEnabled(colorsRoundTrip)&&ColorSettings.ModeSummary(colorsRoundTrip).Contains("不生效（系统浅色模式）"),"loaded light-mode plan reports effective start accent off");
Check(Recommendations.Differences(p).Single(d=>d.Property==nameof(InstallPlan.ColorSettingsMode)).Current.Contains("#010203"),"color recommendation difference shows meaningful current values");
Recommendations.Restore(p,nameof(InstallPlan.ColorSettingsMode));Check(p.ColorSettingsMode==ColorSettings.Default&&p.AccentColorHex=="#010203"&&p.AccentColorOnStart&&!DecodedXml(Xml(p)).Contains("SetColorTheme.ps1"),"single color restore retains choices but stops color generation");
p.ColorSettingsMode=ColorSettings.Custom;Recommendations.SelectNetworkScene(p,"wifi");Check(p.ColorSettingsMode==ColorSettings.Custom&&p.AccentColorHex=="#010203","network scene preserves target Windows colors");
Recommendations.Apply(p,"wired");Check(p.ColorSettingsMode==ColorSettings.Default&&p.AccentColorHex=="#010203"&&p.PreventDeviceEncryption&&p.RecommendationVersion==3,"full recommendation restores default colors without erasing choices or changing baseline");
viewModel.Plan=Plan();viewModel.Plan.ColorSettingsMode=ColorSettings.Custom;viewModel.Plan.WindowsColorTheme=ColorSettings.Dark;viewModel.StartAccentChecked=true;
viewModel.Preview="OLD COLOR PREVIEW";viewModel.Plan.WindowsColorTheme=ColorSettings.Light;
Check(!viewModel.CanUseStartAccent&&!viewModel.StartAccentChecked&&viewModel.Plan.AccentColorOnStart&&viewModel.HelpTips["accentstart"].Body.Contains("不生效")&&viewModel.Preview!="OLD COLOR PREVIEW","light-mode control and tip follow effective state and invalidate preview");
viewModel.StartAccentChecked=false;Check(viewModel.Plan.AccentColorOnStart,"disabled effective start-accent binding preserves retained choice");
viewModel.Plan.WindowsColorTheme=ColorSettings.Dark;Check(viewModel.CanUseStartAccent&&viewModel.StartAccentChecked,"dark system mode restores retained start-accent selection");
viewModel.AccentColorPreset="#E81123";Check(viewModel.Plan.AccentColorHex=="#E81123"&&viewModel.AccentColorPreview=="#E81123"&&viewModel.ConfigurationSummary.Contains("#E81123"),"common color picker updates editor swatch and configuration summary");
viewModel.Plan.AccentColorHex=" #a1b2c3 ";Check(viewModel.AccentColorPreset==null&&viewModel.AccentColorPreview=="#A1B2C3"&&viewModel.AccentColorHint.Contains("#A1B2C3"),"custom RGB input clears preset selection and updates normalized swatch");
viewModel.Plan.AccentColorHex="invalid";Check(viewModel.AccentColorPreview=="Transparent"&&viewModel.AccentColorPreset==null&&viewModel.AccentColorHint.Contains("请输入")&&viewModel.HelpTips["accentcolor"].Body.Contains("颜色无效")&&Recommendations.MissingInputs(viewModel.Plan).Contains("强调色"),"invalid color has safe preview and clear validation hints");
viewModel.Plan.ColorSettingsMode=ColorSettings.Default;Check(!viewModel.IsCustomColors&&!viewModel.CanUseStartAccent&&!viewModel.StartAccentChecked&&viewModel.WindowsColorsSummary.Contains("不写入")&&!Recommendations.MissingInputs(viewModel.Plan).Contains("强调色"),"default mode disables custom controls and ignores retained invalid color");
foreach(var historical in new[]{1,2,3}){p=Plan();p.RecommendationVersion=historical;p.RecommendationWindows=historical==1?"Windows 11":"";p.PreventDeviceEncryption=historical>=3;Check(Recommendations.Differences(p).Count==0,"historic recommendation keeps default target colors "+historical);}
Check(new[]{"windowscolors","systemcolortheme","appscolortheme","accentcolor","colortransparency","accentstart","accentborders"}.All(viewModel.HelpTips.ContainsKey),"all Chinese color tips registered without changing existing tips");
p=Plan();p.ColorSettingsMode=ColorSettings.Custom;p.AutoLogon=false;p.KeepSensitiveFiles=true;Check(!Xml(p).Contains("<AutoLogon>")&&DecodedXml(Xml(p)).Contains("SetColorTheme.ps1"),"colors do not force automatic login");
Console.WriteLine($"Final pre19 total: {count} tests passed.");

// pre23: native desktop solid-background generation and existing scheme/UI linkage.
Check(InstallPlan.New().WallpaperMode==WallpaperSettings.Default&&InstallPlan.NewCustom().WallpaperMode==WallpaperSettings.Default,"new recommended and custom schemes keep native background");
p=PlanStore.Deserialize("{\"SchemaVersion\":1}");Check(p.WallpaperMode==WallpaperSettings.Default&&WallpaperSettings.Build(p) is Schneegans.Unattend.DefaultWallpaperSettings,"old schemes without background fields retain default behavior");
p=Plan();p.WallpaperColorHex="bad';command";Check(!DecodedXml(Xml(p)).Contains("SetWallpaper.ps1")&&!Recommendations.MissingInputs(p).Contains("背景颜色"),"inactive invalid background value does not block or change generation");
foreach(var arch in new[]{"amd64","arm64","x86"})foreach(var hex in new[]{"#000000","#FFFFFF"," #12aB34 "}){
 p=Plan();p.Architecture=arch;p.WallpaperMode=WallpaperSettings.Solid;p.WallpaperColorHex=hex;xml=Xml(p);
 var wallpaperScript=Node(xml,"//*[local-name()='File' and contains(@path,'SetWallpaper.ps1')]")??"";
 var userWallpaper=Node(xml,"//*[local-name()='File' and contains(@path,'UserOnce.ps1')]")??"";
 var rgb=(Schneegans.Unattend.SolidWallpaperSettings)WallpaperSettings.Build(p);ColorSettings.TryNormalize(hex,out var normalized);
 Check(rgb.Color.A==255&&wallpaperScript.TrimEnd().EndsWith("Set-WallpaperColor -HtmlColor '"+normalized+"';")&&System.Drawing.ColorTranslator.ToHtml(rgb.Color)==normalized,"opaque normalized RGB maps to one native color invocation "+arch+normalized);
 Check(userWallpaper.Contains(@"& 'C:\Windows\Setup\Scripts\SetWallpaper.ps1';")&&!(Node(xml,"//*[local-name()='File' and contains(@path,'Specialize.ps1')]")??"").Contains("SetWallpaper.ps1"),"background script executes in each-user first-logon stage "+arch+normalized);
 Check(wallpaperScript.Contains("-Name 'BackgroundType' -Type 'DWord' -Value 1")&&wallpaperScript.Contains("-Name 'WallPaper' -Type 'String' -Value ''")&&wallpaperScript.Contains("-Name 'Background' -Type 'String'")&&!wallpaperScript.Contains("Remove-Item"),"native solid background clears image display without deleting image files "+arch+normalized);
 Check(!DecodedXml(xml).Contains("GetWallpaper.ps1")&&!DecodedXml(xml).Contains("SetColorTheme.ps1")&&!DecodedXml(xml).Contains("LockScreenImage")&&!xml.Contains("<DiskConfiguration>"),"background stays independent of image loaders colors lock screen and disk defaults "+arch+normalized);
}
p=Plan();p.WallpaperMode=WallpaperSettings.Solid;
foreach(var invalid in new[]{"", "red", "#FFF", "#00112233", "#12345G", "123456", "#112233';Write-Host x", null}){p.WallpaperColorHex=invalid!;Reject(p,"invalid active background RGB rejected "+(invalid??"null"));}
p.WallpaperColorHex="#000000";p.WallpaperMode="Unknown";Reject(p,"unknown desktop background mode rejected");
p=Plan();p.WallpaperMode=WallpaperSettings.Solid;p.WallpaperColorHex=" #0123aB ";var wallpaperSaved=PlanStore.Deserialize(PlanStore.Serialize(p,true));Check(wallpaperSaved.WallpaperMode==WallpaperSettings.Solid&&wallpaperSaved.WallpaperColorHex==p.WallpaperColorHex&&WallpaperSettings.ModeSummary(wallpaperSaved).Contains("#0123AB"),"both background fields roundtrip without mutating entered value");
Check(Recommendations.Differences(p).Single(d=>d.Property==nameof(InstallPlan.WallpaperMode)).Current.Contains("#0123AB"),"background difference reports selected color");
Recommendations.Restore(p,nameof(InstallPlan.WallpaperMode));Check(p.WallpaperMode==WallpaperSettings.Default&&p.WallpaperColorHex==" #0123aB "&&!DecodedXml(Xml(p)).Contains("SetWallpaper.ps1"),"single background restore omits script and retains personal color");
p.WallpaperMode=WallpaperSettings.Solid;Recommendations.SelectNetworkScene(p,"wifi");Check(p.WallpaperMode==WallpaperSettings.Solid&&p.WallpaperColorHex==" #0123aB ","network scene retains background selection");
Recommendations.Apply(p,"wired");Check(p.WallpaperMode==WallpaperSettings.Default&&p.WallpaperColorHex==" #0123aB "&&p.PreventDeviceEncryption&&p.RecommendationVersion==3,"full recommendation restores default background and retains baseline and encryption prevention");
p.WallpaperMode=WallpaperSettings.Solid;p.ColorSettingsMode=ColorSettings.Custom;p.AccentColorHex="#E81123";var separateColors=DecodedXml(Xml(p));Check(separateColors.Contains("$htmlAccentColor = '#E81123';")&&separateColors.Contains("Set-WallpaperColor -HtmlColor '#0123AB';"),"background RGB and Windows accent RGB remain independent");
p.AutoLogon=false;p.KeepSensitiveFiles=true;Check(!Xml(p).Contains("<AutoLogon>")&&(Node(Xml(p),"//*[local-name()='File' and contains(@path,'UserOnce.ps1')]")??"").Contains("SetWallpaper.ps1"),"solid background does not force automatic login");
foreach(var historical in new[]{1,2,3}){p=Plan();p.RecommendationVersion=historical;p.RecommendationWindows=historical==1?"Windows 11":"";p.PreventDeviceEncryption=historical>=3;Check(Recommendations.Differences(p).Count==0,"historic recommendations retain native background "+historical);}
viewModel.Plan=Plan();viewModel.Preview="OLD WALLPAPER PREVIEW";viewModel.Plan.WallpaperMode=WallpaperSettings.Solid;viewModel.WallpaperColorPreset="#003366";
Check(viewModel.IsSolidWallpaper&&viewModel.Plan.WallpaperColorHex=="#003366"&&viewModel.WallpaperColorPreview=="#003366"&&viewModel.ConfigurationSummary.Contains("桌面背景：纯色背景 #003366")&&viewModel.Preview!="OLD WALLPAPER PREVIEW","mode picker swatch summary and preview invalidation stay linked");
viewModel.Plan.WallpaperColorHex=" #a1b2c3 ";Check(viewModel.WallpaperColorPreset==null&&viewModel.WallpaperColorPreview=="#A1B2C3"&&viewModel.HelpTips["wallpapercolor"].Body.Contains("#A1B2C3"),"custom background color normalizes and clears common-color selection");
viewModel.Plan.WallpaperColorHex="invalid";Check(viewModel.WallpaperColorPreview=="Transparent"&&viewModel.WallpaperColorHint.Contains("请输入")&&Recommendations.MissingInputs(viewModel.Plan).Contains("背景颜色")&&viewModel.HelpTips["wallpapercolor"].Body.Contains("颜色无效"),"invalid active background has safe swatch and export-check hints");
viewModel.Plan.WallpaperMode=WallpaperSettings.Default;Check(!viewModel.IsSolidWallpaper&&!Recommendations.MissingInputs(viewModel.Plan).Contains("背景颜色")&&viewModel.DesktopWallpaperSummary.Contains("不写入")&&viewModel.Plan.WallpaperColorHex=="invalid","default background hides picker and ignores but retains inactive color");
viewModel.Plan=Plan();viewModel.Plan.WallpaperMode=WallpaperSettings.Solid;viewModel.Plan.WallpaperColorHex="#202020";Check(viewModel.IsSolidWallpaper&&viewModel.WallpaperColorPreset=="#202020"&&viewModel.HelpTips["wallpaper"].Body.Contains("#202020"),"loaded background scheme refreshes UI and current help");
Check(viewModel.HelpTips["wallpaper"].Body.Contains("不删除图片文件")&&viewModel.Plan.ColorSettingsMode==ColorSettings.Default&&viewModel.Plan.VisualEffectsMode==VisualEffectsSettings.Default,"background help describes scope while existing colors and visual effects stay native");
Console.WriteLine($"Final pre23 total: {count} tests passed.");

// pre24: scripts are generated only; no partition/install/script commands are executed here.
InstallPlan ScriptDisk(){var q=Plan();q.DiskMode=DiskScriptSettings.Mode;q.TargetDiskId=0;q.TargetPartitionId=3;q.DiskPartScript=DiskScriptSettings.Example;q.ConfirmDiskScript=true;return q;}
p=ScriptDisk();xml=Xml(p);Check(InstallationSettings.Destructive(p)&&xml.Contains("UnattendLocal-diskpart.txt"),"custom disk script treated as hazardous and scheduled before imaging");
Check(Node(xml,"//u:InstallTo/u:DiskID")=="0"&&Node(xml,"//u:InstallTo/u:PartitionID")=="3"&&!xml.Contains("<DiskConfiguration>"),"script image target explicit and no generated format conflicts");
var scriptDoc=new XmlDocument();scriptDoc.LoadXml(xml);var scriptNs=new XmlNamespaceManager(scriptDoc.NameTable);scriptNs.AddNamespace("u","urn:schemas-microsoft-com:unattend");
var paths=scriptDoc.SelectNodes("//u:settings[@pass='windowsPE']//u:RunSynchronousCommand/u:Path",scriptNs)!.Cast<XmlNode>().Select(n=>n.InnerText).ToArray();
Check(paths.All(s=>s.Length<=255)&&paths.Last().Contains("UnattendLocal-diskpart.cmd"),"PE command length and runner ordering fit schema constraints");
Check(paths.Any(s=>s.Contains("goto fail"))&&paths.Any(s=>s.Contains("if errorlevel 1"))&&paths.Any(s=>s.Contains("pause")),"diskpart error runner remains halted for inspection");
foreach(var line in DiskScriptSettings.Lines(p.DiskPartScript))Check(paths.Any(s=>s.Contains(Schneegans.Unattend.EchoProcessor.Process(new[]{line}).Single())),"upstream diskpart example escaped and retained: "+line);
p.DiskPartScript+="\nrem edited";Check(!p.ConfirmDiskScript,"script edits clear consent");Reject(p,"unconfirmed script rejected");p.ConfirmDiskScript=true;
foreach(var change in new Action<InstallPlan>[] {q=>q.TargetDiskId=2,q=>q.TargetPartitionId=4,q=>q.DiskMode=InstallationSettings.Manual,q=>q.Architecture="arm64"}){p=ScriptDisk();change(p);Check(!p.ConfirmDiskScript,"disk script target/mode/architecture edits clear consent");}
p=ScriptDisk();var scriptSaved=PlanStore.Deserialize(PlanStore.Serialize(p,true));Check(scriptSaved.DiskPartScript==p.DiskPartScript&&!scriptSaved.ConfirmDiskScript&&p.ConfirmDiskScript,"script roundtrip retains content and clears only loaded confirmation");Reject(scriptSaved,"reopened script requires explicit consent");
foreach(var text in new[]{"", " ","SELECT DISK=0 & del X:\\marker", "CLEAN | cmd", "rem %PATH%", "rem !PATH!", "rem ^x", "rem >x", "rem (x)", "rem 中文", "clean noerr", "rem\u0000bad", new string('a',161),string.Join('\n',Enumerable.Repeat("rem x",513)),new string('a',128*1024+1)}){p=ScriptDisk();p.DiskPartScript=text;p.ConfirmDiskScript=true;Reject(p,"invalid custom diskpart source rejected: "+text[..Math.Min(25,text.Length)]);}
p=ScriptDisk();p.TargetPartitionId=0;p.ConfirmDiskScript=true;Reject(p,"script Windows target partition must be positive");p=ScriptDisk();p.TargetDiskId=null;p.ConfirmDiskScript=true;Reject(p,"script Windows target disk must be explicit");
p=Plan();p.DiskPartScript="invalid &";Check(!Xml(p).Contains("UnattendLocal-diskpart.txt"),"inactive disk script ignored");p.DiskMode=DiskScriptSettings.Mode;Check(Recommendations.MissingInputs(p).Contains("分区编号"),"script partition target appears in review");p.DiskPartScript="";Check(Recommendations.MissingInputs(p).Contains("DiskPart"),"empty script placeholder cannot satisfy required content");
viewModel.Plan=ScriptDisk();Check(viewModel.IsScriptDisk&&viewModel.IsTargetDisk&&viewModel.IsPartitionTarget&&viewModel.IsDestructiveDisk&&viewModel.DiskRisk.Contains("任意磁盘"),"script mode target controls summary and risk linked");
p=Plan();Check(p.Scripts.Count==0&&!p.RestartExplorerAfterScripts&&CustomScripts.Build(p).Scripts.Count()==0,"old and new default have no custom scripts");
foreach(var phase in Enum.GetValues<Schneegans.Unattend.ScriptPhase>())foreach(var type in Schneegans.Unattend.ScriptExtensions.GetAllowedTypes(phase)){
 p=Plan();var content=type==Schneegans.Unattend.ScriptType.Reg?"[HKEY_USERS\\DefaultUser\\Software\\Test]\n\"x\"=dword:00000001":"echo phase-marker <&中文>";
 p.Scripts.Add(new ScriptEntry{Phase=phase.ToString(),Type=type.ToString(),Content=content});xml=Xml(p);
 var file=Node(xml,"//*[local-name()='File' and contains(@path,'unattend-01.')]")??"";var sequence=phase switch{Schneegans.Unattend.ScriptPhase.System=>"Specialize.ps1",Schneegans.Unattend.ScriptPhase.DefaultUser=>"DefaultUser.ps1",Schneegans.Unattend.ScriptPhase.FirstLogon=>"FirstLogon.ps1",_=>"UserOnce.ps1"};
 Check(content.Split('\n').All(line=>file.Contains(line))&&(Node(xml,"//*[local-name()='File' and contains(@path,'"+sequence+"')]")??"").Contains("unattend-01."+type.ToString().ToLowerInvariant()),"script embedded as data and called in correct phase: "+phase+type);
 if(type==Schneegans.Unattend.ScriptType.Reg)Check(file.StartsWith("\n\t\t\tWindows Registry")||file.Contains("Windows Registry Editor Version 5.00"),"REG header automatically supplied");
}
p=Plan();p.Scripts.Add(new ScriptEntry{Content="first-marker"});p.Scripts.Add(new ScriptEntry{Content="second-marker"});xml=Xml(p);Check(Node(xml,"//*[local-name()='File' and contains(@path,'unattend-01.ps1')]")!.Contains("first-marker")&&Node(xml,"//*[local-name()='File' and contains(@path,'unattend-02.ps1')]")!.Contains("second-marker"),"same-phase scripts preserve list order");
p.Scripts.Move(1,0);xml=Xml(p);Check(Node(xml,"//*[local-name()='File' and contains(@path,'unattend-01.ps1')]")!.Contains("second-marker"),"reordering scripts changes execution order");
p.Scripts[0].Enabled=false;xml=Xml(p);Check(!DecodedXml(xml).Contains("second-marker"),"disabled scripts are omitted");
var scriptsRoundtrip=PlanStore.Deserialize(PlanStore.Serialize(p,false));Check(scriptsRoundtrip.Scripts.Count==2&&!scriptsRoundtrip.Scripts[0].Enabled&&scriptsRoundtrip.Scripts[0].Content=="second-marker"&&!PlanStore.Serialize(p,false).Contains("\"Example\""),"script order enabled and body retained without saving example guidance");
foreach(var row in new[]{new ScriptEntry(),new ScriptEntry{Phase="999",Content="x"},new ScriptEntry{Type="999",Content="x"},new ScriptEntry{Phase="DefaultUser",Type="Vbs",Content="x"},new ScriptEntry{Content="x\u0000"}}){p=Plan();p.Scripts.Add(row);Reject(p,"invalid enabled script rejected "+row.Phase+row.Type);row.Enabled=false;Check(!DecodedXml(Xml(p)).Contains("unattend-01"),"disabled invalid script ignored");}
p=Plan();for(int i=0;i<16;i++)p.Scripts.Add(new ScriptEntry{Content="echo marker"});Reject(p,"phase count above 15 rejected");p.Scripts.Last().Enabled=false;Check(CustomScripts.Build(p).Scripts.Count()==15,"15 enabled scripts per phase supported");
p.RestartExplorerAfterScripts=true;Check(CustomScripts.Build(p).RestartExplorer,"explicit script Explorer restart mapped");
viewModel.Plan=Plan();var scriptRow=new ScriptEntry{Content="a"};viewModel.Plan.Scripts.Add(scriptRow);viewModel.Preview="stale";scriptRow.Content="b";Check(!viewModel.Preview.Contains("stale")&&viewModel.ConfigurationSummary.Contains("系统安装时 1 条"),"script edits invalidate preview and refresh summary");viewModel.Preview="stale";scriptRow.Enabled=false;Check(!viewModel.Preview.Contains("stale")&&viewModel.ConfigurationSummary.Contains("系统安装时 0 条"),"enabled change updates count and invalidates preview");viewModel.Plan.Scripts.Remove(scriptRow);viewModel.Preview="current";scriptRow.Content="detached";Check(viewModel.Preview=="current","removed script observers detach");
foreach(var phase in new[]{"System","DefaultUser","FirstLogon","UserOnce"}){
 p=Plan();p.Scripts.Add(new ScriptEntry{Phase=phase,Type="Js",Content="WScript.Echo('legacy-marker');"});
 Reject(p,"removed JScript blocked in every phase: "+phase);
 var legacy=PlanStore.Deserialize(PlanStore.Serialize(p,true));
 Check(legacy.Scripts.Count==1&&!legacy.Scripts[0].Enabled&&legacy.Scripts[0].Type=="Js"&&legacy.Scripts[0].Content==p.Scripts[0].Content,"legacy JS retained and disabled: "+phase);
 Check(!DecodedXml(Xml(legacy)).Contains("legacy-marker")&&!DecodedXml(Xml(legacy)).Contains("//E:jscript"),"legacy JS absent from generated XML: "+phase);
 legacy.Scripts[0].Enabled=true;Reject(legacy,"reenabled legacy JS rejected: "+phase);
 legacy.Scripts[0].Enabled=false;var again=PlanStore.Deserialize(PlanStore.Serialize(legacy,false));Check(again.Scripts[0].Content==legacy.Scripts[0].Content&&!again.Scripts[0].Enabled,"disabled JS remains preserved after resave: "+phase);
}
Check(!Enum.GetNames<Schneegans.Unattend.ScriptType>().Contains("Js"),"upstream generation enum has no JS");
p=Plan();p.Scripts.Add(new ScriptEntry{Type="js",Content="old"});var lower=PlanStore.Deserialize(PlanStore.Serialize(p,true));Check(!lower.Scripts[0].Enabled&&lower.Scripts[0].Hint.Contains("已停止支持"),"case variant legacy JS disabled with visible guidance");
Check(DiskSafety.Risk.Contains("任意磁盘")&&DiskSafety.Preparation.Contains("授权")&&DiskSafety.Responsibility.Contains("不免除")&&DiskSafety.Responsibility.Contains("法定权利"),"disk safety text covers scope authorization and statutory rights");
// Initial keyboard states: inspect generated data only; never execute registry commands.
foreach(var arch in new[]{"amd64","arm64","x86"}){
 p=Plan();p.Architecture=arch;
 Check(!DecodedXml(Xml(p)).Contains("InitialKeyboardIndicators")&&!DecodedXml(Xml(p)).Contains("Scancode Map"),"default keyboard states generate no override: "+arch);
 for(var bits=0;bits<8;bits++){
  p=Plan();p.Architecture=arch;p.LockKeyMode=KeyboardLockSettings.Custom;p.InitialCapsLockOn=(bits&1)!=0;p.InitialNumLockOn=(bits&2)!=0;p.InitialScrollLockOn=(bits&4)!=0;
  xml=Xml(p);var defaultScript=Node(xml,"//*[local-name()='File' and contains(@path,'DefaultUser.ps1')]")??"";
  Check(defaultScript.Contains("InitialKeyboardIndicators")&&defaultScript.Contains("-Value "+bits+" -Force")&&defaultScript.Contains("Registry::HKU\\.DEFAULT")&&defaultScript.Contains("Registry::HKU\\DefaultUser")&&!DecodedXml(xml).Contains("Scancode Map"),"keyboard indicator bits and default-user stage; normal key behavior: "+arch+bits);
 }
}
p=Plan();p.LockKeyMode="invalid";Reject(p,"invalid active keyboard mode rejected");
p=Plan();p.LockKeyMode=KeyboardLockSettings.Custom;p.InitialCapsLockOn=true;p.InitialNumLockOn=false;p.InitialScrollLockOn=true;
foreach(var secrets in new[]{false,true}){var round=PlanStore.Deserialize(PlanStore.Serialize(p,secrets));Check(round.LockKeyMode==p.LockKeyMode&&round.InitialCapsLockOn&&!round.InitialNumLockOn&&round.InitialScrollLockOn,"keyboard mode and three states roundtrip: "+secrets);}
var legacyKeyboard=PlanStore.Deserialize("{\"SchemaVersion\":1,\"Users\":[]}");Check(legacyKeyboard.LockKeyMode==KeyboardLockSettings.Default,"old plan without keyboard fields keeps Windows default");
Check(InstallPlan.New().LockKeyMode==KeyboardLockSettings.Default&&InstallPlan.NewCustom().LockKeyMode==KeyboardLockSettings.Default,"recommended and custom new plans have no keyboard override");
Check(Recommendations.Differences(p).Single(d=>d.Property==nameof(InstallPlan.LockKeyMode)).Current.Contains("Num Lock 关闭"),"keyboard difference shows effective three-key values");
p.Ssid="KeyboardTest";p.WifiPassword="test12345";Recommendations.SelectNetworkScene(p,"wifi");Check(p.LockKeyMode==KeyboardLockSettings.Custom&&p.InitialCapsLockOn&&!p.InitialNumLockOn&&p.InitialScrollLockOn,"network scene preserves keyboard settings");
Recommendations.Restore(p,nameof(InstallPlan.LockKeyMode));Check(p.LockKeyMode==KeyboardLockSettings.Default&&p.InitialCapsLockOn&&!p.InitialNumLockOn&&p.InitialScrollLockOn&&!DecodedXml(Xml(p)).Contains("InitialKeyboardIndicators"),"single restore disables override while retaining selected states");
p.LockKeyMode=KeyboardLockSettings.Custom;Recommendations.Apply(p,"wired");Check(p.LockKeyMode==KeyboardLockSettings.Default&&p.InitialCapsLockOn&&!p.InitialNumLockOn&&p.InitialScrollLockOn,"full recommendation restore retains personal keyboard states");
p.LockKeyMode=KeyboardLockSettings.Custom;p.AutoLogon=false;p.KeepSensitiveFiles=true;Check(!Xml(p).Contains("<AutoLogon>")&&DecodedXml(Xml(p)).Contains("InitialKeyboardIndicators"),"keyboard initialization does not require automatic logon");
viewModel.Plan=Plan();viewModel.Preview="stale-keyboard";viewModel.Plan.LockKeyMode=KeyboardLockSettings.Custom;Check(viewModel.IsCustomLockKeys&&!viewModel.Preview.Contains("stale-keyboard"),"keyboard mode enables controls and invalidates preview");
viewModel.Preview="stale-keyboard";viewModel.Plan.InitialNumLockOn=false;Check(!viewModel.Preview.Contains("stale-keyboard")&&viewModel.LockKeyStateSummary.Contains("Num Lock 关闭")&&viewModel.ConfigurationSummary.Contains("键盘初始状态："),"keyboard flags update effective summary and preview");
viewModel.Plan.LockKeyMode=KeyboardLockSettings.Default;Check(!viewModel.IsCustomLockKeys&&viewModel.HelpTips["numlock"].Body.Contains("不参与生成")&&!DecodedXml(Xml(viewModel.Plan)).Contains("InitialKeyboardIndicators"),"default mode disables controls and updates help with preserved inactive states");
// Account lockout: inspect generated commands only; do not run net accounts.
foreach(var arch in new[]{"amd64","arm64","x86"}){
 p=Plan();p.Architecture=arch;Check(!DecodedXml(Xml(p)).Contains("/lockoutthreshold:"),"Windows default lockout emits no override: "+arch);
 p.AccountLockoutMode=AccountLockoutPolicy.Disabled;xml=Xml(p);var installScript=Node(xml,"//*[local-name()='File' and contains(@path,'Specialize.ps1')]")??"";
 Check(installScript.Contains("net.exe accounts /lockoutthreshold:0;")&&!DecodedXml(xml).Contains("/lockoutduration:"),"disabled lockout runs only threshold command in specialize: "+arch);
 p.AccountLockoutMode=AccountLockoutPolicy.Custom;p.LockoutThreshold=5;p.LockoutResetMinutes=8;p.LockoutDurationMinutes=12;xml=Xml(p);
 Check((Node(xml,"//*[local-name()='File' and contains(@path,'Specialize.ps1')]")??"").Contains("net.exe accounts /lockoutthreshold:5 /lockoutduration:12 /lockoutwindow:8;"),"custom lockout maps all three parameters in correct order: "+arch);
 p.LockoutThreshold=0;Check(DecodedXml(Xml(p)).Contains("/lockoutthreshold:0 /lockoutduration:12 /lockoutwindow:8")&&AccountLockoutPolicy.IsDisabled(p),"custom zero threshold follows upstream with disabled warning: "+arch);
 p.LockoutThreshold=999;p.LockoutResetMinutes=99999;p.LockoutDurationMinutes=99999;Check(DecodedXml(Xml(p)).Contains("/lockoutthreshold:999 /lockoutduration:99999 /lockoutwindow:99999"),"upper bounds accepted: "+arch);
 p.LockoutThreshold=1;p.LockoutResetMinutes=1;p.LockoutDurationMinutes=1;Check(DecodedXml(Xml(p)).Contains("/lockoutthreshold:1 /lockoutduration:1 /lockoutwindow:1"),"lower positive bounds accepted: "+arch);
}
foreach(var change in new Action<InstallPlan>[] {q=>q.LockoutThreshold=null,q=>q.LockoutThreshold=-1,q=>q.LockoutThreshold=1000,q=>q.LockoutResetMinutes=null,q=>q.LockoutResetMinutes=0,q=>q.LockoutResetMinutes=100000,q=>q.LockoutDurationMinutes=null,q=>q.LockoutDurationMinutes=0,q=>q.LockoutDurationMinutes=100000,q=>{q.LockoutResetMinutes=11;q.LockoutDurationMinutes=10;}}){p=Plan();p.AccountLockoutMode=AccountLockoutPolicy.Custom;change(p);Reject(p,"invalid custom lockout parameter blocked");Check(Recommendations.MissingInputs(p).Contains(AccountLockoutPolicy.ValidationError(p)!),"invalid custom parameter appears in export review");}
p=Plan();p.AccountLockoutMode="invalid";Reject(p,"unknown lockout mode rejected");
p.AccountLockoutMode=AccountLockoutPolicy.Default;p.LockoutThreshold=null;p.LockoutResetMinutes=-1;p.LockoutDurationMinutes=0;Check(!DecodedXml(Xml(p)).Contains("/lockoutthreshold:"),"default ignores inactive invalid numeric fields");
p.AccountLockoutMode=AccountLockoutPolicy.Disabled;Check(DecodedXml(Xml(p)).Contains("/lockoutthreshold:0;"),"disabled mode ignores unused custom numeric fields");
p=Plan();p.AccountLockoutMode=AccountLockoutPolicy.Custom;p.LockoutThreshold=8;p.LockoutResetMinutes=15;p.LockoutDurationMinutes=20;
foreach(var secrets in new[]{true,false}){var roundtrip=PlanStore.Deserialize(PlanStore.Serialize(p,secrets));Check(roundtrip.AccountLockoutMode==p.AccountLockoutMode&&roundtrip.LockoutThreshold==8&&roundtrip.LockoutResetMinutes==15&&roundtrip.LockoutDurationMinutes==20,"lockout fields roundtrip: "+secrets);}
Check(PlanStore.Deserialize("{\"SchemaVersion\":1,\"Users\":[]}").AccountLockoutMode==AccountLockoutPolicy.Default&&InstallPlan.New().AccountLockoutMode==AccountLockoutPolicy.Default&&InstallPlan.NewCustom().AccountLockoutMode==AccountLockoutPolicy.Default,"legacy and both new modes keep Windows default lockout");
Check(Recommendations.Differences(p).Single(d=>d.Property==nameof(InstallPlan.AccountLockoutMode)).Current.Contains("锁定 20 分钟"),"lockout recommendation difference describes effective values");
p.Ssid="LockoutTest";p.WifiPassword="test12345";Recommendations.SelectNetworkScene(p,"wifi");Check(p.AccountLockoutMode==AccountLockoutPolicy.Custom&&p.LockoutThreshold==8&&p.LockoutResetMinutes==15&&p.LockoutDurationMinutes==20,"network scene preserves lockout configuration");
Recommendations.Restore(p,nameof(InstallPlan.AccountLockoutMode));Check(p.AccountLockoutMode==AccountLockoutPolicy.Default&&p.LockoutThreshold==8&&p.LockoutResetMinutes==15&&p.LockoutDurationMinutes==20&&!DecodedXml(Xml(p)).Contains("/lockoutthreshold:"),"restore lockout default retains personal numeric fields without generating them");
p.AccountLockoutMode=AccountLockoutPolicy.Custom;Recommendations.Apply(p,"wired");Check(p.AccountLockoutMode==AccountLockoutPolicy.Default&&p.LockoutThreshold==8&&p.LockoutResetMinutes==15&&p.LockoutDurationMinutes==20,"full recommendation resets mode only");
p.AccountLockoutMode=AccountLockoutPolicy.Custom;p.AutoLogon=false;p.KeepSensitiveFiles=true;Check(!Xml(p).Contains("<AutoLogon>")&&DecodedXml(Xml(p)).Contains("/lockoutthreshold:8"),"lockout does not require auto logon");
foreach(var accountMode in new[]{"安装时创建本地账户","安装时添加微软账户"}){p=Plan();p.AccountMode=accountMode;p.BypassNetwork=false;p.HideOnlineAccount=accountMode!="安装时添加微软账户";p.AccountLockoutMode=AccountLockoutPolicy.Custom;Check(DecodedXml(Xml(p)).Contains("/lockoutthreshold:10"),"local lockout config remains independent of OOBE account mode: "+accountMode);}
viewModel.Plan=Plan();viewModel.Preview="old-policy";viewModel.Plan.AccountLockoutMode=AccountLockoutPolicy.Custom;Check(viewModel.IsCustomAccountLockout&&!viewModel.IsAccountLockoutDisabled&&!viewModel.Preview.Contains("old-policy"),"custom lockout editor and preview linked");
viewModel.Preview="old-policy";viewModel.Plan.LockoutDurationMinutes=15;Check(viewModel.AccountLockoutSummary.Contains("锁定 15 分钟")&&viewModel.ConfigurationSummary.Contains("账户锁定策略：")&&!viewModel.Preview.Contains("old-policy"),"lockout numeric edit updates summaries and invalidates preview");
viewModel.Plan.LockoutThreshold=0;Check(viewModel.IsAccountLockoutDisabled&&viewModel.HelpTips["accountlockout"].Body.Contains("已禁用"),"zero threshold warning and help reflect disabled state");
viewModel.Plan.AccountLockoutMode=AccountLockoutPolicy.Default;Check(!viewModel.IsCustomAccountLockout&&!viewModel.IsAccountLockoutDisabled&&viewModel.HelpTips["lockoutthreshold"].Body.Contains("不参与生成"),"default mode hides editor and warning with inactive guidance");
// Search real choices, without changing any configuration or generation data.
string Search(GeneratorService.Choice c)=>UnattendLocal.Desktop.SearchTextMatcher.SearchText(c.Label,c.Id);
bool Match(GeneratorService.Choice c,string q)=>UnattendLocal.Desktop.SearchTextMatcher.Matches(Search(c),q);
Check(g.Regions.Where(c=>Match(c,"中国")).Any(c=>c.Id=="45"),"Chinese country query returns original China identifier");
Check(g.Languages.Where(c=>Match(c,"中国")).Any(c=>c.Id.Equals("zh-CN",StringComparison.OrdinalIgnoreCase)),"country alias finds Simplified Chinese language without renaming label");
Check(g.Locales.Any(c=>Match(c,"ZH-HANS-CN")&&c.Id.Equals("zh-Hans-CN",StringComparison.OrdinalIgnoreCase)),"case-insensitive locale identifier search");
Check(g.TimeZones.Any(c=>Match(c,"china time")&&c.Id=="China Standard Time"),"multiple terms match underlying timezone identifier");
Check(g.TimeZones.Any(c=>Match(c,"中国")&&c.Id=="China Standard Time"),"Chinese timezone alias matches original value");
Check(g.Editions.Where(c=>Match(c,"pro")).All(c=>Search(c).Contains("pro",StringComparison.OrdinalIgnoreCase)),"SKU substring query keeps only matching original choices");
Check(g.Regions.All(c=>Match(c,"  "))&&g.Languages.All(c=>Match(c,"")),"empty or cleared query restores complete list");
Check(!g.Regions.Any(c=>Match(c,"no-such-country-483929")),"unmatched query has no candidates");
Check(UnattendLocal.Desktop.SearchTextMatcher.Matches("Windows x64 / AMD64","Ｘ６４"),"full-width input matches ASCII architecture");
Check(UnattendLocal.Desktop.SearchTextMatcher.Matches("初始状态 Num Lock","num lock")&&!UnattendLocal.Desktop.SearchTextMatcher.Matches("Num Lock","num scroll"),"space-separated terms must all match");
Check(UnattendLocal.Desktop.SearchTextMatcher.Matches("禁用小组件","小组")&&!UnattendLocal.Desktop.SearchTextMatcher.Matches("禁用小组件","安全"),"Chinese substring search for ordinary policy values");
Check(UnattendLocal.Desktop.SearchTextMatcher.Matches("系统安装时 System","system")&&UnattendLocal.Desktop.SearchTextMatcher.Matches("PowerShell .ps1 Ps1","ps1"),"script choices searchable by original tags");
// Fast Startup: verify upstream command, default omission and configuration flow.
Check(!InstallPlan.New().DisableFastStartup&&!InstallPlan.NewCustom().DisableFastStartup&&!PlanStore.Deserialize("{\"SchemaVersion\":1,\"Users\":[]}").DisableFastStartup,"new and legacy configurations keep Windows default Fast Startup");
foreach(var arch in new[]{"amd64","arm64","x86"}) {
 p=Plan();p.Architecture=arch;Check(!DecodedXml(Xml(p)).Contains("HiberbootEnabled"),"default Fast Startup emits no override: "+arch);
 p.DisableFastStartup=true;xml=Xml(p);var startupScript=Node(xml,"//*[local-name()='File' and contains(@path,'Specialize.ps1')]")??"";
 Check(startupScript.Contains(@"HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\Power")&&startupScript.Contains("/v HiberbootEnabled /t REG_DWORD /d 0 /f;"),"Fast Startup follows upstream specialize command: "+arch);
 Check(!DecodedXml(xml).Contains("powercfg"),"Fast Startup does not disable hibernation: "+arch);
 p.DisableFastStartup=false;Check(!DecodedXml(Xml(p)).Contains("HiberbootEnabled"),"unchecking omits override again: "+arch);
}
p=Plan();p.DisableFastStartup=true;
foreach(var secrets in new[]{true,false})Check(PlanStore.Deserialize(PlanStore.Serialize(p,secrets)).DisableFastStartup,"Fast Startup choice persists: "+secrets);
Check(Recommendations.Differences(p).Any(d=>d.Property==nameof(InstallPlan.DisableFastStartup))&&Recommendations.Summary(p).Contains("快速启动：禁用"),"Fast Startup appears in differences and summary");
Recommendations.SelectNetworkScene(p,"wired");Check(p.DisableFastStartup,"network scene preserves Fast Startup choice");
Recommendations.Restore(p,nameof(InstallPlan.DisableFastStartup));Check(!p.DisableFastStartup,"single recommended restore keeps default Fast Startup");
p.DisableFastStartup=true;Recommendations.Apply(p,"wired");Check(!p.DisableFastStartup,"full recommendation restores default Fast Startup");
p=Plan();p.DisableFastStartup=true;p.AutoLogon=false;p.KeepSensitiveFiles=true;Check(DecodedXml(Xml(p)).Contains("HiberbootEnabled"),"Fast Startup does not depend on automatic logon");
viewModel.Plan=Plan();viewModel.Preview="stale-startup";viewModel.Plan.DisableFastStartup=true;
Check(!viewModel.Preview.Contains("stale-startup")&&viewModel.ConfigurationSummary.Contains("快速启动：禁用")&&viewModel.HelpTips["faststartup"].Body.Contains("当前：在目标系统关闭快速启动"),"Fast Startup updates help, summary and invalidates preview");
viewModel.Plan.DisableFastStartup=false;Check(viewModel.ConfigurationSummary.Contains("快速启动：遵循 Windows 默认行为")&&viewModel.HelpTips["faststartup"].Body.Contains("当前：遵循 Windows 默认行为"),"unchecked Fast Startup updates default wording");
// Long paths and system sound configuration remain independent and opt-in.
Check(!InstallPlan.New().EnableLongPaths&&!InstallPlan.New().TurnOffSystemSounds&&!InstallPlan.NewCustom().EnableLongPaths&&!InstallPlan.NewCustom().TurnOffSystemSounds,"both new configuration modes keep Windows default");
var legacyTweaks=PlanStore.Deserialize("{\"SchemaVersion\":1,\"Users\":[]}");Check(!legacyTweaks.EnableLongPaths&&!legacyTweaks.TurnOffSystemSounds,"legacy plan keeps Windows default for both new flags");
foreach(var arch in new[]{"amd64","arm64","x86"}){
 foreach(var longPaths in new[]{false,true})foreach(var sounds in new[]{false,true}){
  p=Plan();p.Architecture=arch;p.EnableLongPaths=longPaths;p.TurnOffSystemSounds=sounds;xml=Xml(p);
  var install=Node(xml,"//*[local-name()='File' and contains(@path,'Specialize.ps1')]")??"";
  Check(install.Contains("/v LongPathsEnabled /t REG_DWORD /d 1 /f")==longPaths,"long-path command only when selected: "+arch+longPaths+sounds);
  var defaults=Node(xml,"//*[local-name()='File' and contains(@path,'DefaultUser.ps1')]")??"";var eachUser=Node(xml,"//*[local-name()='File' and contains(@path,'UserOnce.ps1')]")??"";
  Check(defaults.Contains("TurnOffSystemSounds.ps1")==sounds&&eachUser.Contains("-Value '.None'")==sounds&&install.Contains("/v DisableStartupSound /t REG_DWORD /d 1 /f")==sounds&&install.Contains("/v UserSetting_DisableStartupSound /t REG_DWORD /d 1 /f")==sounds,"sound phases only when selected: "+arch+longPaths+sounds);
  if(sounds){var helper=Node(xml,"//*[local-name()='File' and contains(@path,'TurnOffSystemSounds.ps1')]")??"";Check(helper.Contains("ExcludeFromCPL -eq 1")&&helper.Contains("-NotIn $excludes")&&helper.Contains("-Value ''"),"sound helper retains excluded events: "+arch+longPaths);}
 }
}
p=Plan();p.EnableLongPaths=true;p.TurnOffSystemSounds=true;
foreach(var secrets in new[]{false,true}){var reloaded=PlanStore.Deserialize(PlanStore.Serialize(p,secrets));Check(reloaded.EnableLongPaths&&reloaded.TurnOffSystemSounds,"both choices saved and restored: "+secrets);}
Check(Recommendations.Differences(p).Any(d=>d.Property==nameof(InstallPlan.EnableLongPaths))&&Recommendations.Differences(p).Any(d=>d.Property==nameof(InstallPlan.TurnOffSystemSounds)),"both settings appear in recommendation differences");
Recommendations.SelectNetworkScene(p,"wifi");Check(p.EnableLongPaths&&p.TurnOffSystemSounds,"network scene preserves both flags");
Recommendations.Restore(p,nameof(InstallPlan.EnableLongPaths));Check(!p.EnableLongPaths&&p.TurnOffSystemSounds,"single restore changes only selected setting");
p.EnableLongPaths=true;Recommendations.Apply(p,"wired");Check(!p.EnableLongPaths&&!p.TurnOffSystemSounds,"full recommendation restores both Windows defaults");
p=Plan();p.AutoLogon=false;p.KeepSensitiveFiles=true;p.EnableLongPaths=true;p.TurnOffSystemSounds=true;xml=Xml(p);Check(DecodedXml(xml).Contains("LongPathsEnabled")&&DecodedXml(xml).Contains("TurnOffSystemSounds.ps1"),"new settings generate without automatic logon");
viewModel.Plan=Plan();viewModel.Preview="stale-tweaks";viewModel.Plan.EnableLongPaths=true;viewModel.Plan.TurnOffSystemSounds=true;
Check(!viewModel.Preview.Contains("stale-tweaks")&&viewModel.ConfigurationSummary.Contains("长路径：启用（应用须支持）")&&viewModel.ConfigurationSummary.Contains("系统声音：关闭系统提示音方案")&&viewModel.HelpTips["longpaths"].Body.Contains("当前：启用长路径支持")&&viewModel.HelpTips["systemsounds"].Body.Contains("当前：关闭系统提示音方案"),"choices update help/summary and invalidate stale preview");
viewModel.Plan.EnableLongPaths=false;viewModel.Plan.TurnOffSystemSounds=false;Check(viewModel.ConfigurationSummary.Contains("长路径：遵循 Windows 默认行为；系统声音：遵循 Windows 默认行为")&&viewModel.HelpTips["longpaths"].Body.Contains("当前：遵循 Windows 默认行为")&&viewModel.HelpTips["systemsounds"].Body.Contains("当前：遵循 Windows 默认行为"),"unchecking restores Windows-default wording");
// Edge opt-in settings use upstream policy levels and independent flags.
Check(!InstallPlan.New().HideEdgeFre&&!InstallPlan.New().DisableEdgeStartupBoost&&!InstallPlan.NewCustom().HideEdgeFre&&!InstallPlan.NewCustom().DisableEdgeStartupBoost,"both new configuration modes keep Edge defaults");
var legacyEdge=PlanStore.Deserialize("{\"SchemaVersion\":1,\"Users\":[]}");Check(!legacyEdge.HideEdgeFre&&!legacyEdge.DisableEdgeStartupBoost,"legacy plans keep Edge defaults");
foreach(var arch in new[]{"amd64","arm64","x86"})foreach(var fre in new[]{false,true})foreach(var background in new[]{false,true}){
 p=Plan();p.Architecture=arch;p.HideEdgeFre=fre;p.DisableEdgeStartupBoost=background;xml=Xml(p);var script=Node(xml,"//*[local-name()='File' and contains(@path,'Specialize.ps1')]")??"";
 Check(script.Contains("/v HideFirstRunExperience /t REG_DWORD /d 1 /f;")==fre,"first-run policy only when selected: "+arch+fre+background);
 Check(script.Contains("/v BackgroundModeEnabled /t REG_DWORD /d 0 /f;")==background&&script.Contains("/v StartupBoostEnabled /t REG_DWORD /d 0 /f;")==background,"both startup/background policies only when selected: "+arch+fre+background);
 if(background)Check(script.Contains(@"HKLM\Software\Policies\Microsoft\Edge\Recommended")&&!script.Contains(@"Edge"" /v BackgroundModeEnabled")&&!script.Contains(@"Edge"" /v StartupBoostEnabled"),"background policies remain recommended, not enforced: "+arch+fre);
 Check(!DecodedXml(xml).Contains("MakeEdgeUninstallable.ps1")&&!DecodedXml(xml).Contains("/v SyncDisabled")&&!DecodedXml(xml).Contains("/v BrowserSignin"),"Edge options do not introduce uninstall/login/sync policies: "+arch+fre+background);
}
p=Plan();p.HideEdgeFre=true;p.DisableEdgeStartupBoost=true;
foreach(var secrets in new[]{false,true}){var reloaded=PlanStore.Deserialize(PlanStore.Serialize(p,secrets));Check(reloaded.HideEdgeFre&&reloaded.DisableEdgeStartupBoost,"Edge flags save and restore: "+secrets);}
Check(Recommendations.Differences(p).Any(d=>d.Property==nameof(InstallPlan.HideEdgeFre))&&Recommendations.Differences(p).Any(d=>d.Property==nameof(InstallPlan.DisableEdgeStartupBoost)),"both Edge flags appear in recommendation differences");
Recommendations.SelectNetworkScene(p,"wifi");Check(p.HideEdgeFre&&p.DisableEdgeStartupBoost,"network scene preserves Edge choices");
Recommendations.Restore(p,nameof(InstallPlan.HideEdgeFre));Check(!p.HideEdgeFre&&p.DisableEdgeStartupBoost,"single recommended restore changes only chosen Edge flag");
p.HideEdgeFre=true;Recommendations.Apply(p,"wired");Check(!p.HideEdgeFre&&!p.DisableEdgeStartupBoost,"full recommendation restores both Edge defaults");
p=Plan();p.HideEdgeFre=true;p.DisableEdgeStartupBoost=true;p.AutoLogon=false;p.KeepSensitiveFiles=true;Check(DecodedXml(Xml(p)).Contains("HideFirstRunExperience")&&DecodedXml(Xml(p)).Contains("StartupBoostEnabled"),"Edge policies independent of automatic logon");
viewModel.Plan=Plan();viewModel.Preview="stale-edge";viewModel.Plan.HideEdgeFre=true;viewModel.Plan.DisableEdgeStartupBoost=true;
Check(!viewModel.Preview.Contains("stale-edge")&&viewModel.ConfigurationSummary.Contains("Edge：首次引导跳过；启动加速/后台运行默认关闭（用户可调整）")&&viewModel.HelpTips["edgefre"].Body.Contains("当前：跳过 Edge 首次运行引导")&&viewModel.HelpTips["edgebackground"].Body.Contains("用户仍可调整"),"Edge choices refresh summary/help and invalidate preview");
viewModel.Plan.HideEdgeFre=false;viewModel.Plan.DisableEdgeStartupBoost=false;Check(viewModel.ConfigurationSummary.Contains("Edge：首次引导遵循默认；启动加速/后台运行遵循默认")&&viewModel.HelpTips["edgefre"].Body.Contains("当前：遵循 Edge 默认行为")&&viewModel.HelpTips["edgebackground"].Body.Contains("当前：遵循 Edge 默认行为"),"unchecking restores Edge-default wording");
p=Plan();p.HideEdgeFre=true;p.DisableEdgeStartupBoost=true;p.DeleteEdgeDesktopIcon=true;Check(DecodedXml(Xml(p)).Contains("Microsoft Edge.lnk")&&DecodedXml(Xml(p)).Contains("HideFirstRunExperience")&&DecodedXml(Xml(p)).Contains("StartupBoostEnabled"),"existing desktop shortcut removal and both new Edge policies coexist");
// Windows Update suspension: exact upstream task, opt-in and reversible plan behavior.
Check(!InstallPlan.New().DisableWindowsUpdate&&!InstallPlan.NewCustom().DisableWindowsUpdate&&!PlanStore.Deserialize("{\"SchemaVersion\":1,\"Users\":[]}").DisableWindowsUpdate,"new and legacy configurations keep Windows Update default");
foreach(var arch in new[]{"amd64","arm64","x86"}){
 p=Plan();p.Architecture=arch;Check(!DecodedXml(Xml(p)).Contains("PauseWindowsUpdate"),"default emits no update task: "+arch);
 p.DisableWindowsUpdate=true;xml=Xml(p);var script=Node(xml,"//*[local-name()='File' and contains(@path,'Specialize.ps1')]")??"";
 Check(script.Contains("Register-ScheduledTask -TaskName 'PauseWindowsUpdate'")&&script.Contains("PauseWindowsUpdate.xml"),"task registered during specialize: "+arch);
 var task=Node(xml,"//*[local-name()='File' and contains(@path,'PauseWindowsUpdate.xml')]")??"";var taskDoc=new XmlDocument();taskDoc.LoadXml(task);
 Check(taskDoc.SelectSingleNode("//*[local-name()='BootTrigger']/*[local-name()='Repetition']/*[local-name()='Interval']")?.InnerText=="P1D"&&taskDoc.SelectSingleNode("//*[local-name()='BootTrigger']/*[local-name()='Enabled']")?.InnerText=="true","boot/daily trigger follows upstream: "+arch);
 var arguments=taskDoc.SelectSingleNode("//*[local-name()='Exec']/*[local-name()='Arguments']")?.InnerText??"";
 Check(arguments.Contains("UtcNow")&&arguments.Contains("AddDays(7)")&&arguments.Contains("PauseFeatureUpdatesStartTime")&&arguments.Contains("PauseQualityUpdatesEndTime")&&arguments.Contains("PauseUpdatesExpiryTime"),"task uses upstream seven-day update timestamps: "+arch);
 Check(!DecodedXml(xml).Contains("wuauserv",StringComparison.OrdinalIgnoreCase)&&!script.Contains("Start-ScheduledTask -TaskName 'PauseWindowsUpdate'"),"no service disabling or invented immediate task action: "+arch);
 p.DisableWindowsUpdate=false;Check(!DecodedXml(Xml(p)).Contains("PauseWindowsUpdate"),"unchecking removes generated task again: "+arch);
}
p=Plan();p.DisableWindowsUpdate=true;
foreach(var secrets in new[]{false,true})Check(PlanStore.Deserialize(PlanStore.Serialize(p,secrets)).DisableWindowsUpdate,"update choice survives save/reload: "+secrets);
Check(Recommendations.Differences(p).Any(d=>d.Property==nameof(InstallPlan.DisableWindowsUpdate))&&Recommendations.Summary(p).Contains("Windows 更新：循环暂停 7 天"),"update setting visible in differences and summary");
Recommendations.SelectNetworkScene(p,"wifi");Check(p.DisableWindowsUpdate,"network scene preserves update choice");
Recommendations.Restore(p,nameof(InstallPlan.DisableWindowsUpdate));Check(!p.DisableWindowsUpdate,"single restore returns update choice to default");
p.DisableWindowsUpdate=true;Recommendations.Apply(p,"wired");Check(!p.DisableWindowsUpdate,"full recommendation restores update default");
p=Plan();p.DisableWindowsUpdate=true;p.AutoLogon=false;p.KeepSensitiveFiles=true;Check(DecodedXml(Xml(p)).Contains("PauseWindowsUpdate.xml")&&!Xml(p).Contains("<AutoLogon>"),"update task generation does not require automatic login");
viewModel.Plan=Plan();viewModel.Preview="stale-update";viewModel.Plan.DisableWindowsUpdate=true;
Check(!viewModel.Preview.Contains("stale-update")&&viewModel.ConfigurationSummary.Contains("Windows 更新：循环暂停 7 天")&&viewModel.HelpTips["windowsupdate"].Body.Contains("当前：循环暂停更新"),"update choice refreshes help/summary and invalidates preview");
Check(viewModel.HelpTips["windowsupdate"].Body.Contains("安全补丁")&&viewModel.HelpTips["windowsupdate"].Body.Contains("禁用或删除 PauseWindowsUpdate")&&viewModel.HelpTips["windowsupdate"].Body.Contains("不会删除已安装系统里的任务"),"help states maintenance impact and temporary/permanent recovery");
viewModel.Plan.DisableWindowsUpdate=false;Check(viewModel.ConfigurationSummary.Contains("Windows 更新：遵循 Windows 默认行为")&&viewModel.HelpTips["windowsupdate"].Body.Contains("当前：遵循 Windows 默认行为"),"unchecking updates default wording");
// Application suggestions: upstream default-user content settings and machine consumer policy.
Check(!InstallPlan.New().DisableAppSuggestions&&!InstallPlan.NewCustom().DisableAppSuggestions&&!PlanStore.Deserialize("{\"SchemaVersion\":1,\"Users\":[]}").DisableAppSuggestions,"new and legacy plans keep Windows application-suggestion defaults");
foreach(var arch in new[]{"amd64","arm64","x86"}){
 p=Plan();p.Architecture=arch;Check(!DecodedXml(Xml(p)).Contains("DisableWindowsConsumerFeatures"),"default does not generate consumer-policy override: "+arch);
 p.DisableAppSuggestions=true;xml=Xml(p);var defaults=Node(xml,"//*[local-name()='File' and contains(@path,'DefaultUser.ps1')]")??"";var install=Node(xml,"//*[local-name()='File' and contains(@path,'Specialize.ps1')]")??"";
 Check(defaults.Contains("SilentInstalledAppsEnabled")&&defaults.Contains("SystemPaneSuggestionsEnabled")&&defaults.Contains("SubscribedContent-338388Enabled")&&defaults.Contains(@"HKU\DefaultUser\Software\Microsoft\Windows\CurrentVersion\ContentDeliveryManager")&&defaults.Contains("/v $name /t REG_DWORD /d 0 /f"),"content-delivery defaults follow upstream: "+arch);
 Check(install.Contains(@"HKLM\Software\Policies\Microsoft\Windows\CloudContent")&&install.Contains("DisableWindowsConsumerFeatures")&&install.Contains("/t REG_DWORD /d 1 /f;"),"consumer policy applied in specialize: "+arch);
 Check(!DecodedXml(xml).Contains("Remove-AppxPackage")&&!DecodedXml(xml).Contains("RemoveBloatware.ps1"),"suggestion flag does not introduce app removal: "+arch);
 p.DisableAppSuggestions=false;Check(!DecodedXml(Xml(p)).Contains("DisableWindowsConsumerFeatures"),"unchecking omits suggestion override: "+arch);
}
p=Plan();p.DisableAppSuggestions=true;
foreach(var secrets in new[]{false,true})Check(PlanStore.Deserialize(PlanStore.Serialize(p,secrets)).DisableAppSuggestions,"suggestion flag roundtrips: "+secrets);
Check(Recommendations.Differences(p).Any(d=>d.Property==nameof(InstallPlan.DisableAppSuggestions))&&Recommendations.Summary(p).Contains("应用建议：禁用建议与自动推广"),"suggestion flag visible in differences and summary");
Recommendations.SelectNetworkScene(p,"wifi");Check(p.DisableAppSuggestions,"network scene preserves suggestions preference");
Recommendations.Restore(p,nameof(InstallPlan.DisableAppSuggestions));Check(!p.DisableAppSuggestions,"single restore returns suggestion setting to default");
p.DisableAppSuggestions=true;Recommendations.Apply(p,"wired");Check(!p.DisableAppSuggestions,"full recommendation restores suggestion defaults");
p=Plan();p.DisableAppSuggestions=true;p.AutoLogon=false;p.KeepSensitiveFiles=true;Check(DecodedXml(Xml(p)).Contains("DisableWindowsConsumerFeatures")&&!Xml(p).Contains("<AutoLogon>"),"suggestion settings independent of automatic login");
viewModel.Plan=Plan();viewModel.Preview="stale-suggestions";viewModel.Plan.DisableAppSuggestions=true;
Check(!viewModel.Preview.Contains("stale-suggestions")&&viewModel.ConfigurationSummary.Contains("应用建议：禁用建议与自动推广")&&viewModel.HelpTips["appsuggestions"].Body.Contains("当前：禁用应用建议"),"suggestion preference refreshes summary/help and invalidates preview");
Check(viewModel.HelpTips["appsuggestions"].Body.Contains("管理设备按钮不可用")&&viewModel.HelpTips["appsuggestions"].Body.Contains("不会卸载现有应用"),"help documents mobile-device side effect and retained installed apps");
viewModel.Plan.DisableAppSuggestions=false;Check(viewModel.ConfigurationSummary.Contains("应用建议：遵循 Windows 默认行为")&&viewModel.HelpTips["appsuggestions"].Body.Contains("当前：遵循 Windows 默认行为"),"unchecking restores suggestion default wording");

// Input settings: inspect generated XML/embedded data only, never execute scripts.
var legacyInput=PlanStore.Deserialize("{\"SchemaVersion\":1,\"Users\":[]}");
foreach(var candidate in new[]{InstallPlan.New(),InstallPlan.NewCustom(),legacyInput})
 Check(!candidate.DisablePointerPrecision&&candidate.StickyKeysMode==StickyKeySettings.Default&&!candidate.IgnoreCapsLock&&!candidate.IgnoreNumLock&&!candidate.IgnoreScrollLock,"new and legacy input defaults preserve Windows");
foreach(var arch in new[]{"amd64","arm64","x86"}){
 p=Plan();p.Architecture=arch;var defaultInput=DecodedXml(Xml(p));
 Check(!defaultInput.Contains("MouseThreshold1")&&!defaultInput.Contains(@"Accessibility\StickyKeys")&&!defaultInput.Contains("Scancode Map"),"default input options write no overrides: "+arch);
 p.DisablePointerPrecision=true;xml=Xml(p);var mouseUser=Node(xml,"//*[local-name()='File' and contains(@path,'DefaultUser.ps1')]")??"";var mouseMachine=Node(xml,"//*[local-name()='File' and contains(@path,'Specialize.ps1')]")??"";
 Check(mouseUser.Contains(@"Registry::HKU\DefaultUser\Control Panel\Mouse")&&mouseUser.Contains("Value = 0;")&&new[]{"MouseSpeed","MouseThreshold1","MouseThreshold2"}.All(n=>mouseUser.Contains("Set-ItemProperty @params -Name '"+n+"'")),"pointer acceleration writes all three upstream values in default-user stage: "+arch);
 Check(!mouseMachine.Contains("MouseThreshold")&&!mouseUser.Contains("MouseSensitivity"),"pointer option leaves machine settings and pointer speed unchanged: "+arch);
 p.DisablePointerPrecision=false;Check(!DecodedXml(Xml(p)).Contains("MouseThreshold1"),"unchecking pointer precision omits override: "+arch);
 for(var mask=0;mask<64;mask++){
  p=Plan();p.Architecture=arch;p.StickyKeysMode=StickyKeySettings.Custom;
  p.StickyHotKeyActive=(mask&1)!=0;p.StickyHotKeySound=(mask&2)!=0;p.StickyIndicator=(mask&4)!=0;p.StickyAudibleFeedback=(mask&8)!=0;p.StickyTriState=(mask&16)!=0;p.StickyTwoKeysOff=(mask&32)!=0;
  var expected=10+((mask&1)!=0?4:0)+((mask&2)!=0?16:0)+((mask&4)!=0?32:0)+((mask&8)!=0?64:0)+((mask&16)!=0?128:0)+((mask&32)!=0?256:0);
  xml=Xml(p);var user=Node(xml,"//*[local-name()='File' and contains(@path,'DefaultUser.ps1')]")??"";var machine=Node(xml,"//*[local-name()='File' and contains(@path,'Specialize.ps1')]")??"";
  Check(user.Contains(@"HKU\DefaultUser\Control Panel\Accessibility\StickyKeys")&&user.Contains($"/v Flags /t REG_SZ /d {expected} /f;")&&machine.Contains(@"HKU\.DEFAULT\Control Panel\Accessibility\StickyKeys")&&machine.Contains($"/v Flags /t REG_SZ /d {expected} /f;"),"all sticky flags independent with original available/confirm bits: "+arch+"/"+mask);
 }
 p.StickyKeysMode=StickyKeySettings.Disabled;var disabledSticky=DecodedXml(Xml(p));
 Check(disabledSticky.Contains("/v Flags /t REG_SZ /d 10 /f;")&&!disabledSticky.Contains("/v Flags /t REG_SZ /d 510 /f;"),"disabled sticky ignores retained custom flags: "+arch);
 p.StickyKeysMode=StickyKeySettings.Default;Check(!DecodedXml(Xml(p)).Contains(@"Accessibility\StickyKeys"),"sticky default suppresses retained custom flags: "+arch);
 for(var mask=0;mask<8;mask++){
  p=Plan();p.Architecture=arch;p.LockKeyMode=KeyboardLockSettings.Custom;p.IgnoreCapsLock=(mask&1)!=0;p.IgnoreNumLock=(mask&2)!=0;p.IgnoreScrollLock=(mask&4)!=0;
  xml=Xml(p);var mappingText=Node(xml,"//*[local-name()='File' and contains(@path,'Specialize.ps1')]")??"";
  if(mask==0){Check(!mappingText.Contains("Scancode Map"),"normal toggle creates no scan-code override: "+arch);continue;}
  var mapping=new List<byte>(new byte[8]);var pairs=new List<byte[]>();
  if((mask&1)!=0)pairs.Add(new byte[]{0,0,0x3a,0});if((mask&2)!=0)pairs.Add(new byte[]{0,0,0x45,0});if((mask&4)!=0)pairs.Add(new byte[]{0,0,0x46,0});
  mapping.AddRange(new byte[]{(byte)(pairs.Count+1),0,0,0});foreach(var pair in pairs)mapping.AddRange(pair);mapping.AddRange(new byte[4]);
  var match=System.Text.RegularExpressions.Regex.Match(mappingText,@"Scancode Map'.*FromBase64String\('([^']+)'\)");
  Check(match.Success&&Convert.FromBase64String(match.Groups[1].Value).SequenceEqual(mapping),"scan-code binary header count mappings terminator: "+arch+"/"+mask);
  Check((Node(xml,"//*[local-name()='File' and contains(@path,'DefaultUser.ps1')]")??"").Contains("-Value 2 -Force"),"ignored keys keep independently chosen initial indicators: "+arch+"/"+mask);
 }
 p.LockKeyMode=KeyboardLockSettings.Default;Check(!DecodedXml(Xml(p)).Contains("Scancode Map"),"default mode suppresses retained ignored keys: "+arch);
}
p=Plan();p.StickyKeysMode="invalid";Reject(p,"unknown sticky mode cannot export");Check(Recommendations.MissingInputs(p).Contains("有效的粘滞键"),"invalid sticky choice visible before export");
p=Plan();p.StickyKeysMode=null!;Reject(p,"null sticky mode cannot export");
p=Plan();p.LockKeyMode=KeyboardLockSettings.Custom;p.InitialCapsLockOn=true;p.IgnoreCapsLock=true;p.IgnoreNumLock=true;p.DisablePointerPrecision=true;p.StickyKeysMode=StickyKeySettings.Custom;p.StickyHotKeyActive=true;p.StickyIndicator=true;p.StickyTwoKeysOff=true;
foreach(var secrets in new[]{false,true}){
 var round=PlanStore.Deserialize(PlanStore.Serialize(p,secrets));
 Check(round.DisablePointerPrecision&&round.IgnoreCapsLock&&round.IgnoreNumLock&&!round.IgnoreScrollLock&&round.InitialCapsLockOn&&round.StickyKeysMode==StickyKeySettings.Custom&&round.StickyHotKeyActive&&round.StickyIndicator&&round.StickyTwoKeysOff&&!round.StickyAudibleFeedback&&!round.StickyHotKeySound&&!round.StickyTriState,"all input preferences roundtrip without affecting secret choice: "+secrets);
}
Check(Recommendations.Differences(p).Any(d=>d.Property==nameof(InstallPlan.DisablePointerPrecision))&&Recommendations.Differences(p).Single(d=>d.Property==nameof(InstallPlan.StickyKeysMode)).Current.Contains("连续五次 Shift 切换")&&Recommendations.Differences(p).Single(d=>d.Property==nameof(InstallPlan.LockKeyMode)).Current.Contains("Caps Lock 忽略按键"),"input differences show effective custom values");
Recommendations.SelectNetworkScene(p,"wifi");Check(p.DisablePointerPrecision&&p.StickyKeysMode==StickyKeySettings.Custom&&p.IgnoreCapsLock,"network scenes preserve all input preferences");
Recommendations.Restore(p,nameof(InstallPlan.StickyKeysMode));Recommendations.Restore(p,nameof(InstallPlan.LockKeyMode));Recommendations.Restore(p,nameof(InstallPlan.DisablePointerPrecision));
Check(p.StickyKeysMode==StickyKeySettings.Default&&p.LockKeyMode==KeyboardLockSettings.Default&&!p.DisablePointerPrecision&&p.StickyHotKeyActive&&p.IgnoreCapsLock,"single restores deactivate overrides and preserve nested input choices");
p.StickyKeysMode=StickyKeySettings.Disabled;p.LockKeyMode=KeyboardLockSettings.Custom;p.DisablePointerPrecision=true;Recommendations.Apply(p,"wired");
Check(p.StickyKeysMode==StickyKeySettings.Default&&p.LockKeyMode==KeyboardLockSettings.Default&&!p.DisablePointerPrecision&&p.StickyHotKeyActive&&p.IgnoreCapsLock,"full recommendation resets input modes but retains personal sub-options");
p.LockKeyMode=KeyboardLockSettings.Custom;p.StickyKeysMode=StickyKeySettings.Custom;p.DisablePointerPrecision=true;p.AutoLogon=false;p.KeepSensitiveFiles=true;var noLogonInput=DecodedXml(Xml(p));
Check(!Xml(p).Contains("<AutoLogon>")&&noLogonInput.Contains("Scancode Map")&&noLogonInput.Contains("MouseThreshold2")&&noLogonInput.Contains(@"Accessibility\StickyKeys"),"combined input preferences work without auto logon");
viewModel.Plan=Plan();viewModel.Preview="stale-input";viewModel.Plan.StickyKeysMode=StickyKeySettings.Custom;
Check(viewModel.IsCustomStickyKeys&&!viewModel.IsStickyKeysDisabled&&!viewModel.Preview.Contains("stale-input"),"sticky mode activates controls and invalidates preview");
viewModel.Preview="stale-input";viewModel.Plan.StickyHotKeyActive=true;
Check(!viewModel.Preview.Contains("stale-input")&&viewModel.StickyKeysSummary.Contains("连续五次 Shift")&&viewModel.ConfigurationSummary.Contains("粘滞键：自定义")&&viewModel.HelpTips["stickyhotkey"].Body.StartsWith("当前：自定义"),"sticky sub-options refresh summary help and preview");
viewModel.Plan.StickyKeysMode=StickyKeySettings.Disabled;Check(!viewModel.IsCustomStickyKeys&&viewModel.IsStickyKeysDisabled&&viewModel.Plan.StickyHotKeyActive&&viewModel.HelpTips["stickyhotkey"].Body.Contains("不参与生成"),"disabled sticky shows assistance warning and retains inactive sub-options");
viewModel.Plan.StickyKeysMode=StickyKeySettings.Default;Check(!viewModel.IsStickyKeysDisabled&&viewModel.HelpTips["stickykeys"].Body.Contains("Windows 默认设置"),"default sticky closes warning and updates help");
viewModel.Preview="stale-input";viewModel.Plan.DisablePointerPrecision=true;Check(!viewModel.Preview.Contains("stale-input")&&viewModel.ConfigurationSummary.Contains("鼠标指针精确度：关闭")&&viewModel.HelpTips["pointerprecision"].Body.StartsWith("当前：关闭"),"pointer preference refreshes summary help and preview");
viewModel.Plan.LockKeyMode=KeyboardLockSettings.Custom;viewModel.Preview="stale-input";viewModel.Plan.IgnoreNumLock=true;
Check(!viewModel.Preview.Contains("stale-input")&&viewModel.LockKeyStateSummary.Contains("Num Lock 忽略按键")&&viewModel.HelpTips["numlock"].Body.Contains("忽略按键"),"key behavior refreshes effective state and preview");
viewModel.Plan.LockKeyMode=KeyboardLockSettings.Default;Check(viewModel.HelpTips["lockbehavior"].Body.Contains("不参与生成"),"default keyboard describes ignored inactive choices");
Check(viewModel.HelpTips["lockbehavior"].Body.Contains("不会清除已经安装")&&viewModel.HelpTips["lockbehavior"].Body.Contains("不能与其他扫描码映射合并")&&viewModel.HelpTips["stickykeys"].Body.Contains("不会自动开启")&&viewModel.HelpTips["stickykeys"].Body.Contains("需要键盘辅助功能"),"input help documents scope reinstallation and accessibility effects");
var inputSummary=UnattendLocal.Desktop.SummaryPresentation.FromText(viewModel.ConfigurationSummary);
Check(inputSummary.Sections.Single(section=>section.Kind=="basic").Rows.Any(row=>row.SourceLabel=="粘滞键")&&inputSummary.Sections.Single(section=>section.Kind=="basic").Rows.Any(row=>row.SourceLabel=="鼠标指针精确度"),"input rows grouped with basic settings");

// XML import/merge: generated payloads are never executed.
bool SameXml(string left,string right)=>XmlMerge.Equal(XmlMerge.Read(left).DocumentElement,XmlMerge.Read(right).DocumentElement);
string WithoutSnapshot(string text){var d=XmlMerge.Read(text);foreach(XmlComment c in d.SelectNodes("//comment()")!.Cast<XmlComment>().ToArray())c.ParentNode!.RemoveChild(c);return d.OuterXml;}
void BadImport(string text,string name){try{XmlPlanImport.Import(text,g);}catch(Exception e)when(e is ArgumentException or XmlException or FormatException){Check(true,name);return;}throw new Exception("Expected import rejection: "+name);}
foreach(var arch in new[]{"amd64","arm64","x86"}){
 p=Plan();p.Architecture=arch;p.ComputerName="ImportPC";p.DisableAppSuggestions=true;p.HideEdgeFre=true;p.EnableLongPaths=true;p.DisablePointerPrecision=true;p.StickyKeysMode=StickyKeySettings.Custom;p.StickyIndicator=true;p.StickyHotKeyActive=true;p.LockKeyMode=KeyboardLockSettings.Custom;p.IgnoreCapsLock=true;p.InitialCapsLockOn=true;
 p.Scripts.Add(new ScriptEntry{Type="Cmd",Phase="System",Content="echo import-marker"});
 xml=Xml(p);var imported=XmlPlanImport.Import(xml,g);
 Check(imported.ConfigurationMode=="自定义配置"&&imported.CustomStartingPoint=="保留当前设置"&&imported.RecommendationId==""&&imported.Architecture==arch,"XML import uses custom retained-values mode: "+arch);
 Check(imported.Users[0].Password==p.Users[0].Password&&!imported.Users[0].NeedsPassword&&imported.DisableAppSuggestions&&imported.DisablePointerPrecision&&imported.StickyHotKeyActive&&imported.IgnoreCapsLock&&imported.Scripts.Single().Content=="echo import-marker","snapshot restores actual credentials and active settings: "+arch);
 Check(imported.ImportedXml!.Blockers.Length==0&&imported.ImportedXml.Notices.Length==0&&SameXml(xml,Xml(imported)),"own XML roundtrip without changing payload: "+arch);
 imported.ComputerName="ChangedPC";imported.DisablePointerPrecision=false;var changed=Xml(imported);
 Check(changed.Contains("ChangedPC")&&!DecodedXml(changed).Contains("MouseThreshold1")&&DecodedXml(changed).Contains("import-marker"),"imported name and pointer edits preserve existing custom script: "+arch);
 imported.TurnOffSystemSounds=true;Check(DecodedXml(Xml(imported)).Contains("AppEvents"),"add setting to imported XML: "+arch);
 var again=XmlPlanImport.Import(Xml(imported),g);Check(again.ComputerName=="ChangedPC"&&!again.DisablePointerPrecision&&again.TurnOffSystemSounds&&SameXml(Xml(imported),Xml(again)),"edited XML can be imported again: "+arch);
 var legacy=XmlPlanImport.Import(WithoutSnapshot(xml),g);
 Check(legacy.ComputerName=="ImportPC"&&legacy.IgnoreCapsLock&&legacy.StickyIndicator&&legacy.DisableAppSuggestions&&legacy.DisablePointerPrecision&&legacy.Users[0].Password==p.Users[0].Password,"legacy export reads direct XML and generated flags: "+arch);
 Check(legacy.ImportedXml!.Blockers.Length==0&&SameXml(xml,Xml(legacy)),"legacy unchanged XML preserves all original contents: "+arch);
}
// Preserve unimplemented component, extension files and extra commands while editing supported data.
p=Plan();p.ComputerName="BeforePC";xml=WithoutSnapshot(Xml(p));var source=XmlMerge.Read(xml);
var nsImport=new XmlNamespaceManager(source.NameTable);nsImport.AddNamespace("u",XmlMerge.Namespace);
var setupImport=(XmlElement)source.SelectSingleNode("//u:settings[@pass='specialize']/u:component[@name='Microsoft-Windows-Shell-Setup']",nsImport)!;
var extra=source.CreateElement("RegisteredOrganization",XmlMerge.Namespace);extra.InnerText="Preserve organization";setupImport.AppendChild(extra);
var extensions=(XmlElement)source.SelectSingleNode("//*[local-name()='Extensions']")!;
var extraFile=source.CreateElement("File",Schneegans.Unattend.Constants.MyNamespaceUri);extraFile.SetAttribute("path",@"C:\Windows\Setup\Scripts\UnsupportedFeature.ps1");extraFile.InnerText="Write-Output 'keep-original-extra'";extensions.AppendChild(extraFile);
var firstImport=(XmlElement)source.SelectSingleNode("//*[local-name()='File' and contains(@path,'FirstLogon.ps1')]")!;firstImport.InnerText+="\nWrite-Output 'retain-original-command';\n";
var preserved=XmlPlanImport.Import(source.OuterXml,g);Check(preserved.ImportedXml!.Notices.Any(n=>n.Contains("UnsupportedFeature.ps1")),"unknown extension file is listed without leaking its content");
preserved.ComputerName="AfterPC";var preservedOut=Xml(preserved);
Check(preservedOut.Contains("AfterPC")&&preservedOut.Contains("Preserve organization")&&DecodedXml(preservedOut).Contains("keep-original-extra")&&DecodedXml(preservedOut).Contains("retain-original-command"),"unknown component child file and command survive a supported edit");
preserved.EnableLongPaths=true;Check(DecodedXml(Xml(preserved)).Contains("LongPathsEnabled")&&DecodedXml(Xml(preserved)).Contains("keep-original-extra"),"adding a supported script-backed setting preserves unknown file");
// Change within a main script with unsupported content: exact unique context must match.
var scriptBase=Plan();scriptBase.DisableAppSuggestions=true;var scriptSource=XmlMerge.Read(WithoutSnapshot(Xml(scriptBase)));var defaultImport=(XmlElement)scriptSource.SelectSingleNode("//*[local-name()='File' and contains(@path,'DefaultUser.ps1')]")!;defaultImport.InnerText+="\nWrite-Output 'extra-default-user';\n";
var scriptImport=XmlPlanImport.Import(scriptSource.OuterXml,g);scriptImport.DisablePointerPrecision=true;var mergedScript=DecodedXml(Xml(scriptImport));
Check(mergedScript.Contains("MouseThreshold1")&&mergedScript.Contains("extra-default-user"),"script three-way merge preserves unrecognized extra lines");
// A newly generated file cannot overwrite an unrelated existing file with the same path.
var collisionSource=XmlMerge.Read(WithoutSnapshot(Xml(Plan())));var collisionExtensions=(XmlElement)collisionSource.SelectSingleNode("//*[local-name()='Extensions']")!;
var collisionFile=collisionSource.CreateElement("File",Schneegans.Unattend.Constants.MyNamespaceUri);collisionFile.SetAttribute("path",@"C:\Windows\Setup\Scripts\SetColorTheme.ps1");collisionFile.InnerText="Write-Output 'unrelated-file'";collisionExtensions.AppendChild(collisionFile);
var collisionPlan=XmlPlanImport.Import(collisionSource.OuterXml,g);collisionPlan.ColorSettingsMode=ColorSettings.Custom;Reject(collisionPlan,"file collision cannot silently overwrite unsupported source");
// Raw three-way checks isolate conflict behavior from importer inference.
const string mergeStart="<unattend xmlns='urn:schemas-microsoft-com:unattend'><settings pass='specialize'><component name='Example' processorArchitecture='amd64'><Value>old</Value></component></settings></unattend>";
var manualSource=mergeStart.Replace(">old<",">manual<");var newer=XmlMerge.Read(mergeStart.Replace(">old<",">new<"));
try{XmlMerge.Apply(manualSource,mergeStart,newer);throw new Exception("expected conflict");}catch(ArgumentException e){Check(e.Message.Contains("冲突")&&!e.Message.Contains("manual"),"changed source scalar conflicts and messages do not expose original values");}
Check(XmlMerge.Apply(mergeStart,mergeStart,newer).InnerText=="new","matching scalar source updates correctly");
BadImport("<LayoutModificationTemplate/>","layout XML is not an answer file");BadImport("<!DOCTYPE unattend [<!ENTITY x SYSTEM 'file:///etc/passwd'>]><unattend xmlns='urn:schemas-microsoft-com:unattend'>&x;</unattend>","DTD and external entities blocked");BadImport("<unattend>","malformed XML rejected");BadImport("<unattend xmlns='urn:schemas-microsoft-com:unattend'><settings pass='specialize'><component name='a' processorArchitecture='amd64'/><component name='b' processorArchitecture='arm64'/></settings></unattend>","multiple architecture documents explicitly rejected");
BadImport("预览（密码和产品密钥已遮蔽）\n"+Xml(Plan()),"masked preview header cannot be imported");
// Removed support is a blocking issue, never a passthrough execution path.
var jsSource=XmlMerge.Read(WithoutSnapshot(Xml(Plan())));var jsExtension=(XmlElement)jsSource.SelectSingleNode("//*[local-name()='Extensions']")!;var jsFile=jsSource.CreateElement("File",Schneegans.Unattend.Constants.MyNamespaceUri);jsFile.SetAttribute("path",@"C:\Windows\Setup\Scripts\legacy.js");jsFile.InnerText="WScript.Echo('legacy');";jsExtension.AppendChild(jsFile);
var jsImported=XmlPlanImport.Import(jsSource.OuterXml,g);Check(jsImported.ImportedXml!.Blockers.Any(n=>n.Contains("JScript")),"removed JS produces a separate blocking explanation");Reject(jsImported,"JS cannot be exported through preserved original XML");
// A generated wipe must retain danger and must lose previous confirmations.
p=Plan();p.DiskMode=InstallationSettings.Wipe;p.TargetDiskId=0;p.ConfirmWipeDisk=true;var diskImported=XmlPlanImport.Import(Xml(p),g);
Check(diskImported.DiskMode==InstallationSettings.Wipe&&diskImported.ImportedXml!.RequiresDiskReview&&!diskImported.ConfirmWipeDisk&&!diskImported.ConfirmImportedXmlRisk,"XML wipe import clears disk and original-scope consent");Reject(diskImported,"imported disk XML cannot export without new review");diskImported.ConfirmWipeDisk=true;diskImported.ConfirmImportedXmlRisk=true;Check(Xml(diskImported).Contains("echo clean"),"reviewed imported wipe can export");diskImported.TargetDiskId=1;Check(!diskImported.ConfirmWipeDisk&&!diskImported.ConfirmImportedXmlRisk,"disk edit invalidates both old and imported-scope consent");
// JSON persistence must not sneak original credentials into a no-secrets save.
p=Plan();p.Users[0].Password="ImportSecretABC123";p.EditionMode="指定自己的产品密钥";p.CustomProductKey="AAAAA-BBBBB-CCCCC-DDDDD-EEEEE";var saveImported=XmlPlanImport.Import(Xml(p),g);
var withoutSecrets=PlanStore.Serialize(saveImported,false);var noSecretsImported=PlanStore.Deserialize(withoutSecrets);
Check(!withoutSecrets.Contains("ImportSecretABC123")&&!withoutSecrets.Contains("AAAAA-BBBBB")&&noSecretsImported.ImportedXml!.OriginalOmitted&&noSecretsImported.ImportedXml.SourceXml==""&&noSecretsImported.ImportedXml.BaselineXml=="","no-secret JSON excludes raw original and baseline XML credentials");Reject(noSecretsImported,"no-secret saved import requests reimport rather than dropping original XML");
var withSecretsImported=PlanStore.Deserialize(PlanStore.Serialize(saveImported,true));Check(withSecretsImported.Users[0].Password=="ImportSecretABC123"&&SameXml(Xml(saveImported),Xml(withSecretsImported)),"full JSON stores and restores imported original and editable state");
Check(!GeneratorService.Preview(g.Generate(saveImported)).Contains("UnattendLocal:v1")&&!GeneratorService.Preview(g.Generate(saveImported)).Contains("ImportSecretABC123"),"preview removes reversible metadata and masks actual passwords");
viewModel.Plan=preserved;Check(viewModel.HasImportedXml&&viewModel.XmlImportDetails.Contains("UnsupportedFeature.ps1")&&viewModel.ConfigurationSummary.Contains("XML 导入：已导入"),"import state visible in page and configuration summary");
Recommendations.Apply(viewModel.Plan,"wired");Check(viewModel.Plan.ImportedXml!=null&&Xml(viewModel.Plan).Contains("Preserve organization"),"applying recommendations retains unknown imported source");
viewModel.Plan=InstallPlan.NewCustom();Check(!viewModel.HasImportedXml&&!viewModel.HasImportedXmlRisk,"new configuration starts independently without imported source");
// Hash protects against using stale embedded plan metadata after manual XML editing.
p=Plan();p.ComputerName="SnapshotPC";xml=Xml(p);var editedSnapshot=xml.Replace("SnapshotPC</ComputerName>","ManualPC</ComputerName>");var readEdited=XmlPlanImport.Import(editedSnapshot,g);
Check(readEdited.ComputerName=="ManualPC"&&readEdited.ImportedXml!.Notices.Any(n=>n.Contains("还原记录已失效")),"manual XML changes invalidate metadata and actual XML wins");

// Additional input and website-style import scenarios.
p=Plan();p.VisualEffectsMode=VisualEffectsSettings.Custom;p.EffectFontSmoothing=true;p.DesktopIconsMode=DesktopSettings.CustomIcons;p.DesktopThisPC=true;p.StartFoldersMode=DesktopSettings.CustomFolders;p.StartFolderDownloads=true;p.ColorSettingsMode=ColorSettings.Custom;p.AccentColorHex="#112233";p.WallpaperMode=WallpaperSettings.Solid;p.WallpaperColorHex="#443322";
var oldAppearance=XmlPlanImport.Import(WithoutSnapshot(Xml(p)),g);
Check(oldAppearance.VisualEffectsMode==VisualEffectsSettings.Custom&&oldAppearance.EffectFontSmoothing&&oldAppearance.DesktopThisPC&&oldAppearance.StartFolderDownloads&&oldAppearance.AccentColorHex.Equals("#112233",StringComparison.OrdinalIgnoreCase)&&oldAppearance.WallpaperColorHex.Equals("#443322",StringComparison.OrdinalIgnoreCase),"legacy appearance desktop folders and wallpaper restored from generated payloads");
Check(SameXml(Xml(p),Xml(oldAppearance)),"legacy complex appearance XML unchanged on export");
p=Plan();p.WifiMode="预设 WPA2 Wi-Fi";p.Ssid="ImportWifi";p.WifiPassword="SecretWifi123";p.HiddenWifi=true;p.UseActivationKey=true;p.ActivationProductKey="AAAAA-BBBBB-CCCCC-DDDDD-EEEEE";
var readWifi=XmlPlanImport.Import(WithoutSnapshot(Xml(p)),g);Check(readWifi.WifiMode=="预设 WPA2 Wi-Fi"&&readWifi.WifiPassword=="SecretWifi123"&&readWifi.HiddenWifi&&readWifi.UseActivationKey&&readWifi.ActivationProductKey==p.ActivationProductKey,"legacy Wi-Fi actual credentials and independent activation key restored");
Check(SameXml(Xml(p),Xml(readWifi)),"legacy Wi-Fi roundtrip preserves XML content");
var webXml=XmlMerge.Read(WithoutSnapshot(Xml(Plan())));webXml.DocumentElement!.PrependChild(webXml.CreateComment("https://schneegans.de/windows/unattend-generator/?EffectsMode=Custom&FontSmoothing=true&FutureUnsupportedSetting12=true&AccountPassword0=do-not-show-this&WallpaperMode=Default"));
var webPlan=XmlPlanImport.Import(webXml.OuterXml,g);Check(webPlan.ImportedXml!.Notices.Any(n=>n.Contains("FutureUnsupportedSetting12"))&&webPlan.ImportedXml.Notices.All(n=>!n.Contains("do-not-show-this")),"website unimplemented parameters identified by name only");
Check(!GeneratorService.Preview(Encoding.UTF8.GetBytes(webXml.OuterXml)).Contains("do-not-show-this"),"preview removes website URL comment credentials");
Check(!Xml(webPlan).Contains("do-not-show-this"),"export removes stale website parameter comment");
var peSource=XmlMerge.Read(WithoutSnapshot(Xml(Plan())));var peNs=new XmlNamespaceManager(peSource.NameTable);peNs.AddNamespace("u",XmlMerge.Namespace);var peSetup=(XmlElement)peSource.SelectSingleNode("//u:settings[@pass='windowsPE']/u:component[@name='Microsoft-Windows-Setup']",peNs)!;
var peSync=peSource.CreateElement("RunSynchronous",XmlMerge.Namespace);var peCommand=peSource.CreateElement("RunSynchronousCommand",XmlMerge.Namespace);var peOrder=peSource.CreateElement("Order",XmlMerge.Namespace);peOrder.InnerText="99";var pePath=peSource.CreateElement("Path",XmlMerge.Namespace);pePath.InnerText="cmd.exe /c echo arbitrary-pe-command";peCommand.AppendChild(peOrder);peCommand.AppendChild(pePath);peSync.AppendChild(peCommand);peSetup.AppendChild(peSync);
var pePlan=XmlPlanImport.Import(peSource.OuterXml,g);Check(pePlan.ImportedXml!.RequiresDiskReview,"unrecognized PE command requires original-scope review");Reject(pePlan,"unrecognized PE original cannot bypass confirmation");pePlan.ConfirmImportedXmlRisk=true;Check(Xml(pePlan).Contains("arbitrary-pe-command"),"reviewed unsupported PE command preserved");viewModel.Plan=pePlan;Check(viewModel.IsDestructiveDisk&&viewModel.DiskRisk.Contains("原 XML"),"original PE risk visible even when editable disk mode is manual");
var longTree="<unattend xmlns='urn:schemas-microsoft-com:unattend'>"+string.Concat(Enumerable.Repeat("<x>",130))+string.Concat(Enumerable.Repeat("</x>",130))+"</unattend>";BadImport(longTree,"excessive XML nesting rejected before recursive merge");

var importAssembly=System.Reflection.Assembly.GetExecutingAssembly();var fixtureName=importAssembly.GetManifestResourceNames().Single(n=>n.EndsWith("upstream-import.xml"));
using(var fixtureStream=importAssembly.GetManifestResourceStream(fixtureName)!)using(var fixtureReader=new StreamReader(fixtureStream)){
 var officialXml=fixtureReader.ReadToEnd();var officialPlan=XmlPlanImport.Import(officialXml,g);
 Check(officialPlan.Users.Single().Name=="ImportFixture"&&officialPlan.Users.Single().Password=="FixtureOnly123"&&officialPlan.EnableLongPaths,"official website XML fixture restores actual fields");
 Check(officialPlan.DisableUac&&officialPlan.ImportedXml!.Blockers.Length==0,"official native UAC setting is restored and retained");
 Check(SameXml(officialXml,Xml(officialPlan)),"official website XML unchanged roundtrip preserves native UAC command");
 officialPlan.ComputerName="OfficialEdit";var officialEdited=Xml(officialPlan);
 Check(officialEdited.Contains("OfficialEdit")&&DecodedXml(officialEdited).Contains("EnableLUA")&&DecodedXml(officialEdited).Contains("LongPathsEnabled"),"official XML supported edit retains native protection command");
}
Check(!SameXml("<unattend xmlns='urn:schemas-microsoft-com:unattend'><A><B><C/><D/></B></A></unattend>","<unattend xmlns='urn:schemas-microsoft-com:unattend'><A><B><C/></B><D/></A></unattend>"),"XML integrity distinguishes different parent relationships");

var externalJs=XmlMerge.Read(WithoutSnapshot(Xml(Plan())));var externalFirst=(XmlElement)externalJs.SelectSingleNode("//*[local-name()='File' and contains(@path,'FirstLogon.ps1')]")!;externalFirst.InnerText+="\ncscript.exe C:\\legacy.js;\n";var externalJsPlan=XmlPlanImport.Import(externalJs.OuterXml,g);Check(externalJsPlan.ImportedXml!.Blockers.Any(n=>n.Contains("JScript")),"external JS invocation also cannot bypass removed script support");
var outsidePeDisk=XmlMerge.Read(WithoutSnapshot(Xml(Plan())));var outsideFirst=(XmlElement)outsidePeDisk.SelectSingleNode("//*[local-name()='File' and contains(@path,'FirstLogon.ps1')]")!;outsideFirst.InnerText+="\nFormat-Volume -DriveLetter Z;\n";Check(XmlPlanImport.Import(outsidePeDisk.OuterXml,g).ImportedXml!.RequiresDiskReview,"disk operation outside PE still requires original-scope review");

// CompactOS: inspect answer-file data only, never compress the current host.
foreach(var candidate in new[]{InstallPlan.New(),InstallPlan.NewCustom(),PlanStore.Deserialize("{\"SchemaVersion\":1,\"Users\":[]}")})Check(candidate.CompactOsMode==CompactOsSettings.Default,"new and old plans preserve Windows CompactOS defaults");
foreach(var arch in new[]{"amd64","arm64","x86"}){
 foreach(var mode in new[]{CompactOsSettings.Default,CompactOsSettings.Enabled,CompactOsSettings.Disabled}){
  p=Plan();p.Architecture=arch;p.CompactOsMode=mode;xml=Xml(p);
  var compactValue=Node(xml,"//u:settings[@pass='windowsPE']/u:component[@name='Microsoft-Windows-Setup']/u:ImageInstall/u:OSImage/u:Compact");
  Check(compactValue==(mode==CompactOsSettings.Default?null:mode==CompactOsSettings.Enabled?"true":"false"),"CompactOS value and Windows PE stage: "+arch+mode);
  Check(!DecodedXml(xml).Contains("compact.exe")&&!xml.Contains("<DiskConfiguration>")&&!xml.Contains("<InstallTo>"),"CompactOS introduces no run command or automatic disk target: "+arch+mode);
  var round=XmlPlanImport.Import(xml,g);Check(round.CompactOsMode==mode&&SameXml(xml,Xml(round)),"CompactOS XML roundtrip: "+arch+mode);
  var legacyRound=XmlPlanImport.Import(WithoutSnapshot(xml),g);Check(legacyRound.CompactOsMode==mode&&legacyRound.ImportedXml!.Blockers.Length==0&&SameXml(xml,Xml(legacyRound)),"native CompactOS XML without metadata roundtrip: "+arch+mode);
 }
}
p=Plan();p.CompactOsMode="invalid";Reject(p,"invalid CompactOS mode cannot generate");p.CompactOsMode=null!;Reject(p,"null CompactOS mode cannot generate");
p=Plan();p.CompactOsMode=CompactOsSettings.Enabled;p.AutoLogon=false;p.KeepSensitiveFiles=true;Check(Node(Xml(p),"//u:OSImage/u:Compact")=="true"&&!Xml(p).Contains("<AutoLogon>"),"CompactOS does not require automatic logon");
foreach(var secrets in new[]{true,false}){p=Plan();p.CompactOsMode=CompactOsSettings.Disabled;Check(PlanStore.Deserialize(PlanStore.Serialize(p,secrets)).CompactOsMode==CompactOsSettings.Disabled,"CompactOS JSON roundtrip: "+secrets);}
p=Plan();p.CompactOsMode=CompactOsSettings.Enabled;Check(Recommendations.Differences(p).Any(d=>d.Property==nameof(InstallPlan.CompactOsMode)&&d.Current.Contains("压缩 Windows 系统文件")),"CompactOS difference includes effective mode");Recommendations.SelectNetworkScene(p,"wifi");Check(p.CompactOsMode==CompactOsSettings.Enabled,"network scene preserves CompactOS choice");Recommendations.Restore(p,nameof(InstallPlan.CompactOsMode));Check(p.CompactOsMode==CompactOsSettings.Default,"single CompactOS restore removes override");p.CompactOsMode=CompactOsSettings.Disabled;Recommendations.Apply(p,"wired");Check(p.CompactOsMode==CompactOsSettings.Default,"full recommendations restore Windows CompactOS defaults");
viewModel.Plan=Plan();viewModel.Preview="stale-compact";viewModel.Plan.CompactOsMode=CompactOsSettings.Enabled;
Check(!viewModel.Preview.Contains("stale-compact")&&viewModel.CompactOsSummary.Contains("压缩 Windows 系统文件")&&viewModel.HelpTips["compactos"].Body.Contains("当前：安装时压缩")&&viewModel.ConfigurationSummary.Contains("CompactOS：安装时压缩"),"CompactOS UI help summary and preview update together");
Check(viewModel.HelpTips["compactos"].Body.Contains("不保证固定节省量")&&viewModel.HelpTips["compactos"].Body.Contains("不自动解压")&&viewModel.CanEditCompactOs,"CompactOS help explains resource tradeoff and scope");
var compactSummary=UnattendLocal.Desktop.SummaryPresentation.FromText(viewModel.ConfigurationSummary);Check(compactSummary.Sections.Single(section=>section.Kind=="protection").Rows.Any(row=>row.SourceLabel=="CompactOS"),"CompactOS grouped with image and installation settings");
// Native setting composes with image selection and every local disk mode.
foreach(var diskMode in new[]{InstallationSettings.Manual,InstallationSettings.Existing,InstallationSettings.Wipe,DiskScriptSettings.Mode}){
 p=Plan();p.ImageSelection="按镜像索引";p.ImageIndex=2;p.CompactOsMode=CompactOsSettings.Enabled;p.DiskMode=diskMode;p.TargetDiskId=0;p.TargetPartitionId=3;p.ConfirmWipeDisk=true;p.ConfirmDiskScript=true;p.DiskPartScript="SELECT DISK=0\nSELECT PARTITION=3";p.ConfirmDiskScript=true;p.ConfirmWipeDisk=true;
 var combined=Xml(p);var combinedDoc=XmlMerge.Read(combined);var combinedNs=new XmlNamespaceManager(combinedDoc.NameTable);combinedNs.AddNamespace("u",XmlMerge.Namespace);
 Check(combinedDoc.SelectNodes("//u:ImageInstall/u:OSImage",combinedNs)!.Count==1&&Node(combined,"//u:OSImage/u:Compact")=="true"&&Node(combined,"//u:MetaData/u:Value")=="2","CompactOS shares one OSImage with local image/disk settings: "+diskMode);
}
p=Plan();p.CompactOsMode=CompactOsSettings.Enabled;var importedCompact=XmlPlanImport.Import(WithoutSnapshot(Xml(p)),g);importedCompact.CompactOsMode=CompactOsSettings.Disabled;Check(Node(Xml(importedCompact),"//u:OSImage/u:Compact")=="false","native imported CompactOS can switch from true to false");importedCompact.CompactOsMode=CompactOsSettings.Default;Check(Node(Xml(importedCompact),"//u:OSImage/u:Compact")==null,"default removes only known CompactOS-only container");
// Unknown original image settings must survive a supported Compact value edit.
p=Plan();p.CompactOsMode=CompactOsSettings.Enabled;var sourceCompact=XmlMerge.Read(WithoutSnapshot(Xml(p)));var compactNs=new XmlNamespaceManager(sourceCompact.NameTable);compactNs.AddNamespace("u",XmlMerge.Namespace);var compactImage=(XmlElement)sourceCompact.SelectSingleNode("//u:OSImage",compactNs)!;var unknownImage=sourceCompact.CreateElement("WillShowUI",XmlMerge.Namespace);unknownImage.InnerText="Always";compactImage.AppendChild(unknownImage);
var sourceCompactPlan=XmlPlanImport.Import(sourceCompact.OuterXml,g);sourceCompactPlan.CompactOsMode=CompactOsSettings.Disabled;Check(Node(Xml(sourceCompactPlan),"//u:OSImage/u:Compact")=="false"&&Node(Xml(sourceCompactPlan),"//u:OSImage/u:WillShowUI")=="Always","unknown image content preserved when Compact value changes");sourceCompactPlan.CompactOsMode=CompactOsSettings.Default;Reject(sourceCompactPlan,"default cannot silently delete a container with unrepresented image contents");
// Migrating a pre39 saved import makes the previously unrepresented Compact value editable.
var oldCompactJson=System.Text.Json.Nodes.JsonNode.Parse(PlanStore.Serialize(XmlPlanImport.Import(sourceCompact.OuterXml,g),true))!.AsObject();oldCompactJson.Remove(nameof(InstallPlan.CompactOsMode));
var oldBaseline=XmlMerge.Read(oldCompactJson["ImportedXml"]!["BaselineXml"]!.GetValue<string>());var oldNs=new XmlNamespaceManager(oldBaseline.NameTable);oldNs.AddNamespace("u",XmlMerge.Namespace);foreach(XmlNode n in oldBaseline.SelectNodes("//u:Compact",oldNs)!)n.ParentNode!.RemoveChild(n);oldCompactJson["ImportedXml"]!["BaselineXml"]=oldBaseline.OuterXml;
var migratedCompact=PlanStore.Deserialize(oldCompactJson.ToJsonString());Check(migratedCompact.CompactOsMode==CompactOsSettings.Enabled&&migratedCompact.ImportedXml!.BaselineXml.Contains("Compact"),"pre39 imported JSON reads actual compact state and updates editing baseline");migratedCompact.CompactOsMode=CompactOsSettings.Disabled;Check(Node(Xml(migratedCompact),"//u:OSImage/u:Compact")=="false","migrated original compact setting can be edited safely");
// Custom PE scripts are retained and made read-only rather than given ineffective Setup overrides.
p=Plan();var scriptedCompact=XmlMerge.Read(WithoutSnapshot(Xml(p)));var compactExtensions=(XmlElement)scriptedCompact.SelectSingleNode("//*[local-name()='Extensions']")!;var peCopy=scriptedCompact.CreateElement("PEScriptCopy",Schneegans.Unattend.Constants.MyNamespaceUri);peCopy.InnerText="dism.exe /Apply-Image /ImageFile:install.wim /Index:1 /ApplyDir:W:\\ /Compact /CheckIntegrity /Verify\n";compactExtensions.AppendChild(peCopy);
var scriptedPlan=XmlPlanImport.Import(scriptedCompact.OuterXml,g);Check(scriptedPlan.CompactOsMode==CompactOsSettings.Enabled&&!CompactOsSettings.CanEdit(scriptedPlan)&&scriptedPlan.ImportedXml!.Notices.Any(n=>n.Contains("CompactOS 由")),"scripted /Compact is recognized preserved and clearly read-only");Check(SameXml(scriptedCompact.OuterXml,Xml(scriptedPlan)),"unchanged scripted compact XML retains actual /Compact parameter");
scriptedPlan.CompactOsMode=CompactOsSettings.Disabled;Reject(scriptedPlan,"scripted CompactOS cannot be silently overridden by an ineffective XML field");scriptedPlan.CompactOsMode=CompactOsSettings.Enabled;Recommendations.Apply(scriptedPlan,"wired");Check(scriptedPlan.CompactOsMode==CompactOsSettings.Enabled,"recommendations preserve script-controlled compact state");
try{Recommendations.Restore(scriptedPlan,nameof(InstallPlan.CompactOsMode));throw new Exception("expected readonly compact restore rejection");}catch(ArgumentException){Check(scriptedPlan.CompactOsMode==CompactOsSettings.Enabled,"single restore cannot alter read-only script-controlled state");}
viewModel.Plan=scriptedPlan;Check(!viewModel.CanEditCompactOs&&viewModel.CompactOsSummary.Contains("暂不可")&&viewModel.HelpTips["compactos"].Body.Contains("不能在界面"),"script-controlled state disables dropdown and explains restriction");
// Query-only scripts remain compatible; state-changing scripts cannot conflict with the setting.
p=Plan();p.CompactOsMode=CompactOsSettings.Enabled;p.Scripts.Add(new ScriptEntry{Phase="FirstLogon",Type="Cmd",Content="compact.exe /CompactOS:query"});Check(XmlPlanImport.Import(Xml(p),g).CompactOsMode==CompactOsSettings.Enabled,"query-only script does not conflict with compact setting");p.Scripts[0].Content="compact.exe /CompactOS:never";Reject(p,"explicit compact setting cannot overlap with state-changing custom script");

p=Plan();p.CompactOsMode=CompactOsSettings.Enabled;var malformedCompact=WithoutSnapshot(Xml(p)).Replace("<Compact>true</Compact>","<Compact>invalid</Compact>");BadImport(malformedCompact,"invalid native Compact value rejected on import");
var duplicateCompact=WithoutSnapshot(Xml(p)).Replace("<Compact>true</Compact>","<Compact>true</Compact><Compact>false</Compact>");BadImport(duplicateCompact,"duplicate native Compact values rejected");

var compactFixtureName=System.Reflection.Assembly.GetExecutingAssembly().GetManifestResourceNames().Single(n=>n.EndsWith("upstream-compact.xml"));
using(var compactFixtureStream=System.Reflection.Assembly.GetExecutingAssembly().GetManifestResourceStream(compactFixtureName)!)using(var compactFixtureReader=new StreamReader(compactFixtureStream)){
 var actualCompactXml=compactFixtureReader.ReadToEnd();var actualCompactPlan=XmlPlanImport.Import(actualCompactXml,g);
 Check(actualCompactPlan.CompactOsMode==CompactOsSettings.Enabled&&actualCompactPlan.ImportedXml!.CompactOsScriptControlled&&actualCompactPlan.ImportedXml.CompactOsSourceKnown,"official generated PE fixture recognizes actual Compact parameter");
 Check(actualCompactPlan.ImportedXml!.RequiresDiskReview&&!actualCompactPlan.ConfirmImportedXmlRisk,"official generated PE fixture still requires complete original-scope review");
 Check(actualCompactPlan.ImportedXml.Blockers.Length==0,"official generated CompactOS XML has an editable baseline for retained settings");
 actualCompactPlan.ConfirmImportedXmlRisk=true;Check(SameXml(actualCompactXml,Xml(actualCompactPlan)),"official scripted CompactOS XML roundtrip preserves actual commands");
 actualCompactPlan.CompactOsMode=CompactOsSettings.Disabled;Reject(actualCompactPlan,"official script parameter cannot be overridden by ineffective native setting");
}
Console.WriteLine($"Final pre40 total: {count} tests passed.");

// pre41: exercise actual generation/import/editing paths; do not execute host commands.
foreach(var candidate in new[]{InstallPlan.New(),InstallPlan.NewCustom(),PlanStore.Deserialize("{\"SchemaVersion\":1,\"Users\":[]}")})
 Check(!candidate.DisableAutomaticRestartSignOn&&!candidate.PreventDeviceApps&&!candidate.DeleteWindowsOld,"pre41 new and legacy plans retain Windows defaults");
foreach(var arch in new[]{"amd64","arm64","x86"})foreach(var mask in Enumerable.Range(0,8)){
 p=Plan();p.Architecture=arch;p.DisableAutomaticRestartSignOn=(mask&1)!=0;p.PreventDeviceApps=(mask&2)!=0;p.DeleteWindowsOld=(mask&4)!=0;
 xml=Xml(p);var specialize41=Node(xml,"//*[local-name()='File' and contains(@path,'Specialize.ps1')]")??"";
 var firstLogon41=Node(xml,"//*[local-name()='File' and contains(@path,'FirstLogon.ps1')]")??"";
 Check(specialize41.Contains("/v \"DisableAutomaticRestartSignOn\" /t REG_DWORD /d 1 /f;")==p.DisableAutomaticRestartSignOn,"restart policy value and specialize stage: "+arch+mask);
 Check(specialize41.Contains("/v \"PreventDeviceMetadataFromNetwork\" /t REG_DWORD /d 1 /f;")==p.PreventDeviceApps&&specialize41.Contains("/v \"DisableCoInstallers\" /t REG_DWORD /d 1 /f;")==p.PreventDeviceApps,"device app policies are emitted as one complete group: "+arch+mask);
 Check(firstLogon41.Contains("cmd.exe /c \"rmdir C:\\Windows.old\";")==p.DeleteWindowsOld&&!firstLogon41.Contains("rmdir /s",StringComparison.OrdinalIgnoreCase)&&!System.Text.RegularExpressions.Regex.IsMatch(firstLogon41,@"Remove-Item[^
]*Windows\.old",System.Text.RegularExpressions.RegexOptions.IgnoreCase),"Windows.old only uses nonrecursive first-logon removal: "+arch+mask);
 Check(Node(xml,"//u:AutoLogon/u:LogonCount")=="1"&&!xml.Contains("<DiskConfiguration>"),"pre41 does not change initial automatic logon or add disk operations: "+arch+mask);
 foreach(var withMetadata in new[]{false,true}){
  var original41=withMetadata?xml:WithoutSnapshot(xml);var restored41=XmlPlanImport.Import(original41,g);
  Check(restored41.DisableAutomaticRestartSignOn==p.DisableAutomaticRestartSignOn&&restored41.PreventDeviceApps==p.PreventDeviceApps&&restored41.DeleteWindowsOld==p.DeleteWindowsOld&&restored41.ImportedXml!.Blockers.Length==0&&SameXml(original41,Xml(restored41)),"pre41 roundtrip preserves all flag combinations: "+arch+mask+withMetadata);
 }
}
p=Plan();p.DisableAutomaticRestartSignOn=p.PreventDeviceApps=p.DeleteWindowsOld=true;
foreach(var secrets in new[]{false,true}){
 var restored41=PlanStore.Deserialize(PlanStore.Serialize(p,secrets));Check(restored41.DisableAutomaticRestartSignOn&&restored41.PreventDeviceApps&&restored41.DeleteWindowsOld,"pre41 JSON persistence independent of credential choice: "+secrets);
}
Check(CommonSystemSettings.Items.All(item=>Recommendations.Differences(p).Any(d=>d.Property==item.Property)),"all three settings appear in recommendation differences");
Recommendations.SelectNetworkScene(p,"wifi");Check(p.DisableAutomaticRestartSignOn&&p.PreventDeviceApps&&p.DeleteWindowsOld,"network scene preserves all three system choices");
Recommendations.Restore(p,nameof(InstallPlan.PreventDeviceApps));Check(p.DisableAutomaticRestartSignOn&&!p.PreventDeviceApps&&p.DeleteWindowsOld,"single restore changes only requested system option");
p.PreventDeviceApps=true;Recommendations.Apply(p,"wired");Check(!p.DisableAutomaticRestartSignOn&&!p.PreventDeviceApps&&!p.DeleteWindowsOld,"full recommendations restore all three Windows defaults");
p=Plan();p.AutoLogon=false;p.KeepSensitiveFiles=true;p.DisableAutomaticRestartSignOn=p.PreventDeviceApps=p.DeleteWindowsOld=true;
xml=Xml(p);Check(!xml.Contains("<AutoLogon>")&&DecodedXml(xml).Contains("DisableCoInstallers")&&DecodedXml(xml).Contains("rmdir C:\\Windows.old"),"pre41 respects explicitly disabled initial logon and retains first-logon cleanup for later manual logon");
viewModel.Plan=Plan();viewModel.Preview="stale-pre41";viewModel.Plan.DisableAutomaticRestartSignOn=viewModel.Plan.PreventDeviceApps=viewModel.Plan.DeleteWindowsOld=true;
Check(!viewModel.Preview.Contains("stale-pre41")&&viewModel.ConfigurationSummary.Contains("重启后自动登录：禁用")&&viewModel.ConfigurationSummary.Contains("硬件关联应用：阻止")&&viewModel.ConfigurationSummary.Contains("空 Windows.old 目录：首次登录"),"pre41 edits invalidate preview and refresh all summary rows");
Check(viewModel.HelpTips["restartsignon"].Body.Contains("首次自动登录独立")&&viewModel.HelpTips["deviceapps"].Body.Contains("协同安装程序")&&viewModel.HelpTips["windowsold"].Body.Contains("非空目录保留"),"pre41 help reflects selected values and scope");
var grouped41=UnattendLocal.Desktop.SummaryPresentation.FromText(viewModel.ConfigurationSummary);
Check(grouped41.Sections.Single(s=>s.Kind=="basic").Rows.Any(row=>row.SourceLabel=="重启后自动登录")&&grouped41.Sections.Single(s=>s.Kind=="protection").Rows.Count(row=>row.SourceLabel is "硬件关联应用" or "空 Windows.old 目录")==2,"pre41 rows appear in related summary sections");
// Existing imported content must survive a new setting and a subsequent removal.
p=Plan();var unknown41=XmlMerge.Read(WithoutSnapshot(Xml(p)));var ext41=(XmlElement)unknown41.SelectSingleNode("//*[local-name()='Extensions']")!;
var extra41=unknown41.CreateElement("File",Schneegans.Unattend.Constants.MyNamespaceUri);extra41.SetAttribute("path","C:\\Windows\\Setup\\Scripts\\keep-pre41.txt");extra41.InnerText="retain this unknown payload";ext41.AppendChild(extra41);
var imported41=XmlPlanImport.Import(unknown41.OuterXml,g);imported41.DisableAutomaticRestartSignOn=imported41.PreventDeviceApps=imported41.DeleteWindowsOld=true;
var output41=Xml(imported41);Check(output41.Contains("retain this unknown payload")&&XmlPlanImport.Import(output41,g).PreventDeviceApps,"adding pre41 flags preserves unknown source content");
imported41.DisableAutomaticRestartSignOn=imported41.PreventDeviceApps=imported41.DeleteWindowsOld=false;Check(SameXml(unknown41.OuterXml,Xml(imported41)),"removing added pre41 flags returns to original imported document");
p=Plan();p.DisableAutomaticRestartSignOn=p.PreventDeviceApps=p.DeleteWindowsOld=true;
var enabled41=XmlPlanImport.Import(WithoutSnapshot(Xml(p)),g);enabled41.DisableAutomaticRestartSignOn=enabled41.PreventDeviceApps=enabled41.DeleteWindowsOld=false;
var disabled41=Xml(enabled41);Check(!DecodedXml(disabled41).Contains("DisableAutomaticRestartSignOn")&&!DecodedXml(disabled41).Contains("DisableCoInstallers")&&!DecodedXml(disabled41).Contains("rmdir C:\\Windows.old"),"recognized imported groups can all be unchecked safely");
// Values and complete groups are checked from payloads, not stale website parameter names.
var changed41=WithoutSnapshot(Xml(p)).Replace("/v \"DisableAutomaticRestartSignOn\" /t REG_DWORD /d 1 /f;","/v \"DisableAutomaticRestartSignOn\" /t REG_DWORD /d 0 /f;").Replace("reg.exe add \"HKLM\\Software\\Microsoft\\Windows\\CurrentVersion\\Device Installer\" /v \"DisableCoInstallers\" /t REG_DWORD /d 1 /f;","");
var partial41=XmlPlanImport.Import(changed41,g);Check(!partial41.DisableAutomaticRestartSignOn&&!partial41.PreventDeviceApps&&partial41.DeleteWindowsOld&&SameXml(changed41,Xml(partial41)),"altered restart values and partial device policies are preserved without claiming full support");
Check(partial41.ImportedXml!.Notices.Any(n=>n.Contains("部分")),"partial device command group has a clear import notice");
var recursive41=WithoutSnapshot(Xml(p)).Replace("rmdir C:\\Windows.old","rmdir /s /q C:\\Windows.old");var recursivePlan41=XmlPlanImport.Import(recursive41,g);
Check(!recursivePlan41.DeleteWindowsOld&&SameXml(recursive41,Xml(recursivePlan41)),"recursive custom cleanup is never represented as empty-directory cleanup");
// Migration of a pre40 imported JSON: preserve pending edits and rebase only newly recognized flags.
var oldSource41=WithoutSnapshot(Xml(p));var oldPlan41=XmlPlanImport.Import(oldSource41,g);
oldPlan41.DisableAutomaticRestartSignOn=oldPlan41.PreventDeviceApps=oldPlan41.DeleteWindowsOld=false;
var oldBaseline41=PlanStore.Deserialize(PlanStore.Serialize(oldPlan41,true));oldBaseline41.ImportedXml=null;
oldPlan41.ImportedXml!.BaselineXml=Encoding.UTF8.GetString(g.GenerateStandalone(oldBaseline41));oldPlan41.ComputerName="PENDING41";
var oldJson41=System.Text.Json.Nodes.JsonNode.Parse(PlanStore.Serialize(oldPlan41,true))!.AsObject();
foreach(var item in CommonSystemSettings.Items)oldJson41.Remove(item.Property);
var migrated41=PlanStore.Deserialize(oldJson41.ToJsonString());
Check(migrated41.DisableAutomaticRestartSignOn&&migrated41.PreventDeviceApps&&migrated41.DeleteWindowsOld&&migrated41.ComputerName=="PENDING41"&&migrated41.ImportedXml!.Blockers.Length==0,"old imported JSON recovers flags without dropping pending edits");
var migratedOutput41=Xml(migrated41);Check(Node(migratedOutput41,"//u:settings[@pass='specialize']/u:component[@name='Microsoft-Windows-Shell-Setup']/u:ComputerName")=="PENDING41","migrated JSON can still export earlier pending edits");
migrated41.PreventDeviceApps=false;Check(!DecodedXml(Xml(migrated41)).Contains("DisableCoInstallers"),"migrated source setting has a valid editable baseline");
var resaved41=PlanStore.Deserialize(PlanStore.Serialize(migrated41,true));Check(!resaved41.PreventDeviceApps,"explicit new-format unchecked flag is not overwritten by original XML");
Console.WriteLine($"Final pre41 total: {count} tests passed.");

var systemFixtureName41=System.Reflection.Assembly.GetExecutingAssembly().GetManifestResourceNames().Single(n=>n.EndsWith("upstream-system-options.xml"));
using(var systemFixtureStream41=System.Reflection.Assembly.GetExecutingAssembly().GetManifestResourceStream(systemFixtureName41)!)using(var systemFixtureReader41=new StreamReader(systemFixtureStream41)){
 var official41=systemFixtureReader41.ReadToEnd();var officialPlan41=XmlPlanImport.Import(official41,g);
 Check(officialPlan41.DisableAutomaticRestartSignOn&&officialPlan41.PreventDeviceApps&&officialPlan41.DeleteWindowsOld,"official website fixture restores all three system options");
 Check(officialPlan41.AutoLogon&&officialPlan41.ImportedXml!.Blockers.Length==0&&SameXml(official41,Xml(officialPlan41)),"official fixture preserves initial login and unknown content on roundtrip");
 officialPlan41.DisableAutomaticRestartSignOn=officialPlan41.PreventDeviceApps=officialPlan41.DeleteWindowsOld=false;
 var officialEdited41=Xml(officialPlan41);Check(!DecodedXml(officialEdited41).Contains("DisableCoInstallers")&&!DecodedXml(officialEdited41).Contains("rmdir C:\\Windows.old")&&Node(officialEdited41,"//u:AutoLogon/u:LogonCount")=="1","official fixture options can be removed without disabling initial logon");
}
p=Plan();p.DisableAutomaticRestartSignOn=true;var conflictSource41=WithoutSnapshot(Xml(p)).Replace("/v \"DisableAutomaticRestartSignOn\" /t REG_DWORD /d 1 /f;","/v \"DisableAutomaticRestartSignOn\" /t REG_DWORD /d 1 /f;\nreg.exe add \"HKLM\\Software\\Microsoft\\Windows\\CurrentVersion\\Policies\\System\" /v \"DisableAutomaticRestartSignOn\" /t REG_DWORD /d 0 /f;");
var conflictPlan41=XmlPlanImport.Import(conflictSource41,g);Check(!conflictPlan41.DisableAutomaticRestartSignOn&&SameXml(conflictSource41,Xml(conflictPlan41)),"conflicting restart policy scripts are preserved without an incorrect enabled indication");
conflictPlan41.DisableAutomaticRestartSignOn=true;Reject(conflictPlan41,"ambiguous restart policy cannot be overridden from the new switch");
partial41.PreventDeviceApps=true;Reject(partial41,"partial device script cannot be silently overlaid by full option");
recursivePlan41.DeleteWindowsOld=true;Reject(recursivePlan41,"recursive cleanup cannot be silently overlaid by empty-directory option");
var omitted41=PlanStore.Deserialize(PlanStore.Serialize(XmlPlanImport.Import(oldSource41,g),false));Check(omitted41.ImportedXml!.OriginalOmitted&&omitted41.DisableAutomaticRestartSignOn&&omitted41.PreventDeviceApps&&omitted41.DeleteWindowsOld,"secret omission preserves visible flag choices while omitting original XML");Reject(omitted41,"omitted original XML still blocks export after system-option update");
Console.WriteLine($"Final pre41 total: {count} tests passed.");

// pre42: script-backed image personalization is only generated, never executed here.
const string desktopBytes42="[System.IO.File]::ReadAllBytes('D:\\wallpaper & desk.jpg'); # <desktop>";
const string lockBytes42="[System.IO.File]::ReadAllBytes('D:\\lockscreen.jpg'); # <lock>";
foreach(var initial42 in new[]{InstallPlan.New(),InstallPlan.NewCustom(),PlanStore.Deserialize("{\"SchemaVersion\":1,\"Users\":[]}")})
 Check(initial42.WallpaperMode==WallpaperSettings.Default&&initial42.LockScreenMode==ImageBackgroundSettings.LockDefault&&initial42.WallpaperImageScript==""&&initial42.LockScreenImageScript=="","pre42 starts with default backgrounds and empty script fields");
foreach(var arch42 in new[]{"amd64","arm64","x86"})foreach(var desktopMode42 in new[]{WallpaperSettings.Default,WallpaperSettings.Solid,WallpaperSettings.Script})foreach(var lockMode42 in new[]{ImageBackgroundSettings.LockDefault,ImageBackgroundSettings.LockScript}){
 p=Plan();p.Architecture=arch42;p.WallpaperMode=desktopMode42;p.LockScreenMode=lockMode42;p.WallpaperImageScript=desktopBytes42;p.LockScreenImageScript=lockBytes42;p.WallpaperColorHex="#123456";
 var generated42=Xml(p);var setup42=Node(generated42,"//*[local-name()='File' and contains(@path,'Specialize.ps1')]")??"";
 var perUser42=Node(generated42,"//*[local-name()='File' and contains(@path,'UserOnce.ps1')]")??"";
 var first42=Node(generated42,"//*[local-name()='File' and contains(@path,'FirstLogon.ps1')]")??"";
 bool wallpaperEnabled42=desktopMode42==WallpaperSettings.Script, lockEnabled42=lockMode42==ImageBackgroundSettings.LockScript;
 Check((Node(generated42,"//*[local-name()='File' and contains(@path,'GetWallpaper.ps1')]")!=null)==wallpaperEnabled42&&(Node(generated42,"//*[local-name()='File' and contains(@path,'GetLockScreenImage.ps1')]")!=null)==lockEnabled42,"only enabled image loaders are emitted: "+arch42+desktopMode42+lockMode42);
 Check(setup42.Contains("$bytes = & 'C:\\Windows\\Setup\\Scripts\\GetWallpaper.ps1';")==wallpaperEnabled42&&setup42.Contains("/v LockScreenImagePath /t REG_SZ /d \"C:\\Windows\\Setup\\Scripts\\LockScreenImage\" /f;")==lockEnabled42,"image acquisition and lock-screen path use specialize stage: "+arch42+desktopMode42+lockMode42);
 Check(perUser42.Contains("& 'C:\\Windows\\Setup\\Scripts\\SetWallpaper.ps1';")== (desktopMode42!=WallpaperSettings.Default)&&!first42.Contains("GetWallpaper.ps1")&&!first42.Contains("GetLockScreenImage.ps1"),"wallpaper application is user-once and neither loader uses first-logon stage: "+arch42+desktopMode42+lockMode42);
 Check(!setup42.Contains("SetEduPolicies")&&!setup42.Contains("EnableSharedPCMode")&&!generated42.Contains("<DiskConfiguration>"),"image settings do not add shared-PC policies or disk operations: "+arch42+desktopMode42+lockMode42);
 foreach(var metadata42 in new[]{false,true}){
  var original42=metadata42?generated42:WithoutSnapshot(generated42);var imported42=XmlPlanImport.Import(original42,g);
  Check(imported42.WallpaperMode==desktopMode42&&imported42.LockScreenMode==lockMode42&&imported42.ImportedXml!.Blockers.Length==0&&SameXml(original42,Xml(imported42)),"pre42 image modes roundtrip from actual XML: "+arch42+desktopMode42+lockMode42+metadata42);
 }
}
p=Plan();p.WallpaperImageScript="\0inactive-desktop-private";p.LockScreenImageScript="\0inactive-lock-private";
Check(!Xml(p).Contains("inactive-desktop")&&!Xml(p).Contains("inactive-lock")&&!Recommendations.MissingInputs(p).Contains("图片加载脚本"),"inactive invalid image scripts neither generate nor enter restore metadata");
p.WallpaperMode=WallpaperSettings.Script;Reject(p,"active image script rejects invalid XML characters");p.WallpaperImageScript="";Reject(p,"active desktop script cannot be empty");p.WallpaperImageScript=new string('x',2*1024*1024+1);Reject(p,"oversized active desktop script rejected");p.WallpaperImageScript=desktopBytes42;p.LockScreenMode=ImageBackgroundSettings.LockScript;p.LockScreenImageScript="";Reject(p,"active lock-screen script cannot be empty");p.LockScreenImageScript="\0";Reject(p,"active lock-screen script rejects invalid XML characters");p.LockScreenImageScript=lockBytes42;p.LockScreenMode="invalid";Reject(p,"unknown lock-screen mode rejected");
p=Plan();p.WallpaperMode=WallpaperSettings.Script;p.WallpaperImageScript=desktopBytes42;p.LockScreenMode=ImageBackgroundSettings.LockScript;p.LockScreenImageScript=lockBytes42;
foreach(var secrets42 in new[]{true,false}){var stored42=PlanStore.Deserialize(PlanStore.Serialize(p,secrets42));Check(stored42.WallpaperMode==WallpaperSettings.Script&&stored42.WallpaperImageScript==desktopBytes42&&stored42.LockScreenMode==ImageBackgroundSettings.LockScript&&stored42.LockScreenImageScript==lockBytes42,"image scripts and modes save independently of credential selection: "+secrets42);}
Check(Recommendations.Differences(p).Any(d=>d.Property==nameof(InstallPlan.WallpaperMode))&&Recommendations.Differences(p).Any(d=>d.Property==nameof(InstallPlan.LockScreenMode)),"both image modes appear as recommendation differences");
Recommendations.SelectNetworkScene(p,"wifi");Check(WallpaperSettings.IsScript(p)&&ImageBackgroundSettings.IsLockScript(p),"network-scene selection retains both image modes");Recommendations.Restore(p,nameof(InstallPlan.LockScreenMode));Check(WallpaperSettings.IsScript(p)&&p.LockScreenMode==ImageBackgroundSettings.LockDefault&&p.LockScreenImageScript==lockBytes42,"single restore changes lock-screen mode while retaining script and desktop choice");Recommendations.Apply(p,"wired");Check(p.WallpaperMode==WallpaperSettings.Default&&p.LockScreenMode==ImageBackgroundSettings.LockDefault&&p.WallpaperImageScript==desktopBytes42&&p.LockScreenImageScript==lockBytes42,"full recommendations restore modes while retaining personal script content");
p=Plan();p.WallpaperMode=WallpaperSettings.Script;p.WallpaperImageScript=desktopBytes42;p.LockScreenMode=ImageBackgroundSettings.LockScript;p.LockScreenImageScript=lockBytes42;p.AutoLogon=false;p.KeepSensitiveFiles=true;
Check(!Xml(p).Contains("<AutoLogon>")&&(Node(Xml(p),"//*[local-name()='File' and contains(@path,'UserOnce.ps1')]")??"").Contains("SetWallpaper.ps1"),"both image settings can generate without forcing initial automatic logon");
viewModel.Plan=Plan();viewModel.Preview="stale-images42";viewModel.Plan.WallpaperMode=WallpaperSettings.Script;viewModel.Plan.LockScreenMode=ImageBackgroundSettings.LockScript;
Check(viewModel.IsScriptWallpaper&&viewModel.IsScriptLockScreen&&!viewModel.IsSolidWallpaper&&Recommendations.MissingInputs(viewModel.Plan).Contains("桌面图片加载脚本")&&Recommendations.MissingInputs(viewModel.Plan).Contains("锁屏图片加载脚本"),"script controls and required-input summary follow active modes");
viewModel.Plan.WallpaperImageScript=desktopBytes42;viewModel.Plan.LockScreenImageScript=lockBytes42;
Check(!viewModel.Preview.Contains("stale-images42")&&viewModel.ConfigurationSummary.Contains("锁屏图片：脚本加载")&&viewModel.HelpTips["wallpaperimage"].Body.Contains("安装时读取")&&viewModel.HelpTips["lockscreen"].Body.Contains("版本类型"),"image edits update preview state, summary and dynamic help");
Check(!viewModel.ConfigurationSummary.Contains("<desktop>")&&!viewModel.HelpTips.Values.Any(h=>h.Body.Contains("<desktop>")),"image source scripts are not exposed through summary or help text");
var imageGroups42=UnattendLocal.Desktop.SummaryPresentation.FromText(viewModel.ConfigurationSummary);Check(imageGroups42.Sections.Single(s=>s.Kind=="appearance").Rows.Any(row=>row.SourceLabel=="锁屏图片"),"lock-screen summary is grouped with appearance settings");
viewModel.Plan.WallpaperMode=WallpaperSettings.Default;viewModel.Plan.LockScreenMode=ImageBackgroundSettings.LockDefault;Check(!viewModel.IsScriptWallpaper&&!viewModel.IsScriptLockScreen&&viewModel.Plan.WallpaperImageScript==desktopBytes42&&viewModel.Plan.LockScreenImageScript==lockBytes42,"default mode hides editors without discarding saved scripts");
Check(ImageBackgroundSettings.DesktopExample.Contains("wallpaper.jpg")&&ImageBackgroundSettings.LockExample.Contains("lockscreen.jpg")&&!ImageBackgroundSettings.DesktopExample.Contains("Invoke-WebRequest"),"examples use offline file sources and do not require downloading");
// Change and remove image controls while preserving unknown source content.
p=Plan();p.WallpaperMode=WallpaperSettings.Script;p.WallpaperImageScript=desktopBytes42;p.LockScreenMode=ImageBackgroundSettings.LockScript;p.LockScreenImageScript=lockBytes42;
var sourceImages42=XmlMerge.Read(WithoutSnapshot(Xml(p)));var imageExt42=(XmlElement)sourceImages42.SelectSingleNode("//*[local-name()='Extensions']")!;var keepImage42=sourceImages42.CreateElement("File",Schneegans.Unattend.Constants.MyNamespaceUri);keepImage42.SetAttribute("path","C:\\Windows\\Setup\\Scripts\\keep-images42.txt");keepImage42.InnerText="retain image import extras";imageExt42.AppendChild(keepImage42);
var editImages42=XmlPlanImport.Import(sourceImages42.OuterXml,g);editImages42.WallpaperImageScript="[System.IO.File]::ReadAllBytes('D:\\new-desktop.png');";editImages42.LockScreenImageScript="[System.IO.File]::ReadAllBytes('D:\\new-lock.png');";
Check(Xml(editImages42).Contains("retain image import extras")&&(Node(Xml(editImages42),"//*[local-name()='File' and contains(@path,'GetWallpaper.ps1')]")??"").Contains("new-desktop.png"),"changing image scripts preserves unrelated imported payloads");
editImages42.WallpaperMode=WallpaperSettings.Default;editImages42.LockScreenMode=ImageBackgroundSettings.LockDefault;var removedImages42=Xml(editImages42);Check(!DecodedXml(removedImages42).Contains("GetWallpaper.ps1")&&!DecodedXml(removedImages42).Contains("GetLockScreenImage.ps1")&&removedImages42.Contains("retain image import extras"),"returning imported image settings to defaults removes only known image pipeline");
var changedImageSource42=sourceImages42.OuterXml.Replace("Set-WallpaperImage -LiteralPath 'C:\\Windows\\Setup\\Scripts\\Wallpaper';","Set-WallpaperImage -LiteralPath 'C:\\custom-wallpaper';");var changedImagePlan42=XmlPlanImport.Import(changedImageSource42,g);
Check(changedImagePlan42.WallpaperMode==WallpaperSettings.Default&&changedImagePlan42.ImportedXml!.Notices.Any(n=>n.Contains("桌面图片"))&&SameXml(changedImageSource42,Xml(changedImagePlan42)),"altered image application helper is preserved rather than misrepresented");changedImagePlan42.WallpaperMode=WallpaperSettings.Solid;Reject(changedImagePlan42,"unknown image application helper cannot be silently replaced by solid background");
var changedLockSource42=sourceImages42.OuterXml.Replace("LockScreenImagePath /t REG_SZ","CustomLockScreenPath /t REG_SZ");var changedLockPlan42=XmlPlanImport.Import(changedLockSource42,g);Check(changedLockPlan42.LockScreenMode==ImageBackgroundSettings.LockDefault&&SameXml(changedLockSource42,Xml(changedLockPlan42)),"altered lock-screen script is preserved with an unsupported-state notice");changedLockPlan42.LockScreenMode=ImageBackgroundSettings.LockScript;changedLockPlan42.LockScreenImageScript=lockBytes42;Reject(changedLockPlan42,"unknown lock-screen pipeline cannot be silently overlaid");
// A pre41 imported JSON retains the original unrepresented image pipeline and pending edits.
var oldImagePlan42=XmlPlanImport.Import(sourceImages42.OuterXml,g);oldImagePlan42.WallpaperMode=WallpaperSettings.Default;oldImagePlan42.LockScreenMode=ImageBackgroundSettings.LockDefault;var oldImageBaseline42=PlanStore.Deserialize(PlanStore.Serialize(oldImagePlan42,true));oldImageBaseline42.ImportedXml=null;oldImagePlan42.ImportedXml!.BaselineXml=Encoding.UTF8.GetString(g.GenerateStandalone(oldImageBaseline42));oldImagePlan42.ComputerName="PENDING42";
var oldImageJson42=System.Text.Json.Nodes.JsonNode.Parse(PlanStore.Serialize(oldImagePlan42,true))!.AsObject();foreach(var field42 in new[]{nameof(InstallPlan.WallpaperImageScript),nameof(InstallPlan.LockScreenMode),nameof(InstallPlan.LockScreenImageScript)})oldImageJson42.Remove(field42);
var migratedImage42=PlanStore.Deserialize(oldImageJson42.ToJsonString());Check(WallpaperSettings.IsScript(migratedImage42)&&ImageBackgroundSettings.IsLockScript(migratedImage42)&&migratedImage42.ImportedXml!.Blockers.Length==0&&migratedImage42.ComputerName=="PENDING42","old imported JSON recovers both image modes while retaining pending changes");
Check(Node(Xml(migratedImage42),"//u:settings[@pass='specialize']/u:component[@name='Microsoft-Windows-Shell-Setup']/u:ComputerName")=="PENDING42","old imported image JSON remains exportable after baseline migration");migratedImage42.LockScreenMode=ImageBackgroundSettings.LockDefault;var explicitImage42=PlanStore.Deserialize(PlanStore.Serialize(migratedImage42,true));Check(explicitImage42.LockScreenMode==ImageBackgroundSettings.LockDefault,"explicit current-format default is not overwritten by original image XML");
var omittedImage42=PlanStore.Deserialize(PlanStore.Serialize(XmlPlanImport.Import(sourceImages42.OuterXml,g),false));Check(omittedImage42.ImportedXml!.OriginalOmitted&&WallpaperSettings.IsScript(omittedImage42),"omitting original XML preserves visible image mode");Reject(omittedImage42,"image settings do not bypass omitted-original export restriction");
Console.WriteLine($"Final pre42 total: {count} tests passed.");

var officialImageResource42=System.Reflection.Assembly.GetExecutingAssembly().GetManifestResourceNames().Single(n=>n.EndsWith("upstream-image-backgrounds.xml"));
using(var officialImageStream42=System.Reflection.Assembly.GetExecutingAssembly().GetManifestResourceStream(officialImageResource42)!)using(var officialImageReader42=new StreamReader(officialImageStream42)){
 var officialImageXml42=officialImageReader42.ReadToEnd();var officialImagePlan42=XmlPlanImport.Import(officialImageXml42,g);
 Check(WallpaperSettings.IsScript(officialImagePlan42)&&ImageBackgroundSettings.IsLockScript(officialImagePlan42)&&officialImagePlan42.WallpaperImageScript.Contains("[byte[]](1,2,3)")&&officialImagePlan42.LockScreenImageScript.Contains("[byte[]](4,5,6)"),"credential-free official fixture restores both actual image loaders");
 Check(officialImagePlan42.ImportedXml!.Blockers.Length==0&&SameXml(officialImageXml42,Xml(officialImagePlan42)),"official image fixture roundtrip retains original unrelated fields");
 officialImagePlan42.WallpaperImageScript="[byte[]](7,8,9);";officialImagePlan42.LockScreenImageScript="[byte[]](10,11,12);";
 var editedOfficial42=Xml(officialImagePlan42);var reimportOfficial42=XmlPlanImport.Import(editedOfficial42,g);Check(reimportOfficial42.WallpaperImageScript.Contains("7,8,9")&&reimportOfficial42.LockScreenImageScript.Contains("10,11,12"),"official fixture supports editing both image loader scripts");
 officialImagePlan42.WallpaperMode=WallpaperSettings.Default;officialImagePlan42.LockScreenMode=ImageBackgroundSettings.LockDefault;Check(!DecodedXml(Xml(officialImagePlan42)).Contains("GetWallpaper.ps1")&&!DecodedXml(Xml(officialImagePlan42)).Contains("GetLockScreenImage.ps1"),"official image settings can return to defaults without a conflicting leftover pipeline");
 var officialDoc42=XmlMerge.Read(officialImageXml42);Check(officialDoc42.SelectNodes("//*[local-name()='Password' or local-name()='LocalAccount']")!.Count==0,"official pre42 fixture contains no account or password nodes");
}
Console.WriteLine($"Final pre42 total: {count} tests passed.");

// pre43: app removal does not execute on the host; verify native payloads and editable imports.
Check(AppRemovalSettings.Items.Count==60&&AppRemovalSettings.Items.Select(i=>i.Id).Order().SequenceEqual(upstream.Bloatwares.Keys.Order()),"full upstream removal catalog has Chinese labels and exact stable IDs");
Check(AppRemovalSettings.Items.All(i=>!string.IsNullOrWhiteSpace(i.Description))&&AppRemovalSettings.Items.Single(i=>i.Id=="RemoveStore").Description.Contains("更新"),"each removal item has an impact description including Store dependency warning");
p=Plan();Check(p.RemovedApps.Count==0&&!DecodedXml(Xml(p)).Contains("RemovePackage.ps1"),"default does not remove apps");
foreach(var item43 in AppRemovalSettings.Items){
 p=Plan();p.RemovedApps.Add(item43.Id);var x43=Xml(p);
 foreach(var snapshot43 in new[]{true,false}){
  var input43=snapshot43?x43:WithoutSnapshot(x43);var q43=XmlPlanImport.Import(input43,g);
  Check(q43.RemovedApps.SequenceEqual(new[]{item43.Id})&&q43.ImportedXml!.Blockers.Length==0,"single app import restores actual native payload: "+item43.Id+snapshot43);
  Check(SameXml(input43,Xml(q43)),"single app import preserves unchanged XML: "+item43.Id+snapshot43);
  q43.RemovedApps.Clear();var output43=Xml(q43);var empty43=XmlPlanImport.Import(output43,g);
  Check(empty43.RemovedApps.Count==0&&!DecodedXml(output43).Contains("RemovePackage.ps1")&&!DecodedXml(output43).Contains("RemoveCapability.ps1")&&!DecodedXml(output43).Contains("RemoveFeature.ps1"),"unchecking removes native helpers and custom side effects: "+item43.Id+snapshot43);
 }
 foreach(bool secrets43 in new[]{false,true})Check(PlanStore.Deserialize(PlanStore.Serialize(p,secrets43)).RemovedApps.SequenceEqual(p.RemovedApps),"removal selections save without credential dependence: "+item43.Id+secrets43);
}
foreach(var arch43 in new[]{"amd64","arm64","x86"}){
 p=Plan();p.Architecture=arch43;p.RemovedApps=new(AppRemovalSettings.Items.Select(i=>i.Id));var all43=Xml(p);var q43=XmlPlanImport.Import(WithoutSnapshot(all43),g);
 Check(q43.RemovedApps.Order().SequenceEqual(p.RemovedApps.Order())&&q43.ImportedXml!.Notices.All(n=>!n.StartsWith("应用移除脚本")),"combined catalog is fully recognized on "+arch43);
 q43.RemovedApps.Remove("RemoveTeams");q43.RemovedApps.Remove("RemoveCopilot");q43.RemovedApps.Remove("RemoveStore");var changed43=Xml(q43);var re43=XmlPlanImport.Import(changed43,g);
 Check(re43.RemovedApps.Order().SequenceEqual(q43.RemovedApps.Order()),"combined imports allow removing individual choices on "+arch43);
}
p=Plan();p.RemovedApps.Add("RemovePhotos");p.RemovedApps.Add("RemovePhotos");Reject(p,"duplicate removal IDs rejected");p.RemovedApps=new(){"RemoveUnknown"};Reject(p,"unknown removal IDs rejected");p.RemovedApps=new(){null!};Reject(p,"null removal IDs rejected");p.RemovedApps=null!;Reject(p,"null removal list rejected");
foreach(string bad43 in new[]{"{\"SchemaVersion\":1,\"RemovedApps\":null}","{\"SchemaVersion\":1,\"RemovedApps\":[\"unknown\"]}","{\"SchemaVersion\":1,\"RemovedApps\":[\"RemovePhotos\",\"RemovePhotos\"]}"}){bool rejected43=false;try{PlanStore.Deserialize(bad43);}catch(ArgumentException){rejected43=true;}Check(rejected43,"invalid JSON removal list is rejected");}
p=Plan();p.RemovedApps.Add("RemovePhotos");p.ComputerName="PRE43";var beforeApps43=Xml(p);var editApps43=XmlPlanImport.Import(beforeApps43,g);editApps43.RemovedApps.Add("RemoveCalculator");
Check(XmlPlanImport.Import(Xml(editApps43),g).RemovedApps.Order().SequenceEqual(editApps43.RemovedApps.Order()),"add app to imported native removal helper");
var mutatedApps43=beforeApps43.Replace("Get-AppxProvisionedPackage -Online;","Get-AppxProvisionedPackage -Online | Where-Object DisplayName -eq 'changed';");var unknownApps43=XmlPlanImport.Import(mutatedApps43,g);
Check(unknownApps43.RemovedApps.Count==0&&unknownApps43.ImportedXml!.Notices.Any(n=>n.StartsWith("应用移除脚本"))&&SameXml(mutatedApps43,Xml(unknownApps43)),"modified removal helper preserved and not represented as checkbox");unknownApps43.RemovedApps.Add("RemoveCalculator");Reject(unknownApps43,"unknown native removal helper blocks overlap");
var unknownSelector43=beforeApps43.Replace("'Microsoft.Windows.Photos';","'Synthetic.Unknown.Package';");var unknownSelectorPlan43=XmlPlanImport.Import(unknownSelector43,g);Check(unknownSelectorPlan43.RemovedApps.Count==0&&SameXml(unknownSelector43,Xml(unknownSelectorPlan43)),"unknown removal selectors retained");unknownSelectorPlan43.RemovedApps.Add("RemovePhotos");Reject(unknownSelectorPlan43,"unknown selector list cannot be silently changed");
p=Plan();p.RemovedApps.Add("RemoveOneDrive");var partialOneDrive43=Xml(p).Replace("-Name 'OneDriveSetup'","-Name 'ModifiedSetup'");var partialPlan43=XmlPlanImport.Import(partialOneDrive43,g);Check(!partialPlan43.RemovedApps.Contains("RemoveOneDrive")&&SameXml(partialOneDrive43,Xml(partialPlan43)),"partial custom-only OneDrive removal retained");partialPlan43.RemovedApps.Add("RemoveOneDrive");Reject(partialPlan43,"partial OneDrive pipeline blocks new removal setting");
var oldPlan43=XmlPlanImport.Import(beforeApps43,g);oldPlan43.RemovedApps.Clear();var oldBaseline43=PlanStore.Deserialize(PlanStore.Serialize(oldPlan43,true));oldBaseline43.ImportedXml=null;oldPlan43.ImportedXml!.BaselineXml=Encoding.UTF8.GetString(g.GenerateStandalone(oldBaseline43));oldPlan43.ComputerName="PENDING43";var oldJson43=System.Text.Json.Nodes.JsonNode.Parse(PlanStore.Serialize(oldPlan43,true))!.AsObject();oldJson43.Remove(nameof(InstallPlan.RemovedApps));var migrated43=PlanStore.Deserialize(oldJson43.ToJsonString());
Check(migrated43.RemovedApps.SequenceEqual(new[]{"RemovePhotos"})&&migrated43.ComputerName=="PENDING43"&&migrated43.ImportedXml!.Blockers.Length==0,"older imported plan migrates actual removal state and preserves pending edits");
Check(XmlPlanImport.Import(Xml(migrated43),g).RemovedApps.Contains("RemovePhotos")&&Node(Xml(migrated43),"//u:ComputerName")=="PENDING43","older removal baseline remains editable");migrated43.RemovedApps.Clear();Check(PlanStore.Deserialize(PlanStore.Serialize(migrated43,true)).RemovedApps.Count==0,"explicit empty current-format choices are retained");
p=Plan();Recommendations.Apply(p,"wired");Check(!Recommendations.Differences(p).Any(d=>d.Property==nameof(InstallPlan.RemovedApps)),"empty collection equals recommendation baseline");p.RemovedApps.Add("RemovePhotos");Check(Recommendations.Differences(p).Any(d=>d.Property==nameof(InstallPlan.RemovedApps)&&d.Current.Contains("照片")),"removal difference shows Chinese selected labels");Recommendations.SelectNetworkScene(p,"wifi");Check(p.RemovedApps.Contains("RemovePhotos"),"network scene does not clear app choices");Recommendations.Restore(p,nameof(InstallPlan.RemovedApps));Check(p.RemovedApps.Count==0,"restore app removal defaults to empty");p.RemovedApps.Add("RemovePhotos");Recommendations.Apply(p,"wired");Check(p.RemovedApps.Count==0,"full recommendation reset returns to no removal");
viewModel.Plan=Plan();viewModel.Preview="STALE43";var choice43=viewModel.AppRemovalChoices.Single(i=>i.Id=="RemovePhotos");choice43.IsSelected=true;Check(viewModel.Plan.RemovedApps.Contains("RemovePhotos")&&viewModel.AppRemovalSummary.Contains("照片")&&viewModel.ConfigurationSummary.Contains("应用移除：已选 1 项")&&viewModel.Preview!="STALE43","checkbox updates selection, summary and preview");var detached43=viewModel.Plan;viewModel.Plan=Plan();Check(!choice43.IsSelected,"replace plan refreshes checkbox state");viewModel.Plan.RemovedApps=new(){"RemoveCalculator"};Check(viewModel.AppRemovalChoices.Single(i=>i.Id=="RemoveCalculator").IsSelected&&viewModel.AppRemovalSummary.Contains("计算器"),"replace collection rebinds removal observer");viewModel.Preview="CURRENT43";detached43.RemovedApps.Add("RemoveTeams");Check(viewModel.Preview=="CURRENT43","old plan collection unhooked");viewModel.Plan.RemovedApps.Clear();Check(viewModel.AppRemovalSummary.Contains("未选择")&&viewModel.HelpTips.ContainsKey("appremoval"),"clear selection updates removal summary and help");
Console.WriteLine($"Final pre43 total: {count} tests passed.");

viewModel.AppRemovalFilter="商店";Check(viewModel.FilteredAppRemovalChoices.Any(i=>i.Id=="RemoveStore")&&viewModel.FilteredAppRemovalChoices.Count<60,"Chinese app search filters list");viewModel.AppRemovalFilter="no_match_43";Check(viewModel.FilteredAppRemovalChoices.Count==0&&viewModel.AppRemovalFilterSummary.Contains("0 / 60"),"no search matches explicitly shown");viewModel.AppRemovalFilter="";
Console.WriteLine($"Final pre43 total: {count} tests passed.");

var nativeRemoval43=typeof(Program).Assembly.GetManifestResourceNames().First(n=>n.EndsWith("upstream-app-removal.xml"));using(var nativeStream43=typeof(Program).Assembly.GetManifestResourceStream(nativeRemoval43)!){using var reader43=new StreamReader(nativeStream43);var actual43=reader43.ReadToEnd();var imported43=XmlPlanImport.Import(actual43,g);Check(imported43.RemovedApps.Order().SequenceEqual(new[]{"RemovePhotos","RemoveTeams","RemoveCopilot","RemoveOneDrive","RemoveNotepad","RemoveRecall"}.Order()),"credential-free official removal fixture restores six choices");Check(SameXml(actual43,Xml(imported43)),"official removal fixture unchanged export retains unrelated settings");imported43.RemovedApps.Remove("RemoveCopilot");imported43.RemovedApps.Add("RemoveCalculator");Check(XmlPlanImport.Import(Xml(imported43),g).RemovedApps.Order().SequenceEqual(imported43.RemovedApps.Order()),"official removal fixture supports changing mixed selectors and custom policies");imported43.RemovedApps.Clear();Check(XmlPlanImport.Import(Xml(imported43),g).RemovedApps.Count==0,"official removal fixture can cancel all six choices");Check(XmlMerge.Read(actual43).SelectNodes("//*[local-name()='Password' or local-name()='LocalAccount']")!.Count==0,"official removal fixture has no account/password nodes");}
Console.WriteLine($"Final pre43 total: {count} tests passed.");

p=Plan();p.RemovedApps.Add("RemovePhotos");var activeRemoval43=Xml(p);
var noBootstrapDoc43=XmlMerge.Read(activeRemoval43);foreach(XmlElement path43 in noBootstrapDoc43.SelectNodes("//*[local-name()='Path']")!)if(path43.InnerText.Contains("-File")&&path43.InnerText.Contains("Specialize.ps1"))path43.InnerText=path43.InnerText.Replace("Specialize.ps1","Different.ps1");
foreach(var altered43 in new[]{activeRemoval43.Replace("foreach( $script in $scripts ) {","foreach( $script in @() ) {"),activeRemoval43.Replace("&amp; 'C:\\Windows\\Setup\\Scripts\\RemovePackage.ps1';","if ($false) { &amp; 'C:\\Windows\\Setup\\Scripts\\RemovePackage.ps1'; }"),noBootstrapDoc43.OuterXml}){
 Check(altered43!=activeRemoval43,"synthetic altered pipeline actually changes source");var inactive43=XmlPlanImport.Import(altered43,g);Check(inactive43.RemovedApps.Count==0&&SameXml(altered43,Xml(inactive43)),"inactive or changed pipeline not claimed as enabled");inactive43.RemovedApps.Add("RemovePhotos");Reject(inactive43,"inactive or changed pipeline cannot be overlaid");
}
viewModel.Plan=Plan();viewModel.AppRemovalChoices.Single(i=>i.Id=="RemovePhotos").IsSelected=true;viewModel.AppRemovalFilter="Store";Check(viewModel.Plan.RemovedApps.SequenceEqual(new[]{"RemovePhotos"})&&!viewModel.FilteredAppRemovalChoices.Any(i=>i.Id=="RemovePhotos"),"filtering hides checked row without dropping selection");viewModel.AppRemovalFilter="";
Console.WriteLine($"Final pre43 total: {count} tests passed.");

p=Plan();p.RemovedApps.Add("RemoveXboxApps");var partialPolicyDoc43=XmlMerge.Read(Xml(p));foreach(XmlElement f43 in partialPolicyDoc43.SelectNodes("//*[local-name()='File']")!.Cast<XmlElement>().ToArray()){if(f43.GetAttribute("path").EndsWith("RemovePackage.ps1"))f43.ParentNode!.RemoveChild(f43);if(f43.GetAttribute("path").EndsWith("DefaultUser.ps1"))f43.InnerText=f43.InnerText.Replace("AppCaptureEnabled","AlteredCapture");}var partialPolicy43=XmlPlanImport.Import(partialPolicyDoc43.OuterXml,g);Check(partialPolicy43.RemovedApps.Count==0&&SameXml(partialPolicyDoc43.OuterXml,Xml(partialPolicy43)),"partial policy-only Xbox script remains untouched");partialPolicy43.RemovedApps.Add("RemoveXboxApps");Reject(partialPolicy43,"partial policy-only Xbox script cannot be overlaid");
Console.WriteLine($"Final pre43 total: {count} tests passed.");

// pre44: remote access and persistent execution policy are opt-in target installation settings.
p=Plan();Check(!p.EnableRemoteDesktop&&!p.AllowPowerShellScripts&&!DecodedXml(Xml(p)).Contains("fDenyTSConnections")&&!DecodedXml(Xml(p)).Contains("Set-ExecutionPolicy"),"pre44 defaults do not enable RDP or change persistent execution policy");
foreach(var arch44 in new[]{"amd64","arm64","x86"})foreach(bool rdp44 in new[]{false,true})foreach(bool scripts44 in new[]{false,true})foreach(bool hidden44 in new[]{false,true}){
 p=Plan();p.Architecture=arch44;p.EnableRemoteDesktop=rdp44;p.AllowPowerShellScripts=scripts44;p.HidePowerShellWindows=hidden44;var xml44=Xml(p);var doc44=XmlMerge.Read(xml44);var states44=AdvancedSystemSettings.Inspect(doc44);
 Check(states44[nameof(InstallPlan.EnableRemoteDesktop)].Enabled==rdp44&&!states44[nameof(InstallPlan.EnableRemoteDesktop)].Ambiguous&&states44[nameof(InstallPlan.AllowPowerShellScripts)].Enabled==scripts44&&!states44[nameof(InstallPlan.AllowPowerShellScripts)].Ambiguous,"complete native commands recognized: "+arch44+rdp44+scripts44+hidden44);
 var setup44=doc44.SelectNodes("//*[local-name()='File']")!.Cast<XmlElement>().SingleOrDefault(f=>f.GetAttribute("path").EndsWith("Specialize.ps1"))?.InnerText??"";
 Check(setup44.Contains("fDenyTSConnections")==rdp44&&setup44.Contains("@FirewallAPI.dll,-28752")==rdp44&&setup44.Contains("Set-ExecutionPolicy -Scope 'LocalMachine' -ExecutionPolicy 'RemoteSigned' -Force;")==scripts44,"both opt-ins use exact original specialize commands: "+arch44+rdp44+scripts44+hidden44);
 Check(!DecodedXml(xml44).Contains("Enable-PSRemoting")&&!DecodedXml(xml44).Contains("UserAuthentication")&&!DecodedXml(xml44).Contains("netsh.exe interface portproxy"),"new options do not add WinRM, NLA override or port forwarding");
 foreach(bool meta44 in new[]{false,true}){var input44=meta44?xml44:WithoutSnapshot(xml44);var imported44=XmlPlanImport.Import(input44,g);Check(imported44.EnableRemoteDesktop==rdp44&&imported44.AllowPowerShellScripts==scripts44&&SameXml(input44,Xml(imported44)),"import restores actual values with and without app metadata: "+arch44+rdp44+scripts44+hidden44+meta44);imported44.EnableRemoteDesktop=!rdp44;imported44.AllowPowerShellScripts=!scripts44;var changed44=XmlPlanImport.Import(Xml(imported44),g);Check(changed44.EnableRemoteDesktop==!rdp44&&changed44.AllowPowerShellScripts==!scripts44,"imported native settings can independently add and cancel: "+arch44+rdp44+scripts44+hidden44+meta44);}
 foreach(bool secrets44 in new[]{false,true}){var copy44=PlanStore.Deserialize(PlanStore.Serialize(p,secrets44));Check(copy44.EnableRemoteDesktop==rdp44&&copy44.AllowPowerShellScripts==scripts44,"both opt-ins serialize independently from credentials");}
}
foreach(var mode44 in new[]{"安装时创建本地账户","安装时添加微软账户"}){p=Plan();p.AccountMode=mode44;p.BypassNetwork=false;p.EnableRemoteDesktop=true;p.AllowPowerShellScripts=true;Check(AdvancedSystemSettings.Inspect(XmlMerge.Read(Xml(p))).Values.All(s=>s.Enabled&&!s.Ambiguous),"advanced settings independent from automatic local accounts: "+mode44);}
p=Plan();p.EnableRemoteDesktop=true;p.AllowPowerShellScripts=true;p.EditionId="home";Check(DecodedXml(Xml(p)).Contains("fDenyTSConnections")&&AdvancedSystemSettings.RdpDescription.Contains("家庭版"),"Home compatibility limitation is stated without inventing an edition bypass");
p=Plan();p.EnableRemoteDesktop=true;p.AllowPowerShellScripts=true;p.AutoLogon=false;p.KeepSensitiveFiles=true;Check(AdvancedSystemSettings.Inspect(XmlMerge.Read(Xml(p))).Values.All(s=>s.Enabled),"advanced settings do not require initial automatic logon");
p=Plan();p.EnableRemoteDesktop=true;p.AllowPowerShellScripts=true;var originalAdvanced44=Xml(p);
var changedCommand44=originalAdvanced44.Replace("/v fDenyTSConnections /t REG_DWORD /d 0","/v fDenyTSConnections /t REG_DWORD /d 1");var unsupportedRdp44=XmlPlanImport.Import(changedCommand44,g);Check(!unsupportedRdp44.EnableRemoteDesktop&&unsupportedRdp44.AllowPowerShellScripts&&SameXml(changedCommand44,Xml(unsupportedRdp44)),"altered RDP command group preserved while other setting restores");unsupportedRdp44.EnableRemoteDesktop=true;Reject(unsupportedRdp44,"partial altered RDP cannot be overlaid");
var changedPolicy44=originalAdvanced44.Replace("ExecutionPolicy 'RemoteSigned'","ExecutionPolicy 'AllSigned'");var unsupportedPolicy44=XmlPlanImport.Import(changedPolicy44,g);Check(unsupportedPolicy44.EnableRemoteDesktop&&!unsupportedPolicy44.AllowPowerShellScripts&&SameXml(changedPolicy44,Xml(unsupportedPolicy44)),"altered execution policy preserved without claiming RemoteSigned");unsupportedPolicy44.AllowPowerShellScripts=true;Reject(unsupportedPolicy44,"altered execution policy cannot be overlaid");
var noCallDoc44=XmlMerge.Read(originalAdvanced44);foreach(XmlElement path44 in noCallDoc44.SelectNodes("//*[local-name()='Path']")!)if(path44.InnerText.Contains("-File")&&path44.InnerText.Contains("Specialize.ps1"))path44.InnerText=path44.InnerText.Replace("Specialize.ps1","Uncalled.ps1");
foreach(var inactive44 in new[]{originalAdvanced44.Replace("foreach( $script in $scripts ) {","foreach( $script in @() ) {"),noCallDoc44.OuterXml,originalAdvanced44.Replace("Set-ExecutionPolicy -Scope 'LocalMachine' -ExecutionPolicy 'RemoteSigned' -Force;","if ($false) { Set-ExecutionPolicy -Scope 'LocalMachine' -ExecutionPolicy 'RemoteSigned' -Force; }")}){
 var q44=XmlPlanImport.Import(inactive44,g);Check(!q44.AllowPowerShellScripts&&SameXml(inactive44,Xml(q44)),"inactive/conditional policy invocation not represented as enabled");q44.AllowPowerShellScripts=true;Reject(q44,"inactive/conditional policy cannot be replaced silently");
}
var extrasDoc44=XmlMerge.Read(originalAdvanced44);var extensions44=extrasDoc44.SelectSingleNode("//*[local-name()='Extensions']")!;extensions44.AppendChild(extrasDoc44.CreateComment("unrelated pre44 import data"));var foreign44=extrasDoc44.CreateElement("File",extensions44.NamespaceURI);foreign44.SetAttribute("path",@"C:\Windows\Setup\Scripts\Private44.ps1");foreign44.InnerText="Write-Output 'Keep custom payload';";extensions44.AppendChild(foreign44);var extraPlan44=XmlPlanImport.Import(extrasDoc44.OuterXml,g);extraPlan44.EnableRemoteDesktop=false;extraPlan44.AllowPowerShellScripts=false;var cleaned44=Xml(extraPlan44);Check(cleaned44.Contains("unrelated pre44 import data")&&cleaned44.Contains("Keep custom payload")&&!DecodedXml(cleaned44).Contains("fDenyTSConnections")&&!DecodedXml(cleaned44).Contains("Set-ExecutionPolicy"),"canceling native settings preserves unknown source extensions");
var oldAdvanced44=XmlPlanImport.Import(originalAdvanced44,g);oldAdvanced44.EnableRemoteDesktop=false;oldAdvanced44.AllowPowerShellScripts=false;var oldStandalone44=PlanStore.Deserialize(PlanStore.Serialize(oldAdvanced44,true));oldStandalone44.ImportedXml=null;oldAdvanced44.ImportedXml!.BaselineXml=Encoding.UTF8.GetString(g.GenerateStandalone(oldStandalone44));oldAdvanced44.ComputerName="PENDING44";var oldJson44=System.Text.Json.Nodes.JsonNode.Parse(PlanStore.Serialize(oldAdvanced44,true))!.AsObject();foreach(string field44 in new[]{nameof(InstallPlan.EnableRemoteDesktop),nameof(InstallPlan.AllowPowerShellScripts)})oldJson44.Remove(field44);var migrated44=PlanStore.Deserialize(oldJson44.ToJsonString());Check(migrated44.EnableRemoteDesktop&&migrated44.AllowPowerShellScripts&&migrated44.ComputerName=="PENDING44"&&migrated44.ImportedXml!.Blockers.Length==0,"old imported JSON migrates both actual settings and retains pending changes");Check(Node(Xml(migrated44),"//u:ComputerName")=="PENDING44"&&XmlPlanImport.Import(Xml(migrated44),g).EnableRemoteDesktop,"migrated baseline remains exportable");migrated44.EnableRemoteDesktop=false;migrated44.AllowPowerShellScripts=false;Check(!PlanStore.Deserialize(PlanStore.Serialize(migrated44,true)).EnableRemoteDesktop&&!PlanStore.Deserialize(PlanStore.Serialize(migrated44,true)).AllowPowerShellScripts,"explicit current-format false choices are preserved");
p=Plan();Recommendations.Apply(p,"wired");p.EnableRemoteDesktop=true;p.AllowPowerShellScripts=true;Check(Recommendations.Differences(p).Any(d=>d.Property==nameof(InstallPlan.EnableRemoteDesktop))&&Recommendations.Differences(p).Any(d=>d.Property==nameof(InstallPlan.AllowPowerShellScripts)),"both advanced opt-ins appear as recommendation differences");Recommendations.SelectNetworkScene(p,"wifi");Check(p.EnableRemoteDesktop&&p.AllowPowerShellScripts,"network scene retains advanced opt-ins");Recommendations.Restore(p,nameof(InstallPlan.EnableRemoteDesktop));Check(!p.EnableRemoteDesktop&&p.AllowPowerShellScripts,"single restore is independent");Recommendations.Apply(p,"wired");Check(!p.EnableRemoteDesktop&&!p.AllowPowerShellScripts,"full recommendation restore clears both opt-ins");
viewModel.Plan=Plan();viewModel.Preview="stale44";viewModel.Plan.EnableRemoteDesktop=true;viewModel.Plan.AllowPowerShellScripts=true;Check(viewModel.Preview!="stale44"&&viewModel.ConfigurationSummary.Contains("远程桌面：启用")&&viewModel.ConfigurationSummary.Contains("RemoteSigned")&&viewModel.HelpTips["remotedesktop"].Body.StartsWith("当前：启用")&&viewModel.HelpTips["pspolicy"].Body.Contains("更高优先级"),"advanced checkbox changes refresh summary, help and preview");
Check(UnattendLocal.Desktop.SummaryPresentation.FromText(viewModel.ConfigurationSummary).Sections.Any(s=>s.Rows.Any(row=>row.SourceLabel=="远程桌面")&&s.Rows.Any(row=>row.SourceLabel=="PowerShell 执行策略")),"advanced settings grouped in existing summary");
var fixtureName44=typeof(Program).Assembly.GetManifestResourceNames().Single(n=>n.EndsWith("upstream-advanced-system.xml"));using(var stream44=typeof(Program).Assembly.GetManifestResourceStream(fixtureName44)!){using var reader44=new StreamReader(stream44);var native44=reader44.ReadToEnd();var actualPlan44=XmlPlanImport.Import(native44,g);Check(actualPlan44.EnableRemoteDesktop&&actualPlan44.AllowPowerShellScripts&&SameXml(native44,Xml(actualPlan44)),"credential-free official advanced fixture recognizes native payloads and roundtrips");actualPlan44.EnableRemoteDesktop=false;actualPlan44.AllowPowerShellScripts=false;var cancel44=Xml(actualPlan44);Check(!DecodedXml(cancel44).Contains("fDenyTSConnections")&&!DecodedXml(cancel44).Contains("Set-ExecutionPolicy"),"official advanced fixture can cancel both settings");Check(XmlMerge.Read(native44).SelectNodes("//*[local-name()='Password' or local-name()='LocalAccount']")!.Count==0,"official advanced fixture contains no account/password nodes");}
Console.WriteLine($"Final pre44 total: {count} tests passed.");

var duplicatedCallDoc44=XmlMerge.Read(originalAdvanced44);var duplicateNode44=duplicatedCallDoc44.SelectNodes("//*[local-name()='RunSynchronousCommand']")!.Cast<XmlElement>().Single(node=>node.SelectNodes("*[local-name()='Path']")!.Cast<XmlElement>().Any(path=>path.InnerText.Contains("-File")&&path.InnerText.Contains("Specialize.ps1")));var callCopy44=(XmlElement)duplicateNode44.CloneNode(true);callCopy44.SelectSingleNode("*[local-name()='Order']")!.InnerText="999";duplicateNode44.ParentNode!.AppendChild(callCopy44);var duplicatedCallPlan44=XmlPlanImport.Import(duplicatedCallDoc44.OuterXml,g);Check(!duplicatedCallPlan44.EnableRemoteDesktop&&!duplicatedCallPlan44.AllowPowerShellScripts&&SameXml(duplicatedCallDoc44.OuterXml,Xml(duplicatedCallPlan44)),"duplicate specialize invocation retained without representing a simple opt-in");duplicatedCallPlan44.EnableRemoteDesktop=true;Reject(duplicatedCallPlan44,"duplicate specialize invocation blocks RDP override");
// V0.2: native maintenance settings and conservative XML editing.
p=Plan();Check(!p.PreventAutomaticReboot&&!p.DisableLastAccess,"V0.2 defaults retain Windows maintenance behavior");
Check(!DecodedXml(Xml(p)).Contains("MoveActiveHours")&&!DecodedXml(Xml(p)).Contains("disableLastAccess"),"V0.2 defaults do not emit maintenance payloads");
foreach(var arch02 in new[]{"amd64","arm64","x86"})
foreach(bool restart02 in new[]{false,true})
foreach(bool access02 in new[]{false,true})
foreach(bool hidden02 in new[]{false,true}){
 p=Plan();p.Architecture=arch02;p.PreventAutomaticReboot=restart02;p.DisableLastAccess=access02;p.HidePowerShellWindows=hidden02;
 var xml02=Xml(p);var doc02=XmlMerge.Read(xml02);var state02=SystemMaintenanceSettings.Inspect(doc02);
 Check(state02[nameof(InstallPlan.PreventAutomaticReboot)].Enabled==restart02&&state02[nameof(InstallPlan.DisableLastAccess)].Enabled==access02&&state02.Values.All(s=>!s.Ambiguous),"maintenance payloads recognized across architecture/options/window styles "+arch02+restart02+access02+hidden02);
 var plain02=DecodedXml(xml02);
 Check(plain02.Contains("NoAutoRebootWithLoggedOnUsers")==restart02&&plain02.Contains("disableLastAccess 1")==access02,"maintenance choices independently reach generated XML");
 Check(doc02.SelectNodes("//*[local-name()='File']")!.Cast<XmlElement>().Count(f=>f.GetAttribute("path").EndsWith("MoveActiveHours.xml",StringComparison.Ordinal))==(restart02?1:0),"restart choice controls native task embedding");
 foreach(bool meta02 in new[]{false,true}){
  var input02=meta02?xml02:WithoutSnapshot(xml02);var imported02=XmlPlanImport.Import(input02,g);
  Check(imported02.PreventAutomaticReboot==restart02&&imported02.DisableLastAccess==access02&&SameXml(input02,Xml(imported02)),"maintenance XML imports actual choices and roundtrips with/without metadata");
  imported02.PreventAutomaticReboot=!restart02;imported02.DisableLastAccess=!access02;
  var editedXml02=Xml(imported02);var edited02=XmlPlanImport.Import(editedXml02,g);
  Check(edited02.PreventAutomaticReboot==!restart02&&edited02.DisableLastAccess==!access02,"maintenance XML choices can be added/canceled independently");
 }
 foreach(bool secrets02 in new[]{false,true}){
  var saved02=PlanStore.Deserialize(PlanStore.Serialize(p,secrets02));
  Check(saved02.PreventAutomaticReboot==restart02&&saved02.DisableLastAccess==access02,"maintenance JSON choices survive either credential save mode");
 }
}
p=Plan();p.PreventAutomaticReboot=true;p.DisableLastAccess=true;p.DisableWindowsUpdate=true;
var original02=Xml(p);
Check(DecodedXml(original02).Contains("PauseWindowsUpdate")&&DecodedXml(original02).Contains("MoveActiveHours"),"pause and restart options remain independent and can coexist");
p.DisableWindowsUpdate=false;original02=Xml(p);
var mutations02=new[]{
 original02.Replace("disableLastAccess 1;","disableLastAccess 0;"),
 original02.Replace("/v AUOptions /t REG_DWORD /d 4","/v AUOptions /t REG_DWORD /d 3"),
 original02.Replace("PT4H","PT8H"),
 original02.Replace("-TaskName 'MoveActiveHours'","-TaskName 'OtherTask'"),
 original02.Replace("foreach( $script in $scripts ) {","foreach( $script in @() ) {")
};
foreach(var modified02 in mutations02){
 var parsed02=XmlPlanImport.Import(modified02,g);var states02=SystemMaintenanceSettings.Inspect(XmlMerge.Read(modified02));
 Check(states02.Values.Any(s=>s.Ambiguous)&&SameXml(modified02,Xml(parsed02)),"modified maintenance payload is retained without silent normalization");
 foreach(var item02 in SystemMaintenanceSettings.Items.Where(i=>states02[i.Property].Ambiguous)){
  var attempt02=XmlPlanImport.Import(modified02,g);typeof(InstallPlan).GetProperty(item02.Property)!.SetValue(attempt02,true);
  Reject(attempt02,"ambiguous maintenance payload cannot be overlaid: "+item02.Property);
 }
}
XmlDocument ModifiedDoc02()=>XmlMerge.Read(original02);
var missingTask02=ModifiedDoc02();var task02=missingTask02.SelectNodes("//*[local-name()='File']")!.Cast<XmlElement>().Single(f=>f.GetAttribute("path").EndsWith("MoveActiveHours.xml"));task02.ParentNode!.RemoveChild(task02);
var duplicateTask02=ModifiedDoc02();task02=duplicateTask02.SelectNodes("//*[local-name()='File']")!.Cast<XmlElement>().Single(f=>f.GetAttribute("path").EndsWith("MoveActiveHours.xml"));task02.ParentNode!.AppendChild(task02.CloneNode(true));
var disabledExtraction02=ModifiedDoc02();disabledExtraction02.SelectSingleNode("//*[local-name()='ExtractScript']")!.InnerText="Write-Output 'do not extract';";
var stoppedCall02=ModifiedDoc02();foreach(XmlElement path02 in stoppedCall02.SelectNodes("//*[local-name()='Path']")!)if(path02.InnerText.Contains("-File")&&path02.InnerText.Contains("Specialize.ps1"))path02.InnerText=path02.InnerText.Replace("Specialize.ps1","Uncalled.ps1");
var duplicateCall02=ModifiedDoc02();var call02=duplicateCall02.SelectNodes("//*[local-name()='RunSynchronousCommand']")!.Cast<XmlElement>().Single(node=>node.SelectNodes("*[local-name()='Path']")!.Cast<XmlElement>().Any(path=>path.InnerText.Contains("-File")&&path.InnerText.Contains("Specialize.ps1")));var copyCall02=call02.CloneNode(true);copyCall02.SelectSingleNode("*[local-name()='Order']")!.InnerText="999";call02.ParentNode!.AppendChild(copyCall02);
var foreignCommandDoc02=ModifiedDoc02();var foreignCommand02=foreignCommandDoc02.CreateElement("RunSynchronousCommand",XmlMerge.Namespace);var foreignOrder02=foreignCommandDoc02.CreateElement("Order",XmlMerge.Namespace);foreignOrder02.InnerText="998";foreignCommand02.AppendChild(foreignOrder02);var foreignPath02=foreignCommandDoc02.CreateElement("Path",XmlMerge.Namespace);foreignPath02.InnerText="reg.exe add HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows\\WindowsUpdate\\AU /v NoAutoRebootWithLoggedOnUsers /t REG_DWORD /d 0 /f";foreignCommand02.AppendChild(foreignPath02);foreignCommandDoc02.SelectSingleNode("//*[local-name()='settings' and @pass='specialize']/*[local-name()='component']/*[local-name()='RunSynchronous']")!.AppendChild(foreignCommand02);
foreach(var modifiedDoc02 in new[]{missingTask02,duplicateTask02,disabledExtraction02,stoppedCall02,duplicateCall02,foreignCommandDoc02}){
 var loaded02=XmlPlanImport.Import(modifiedDoc02.OuterXml,g);
 Check(!loaded02.PreventAutomaticReboot&&SameXml(modifiedDoc02.OuterXml,Xml(loaded02)),"missing/duplicate/uncalled maintenance task retained with opt-in disabled");
 loaded02.PreventAutomaticReboot=true;Reject(loaded02,"missing/duplicate/uncalled maintenance pipeline blocks overlay");
}
var extraDoc02=ModifiedDoc02();var ext02=extraDoc02.SelectSingleNode("//*[local-name()='Extensions']")!;ext02.AppendChild(extraDoc02.CreateComment("keep foreign v02 content"));var foreignFile02=extraDoc02.CreateElement("File",ext02.NamespaceURI);foreignFile02.SetAttribute("path",@"C:\Windows\Setup\Scripts\Private02.ps1");foreignFile02.InnerText="Write-Output 'private v02 payload';";ext02.AppendChild(foreignFile02);
var cancel02=XmlPlanImport.Import(extraDoc02.OuterXml,g);cancel02.PreventAutomaticReboot=false;cancel02.DisableLastAccess=false;var canceled02=Xml(cancel02);
Check(canceled02.Contains("keep foreign v02 content")&&canceled02.Contains("private v02 payload")&&!DecodedXml(canceled02).Contains("MoveActiveHours")&&!DecodedXml(canceled02).Contains("disableLastAccess"),"canceling native maintenance settings preserves unknown files/comments");
var old02=XmlPlanImport.Import(original02,g);old02.PreventAutomaticReboot=false;old02.DisableLastAccess=false;var baseline02=PlanStore.Deserialize(PlanStore.Serialize(old02,true));baseline02.ImportedXml=null;old02.ImportedXml!.BaselineXml=Encoding.UTF8.GetString(g.GenerateStandalone(baseline02));old02.ComputerName="PENDING02";
var json02=System.Text.Json.Nodes.JsonNode.Parse(PlanStore.Serialize(old02,true))!.AsObject();json02.Remove(nameof(InstallPlan.PreventAutomaticReboot));json02.Remove(nameof(InstallPlan.DisableLastAccess));
var migrated02=PlanStore.Deserialize(json02.ToJsonString());
Check(migrated02.PreventAutomaticReboot&&migrated02.DisableLastAccess&&migrated02.ComputerName=="PENDING02"&&migrated02.ImportedXml!.Blockers.Length==0,"old imported JSON restores actual maintenance state and pending edits");
Check(Node(Xml(migrated02),"//u:ComputerName")=="PENDING02"&&XmlPlanImport.Import(Xml(migrated02),g).PreventAutomaticReboot,"old maintenance baseline migration remains exportable");
migrated02.PreventAutomaticReboot=false;migrated02.DisableLastAccess=false;var explicitFalse02=PlanStore.Deserialize(PlanStore.Serialize(migrated02,true));Check(!explicitFalse02.PreventAutomaticReboot&&!explicitFalse02.DisableLastAccess&&!DecodedXml(Xml(explicitFalse02)).Contains("MoveActiveHours"),"explicit saved false choices remain editable after migration");
p=Plan();Recommendations.Apply(p,"wired");p.PreventAutomaticReboot=true;p.DisableLastAccess=true;
Check(Recommendations.Differences(p).Any(d=>d.Property==nameof(InstallPlan.PreventAutomaticReboot))&&Recommendations.Differences(p).Any(d=>d.Property==nameof(InstallPlan.DisableLastAccess)),"maintenance options appear in recommendation differences");Recommendations.SelectNetworkScene(p,"wifi");Check(p.PreventAutomaticReboot&&p.DisableLastAccess,"network scene retains maintenance choices");Recommendations.Restore(p,nameof(InstallPlan.PreventAutomaticReboot));Check(!p.PreventAutomaticReboot&&p.DisableLastAccess,"maintenance individual restore retains other choice");Recommendations.Apply(p,"wired");Check(!p.PreventAutomaticReboot&&!p.DisableLastAccess,"maintenance full restore returns to default");
viewModel.Plan=Plan();viewModel.Preview="stale02";viewModel.Plan.PreventAutomaticReboot=true;viewModel.Plan.DisableLastAccess=true;
Check(viewModel.Preview!="stale02"&&viewModel.ConfigurationSummary.Contains("MoveActiveHours")&&viewModel.ConfigurationSummary.Contains("NTFS 最后访问时间：禁用")&&viewModel.HelpTips["updaterestart"].Body.StartsWith("当前：添加")&&viewModel.HelpTips["lastaccess"].Body.StartsWith("当前：禁用"),"maintenance changes refresh help/summary and invalidate preview");
// V0.3 process audit: native payloads, privacy semantics, and conservative editing.
p=Plan();Check(p.ProcessAuditMode==ProcessAuditSettings.Default&&!DecodedXml(Xml(p)).Contains("0CCE922B-69AE-11D9-BED3-505054503030"),"V0.3 audit defaults do not change target policies");
p.ProcessAuditMode="unknown mode";Reject(p,"unknown audit mode cannot export");
foreach(var arch03 in new[]{"amd64","arm64","x86"})
foreach(var mode03 in ProcessAuditSettings.Modes)
foreach(bool hidden03 in new[]{false,true}){
 p=Plan();p.Architecture=arch03;p.ProcessAuditMode=mode03;p.HidePowerShellWindows=hidden03;
 var xml03=Xml(p);var doc03=XmlMerge.Read(xml03);var source03=ProcessAuditSettings.Inspect(doc03);
 Check(source03.Mode==mode03&&!source03.Ambiguous,"audit mode recognized for architecture and script window style "+arch03+mode03+hidden03);
 var text03=DecodedXml(xml03);
 Check(text03.Contains(ProcessAuditSettings.AuditCommand)==(mode03!=ProcessAuditSettings.Default)&&text03.Contains(ProcessAuditSettings.CommandLineCommand)==(mode03==ProcessAuditSettings.WithCommandLine),"audit mode writes only exact intended native commands");
 Check(!text03.Contains("/success:disable")&&!text03.Contains("ProcessCreationIncludeCmdLine_Enabled /t REG_DWORD /d 0"),"default/plain audit modes do not invent explicit policy disabling");
 foreach(bool metadata03 in new[]{false,true}){
  var input03=metadata03?xml03:WithoutSnapshot(xml03);var imported03=XmlPlanImport.Import(input03,g);
  Check(imported03.ProcessAuditMode==mode03&&SameXml(input03,Xml(imported03)),"audit import with and without metadata roundtrips unchanged");
  foreach(var target03 in ProcessAuditSettings.Modes){
   var changed03=XmlPlanImport.Import(input03,g);changed03.ProcessAuditMode=target03;
   var changedXml03=Xml(changed03);var reparsed03=XmlPlanImport.Import(changedXml03,g);
   Check(reparsed03.ProcessAuditMode==target03,"all imported audit transitions are editable: "+mode03+" -> "+target03);
  }
 }
 foreach(bool credentials03 in new[]{false,true}){
  var saved03=PlanStore.Deserialize(PlanStore.Serialize(p,credentials03));
  Check(saved03.ProcessAuditMode==mode03,"audit selection survives either JSON credential save mode");
 }
}
p=Plan();p.ProcessAuditMode=ProcessAuditSettings.WithCommandLine;
var original03=Xml(p);
var alteredAudit03=original03.Replace("/success:enable /failure:enable","/success:disable /failure:disable");
var alteredCommand03=original03.Replace("ProcessCreationIncludeCmdLine_Enabled /t REG_DWORD /d 1","ProcessCreationIncludeCmdLine_Enabled /t REG_DWORD /d 0");
// Edit embedded text through XML to test removed/duplicate/conditional commands without serialization assumptions.
XmlDocument AuditDoc03()=>XmlMerge.Read(original03);
var noAuditDoc03=AuditDoc03();var specialize03=noAuditDoc03.SelectNodes("//*[local-name()='File']")!.Cast<XmlElement>().Single(f=>f.GetAttribute("path").EndsWith("Specialize.ps1"));specialize03.InnerText=specialize03.InnerText.Replace(ProcessAuditSettings.AuditCommand,"Write-Output 'audit removed';");
var duplicateAuditDoc03=AuditDoc03();specialize03=duplicateAuditDoc03.SelectNodes("//*[local-name()='File']")!.Cast<XmlElement>().Single(f=>f.GetAttribute("path").EndsWith("Specialize.ps1"));specialize03.InnerText=specialize03.InnerText.Replace(ProcessAuditSettings.AuditCommand,ProcessAuditSettings.AuditCommand+"\n\t\t"+ProcessAuditSettings.AuditCommand);
var conditionAuditDoc03=AuditDoc03();specialize03=conditionAuditDoc03.SelectNodes("//*[local-name()='File']")!.Cast<XmlElement>().Single(f=>f.GetAttribute("path").EndsWith("Specialize.ps1"));specialize03.InnerText=specialize03.InnerText.Replace(ProcessAuditSettings.AuditCommand,"if ($false) { "+ProcessAuditSettings.AuditCommand+" }");
var noExtraction03=AuditDoc03();noExtraction03.SelectSingleNode("//*[local-name()='ExtractScript']")!.InnerText="Write-Output 'do not extract';";
var noInvocation03=AuditDoc03();foreach(XmlElement path03 in noInvocation03.SelectNodes("//*[local-name()='Path']")!)if(path03.InnerText.Contains("-File")&&path03.InnerText.Contains("Specialize.ps1"))path03.InnerText=path03.InnerText.Replace("Specialize.ps1","Uncalled.ps1");
var extraAudit03=AuditDoc03();var extraFile03=extraAudit03.CreateElement("File",extraAudit03.SelectSingleNode("//*[local-name()='Extensions']")!.NamespaceURI);extraFile03.SetAttribute("path",@"C:\Windows\Setup\Scripts\ForeignAudit.ps1");extraFile03.InnerText=ProcessAuditSettings.AuditCommand;extraAudit03.SelectSingleNode("//*[local-name()='Extensions']")!.AppendChild(extraFile03);
foreach(var altered03 in new[]{alteredAudit03,alteredCommand03,noAuditDoc03.OuterXml,duplicateAuditDoc03.OuterXml,conditionAuditDoc03.OuterXml,noExtraction03.OuterXml,noInvocation03.OuterXml,extraAudit03.OuterXml}){
 var loaded03=XmlPlanImport.Import(altered03,g);
 Check(loaded03.ProcessAuditMode==ProcessAuditSettings.Default&&loaded03.ImportedXml!.Notices.Any(n=>n.Contains("进程创建审计"))&&SameXml(altered03,Xml(loaded03)),"unknown/partial/duplicate/inactive audit is retained with default selection");
 foreach(var desired03 in new[]{ProcessAuditSettings.Enabled,ProcessAuditSettings.WithCommandLine}){loaded03.ProcessAuditMode=desired03;Reject(loaded03,"ambiguous audit payload blocks both opt-in modes");}
}
var keepDoc03=AuditDoc03();var auditExt03=keepDoc03.SelectSingleNode("//*[local-name()='Extensions']")!;auditExt03.AppendChild(keepDoc03.CreateComment("keep foreign audit content"));var keepFile03=keepDoc03.CreateElement("File",auditExt03.NamespaceURI);keepFile03.SetAttribute("path",@"C:\Windows\Setup\Scripts\Other03.ps1");keepFile03.InnerText="Write-Output 'retain other script';";auditExt03.AppendChild(keepFile03);
var keep03=XmlPlanImport.Import(keepDoc03.OuterXml,g);keep03.ProcessAuditMode=ProcessAuditSettings.Default;var kept03=Xml(keep03);
Check(kept03.Contains("keep foreign audit content")&&kept03.Contains("retain other script")&&!DecodedXml(kept03).Contains(ProcessAuditSettings.AuditCommand)&&!DecodedXml(kept03).Contains(ProcessAuditSettings.CommandLineCommand),"canceling known audit removes only its own commands");
p=Plan();p.ProcessAuditMode=ProcessAuditSettings.WithCommandLine;p.PreventAutomaticReboot=true;p.DisableLastAccess=true;p.EnableRemoteDesktop=true;p.AllowPowerShellScripts=true;var together03=Xml(p);
Check(ProcessAuditSettings.Inspect(XmlMerge.Read(together03)).Mode==ProcessAuditSettings.WithCommandLine&&SystemMaintenanceSettings.Inspect(XmlMerge.Read(together03)).Values.All(s=>s.Enabled)&&AdvancedSystemSettings.Inspect(XmlMerge.Read(together03)).Values.All(s=>s.Enabled),"audit coexists with maintenance and remote settings");
var oldAudit03=XmlPlanImport.Import(original03,g);oldAudit03.ProcessAuditMode=ProcessAuditSettings.Default;var oldBaseline03=PlanStore.Deserialize(PlanStore.Serialize(oldAudit03,true));oldBaseline03.ImportedXml=null;oldAudit03.ImportedXml!.BaselineXml=Encoding.UTF8.GetString(g.GenerateStandalone(oldBaseline03));oldAudit03.ComputerName="PENDING03";
var oldJson03=System.Text.Json.Nodes.JsonNode.Parse(PlanStore.Serialize(oldAudit03,true))!.AsObject();oldJson03.Remove(nameof(InstallPlan.ProcessAuditMode));var migrated03=PlanStore.Deserialize(oldJson03.ToJsonString());
Check(migrated03.ProcessAuditMode==ProcessAuditSettings.WithCommandLine&&migrated03.ComputerName=="PENDING03"&&migrated03.ImportedXml!.Blockers.Length==0,"old imported JSON restores actual audit mode while retaining pending changes");
Check(Node(Xml(migrated03),"//u:ComputerName")=="PENDING03"&&XmlPlanImport.Import(Xml(migrated03),g).ProcessAuditMode==ProcessAuditSettings.WithCommandLine,"audit migration baseline remains editable");
migrated03.ProcessAuditMode=ProcessAuditSettings.Default;var explicitAuditDefault03=PlanStore.Deserialize(PlanStore.Serialize(migrated03,true));Check(explicitAuditDefault03.ProcessAuditMode==ProcessAuditSettings.Default&&!DecodedXml(Xml(explicitAuditDefault03)).Contains(ProcessAuditSettings.AuditCommand),"explicit saved default is not overwritten during audit migration");
p=Plan();Recommendations.Apply(p,"wired");p.ProcessAuditMode=ProcessAuditSettings.WithCommandLine;Check(Recommendations.Differences(p).Any(d=>d.Property==nameof(InstallPlan.ProcessAuditMode)),"audit appears in recommendation differences");Recommendations.SelectNetworkScene(p,"wifi");Check(p.ProcessAuditMode==ProcessAuditSettings.WithCommandLine,"network scene retains audit selection");Recommendations.Restore(p,nameof(InstallPlan.ProcessAuditMode));Check(p.ProcessAuditMode==ProcessAuditSettings.Default,"individual recommendation restore clears audit opt-in");
viewModel.Plan=Plan();viewModel.Preview="stale03";viewModel.Plan.ProcessAuditMode=ProcessAuditSettings.WithCommandLine;
Check(viewModel.IsAuditCommandLine&&viewModel.Preview!="stale03"&&viewModel.ConfigurationSummary.Contains("明文记录命令行")&&viewModel.HelpTips["processaudit"].Body.Contains("明文"),"audit changes refresh summary/help/privacy warning and invalidate preview");viewModel.Plan.ProcessAuditMode=ProcessAuditSettings.Enabled;Check(!viewModel.IsAuditCommandLine&&viewModel.ConfigurationSummary.Contains("遵循已有策略"),"plain audit mode hides command-line opt-in warning without claiming it disables existing policy");
foreach(bool nativeLine03 in new[]{false,true}){
 var nativeAudit03=new Schneegans.Unattend.UnattendGenerator().GenerateXml(Schneegans.Unattend.Configuration.Default with{ProcessAuditSettings=new Schneegans.Unattend.EnabledProcessAuditSettings(nativeLine03)}).OuterXml;
 var nativePlan03=XmlPlanImport.Import(nativeAudit03,g);
 Check(nativePlan03.ProcessAuditMode==(nativeLine03?ProcessAuditSettings.WithCommandLine:ProcessAuditSettings.Enabled)&&SameXml(nativeAudit03,Xml(nativePlan03)),"unmodified upstream audit XML recognizes and roundtrips without app metadata");nativePlan03.ProcessAuditMode=ProcessAuditSettings.Default;Check(!DecodedXml(Xml(nativePlan03)).Contains(ProcessAuditSettings.AuditCommand),"native upstream audit can be canceled");
}
// V0.5: conservative UAC/WPBT opt-ins and native compatibility.
p=Plan();Check(!p.DisableUac&&!p.DisableWpbt&&!DecodedXml(Xml(p)).Contains("EnableLUA")&&!DecodedXml(Xml(p)).Contains("DisableWpbtExecution"),"V0.5 defaults retain Windows protection policies");
foreach(var arch04 in new[]{"amd64","arm64","x86"})
foreach(bool uac04 in new[]{false,true})
foreach(bool wpbt04 in new[]{false,true})
foreach(bool hidden04 in new[]{false,true}){
 p=Plan();p.Architecture=arch04;p.DisableUac=uac04;p.DisableWpbt=wpbt04;p.HidePowerShellWindows=hidden04;
 var xml04=Xml(p);var state04=ProtectionSettings.Inspect(XmlMerge.Read(xml04));
 Check(state04[nameof(InstallPlan.DisableUac)].Enabled==uac04&&state04[nameof(InstallPlan.DisableWpbt)].Enabled==wpbt04&&state04.Values.All(s=>!s.Ambiguous),"native protection payloads recognized across options/architecture/window styles");
 Check(DecodedXml(xml04).Contains(ProtectionSettings.Items[0].Command)==uac04&&DecodedXml(xml04).Contains(ProtectionSettings.Items[1].Command)==wpbt04,"both protection options independently emit exact original commands");
 foreach(bool metadata04 in new[]{false,true}){
  var input04=metadata04?xml04:WithoutSnapshot(xml04);var imported04=XmlPlanImport.Import(input04,g);
  Check(imported04.DisableUac==uac04&&imported04.DisableWpbt==wpbt04&&SameXml(input04,Xml(imported04)),"protection choices restored from XML with/without app metadata");
  imported04.DisableUac=!uac04;imported04.DisableWpbt=!wpbt04;var changed04=XmlPlanImport.Import(Xml(imported04),g);
  Check(changed04.DisableUac==!uac04&&changed04.DisableWpbt==!wpbt04,"imported protection choices can be independently added/canceled");
 }
 foreach(bool credentials04 in new[]{false,true}){var loaded04=PlanStore.Deserialize(PlanStore.Serialize(p,credentials04));Check(loaded04.DisableUac==uac04&&loaded04.DisableWpbt==wpbt04,"protection choices survive both JSON credential modes");}
}
p=Plan();p.DisableUac=true;p.DisableWpbt=true;var original04=Xml(p);
foreach(var item04 in ProtectionSettings.Items){
 var altered04=original04.Replace(item04.Command.Replace("\"","&quot;"),item04.Command.Replace("/d 0","/d 1").Replace("/d 1","/d 2").Replace("\"","&quot;"));
 var alterationDoc04=XmlMerge.Read(original04);var stage04=alterationDoc04.SelectNodes("//*[local-name()='File']")!.Cast<XmlElement>().Single(f=>f.GetAttribute("path").EndsWith("Specialize.ps1"));stage04.InnerText=stage04.InnerText.Replace(item04.Command,item04.Command.Replace("/d 0","/d 2").Replace("/d 1","/d 2"));altered04=alterationDoc04.OuterXml;
 var duplicate04=XmlMerge.Read(original04);stage04=duplicate04.SelectNodes("//*[local-name()='File']")!.Cast<XmlElement>().Single(f=>f.GetAttribute("path").EndsWith("Specialize.ps1"));stage04.InnerText=stage04.InnerText.Replace(item04.Command,item04.Command+"\n\t\t"+item04.Command);
 var conditional04=XmlMerge.Read(original04);stage04=conditional04.SelectNodes("//*[local-name()='File']")!.Cast<XmlElement>().Single(f=>f.GetAttribute("path").EndsWith("Specialize.ps1"));stage04.InnerText=stage04.InnerText.Replace(item04.Command,"if ($false) { "+item04.Command+" }");
 foreach(var input04 in new[]{altered04,duplicate04.OuterXml,conditional04.OuterXml}){
  var parsed04=XmlPlanImport.Import(input04,g);
  Check(!(bool)typeof(InstallPlan).GetProperty(item04.Property)!.GetValue(parsed04)!&&SameXml(input04,Xml(parsed04)),"altered/duplicate/conditional protection command retained without claiming an opt-in");
  typeof(InstallPlan).GetProperty(item04.Property)!.SetValue(parsed04,true);Reject(parsed04,"unknown protection payload cannot be overlaid");
 }
}
var uncalled04=XmlMerge.Read(original04);foreach(XmlElement path04 in uncalled04.SelectNodes("//*[local-name()='Path']")!)if(path04.InnerText.Contains("-File")&&path04.InnerText.Contains("Specialize.ps1"))path04.InnerText=path04.InnerText.Replace("Specialize.ps1","Uncalled.ps1");
var noExtract04=XmlMerge.Read(original04);noExtract04.SelectSingleNode("//*[local-name()='ExtractScript']")!.InnerText="Write-Output 'no extraction';";
foreach(var inactive04 in new[]{uncalled04.OuterXml,noExtract04.OuterXml}){
 var loaded04=XmlPlanImport.Import(inactive04,g);Check(!loaded04.DisableUac&&!loaded04.DisableWpbt&&SameXml(inactive04,Xml(loaded04)),"inactive protection pipeline preserved");loaded04.DisableUac=true;Reject(loaded04,"inactive pipeline blocks UAC overlay");loaded04.DisableUac=false;loaded04.DisableWpbt=true;Reject(loaded04,"inactive pipeline blocks WPBT overlay");
}
var foreign04=XmlMerge.Read(original04);var ext04=foreign04.SelectSingleNode("//*[local-name()='Extensions']")!;ext04.AppendChild(foreign04.CreateComment("preserve foreign protection content"));var f04=foreign04.CreateElement("File",ext04.NamespaceURI);f04.SetAttribute("path",@"C:\Windows\Setup\Scripts\Private04.ps1");f04.InnerText="Write-Output 'keep private script';";ext04.AppendChild(f04);
var cancel04=XmlPlanImport.Import(foreign04.OuterXml,g);cancel04.DisableUac=false;cancel04.DisableWpbt=false;var canceled04=Xml(cancel04);Check(canceled04.Contains("preserve foreign protection content")&&canceled04.Contains("keep private script")&&!DecodedXml(canceled04).Contains("EnableLUA")&&!DecodedXml(canceled04).Contains("DisableWpbtExecution"),"canceling native protection options keeps unknown XML content");
var old04=XmlPlanImport.Import(original04,g);old04.DisableUac=false;old04.DisableWpbt=false;var oldBaseline04=PlanStore.Deserialize(PlanStore.Serialize(old04,true));oldBaseline04.ImportedXml=null;old04.ImportedXml!.BaselineXml=Encoding.UTF8.GetString(g.GenerateStandalone(oldBaseline04));old04.ComputerName="PENDING04";old04.ImportedXml.Notices=old04.ImportedXml.Notices.Append("界面尚未实现或无法完整还原的生成参数：DisableUac；原 XML 保留。").ToArray();
var oldJson04=System.Text.Json.Nodes.JsonNode.Parse(PlanStore.Serialize(old04,true))!.AsObject();oldJson04.Remove(nameof(InstallPlan.DisableUac));oldJson04.Remove(nameof(InstallPlan.DisableWpbt));var migrated04=PlanStore.Deserialize(oldJson04.ToJsonString());
Check(migrated04.DisableUac&&migrated04.DisableWpbt&&migrated04.ComputerName=="PENDING04"&&migrated04.ImportedXml!.Blockers.Length==0&&!migrated04.ImportedXml.Notices.Any(n=>n.Contains("生成参数：DisableUac；")),"old imported JSON migrates protection state and removes only stale supported-parameter notice");
Check(Node(Xml(migrated04),"//u:ComputerName")=="PENDING04"&&XmlPlanImport.Import(Xml(migrated04),g).DisableUac,"protection migration retains pending edits and valid baseline");migrated04.DisableUac=false;migrated04.DisableWpbt=false;var explicitFalse04=PlanStore.Deserialize(PlanStore.Serialize(migrated04,true));Check(!explicitFalse04.DisableUac&&!explicitFalse04.DisableWpbt&&!DecodedXml(Xml(explicitFalse04)).Contains("EnableLUA"),"explicit saved false protection choices are preserved");
p=Plan();Recommendations.Apply(p,"wired");p.DisableUac=true;p.DisableWpbt=true;Check(Recommendations.Differences(p).Any(d=>d.Property==nameof(InstallPlan.DisableUac))&&Recommendations.Differences(p).Any(d=>d.Property==nameof(InstallPlan.DisableWpbt)),"protection settings appear in recommendation differences");Recommendations.SelectNetworkScene(p,"wifi");Check(p.DisableUac&&p.DisableWpbt,"network scene does not alter protection choices");Recommendations.Restore(p,nameof(InstallPlan.DisableUac));Check(!p.DisableUac&&p.DisableWpbt,"single UAC restore retains WPBT choice");Recommendations.Apply(p,"wired");Check(!p.DisableUac&&!p.DisableWpbt,"full recommendation returns both protection opt-ins to defaults");
viewModel.Plan=Plan();viewModel.Preview="stale04";viewModel.Plan.DisableUac=true;viewModel.Plan.DisableWpbt=true;Check(viewModel.Preview!="stale04"&&viewModel.ConfigurationSummary.Contains("UAC）：关闭")&&viewModel.ConfigurationSummary.Contains("WPBT 固件程序：阻止")&&viewModel.HelpTips["uac"].Body.StartsWith("当前：关闭")&&viewModel.HelpTips["wpbt"].Body.StartsWith("当前：阻止"),"protection opt-ins refresh summary/help and invalidate preview");
foreach(bool uac04 in new[]{false,true})foreach(bool wpbt04 in new[]{false,true}){
 var native04=new Schneegans.Unattend.UnattendGenerator().GenerateXml(Schneegans.Unattend.Configuration.Default with{DisableUac=uac04,DisableWpbt=wpbt04}).OuterXml;var loaded04=XmlPlanImport.Import(native04,g);Check(loaded04.DisableUac==uac04&&loaded04.DisableWpbt==wpbt04&&SameXml(native04,Xml(loaded04)),"native upstream protection XML recognizes and roundtrips without app metadata");
}
// V0.6: four native VM guest tool installers. Tests do not execute installers.
p=Plan();Check(VmGuestToolsSettings.Items.All(i=>!VmGuestToolsSettings.Selected(p,i)),"V0.6 all four VM tools default off");
Check(VmGuestToolsSettings.Items.All(i=>!DecodedXml(Xml(p)).Contains(i.Resource)),"V0.6 default XML does not include guest installers");
foreach(var arch06 in new[]{"amd64","arm64","x86"})
foreach(int mask06 in Enumerable.Range(0,16)){
 p=Plan();p.Architecture=arch06;p.HidePowerShellWindows=(mask06%2)==0;
 for(int bit06=0;bit06<4;bit06++)typeof(InstallPlan).GetProperty(VmGuestToolsSettings.Items[bit06].Property)!.SetValue(p,(mask06&(1<<bit06))!=0);
 var xml06=Xml(p);var doc06=XmlMerge.Read(xml06);var states06=VmGuestToolsSettings.Inspect(doc06);
 foreach(var item06 in VmGuestToolsSettings.Items){
  bool selected06=VmGuestToolsSettings.Selected(p,item06);
  Check(states06[item06.Property].Enabled==selected06&&!states06[item06.Property].Ambiguous,"native guest tool option recognized: "+arch06+mask06+item06.Property);
  var resources06=doc06.SelectNodes("//*[local-name()='File']")!.Cast<XmlElement>().Where(f=>f.GetAttribute("path").EndsWith(item06.Resource,StringComparison.Ordinal)).ToArray();
  Check(resources06.Length==(selected06?1:0),"tool selection independently embeds native helper");
 }
 foreach(bool metadata06 in new[]{false,true}){
  var input06=metadata06?xml06:WithoutSnapshot(xml06);var loaded06=XmlPlanImport.Import(input06,g);
  Check(VmGuestToolsSettings.Items.All(i=>VmGuestToolsSettings.Selected(loaded06,i)==VmGuestToolsSettings.Selected(p,i))&&SameXml(input06,Xml(loaded06)),"VM tool XML with/without app metadata roundtrips and restores all flags");
  foreach(var item06 in VmGuestToolsSettings.Items)typeof(InstallPlan).GetProperty(item06.Property)!.SetValue(loaded06,!VmGuestToolsSettings.Selected(p,item06));
  var switched06=XmlPlanImport.Import(Xml(loaded06),g);
  Check(VmGuestToolsSettings.Items.All(i=>VmGuestToolsSettings.Selected(switched06,i)!=VmGuestToolsSettings.Selected(p,i)),"all imported tool choices can add/cancel without changing other source content");
 }
 foreach(bool credentials06 in new[]{false,true}){
  var json06=PlanStore.Deserialize(PlanStore.Serialize(p,credentials06));Check(VmGuestToolsSettings.Items.All(i=>VmGuestToolsSettings.Selected(json06,i)==VmGuestToolsSettings.Selected(p,i)),"four tool flags survive both JSON credential modes");
 }
}
p=Plan();foreach(var item06 in VmGuestToolsSettings.Items)typeof(InstallPlan).GetProperty(item06.Property)!.SetValue(p,true);var allTools06=Xml(p);
XmlDocument ToolsDoc06()=>XmlMerge.Read(allTools06);
foreach(var item06 in VmGuestToolsSettings.Items){
 var changedHelper06=ToolsDoc06();var helper06=changedHelper06.SelectNodes("//*[local-name()='File']")!.Cast<XmlElement>().Single(f=>f.GetAttribute("path").EndsWith(item06.Resource));helper06.InnerText+="\nWrite-Output 'custom installer change';";
 var missingHelper06=ToolsDoc06();helper06=missingHelper06.SelectNodes("//*[local-name()='File']")!.Cast<XmlElement>().Single(f=>f.GetAttribute("path").EndsWith(item06.Resource));helper06.ParentNode!.RemoveChild(helper06);
 var duplicateHelper06=ToolsDoc06();helper06=duplicateHelper06.SelectNodes("//*[local-name()='File']")!.Cast<XmlElement>().Single(f=>f.GetAttribute("path").EndsWith(item06.Resource));helper06.ParentNode!.AppendChild(helper06.CloneNode(true));
 var noCall06=ToolsDoc06();var stage06=noCall06.SelectNodes("//*[local-name()='File']")!.Cast<XmlElement>().Single(f=>f.GetAttribute("path").EndsWith("FirstLogon.ps1"));string call06="& 'C:\\Windows\\Setup\\Scripts\\"+item06.Resource+"';";stage06.InnerText=stage06.InnerText.Replace(call06,"Write-Output 'installer not called';");
 var conditional06=ToolsDoc06();stage06=conditional06.SelectNodes("//*[local-name()='File']")!.Cast<XmlElement>().Single(f=>f.GetAttribute("path").EndsWith("FirstLogon.ps1"));stage06.InnerText=stage06.InnerText.Replace(call06,"if ($false) { "+call06+" }");
 var duplicateCall06=ToolsDoc06();stage06=duplicateCall06.SelectNodes("//*[local-name()='File']")!.Cast<XmlElement>().Single(f=>f.GetAttribute("path").EndsWith("FirstLogon.ps1"));stage06.InnerText=stage06.InnerText.Replace(call06,call06+"\n\t\t"+call06);
 var wrongStage06=ToolsDoc06();stage06=wrongStage06.SelectNodes("//*[local-name()='File']")!.Cast<XmlElement>().Single(f=>f.GetAttribute("path").EndsWith("FirstLogon.ps1"));stage06.InnerText=stage06.InnerText.Replace(call06,"Write-Output 'moved installer';");var foreignStage06=wrongStage06.CreateElement("File",stage06.NamespaceURI);foreignStage06.SetAttribute("path",@"C:\Windows\Setup\Scripts\OtherStage06.ps1");foreignStage06.InnerText=call06;stage06.ParentNode!.AppendChild(foreignStage06);
 foreach(var inputDoc06 in new[]{changedHelper06,missingHelper06,duplicateHelper06,noCall06,conditional06,duplicateCall06,wrongStage06}){
  var loaded06=XmlPlanImport.Import(inputDoc06.OuterXml,g);
  Check(!VmGuestToolsSettings.Selected(loaded06,item06)&&SameXml(inputDoc06.OuterXml,Xml(loaded06)),"modified/missing/duplicate/inactive tool payload preserved: "+item06.Property);
  Check(VmGuestToolsSettings.Items.Where(i=>i.Property!=item06.Property).All(i=>VmGuestToolsSettings.Selected(loaded06,i)),"one ambiguous tool does not hide the other valid selections");
  typeof(InstallPlan).GetProperty(item06.Property)!.SetValue(loaded06,true);Reject(loaded06,"ambiguous guest installer cannot be overlaid");
 }
 // Cancel a single valid tool while retaining all other valid installer helpers.
 var cancelOne06=XmlPlanImport.Import(allTools06,g);typeof(InstallPlan).GetProperty(item06.Property)!.SetValue(cancelOne06,false);var canceledOne06=XmlPlanImport.Import(Xml(cancelOne06),g);
 Check(!VmGuestToolsSettings.Selected(canceledOne06,item06)&&VmGuestToolsSettings.Items.Where(i=>i.Property!=item06.Property).All(i=>VmGuestToolsSettings.Selected(canceledOne06,i)),"canceling one tool preserves all others");
}
var noExtractTools06=ToolsDoc06();noExtractTools06.SelectSingleNode("//*[local-name()='ExtractScript']")!.InnerText="Write-Output 'no extraction';";
var uncalledTools06=ToolsDoc06();foreach(XmlElement path06 in uncalledTools06.SelectNodes("//*[local-name()='CommandLine']")!)if(path06.InnerText.Contains("-File")&&path06.InnerText.Contains("FirstLogon.ps1"))path06.InnerText=path06.InnerText.Replace("FirstLogon.ps1","Uncalled.ps1");
var duplicateInvocation06=ToolsDoc06();var invoke06=duplicateInvocation06.SelectNodes("//*[local-name()='SynchronousCommand']")!.Cast<XmlElement>().Single(n=>n.SelectNodes("*[local-name()='CommandLine']")!.Cast<XmlElement>().Any(c=>c.InnerText.Contains("-File")&&c.InnerText.Contains("FirstLogon.ps1")));var invokeCopy06=invoke06.CloneNode(true);invokeCopy06.SelectSingleNode("*[local-name()='Order']")!.InnerText="999";invoke06.ParentNode!.AppendChild(invokeCopy06);
foreach(var inputDoc06 in new[]{noExtractTools06,uncalledTools06,duplicateInvocation06}){
 var loaded06=XmlPlanImport.Import(inputDoc06.OuterXml,g);Check(VmGuestToolsSettings.Items.All(i=>!VmGuestToolsSettings.Selected(loaded06,i))&&SameXml(inputDoc06.OuterXml,Xml(loaded06)),"inactive/duplicate native pipeline retains all guest payloads unrepresented");
 foreach(var item06 in VmGuestToolsSettings.Items){typeof(InstallPlan).GetProperty(item06.Property)!.SetValue(loaded06,true);Reject(loaded06,"inactive pipeline blocks guest tool opt-in");typeof(InstallPlan).GetProperty(item06.Property)!.SetValue(loaded06,false);}
}
var extrasTools06=ToolsDoc06();var toolsExt06=extrasTools06.SelectSingleNode("//*[local-name()='Extensions']")!;toolsExt06.AppendChild(extrasTools06.CreateComment("retain unrelated VM configuration"));var toolsOther06=extrasTools06.CreateElement("File",toolsExt06.NamespaceURI);toolsOther06.SetAttribute("path",@"C:\Windows\Setup\Scripts\PrivateGuest06.ps1");toolsOther06.InnerText="Write-Output 'retain custom guest payload';";toolsExt06.AppendChild(toolsOther06);
var cancelAll06=XmlPlanImport.Import(extrasTools06.OuterXml,g);foreach(var item06 in VmGuestToolsSettings.Items)typeof(InstallPlan).GetProperty(item06.Property)!.SetValue(cancelAll06,false);var canceledAll06=Xml(cancelAll06);Check(canceledAll06.Contains("retain unrelated VM configuration")&&canceledAll06.Contains("retain custom guest payload")&&VmGuestToolsSettings.Items.All(i=>!DecodedXml(canceledAll06).Contains(i.Resource)),"canceling all native tools preserves unknown comments/files");
var legacyTools06=XmlPlanImport.Import(allTools06,g);foreach(var item06 in VmGuestToolsSettings.Items)typeof(InstallPlan).GetProperty(item06.Property)!.SetValue(legacyTools06,false);var legacyBaseline06=PlanStore.Deserialize(PlanStore.Serialize(legacyTools06,true));legacyBaseline06.ImportedXml=null;legacyTools06.ImportedXml!.BaselineXml=Encoding.UTF8.GetString(g.GenerateStandalone(legacyBaseline06));legacyTools06.ComputerName="PENDING06";var legacyJson06=System.Text.Json.Nodes.JsonNode.Parse(PlanStore.Serialize(legacyTools06,true))!.AsObject();foreach(var item06 in VmGuestToolsSettings.Items)legacyJson06.Remove(item06.Property);var migratedTools06=PlanStore.Deserialize(legacyJson06.ToJsonString());
Check(VmGuestToolsSettings.Items.All(i=>VmGuestToolsSettings.Selected(migratedTools06,i))&&migratedTools06.ComputerName=="PENDING06"&&migratedTools06.ImportedXml!.Blockers.Length==0,"old imported JSON migrates actual guest tool selections and retains pending edits");
Check(Node(Xml(migratedTools06),"//u:ComputerName")=="PENDING06"&&VmGuestToolsSettings.Items.All(i=>VmGuestToolsSettings.Selected(XmlPlanImport.Import(Xml(migratedTools06),g),i)),"migrated guest baseline remains exportable");foreach(var item06 in VmGuestToolsSettings.Items)typeof(InstallPlan).GetProperty(item06.Property)!.SetValue(migratedTools06,false);var explicitToolsOff06=PlanStore.Deserialize(PlanStore.Serialize(migratedTools06,true));Check(VmGuestToolsSettings.Items.All(i=>!VmGuestToolsSettings.Selected(explicitToolsOff06,i)),"explicit saved guest tool cancellations are not overwritten");
p=Plan();Recommendations.Apply(p,"wired");foreach(var item06 in VmGuestToolsSettings.Items)typeof(InstallPlan).GetProperty(item06.Property)!.SetValue(p,true);Check(VmGuestToolsSettings.Items.All(i=>Recommendations.Differences(p).Any(d=>d.Property==i.Property)),"all four tools appear in recommendation differences");Recommendations.SelectNetworkScene(p,"wifi");Check(VmGuestToolsSettings.Items.All(i=>VmGuestToolsSettings.Selected(p,i)),"network scene preserves guest choices");Recommendations.Restore(p,nameof(InstallPlan.VMwareTools));Check(!p.VMwareTools&&p.VBoxGuestAdditions&&p.VirtIoGuestTools&&p.ParallelsTools,"individual guest restore is independent");Recommendations.Apply(p,"wired");Check(VmGuestToolsSettings.Items.All(i=>!VmGuestToolsSettings.Selected(p,i)),"full recommendation restore clears guest tool opt-ins");
viewModel.Plan=Plan();viewModel.Preview="stale06";viewModel.Plan.VMwareTools=true;viewModel.Plan.VirtIoGuestTools=true;Check(viewModel.Preview!="stale06"&&viewModel.ConfigurationSummary.Contains("VMware Tools")&&viewModel.ConfigurationSummary.Contains("VirtIO Guest Tools")&&viewModel.HelpTips["vmwaretools"].Body.StartsWith("当前：安装")&&viewModel.HelpTips["virtiotools"].Body.Contains("Windows PE"),"guest changes refresh summary/help and invalidate preview");
foreach(bool hidden06 in new[]{false,true}){
 var native06=new Schneegans.Unattend.UnattendGenerator().GenerateXml(Schneegans.Unattend.Configuration.Default with{VMwareTools=true,VBoxGuestAdditions=true,VirtIoGuestTools=true,ParallelsTools=true,HidePowerShellWindows=hidden06}).OuterXml;var loaded06=XmlPlanImport.Import(native06,g);
 Check(VmGuestToolsSettings.Items.All(i=>VmGuestToolsSettings.Selected(loaded06,i))&&SameXml(native06,Xml(loaded06)),"unmodified upstream guest-tool XML imports without app metadata");
}
foreach(var mode06 in new[]{"安装时创建本地账户","安装时添加微软账户"}){p=Plan();p.AccountMode=mode06;p.BypassNetwork=false;p.VMwareTools=true;p.VBoxGuestAdditions=true;p.VirtIoGuestTools=true;p.ParallelsTools=true;Check(VmGuestToolsSettings.Inspect(XmlMerge.Read(Xml(p))).Values.All(s=>s.Enabled&&!s.Ambiguous),"guest payload generation independent of automatic local account mode");}
p=Plan();p.AutoLogon=false;p.KeepSensitiveFiles=true;p.VMwareTools=true;Check(VmGuestToolsSettings.Inspect(XmlMerge.Read(Xml(p)))[nameof(InstallPlan.VMwareTools)].Enabled,"VM tools do not require automatic initial login; actual first login still required");
var retainedPe06=ToolsDoc06();var retainedPeExt06=retainedPe06.SelectSingleNode("//*[local-name()='Extensions']")!;var peCopy06=retainedPe06.CreateElement("PEScriptCopy",retainedPeExt06.NamespaceURI);peCopy06.InnerText="set VIRTIO_DRIVE=E:\r\ndrvload.exe %VIRTIO_DRIVE%\\viostor\\w11\\amd64\\viostor.inf";retainedPeExt06.AppendChild(peCopy06);
var retainedPePlan06=XmlPlanImport.Import(retainedPe06.OuterXml,g);Check(retainedPePlan06.VirtIoGuestTools&&retainedPePlan06.ImportedXml!.Notices.Any(n=>n.Contains("PE 驱动脚本")),"imported VirtIO PE content is disclosed separately from guest installer");retainedPePlan06.VirtIoGuestTools=false;var peAfterCancel06=Xml(retainedPePlan06);Check(peAfterCancel06.Contains("VIRTIO_DRIVE")&&!DecodedXml(peAfterCancel06).Contains("VirtIoGuestTools.ps1"),"canceling VirtIO guest tools preserves existing PE driver payload");
Console.WriteLine($"Final V0.6 total: {count} tests passed.");




// V0.7: native accessibility, Edge uninstall permission and system restore choices.
p=Plan();Check(AdditionalSetupSettings.Items.All(i=>!AdditionalSetupSettings.Selected(p,i)),"V0.7 defaults are all off");
foreach(var arch07 in new[]{"amd64","arm64","x86"})foreach(int mask07 in Enumerable.Range(0,8)){
 p=Plan();p.Architecture=arch07;p.HidePowerShellWindows=mask07%2==0;
 for(int bit07=0;bit07<3;bit07++)typeof(InstallPlan).GetProperty(AdditionalSetupSettings.Items[bit07].Property)!.SetValue(p,(mask07&(1<<bit07))!=0);
 var xml07=Xml(p);var states07=AdditionalSetupSettings.Inspect(XmlMerge.Read(xml07));
 foreach(var item07 in AdditionalSetupSettings.Items)Check(states07[item07.Property].Enabled==AdditionalSetupSettings.Selected(p,item07)&&!states07[item07.Property].Ambiguous,"V0.7 native choice recognized: "+arch07+mask07+item07.Property);
 foreach(bool metadata07 in new[]{false,true}){
  var input07=metadata07?xml07:WithoutSnapshot(xml07);var loaded07=XmlPlanImport.Import(input07,g);
  Check(AdditionalSetupSettings.Items.All(i=>AdditionalSetupSettings.Selected(loaded07,i)==AdditionalSetupSettings.Selected(p,i))&&SameXml(input07,Xml(loaded07)),"V0.7 XML with/without snapshot restores selections without drift");
  foreach(var item07 in AdditionalSetupSettings.Items)typeof(InstallPlan).GetProperty(item07.Property)!.SetValue(loaded07,!AdditionalSetupSettings.Selected(p,item07));
  var switched07=XmlPlanImport.Import(Xml(loaded07),g);
  Check(AdditionalSetupSettings.Items.All(i=>AdditionalSetupSettings.Selected(switched07,i)!=AdditionalSetupSettings.Selected(p,i)),"V0.7 imported options can be added and removed");
 }
 foreach(bool secrets07 in new[]{false,true}){
  var json07=PlanStore.Deserialize(PlanStore.Serialize(p,secrets07));Check(AdditionalSetupSettings.Items.All(i=>AdditionalSetupSettings.Selected(json07,i)==AdditionalSetupSettings.Selected(p,i)),"V0.7 flags survive JSON save mode");
 }
}
Console.WriteLine($"Final V0.7 total: {count} tests passed.");
p=Plan();p.UseNarrator=p.MakeEdgeUninstallable=p.DisableSystemRestore=true;var allSetup07=Xml(p);
XmlDocument SetupDoc07()=>XmlMerge.Read(allSetup07);
XmlElement SetupFile07(XmlDocument doc,string name)=>doc.SelectNodes("//*[local-name()='File']")!.Cast<XmlElement>().Single(f=>f.GetAttribute("path").EndsWith(name,StringComparison.Ordinal));
foreach(var item07 in AdditionalSetupSettings.Items){
 string stageName07=item07.Property==nameof(InstallPlan.DisableSystemRestore)?"FirstLogon.ps1":"Specialize.ps1";
 var goodDoc07=SetupDoc07();string stageText07=SetupFile07(goodDoc07,stageName07).InnerText;
 string command07=stageText07.Replace("\r\n","\n").Split('\n').Select(l=>l.Trim()).First(l=>item07.Markers.Any(m=>l.Contains(m,StringComparison.OrdinalIgnoreCase)));
 var altered07=SetupDoc07();SetupFile07(altered07,stageName07).InnerText=stageText07.Replace(command07,"if ($false) { "+command07+" }");
 var duplicate07=SetupDoc07();SetupFile07(duplicate07,stageName07).InnerText=stageText07.Replace(command07,command07+"\n\t\t"+command07);
 var moved07=SetupDoc07();SetupFile07(moved07,stageName07).InnerText=stageText07.Replace(command07,"Write-Output 'moved';");var movedFile07=moved07.CreateElement("File",SetupFile07(moved07,stageName07).NamespaceURI);movedFile07.SetAttribute("path",@"C:\Windows\Setup\Scripts\Moved07.ps1");movedFile07.InnerText=command07;SetupFile07(moved07,stageName07).ParentNode!.AppendChild(movedFile07);
 foreach(var inputDoc07 in new[]{altered07,duplicate07,moved07}){
  var loaded07=XmlPlanImport.Import(inputDoc07.OuterXml,g);loaded07.ConfirmImportedXmlRisk=true;
  Check(!AdditionalSetupSettings.Selected(loaded07,item07)&&SameXml(inputDoc07.OuterXml,Xml(loaded07)),"altered/conditional/duplicate/wrong-stage setup option retained as unknown: "+item07.Property);
  Check(AdditionalSetupSettings.Items.Where(i=>i.Property!=item07.Property).All(i=>AdditionalSetupSettings.Selected(loaded07,i)),"other valid setup options remain recognized");
  typeof(InstallPlan).GetProperty(item07.Property)!.SetValue(loaded07,true);Reject(loaded07,"unknown setup option cannot be overwritten");
 }
 var cancelOne07=XmlPlanImport.Import(allSetup07,g);typeof(InstallPlan).GetProperty(item07.Property)!.SetValue(cancelOne07,false);var canceled07=XmlPlanImport.Import(Xml(cancelOne07),g);
 Check(!AdditionalSetupSettings.Selected(canceled07,item07)&&AdditionalSetupSettings.Items.Where(i=>i.Property!=item07.Property).All(i=>AdditionalSetupSettings.Selected(canceled07,i)),"single setup cancellation is independent");
}
// Edge helper content, presence, uniqueness, and stage invocation must all agree.
foreach(var change07 in new[]{"altered","missing","duplicate"}){
 var doc07=SetupDoc07();var helper07=SetupFile07(doc07,"MakeEdgeUninstallable.ps1");
 if(change07=="altered")helper07.InnerText+="\nWrite-Output 'custom edge policy';";
 else if(change07=="missing")helper07.ParentNode!.RemoveChild(helper07);
 else helper07.ParentNode!.AppendChild(helper07.CloneNode(true));
 var loaded07=XmlPlanImport.Import(doc07.OuterXml,g);loaded07.ConfirmImportedXmlRisk=true;Check(!loaded07.MakeEdgeUninstallable&&loaded07.UseNarrator&&loaded07.DisableSystemRestore&&SameXml(doc07.OuterXml,Xml(loaded07)),"unknown Edge helper retained: "+change07);loaded07.MakeEdgeUninstallable=true;Reject(loaded07,"unknown Edge helper blocks overlap");
}
// Narrator spans PE, specialize and user-once: each part and the registration pipeline is required.
foreach(var change07 in new[]{"missingPE","duplicatePE","userOnceConditional","missingRunOnce","missingLoad","duplicateLoad","wrongUserStage"}){
 var doc07=SetupDoc07();
 if(change07 is "missingPE" or "duplicatePE"){
  var command07=doc07.SelectNodes("//*[local-name()='settings'][@pass='windowsPE']//*[local-name()='RunSynchronousCommand']")!.Cast<XmlElement>().Single(n=>n.InnerText.Contains("Narrator.exe"));
  if(change07=="missingPE")command07.ParentNode!.RemoveChild(command07);else{var copy07=command07.CloneNode(true);copy07.SelectSingleNode("*[local-name()='Order']")!.InnerText="999";command07.ParentNode!.AppendChild(copy07);}
 }else if(change07=="userOnceConditional"){
  var stage07=SetupFile07(doc07,"UserOnce.ps1");stage07.InnerText=stage07.InnerText.Replace("& 'C:\\Windows\\System32\\Narrator.exe';","if ($false) { & 'C:\\Windows\\System32\\Narrator.exe'; }");
 }else if(change07=="missingRunOnce"){
  var stage07=SetupFile07(doc07,"DefaultUser.ps1");stage07.InnerText=stage07.InnerText.Replace("/v \"UnattendedSetup\"","/v \"OtherSetup\"");
 }else if(change07 is "missingLoad" or "duplicateLoad"){
  var command07=doc07.SelectNodes("//*[local-name()='settings'][@pass='specialize']//*[local-name()='RunSynchronousCommand']")!.Cast<XmlElement>().Single(n=>n.InnerText.Contains("reg.exe load \"HKU\\DefaultUser\""));
  if(change07=="missingLoad")command07.ParentNode!.RemoveChild(command07);else{var copy07=command07.CloneNode(true);copy07.SelectSingleNode("*[local-name()='Order']")!.InnerText="6";command07.ParentNode!.AppendChild(copy07);}
 }else SetupFile07(doc07,"UserOnce.ps1").SetAttribute("path",@"C:\Windows\Setup\Scripts\OtherOnce.ps1");
 var loaded07=XmlPlanImport.Import(doc07.OuterXml,g);loaded07.ConfirmImportedXmlRisk=true;Check(!loaded07.UseNarrator&&loaded07.MakeEdgeUninstallable&&loaded07.DisableSystemRestore&&SameXml(doc07.OuterXml,Xml(loaded07)),"incomplete narrator group retained: "+change07);loaded07.UseNarrator=true;Reject(loaded07,"incomplete narrator pipeline blocks overwrite");
}
foreach(var change07 in new[]{"extract","firstlogon","specialize","duplicatefirst"}){
 var doc07=SetupDoc07();
 if(change07=="extract")doc07.SelectSingleNode("//*[local-name()='ExtractScript']")!.InnerText="Write-Output 'not extracted';";
 else{
  string name07=change07=="specialize"?"Specialize.ps1":"FirstLogon.ps1";
  var call07=doc07.SelectNodes("//*[local-name()='Path']|//*[local-name()='CommandLine']")!.Cast<XmlElement>().Single(n=>n.InnerText.Contains("-File")&&n.InnerText.Contains(name07));
  if(change07=="duplicatefirst"){var copy07=call07.ParentNode!.CloneNode(true);copy07.SelectSingleNode("*[local-name()='Order']")!.InnerText="999";call07.ParentNode!.ParentNode!.AppendChild(copy07);}else call07.InnerText=call07.InnerText.Replace(name07,"Uncalled07.ps1");
 }
 var loaded07=XmlPlanImport.Import(doc07.OuterXml,g);loaded07.ConfirmImportedXmlRisk=true;
 var blocked07=change07=="extract"?AdditionalSetupSettings.Items:AdditionalSetupSettings.Items.Where(i=>change07=="specialize"?i.Property!=nameof(InstallPlan.DisableSystemRestore):i.Property==nameof(InstallPlan.DisableSystemRestore)).ToArray();
 Check(blocked07.All(i=>!AdditionalSetupSettings.Selected(loaded07,i))&&SameXml(doc07.OuterXml,Xml(loaded07)),"inactive extraction/stage pipeline retained: "+change07);
 foreach(var item07 in blocked07){typeof(InstallPlan).GetProperty(item07.Property)!.SetValue(loaded07,true);Reject(loaded07,"inactive setup pipeline blocks overlap");typeof(InstallPlan).GetProperty(item07.Property)!.SetValue(loaded07,false);}
}
// Old imported plans gain the actual native state and a matching baseline without losing pending edits.
var legacy07=XmlPlanImport.Import(allSetup07,g);foreach(var item07 in AdditionalSetupSettings.Items)typeof(InstallPlan).GetProperty(item07.Property)!.SetValue(legacy07,false);var oldBase07=PlanStore.Deserialize(PlanStore.Serialize(legacy07,true));oldBase07.ImportedXml=null;legacy07.ImportedXml!.BaselineXml=Encoding.UTF8.GetString(g.GenerateStandalone(oldBase07));legacy07.ComputerName="PENDING07";legacy07.ImportedXml.Notices=legacy07.ImportedXml.Notices.Append("界面尚未实现或无法完整还原的生成参数：UseNarrator；原 XML 保留。").ToArray();
var oldJson07=System.Text.Json.Nodes.JsonNode.Parse(PlanStore.Serialize(legacy07,true))!.AsObject();foreach(var item07 in AdditionalSetupSettings.Items)oldJson07.Remove(item07.Property);var migrated07=PlanStore.Deserialize(oldJson07.ToJsonString());
Check(AdditionalSetupSettings.Items.All(i=>AdditionalSetupSettings.Selected(migrated07,i))&&migrated07.ComputerName=="PENDING07"&&migrated07.ImportedXml!.Blockers.Length==0&&!migrated07.ImportedXml.Notices.Any(n=>n.Contains("生成参数：UseNarrator；")),"V0.7 old JSON migration retains pending edits and removes stale notices");
Check(Node(Xml(migrated07),"//u:ComputerName")=="PENDING07"&&AdditionalSetupSettings.Items.All(i=>AdditionalSetupSettings.Selected(XmlPlanImport.Import(Xml(migrated07),g),i)),"V0.7 migrated baseline stays editable");
foreach(var item07 in AdditionalSetupSettings.Items)typeof(InstallPlan).GetProperty(item07.Property)!.SetValue(migrated07,false);var explicit07=PlanStore.Deserialize(PlanStore.Serialize(migrated07,true));Check(AdditionalSetupSettings.Items.All(i=>!AdditionalSetupSettings.Selected(explicit07,i)),"saved explicit false survives V0.7 migration");
p=Plan();Recommendations.Apply(p,"wired");foreach(var item07 in AdditionalSetupSettings.Items)typeof(InstallPlan).GetProperty(item07.Property)!.SetValue(p,true);Check(AdditionalSetupSettings.Items.All(i=>Recommendations.Differences(p).Any(d=>d.Property==i.Property)),"V0.7 recommendation differences include three choices");Recommendations.SelectNetworkScene(p,"wifi");Check(p.UseNarrator&&p.MakeEdgeUninstallable&&p.DisableSystemRestore,"V0.7 network-only change retains choices");Recommendations.Restore(p,nameof(InstallPlan.DisableSystemRestore));Check(!p.DisableSystemRestore&&p.UseNarrator&&p.MakeEdgeUninstallable,"V0.7 individual restore independent");Recommendations.Apply(p,"wired");Check(!p.UseNarrator&&!p.MakeEdgeUninstallable&&!p.DisableSystemRestore,"V0.7 full recommendation resets all opt-ins");
viewModel.Plan=Plan();viewModel.Preview="stale07";viewModel.Plan.UseNarrator=viewModel.Plan.MakeEdgeUninstallable=viewModel.Plan.DisableSystemRestore=true;Check(viewModel.Preview!="stale07"&&viewModel.ConfigurationSummary.Contains("不实际卸载")&&AdditionalSetupSettings.Items.All(i=>viewModel.HelpTips[i.HelpId].Body.StartsWith("当前：")&&!viewModel.HelpTips[i.HelpId].Body.StartsWith("当前：不添加")),"V0.7 settings refresh help/summary and invalidate preview");
foreach(bool hide07 in new[]{false,true}){
 var native07=new Schneegans.Unattend.UnattendGenerator().GenerateXml(Schneegans.Unattend.Configuration.Default with{UseNarrator=true,MakeEdgeUninstallable=true,DisableSystemRestore=true,HidePowerShellWindows=hide07}).OuterXml;
 var parsed07=XmlPlanImport.Import(native07,g);Check(AdditionalSetupSettings.Items.All(i=>AdditionalSetupSettings.Selected(parsed07,i))&&SameXml(native07,Xml(parsed07)),"native V0.7 options import independently of app metadata");
}
foreach(var mode07 in new[]{"安装时创建本地账户","安装时添加微软账户"}){p=Plan();p.AccountMode=mode07;p.BypassNetwork=false;p.UseNarrator=p.MakeEdgeUninstallable=p.DisableSystemRestore=true;Check(AdditionalSetupSettings.Inspect(XmlMerge.Read(Xml(p))).Values.All(s=>s.Enabled&&!s.Ambiguous),"V0.7 options independent of account mode");}
p=Plan();p.AutoLogon=false;p.KeepSensitiveFiles=true;p.UseNarrator=p.MakeEdgeUninstallable=p.DisableSystemRestore=true;Check(AdditionalSetupSettings.Inspect(XmlMerge.Read(Xml(p))).Values.All(s=>s.Enabled&&!s.Ambiguous),"V0.7 does not force automatic logon");
Console.WriteLine($"Final V0.7 total: {count} tests passed.");
Check(!XmlPlanImport.Import(allSetup07,g).ImportedXml!.RequiresDiskReview,"complete native Narrator PE startup alone does not require disk-risk review");
var unknownPe07=SetupDoc07();var peParent07=unknownPe07.SelectNodes("//*[local-name()='settings'][@pass='windowsPE']//*[local-name()='RunSynchronous']")!.Cast<XmlElement>().First();var extraPe07=unknownPe07.CreateElement("RunSynchronousCommand",XmlMerge.Namespace);foreach(var pair07 in new[]{("Order","999"),("Path","cmd.exe /c echo unknown PE command")}){var child07=unknownPe07.CreateElement(pair07.Item1,XmlMerge.Namespace);child07.InnerText=pair07.Item2;extraPe07.AppendChild(child07);}peParent07.AppendChild(extraPe07);
var unknownPePlan07=XmlPlanImport.Import(unknownPe07.OuterXml,g);Check(unknownPePlan07.UseNarrator&&unknownPePlan07.ImportedXml!.RequiresDiskReview,"unknown PE command still needs review alongside recognized Narrator");Reject(unknownPePlan07,"unknown PE commands cannot be exported without review");unknownPePlan07.ConfirmImportedXmlRisk=true;Check(SameXml(unknownPe07.OuterXml,Xml(unknownPePlan07)),"reviewed unknown PE command remains unchanged");
p=Plan();p.UseNarrator=true;p.DiskMode=InstallationSettings.Wipe;p.TargetDiskId=0;p.ConfirmWipeDisk=true;var diskNarrator07=XmlPlanImport.Import(Xml(p),g);Check(diskNarrator07.UseNarrator&&diskNarrator07.ImportedXml!.RequiresDiskReview,"Narrator does not exempt real disk operations");
Console.WriteLine($"Final V0.7 total: {count} tests passed.");
// V0.8: WPA3-Personal, open Wi-Fi and bounded WLANProfile XML. No network calls.
string WifiProfile08(string mode=WifiSettings.Wpa3,bool auto=true,bool hidden=false)=>((Schneegans.Unattend.IProfileWifiSettings)WifiSettings.Build(new InstallPlan{WifiMode=mode,Ssid="WifiFixture08",WifiPassword="WirelessSecret08",ConnectWifi=auto,HiddenWifi=hidden})).ProfileXml.OuterXml;
XmlElement WifiFile08(XmlDocument doc)=>doc.SelectNodes("//*[local-name()='File']")!.Cast<XmlElement>().Single(f=>f.GetAttribute("path").EndsWith("\\Wifi.xml",StringComparison.OrdinalIgnoreCase));
foreach(var mode08 in new[]{WifiSettings.Wpa2,WifiSettings.Wpa3,WifiSettings.Open})foreach(bool hidden08 in new[]{false,true})foreach(bool connect08 in new[]{false,true})foreach(bool window08 in new[]{false,true}){
 p=Plan();p.WifiMode=mode08;p.Ssid="WifiFixture08";p.WifiPassword="WirelessSecret08";p.HiddenWifi=hidden08;p.ConnectWifi=connect08;p.HidePowerShellWindows=window08;
 var xml08=Xml(p);var doc08=XmlMerge.Read(xml08);var profile08=WifiFile08(doc08).InnerText;var state08=WifiSettings.Inspect(doc08);
 Check(state08.Enabled&&!state08.Ambiguous,"V0.8 complete native Wi-Fi pipeline recognized: "+mode08+hidden08+connect08+window08);
 Check(profile08.Contains(mode08==WifiSettings.Wpa3?"WPA3SAE":mode08==WifiSettings.Wpa2?"WPA2PSK":"open")&&profile08.Contains("sharedKey")==WifiSettings.NeedsPassword(p),"V0.8 authentication and credential presence match selected mode");
 Check(profile08.Contains("transitionMode")== (mode08==WifiSettings.Wpa3)&&profile08.Contains("nonBroadcast")==hidden08,"V0.8 native transition and hidden-network fields");
 Check((DecodedXml(xml08).Contains("netsh.exe wlan connect"))==connect08,"V0.8 explicit connection command follows selection");
 foreach(bool metadata08 in new[]{false,true}){
  var input08=metadata08?xml08:WithoutSnapshot(xml08);var parsed08=XmlPlanImport.Import(input08,g);
  Check(parsed08.WifiMode==mode08&&parsed08.Ssid==p.Ssid&&parsed08.ConnectWifi==connect08&&parsed08.HiddenWifi==hidden08&&SameXml(input08,Xml(parsed08)),"V0.8 actual Wi-Fi data restore with/without snapshot");
  parsed08.WifiMode="隐藏无线网络选择页";var canceled08=XmlMerge.Read(Xml(parsed08));Check(!canceled08.SelectNodes("//*[local-name()='File']")!.Cast<XmlElement>().Any(f=>f.GetAttribute("path").EndsWith("\\Wifi.xml"))&&!canceled08.InnerText.Contains("netsh.exe wlan add profile"),"V0.8 cancel Wi-Fi removes native profile and install commands");
 }
 var saved08=PlanStore.Deserialize(PlanStore.Serialize(p,true));Check(saved08.WifiMode==mode08&&saved08.WifiPassword==p.WifiPassword&&saved08.ConnectWifi==connect08,"V0.8 credential-inclusive JSON preserves Wi-Fi choices");
 Check(!GeneratorService.Preview(g.Generate(p)).Contains("WirelessSecret08"),"V0.8 preview does not reveal Wi-Fi credentials");
}
foreach(var arch08 in new[]{"amd64","arm64","x86"})foreach(var mode08 in new[]{WifiSettings.Wpa3,WifiSettings.Open,WifiSettings.Profile}){
 p=Plan();p.Architecture=arch08;p.WifiMode=mode08;p.Ssid="Architecture08";p.WifiPassword="ArchitectureSecret08";p.WifiProfileXml=WifiProfile08();var xml08=Xml(p);Check(WifiSettings.Inspect(XmlMerge.Read(xml08)).Enabled,"V0.8 supports each configuration architecture: "+arch08+mode08);
}
p=Plan();p.WifiMode=WifiSettings.Open;p.Ssid="PublicWifi08";p.WifiPassword="RetainedButUnused08";p.NeedsWifiPassword=true;Check(!DecodedXml(Xml(p)).Contains("RetainedButUnused08")&&!Recommendations.MissingInputs(p).Contains("Wi-Fi 密码"),"open Wi-Fi ignores retained password and missing-password flag");
var omittedOpen08=PlanStore.Deserialize(PlanStore.Serialize(p,false));omittedOpen08.Users[0].Password="RestoredUser08";omittedOpen08.Users[0].NeedsPassword=false;Check(WifiSettings.Inspect(XmlMerge.Read(Xml(omittedOpen08))).Enabled,"open network does not require re-entering a Wi-Fi password");
foreach(var mode08 in new[]{WifiSettings.Wpa2,WifiSettings.Wpa3}){
 p=Plan();p.WifiMode=mode08;p.Ssid="ProtectedWifi08";
 foreach(var bad08 in new[]{"", "short",new string('x',64),"密码不可用123456", "1234567\n"}){p.WifiPassword=bad08;Reject(p,"invalid personal Wi-Fi passphrase rejected");}
 p.WifiPassword="ValidWifiSecret08";var omitted08=PlanStore.Deserialize(PlanStore.Serialize(p,false));Check(!omitted08.WifiPassword.Any()&&omitted08.NeedsWifiPassword,"protected Wi-Fi secret omitted and marked missing");omitted08.Users[0].Password="RestoredUser08";omitted08.Users[0].NeedsPassword=false;Reject(omitted08,"omitted personal Wi-Fi credential blocks export");
 foreach(var badName08 in new[]{"",new string('x',33),"unsafe$ssid","unsafe`ssid","unsafe\"ssid","line\nssid"}){p.Ssid=badName08;Reject(p,"unsafe Wi-Fi SSID rejected for both authentication modes");}
}
foreach(bool connect08 in new[]{false,true})foreach(bool hidden08 in new[]{false,true}){
 p=Plan();p.WifiMode=WifiSettings.Profile;p.WifiProfileXml=WifiProfile08(WifiSettings.Wpa3,connect08,hidden08);p.Ssid="UnusedParameter08";p.WifiPassword="UnusedPassword08";p.ConnectWifi=!connect08;p.HiddenWifi=!hidden08;
 var xml08=Xml(p);Check(!DecodedXml(xml08).Contains("UnusedParameter08")&&!DecodedXml(xml08).Contains("UnusedPassword08")&&DecodedXml(xml08).Contains("netsh.exe wlan connect")==connect08,"XML profile owns identity/credentials/connection flags");
 var parsed08=XmlPlanImport.Import(xml08,g);Check(parsed08.WifiMode==WifiSettings.Profile&&SameXml(xml08,Xml(parsed08))&&WifiSettings.ReadProfile(parsed08.WifiProfileXml).ConnectAutomatically==connect08,"XML mode retained through snapshot import");
 var saved08=PlanStore.Deserialize(PlanStore.Serialize(p,true));Check(saved08.WifiProfileXml==p.WifiProfileXml&&!saved08.NeedsWifiProfile,"full profile XML survives credential-inclusive JSON");
 var omitted08=PlanStore.Deserialize(PlanStore.Serialize(p,false));Check(omitted08.WifiProfileXml==""&&omitted08.NeedsWifiProfile&&!PlanStore.Serialize(p,false).Contains("WirelessSecret08"),"credential omission drops entire XML profile");omitted08.Users[0].Password="RestoredUser08";omitted08.Users[0].NeedsPassword=false;Reject(omitted08,"missing XML profile blocks export");omitted08.WifiProfileXml=p.WifiProfileXml;Check(!omitted08.NeedsWifiProfile&&WifiSettings.Inspect(XmlMerge.Read(Xml(omitted08))).Enabled,"re-entered profile clears missing state and restores export");
 Check(!GeneratorService.Preview(g.Generate(p)).Contains("WirelessSecret08")&&GeneratorService.Preview(g.Generate(p)).Contains("无线配置 XML 已整体遮蔽"),"preview masks whole wireless profile");
}
// Namespace-qualified content and opaque extension secrets cannot bypass preview redaction.
var prefixedProfile08=new XmlDocument();prefixedProfile08.LoadXml(WifiProfile08());foreach(XmlElement element08 in prefixedProfile08.SelectNodes("//*")!)if(element08.NamespaceURI=="http://www.microsoft.com/networking/WLAN/profile/v1")element08.Prefix="w";
p=Plan();p.WifiMode=WifiSettings.Profile;p.WifiProfileXml=prefixedProfile08.OuterXml;Check(!GeneratorService.Preview(g.Generate(p)).Contains("WirelessSecret08"),"namespace-qualified key material is hidden in preview");
// Manual profiles may use a distinct profile name; the native automatic connector requires matching names.
var namedProfile08=WifiProfile08(auto:false).Replace("<name>WifiFixture08</name>","<name>DifferentProfile08</name>");Check(WifiSettings.ReadProfile(namedProfile08).Name=="DifferentProfile08","manual profile accepts a different name");
try{_=WifiSettings.ReadProfile(namedProfile08.Replace("<connectionMode>manual</connectionMode>","<connectionMode>auto</connectionMode>"));throw new Exception("expected name restriction");}catch(ArgumentException){Check(true,"native automatic profile naming restriction explained");}
var invalidProfiles08=new[]{"", "<unattend/>","<WLANProfile/>","<!DOCTYPE WLANProfile [<!ENTITY x 'unsafe'>]>"+WifiProfile08(),"<?test unsafe?>"+WifiProfile08(),WifiProfile08().Replace("<protected>false</protected>","<protected>true</protected>"),WifiProfile08().Replace("<name>WifiFixture08</name>","<name>bad$profile</name>"),new string('x',WifiSettings.MaxProfileLength+1),WifiProfile08().Replace("57696669466978747572653038","not-hex")};
foreach(var invalid08 in invalidProfiles08){p=Plan();p.WifiMode=WifiSettings.Profile;p.WifiProfileXml=invalid08;Reject(p,"invalid/unsafe/nonportable WLAN profile rejected");}
// Native upstream outputs without app metadata recognize both new parameter modes.
foreach(var auth08 in new[]{Schneegans.Unattend.WifiAuthentications.WPA3SAE,Schneegans.Unattend.WifiAuthentications.Open}){
 var native08=new Schneegans.Unattend.UnattendGenerator().GenerateXml(Schneegans.Unattend.Configuration.Default with{WifiSettings=new Schneegans.Unattend.ParameterizedWifiSettings("NativeWifi08","NativeSecret08",true,auth08,true)}).OuterXml;
 var parsed08=XmlPlanImport.Import(native08,g);Check(parsed08.WifiMode==(auth08==Schneegans.Unattend.WifiAuthentications.Open?WifiSettings.Open:WifiSettings.Wpa3)&&SameXml(native08,Xml(parsed08)),"native new Wi-Fi authentication imports without app metadata");
}
Console.WriteLine($"Final V0.8 total: {count} tests passed.");
// Unknown or inactive wireless payloads must not become an editable switch.
p=Plan();p.WifiMode=WifiSettings.Wpa3;p.Ssid="WifiFixture08";p.WifiPassword="WirelessSecret08";p.ConnectWifi=true;var originalWifi08=Xml(p);
foreach(var change08 in new[]{"missingFile","duplicateFile","alteredWait","conditionalAdd","duplicateAdd","missingConnect","wrongStage","uncalled","noExtraction","protectedKey"}){
 var doc08=XmlMerge.Read(originalWifi08);var stage08=doc08.SelectNodes("//*[local-name()='File']")!.Cast<XmlElement>().Single(f=>f.GetAttribute("path").EndsWith("\\Specialize.ps1"));
 string add08="netsh.exe wlan add profile filename=\"C:\\Windows\\Setup\\Scripts\\Wifi.xml\" user=all;";
 if(change08=="missingFile"){var f08=WifiFile08(doc08);f08.ParentNode!.RemoveChild(f08);}
 else if(change08=="duplicateFile"){var f08=WifiFile08(doc08);f08.ParentNode!.AppendChild(f08.CloneNode(true));}
 else if(change08=="alteredWait")stage08.InnerText=stage08.InnerText.Replace("$start.AddMinutes( 1 )","$start.AddMinutes( 2 )");
 else if(change08=="conditionalAdd")stage08.InnerText=stage08.InnerText.Replace(add08,"if ($false) { "+add08+" }");
 else if(change08=="duplicateAdd")stage08.InnerText=stage08.InnerText.Replace(add08,add08+"\n\t\t"+add08);
 else if(change08=="missingConnect")stage08.InnerText=stage08.InnerText.Replace("netsh.exe wlan connect name=\"WifiFixture08\" ssid=\"WifiFixture08\";","Write-Output 'not connecting';");
 else if(change08=="wrongStage"){stage08.InnerText=stage08.InnerText.Replace(add08,"Write-Output 'moved add';");var f08=doc08.CreateElement("File",stage08.NamespaceURI);f08.SetAttribute("path",@"C:\Windows\Setup\Scripts\OtherWifi08.ps1");f08.InnerText=add08;stage08.ParentNode!.AppendChild(f08);}
 else if(change08=="uncalled"){foreach(XmlElement path08 in doc08.SelectNodes("//*[local-name()='Path']")!)if(path08.InnerText.Contains("-File")&&path08.InnerText.Contains("Specialize.ps1"))path08.InnerText=path08.InnerText.Replace("Specialize.ps1","UncalledWifi08.ps1");}
 else if(change08=="noExtraction")doc08.SelectSingleNode("//*[local-name()='ExtractScript']")!.InnerText="Write-Output 'not extracting';";
 else WifiFile08(doc08).InnerText=WifiFile08(doc08).InnerText.Replace("<protected>false</protected>","<protected>true</protected>");
 var state08=WifiSettings.Inspect(doc08);var parsed08=XmlPlanImport.Import(doc08.OuterXml,g);
 Check(!state08.Enabled&&state08.Ambiguous&&!WifiSettings.HasProfile(parsed08)&&SameXml(doc08.OuterXml,Xml(parsed08)),"ambiguous Wi-Fi payload preserved and not represented: "+change08);
 parsed08.WifiMode=WifiSettings.Open;parsed08.Ssid="NewWifi08";Reject(parsed08,"ambiguous wireless payload blocks settings overlay");
}
// Adding Wi-Fi to a previously wired import and editing a recognized profile retain unrelated content.
var foreignWifi08=XmlMerge.Read(Xml(Plan()));var extWifi08=foreignWifi08.SelectSingleNode("//*[local-name()='Extensions']")!;extWifi08.AppendChild(foreignWifi08.CreateComment("retain unrelated Wi-Fi neighbor"));
var addedWifi08=XmlPlanImport.Import(foreignWifi08.OuterXml,g);addedWifi08.WifiMode=WifiSettings.Wpa3;addedWifi08.Ssid="AddedWifi08";addedWifi08.WifiPassword="AddedSecret08";addedWifi08.ConnectWifi=true;var addedOutput08=Xml(addedWifi08);Check(addedOutput08.Contains("retain unrelated Wi-Fi neighbor")&&XmlPlanImport.Import(addedOutput08,g).WifiMode==WifiSettings.Wpa3,"adding WPA3 to a wired import preserves unrelated XML");
var editedWifi08=XmlPlanImport.Import(originalWifi08,g);editedWifi08.WifiMode=WifiSettings.Open;editedWifi08.Ssid="RenamedOpen08";editedWifi08.HiddenWifi=true;editedWifi08.ConnectWifi=false;var editedOutput08=XmlPlanImport.Import(Xml(editedWifi08),g);Check(editedOutput08.WifiMode==WifiSettings.Open&&editedOutput08.Ssid=="RenamedOpen08"&&editedOutput08.HiddenWifi&&!editedOutput08.ConnectWifi,"recognized Wi-Fi authentication/name/flags independently editable");
// Old imported JSON had no fields for XML profiles and could not recognize WPA3.
var legacyWifi08=XmlPlanImport.Import(originalWifi08,g);legacyWifi08.WifiMode="隐藏无线网络选择页";legacyWifi08.WifiPassword="";legacyWifi08.Ssid="";var legacyWifiBase08=PlanStore.Deserialize(PlanStore.Serialize(legacyWifi08,true));legacyWifiBase08.ImportedXml=null;legacyWifi08.ImportedXml!.BaselineXml=Encoding.UTF8.GetString(g.GenerateStandalone(legacyWifiBase08));legacyWifi08.ComputerName="PENDING08";legacyWifi08.ImportedXml.Notices=legacyWifi08.ImportedXml.Notices.Append("Wi-Fi 配置并非当前支持的未加密 WPA2-Personal 参数格式，原配置保留在 XML 中，网络页仅显示可识别模式。").ToArray();
var oldWifiJson08=System.Text.Json.Nodes.JsonNode.Parse(PlanStore.Serialize(legacyWifi08,true))!.AsObject();oldWifiJson08.Remove(nameof(InstallPlan.WifiProfileXml));oldWifiJson08.Remove(nameof(InstallPlan.NeedsWifiProfile));var migratedWifi08=PlanStore.Deserialize(oldWifiJson08.ToJsonString());
Check(migratedWifi08.WifiMode==WifiSettings.Wpa3&&migratedWifi08.ComputerName=="PENDING08"&&migratedWifi08.ImportedXml!.Blockers.Length==0&&!migratedWifi08.ImportedXml.Notices.Any(n=>n.StartsWith("Wi-Fi 配置并非当前支持的")),"old imported JSON migrates actual WPA3 state without losing pending edits");
Check(Node(Xml(migratedWifi08),"//u:ComputerName")=="PENDING08"&&XmlPlanImport.Import(Xml(migratedWifi08),g).WifiMode==WifiSettings.Wpa3,"migrated wireless baseline remains editable");migratedWifi08.WifiMode="隐藏无线网络选择页";var explicitlyOffWifi08=PlanStore.Deserialize(PlanStore.Serialize(migratedWifi08,true));Check(!WifiSettings.HasProfile(explicitlyOffWifi08)&&!WifiSettings.Inspect(XmlMerge.Read(Xml(explicitlyOffWifi08))).Enabled,"explicit saved Wi-Fi cancellation is not overwritten");
// Saved pending network edits from an old plan are not replaced by migration.
oldWifiJson08[nameof(InstallPlan.WifiMode)]="安装时手动选择";var pendingWifi08=PlanStore.Deserialize(oldWifiJson08.ToJsonString());Check(pendingWifi08.WifiMode=="安装时手动选择","legacy pending network-mode edit takes priority over migration");
viewModel.Plan=Plan();viewModel.Preview="stale08";viewModel.Plan.WifiMode=WifiSettings.Wpa3;Check(viewModel.IsPresetWifi&&viewModel.IsPasswordWifi&&!viewModel.IsXmlWifi&&viewModel.Plan.ConnectWifi&&viewModel.Preview!="stale08","WPA3 mode refreshes controls and invalidates preview");viewModel.Plan.WifiMode=WifiSettings.Open;Check(viewModel.IsPresetWifi&&!viewModel.IsPasswordWifi&&!Recommendations.MissingInputs(viewModel.Plan).Contains("Wi-Fi 密码"),"open mode disables password requirements");viewModel.Plan.WifiMode=WifiSettings.Profile;Check(!viewModel.IsPresetWifi&&viewModel.IsXmlWifi&&viewModel.HelpTips["wifiprofile"].Body.Contains("可能含凭据")&&Recommendations.MissingInputs(viewModel.Plan).Contains("无线配置 XML"),"XML mode refreshes help and missing-input summary");
p=Plan();Recommendations.Apply(p,"wifi");p.WifiMode=WifiSettings.Wpa3;Check(Recommendations.Differences(p).Any(d=>d.Property==nameof(InstallPlan.WifiMode)),"WPA3 appears as a deliberate recommendation difference");Recommendations.Restore(p,nameof(InstallPlan.WifiMode));Check(p.WifiMode==WifiSettings.Wpa2,"network recommendation remains backward compatible with WPA2");
p=Plan();p.WifiMode=WifiSettings.Wpa3;p.Ssid="LegacyCompatible08";p.WifiPassword="WirelessSecret08";p.WifiProfileXml=WifiProfile08();Recommendations.SelectNetworkScene(p,"wired");Check(p.WifiProfileXml.Contains("WirelessSecret08")&&p.WifiPassword=="WirelessSecret08","network scene change retains inactive editable data");Check(!PlanStore.Serialize(p,false).Contains("WirelessSecret08"),"secret omission also clears inactive profile XML");
Console.WriteLine($"Final V0.8 total: {count} tests passed.");
// V0.9: native computer-name scripts/dialog and additional locale/keyboard pairs.
Check(Plan().ComputerNameMode==ComputerNamingSettings.Existing&&!Plan().UseSecondLanguage&&!Plan().UseThirdLanguage,"V0.9 preserves fixed/random and single-language defaults");
string Input09(string text)=>Node(text,"//u:component[@name='Microsoft-Windows-International-Core']/u:InputLocale")??"";
void Extra09(InstallPlan plan){plan.SecondLocale="en-US";plan.SecondKeyboard="00000409";plan.ThirdLocale="de-DE";plan.ThirdKeyboard="00000407";}
foreach(var arch09 in new[]{"amd64","arm64","x86"})foreach(var mode09 in new[]{ComputerNamingSettings.Existing,ComputerNamingSettings.Prompt,ComputerNamingSettings.Script})foreach(bool hidden09 in new[]{false,true}){
 p=Plan();p.Architecture=arch09;p.ComputerNameMode=mode09;p.ComputerNameScript="return 'NAMED09';";p.HidePowerShellWindows=hidden09;
 var xml09=Xml(p);var state09=ComputerNamingSettings.Inspect(XmlMerge.Read(xml09));
 Check(state09.Enabled==(mode09!=ComputerNamingSettings.Existing)&&!state09.Ambiguous,"native naming mode recognized: "+arch09+mode09+hidden09);
 if(mode09==ComputerNamingSettings.Prompt)Check(DecodedXml(xml09).Contains("Microsoft.VisualBasic.Interaction")&&DecodedXml(xml09).Contains("Test-ComputerName"),"native dialog script includes name validation and works with hidden PowerShell");
 foreach(bool metadata09 in new[]{false,true}){
  var input09=metadata09?xml09:WithoutSnapshot(xml09);var loaded09=XmlPlanImport.Import(input09,g);
  Check(loaded09.ComputerNameMode==mode09&&SameXml(input09,Xml(loaded09)),"naming XML with/without snapshot restores mode without drift");
  if(mode09!=ComputerNamingSettings.Existing){loaded09.ComputerNameMode=ComputerNamingSettings.Existing;loaded09.ComputerName="CANCELED09";var canceled09=Xml(loaded09);Check(!DecodedXml(canceled09).Contains("GetComputerName.ps1")&&Node(canceled09,"//u:ComputerName")=="CANCELED09","cancel native naming pipeline and set fixed name");}
 }
 foreach(bool secrets09 in new[]{false,true}){var loaded09=PlanStore.Deserialize(PlanStore.Serialize(p,secrets09));Check(loaded09.ComputerNameMode==mode09&&loaded09.ComputerNameScript==p.ComputerNameScript,"naming scripts remain plain text in both JSON modes, like custom scripts");}
}
p=Plan();p.ComputerNameMode=ComputerNamingSettings.Script;Reject(p,"blank naming script is not replaced by example");p.ComputerNameScript=new string('x',65537);Reject(p,"oversized naming script rejected");p.ComputerNameMode="unknown";Reject(p,"unknown naming mode rejected");p=Plan();p.ComputerNameMode=ComputerNamingSettings.Script;p.ComputerNameScript="throw 'do not run on editing computer';";Check(DecodedXml(Xml(p)).Contains("do not run on editing computer"),"name script embedded without local execution");
p=Plan();p.ComputerNameMode=ComputerNamingSettings.Script;p.ComputerNameScript="return 'NATIVE09';";var namedXml09=Xml(p);
foreach(var change09 in new[]{"getterMissing","getterDuplicate","setterAltered","blockConditional","noExtract","noCall","wrongTemporaryName"}){
 var doc09=XmlMerge.Read(namedXml09);var files09=doc09.SelectNodes("//*[local-name()='File']")!.Cast<XmlElement>().ToArray();
 var getter09=files09.Single(f=>f.GetAttribute("path").EndsWith("GetComputerName.ps1"));var setter09=files09.Single(f=>f.GetAttribute("path").EndsWith("SetComputerName.ps1"));var stage09=files09.Single(f=>f.GetAttribute("path").EndsWith("\\Specialize.ps1"));
 if(change09=="getterMissing")getter09.ParentNode!.RemoveChild(getter09);
 else if(change09=="getterDuplicate")getter09.ParentNode!.AppendChild(getter09.CloneNode(true));
 else if(change09=="setterAltered")setter09.InnerText+="\nWrite-Output 'custom setter';";
 else if(change09=="blockConditional")stage09.InnerText=stage09.InnerText.Replace("[string] $newName = &", "if ($false) { [string] $newName = &").Replace("Start-Sleep -Seconds 10;","Start-Sleep -Seconds 10; }");
 else if(change09=="noExtract")doc09.SelectSingleNode("//*[local-name()='ExtractScript']")!.InnerText="Write-Output 'no extraction';";
 else if(change09=="noCall"){foreach(XmlElement call09 in doc09.SelectNodes("//*[local-name()='Path']")!)if(call09.InnerText.Contains("-File")&&call09.InnerText.Contains("Specialize.ps1"))call09.InnerText=call09.InnerText.Replace("Specialize.ps1","Uncalled09.ps1");}
 else doc09.SelectSingleNode("//*[local-name()='ComputerName']")!.InnerText="OTHER09";
 var loaded09=XmlPlanImport.Import(doc09.OuterXml,g);Check(loaded09.ComputerNameMode==ComputerNamingSettings.Existing&&ComputerNamingSettings.Inspect(doc09).Ambiguous&&SameXml(doc09.OuterXml,Xml(loaded09)),"unknown naming flow retained without claiming editable mode: "+change09);loaded09.ComputerName="NEW09";Reject(loaded09,"unknown naming pipeline blocks fixed-name changes");loaded09.ComputerNameMode=ComputerNamingSettings.Script;loaded09.ComputerNameScript="return 'NEW09';";Reject(loaded09,"unknown naming pipeline blocks script overlay");
}
// Extra groups are independent and their order follows the fixed generator.
foreach(var arch09 in new[]{"amd64","arm64","x86"})foreach(int mask09 in Enumerable.Range(0,4)){
 p=Plan();p.Architecture=arch09;Extra09(p);p.UseSecondLanguage=(mask09&1)!=0;p.UseThirdLanguage=(mask09&2)!=0;
 var xml09=Xml(p);string tokens09=Input09(xml09);Check(tokens09.Split(';').Length==1+((mask09&1)!=0?1:0)+((mask09&2)!=0?1:0),"native extra input group count: "+arch09+mask09);
 Check(tokens09.Contains("0409:00000409")==p.UseSecondLanguage&&tokens09.Contains("0407:00000407")==p.UseThirdLanguage,"extra keyboard groups emitted independently");
 foreach(bool metadata09 in new[]{false,true}){
  var input09=metadata09?xml09:WithoutSnapshot(xml09);var loaded09=XmlPlanImport.Import(input09,g);Check(SameXml(input09,Xml(loaded09))&&Input09(Xml(loaded09))==tokens09,"extra language XML restores actual inputs without drift");
  if(metadata09)Check(loaded09.UseSecondLanguage==p.UseSecondLanguage&&loaded09.UseThirdLanguage==p.UseThirdLanguage,"snapshot retains independent second/third slot selection");
  loaded09.UseSecondLanguage=loaded09.UseThirdLanguage=false;Check(Input09(Xml(loaded09)).Split(';').Length==1,"extra groups can be removed from imported XML");
 }
 foreach(bool secrets09 in new[]{false,true}){var loaded09=PlanStore.Deserialize(PlanStore.Serialize(p,secrets09));Check(loaded09.UseSecondLanguage==p.UseSecondLanguage&&loaded09.UseThirdLanguage==p.UseThirdLanguage&&loaded09.SecondKeyboard==p.SecondKeyboard&&loaded09.ThirdLocale==p.ThirdLocale,"extra language values survive both JSON modes");}
}
p=Plan();Extra09(p);p.UseSecondLanguage=p.UseThirdLanguage=true;p.InteractiveLanguage=true;Check(Input09(Xml(p))=="","interactive language choice suppresses retained extra groups");var retained09=PlanStore.Deserialize(PlanStore.Serialize(p,true));Check(retained09.UseSecondLanguage&&retained09.UseThirdLanguage&&retained09.SecondLocale=="en-US","interactive language choice retains extra values in JSON");
p=Plan();p.UseSecondLanguage=true;Reject(p,"enabled second group requires both fields");Extra09(p);p.UseThirdLanguage=true;p.ThirdKeyboard="unknown";Reject(p,"third group rejects unknown keyboard");p.UseThirdLanguage=false;Check(Input09(Xml(p)).Contains("0409:00000409"),"inactive third group ignores retained invalid keyboard");
p=Plan();p.UseSecondLanguage=true;p.SecondLocale=p.Locale;p.SecondKeyboard=p.Keyboard;var ime09=WithoutSnapshot(Xml(p));Check(SameXml(ime09,Xml(XmlPlanImport.Import(ime09,g))),"extra IME group survives XML without metadata");
p=Plan();Extra09(p);p.UseSecondLanguage=p.UseThirdLanguage=true;var threeGroups09=Xml(p);var tooMany09=XmlMerge.Read(threeGroups09);tooMany09.SelectSingleNode("//*[local-name()='InputLocale']")!.InnerText+=";0419:00000419";var preservedGroups09=XmlPlanImport.Import(tooMany09.OuterXml,g);Check(!preservedGroups09.UseSecondLanguage&&!preservedGroups09.UseThirdLanguage&&SameXml(tooMany09.OuterXml,Xml(preservedGroups09)),"more than three groups retained as unknown");Extra09(preservedGroups09);preservedGroups09.UseSecondLanguage=true;Reject(preservedGroups09,"unknown extra groups block overlay");
// Import old plans by migrating the baseline, retaining pending unrelated and fixed-name edits.
var legacyName09=XmlPlanImport.Import(namedXml09,g);legacyName09.ComputerNameMode=ComputerNamingSettings.Existing;legacyName09.ComputerNameScript="";var oldNameBase09=PlanStore.Deserialize(PlanStore.Serialize(legacyName09,true));oldNameBase09.ImportedXml=null;legacyName09.ImportedXml!.BaselineXml=Encoding.UTF8.GetString(g.GenerateStandalone(oldNameBase09));var legacyNameJson09=System.Text.Json.Nodes.JsonNode.Parse(PlanStore.Serialize(legacyName09,true))!.AsObject();legacyNameJson09.Remove(nameof(InstallPlan.ComputerNameMode));legacyNameJson09.Remove(nameof(InstallPlan.ComputerNameScript));var migratedName09=PlanStore.Deserialize(legacyNameJson09.ToJsonString());Check(migratedName09.ComputerNameMode==ComputerNamingSettings.Script&&migratedName09.ImportedXml!.Blockers.Length==0&&SameXml(namedXml09,Xml(migratedName09)),"legacy imported name script gains editable baseline");
legacyNameJson09[nameof(InstallPlan.ComputerName)]="PENDING09";var pendingName09=PlanStore.Deserialize(legacyNameJson09.ToJsonString());var pendingNameXml09=Xml(pendingName09);Check(pendingName09.ComputerNameMode==ComputerNamingSettings.Existing&&Node(pendingNameXml09,"//u:ComputerName")=="PENDING09"&&!DecodedXml(pendingNameXml09).Contains("GetComputerName.ps1"),"legacy pending fixed name cancels native script after baseline migration");
var legacyLanguage09=XmlPlanImport.Import(threeGroups09,g);legacyLanguage09.UseSecondLanguage=legacyLanguage09.UseThirdLanguage=false;var oldLanguageBase09=PlanStore.Deserialize(PlanStore.Serialize(legacyLanguage09,true));oldLanguageBase09.ImportedXml=null;legacyLanguage09.ImportedXml!.BaselineXml=Encoding.UTF8.GetString(g.GenerateStandalone(oldLanguageBase09));legacyLanguage09.ComputerName="PENDINGLANG09";var legacyLanguageJson09=System.Text.Json.Nodes.JsonNode.Parse(PlanStore.Serialize(legacyLanguage09,true))!.AsObject();foreach(var name09 in new[]{nameof(InstallPlan.UseSecondLanguage),nameof(InstallPlan.UseThirdLanguage),nameof(InstallPlan.SecondLocale),nameof(InstallPlan.SecondKeyboard),nameof(InstallPlan.ThirdLocale),nameof(InstallPlan.ThirdKeyboard)})legacyLanguageJson09.Remove(name09);var migratedLanguage09=PlanStore.Deserialize(legacyLanguageJson09.ToJsonString());Check(migratedLanguage09.UseSecondLanguage&&migratedLanguage09.UseThirdLanguage&&migratedLanguage09.ComputerName=="PENDINGLANG09"&&Input09(Xml(migratedLanguage09))==Input09(threeGroups09),"legacy extra groups migrate without losing pending computer name");migratedLanguage09.UseSecondLanguage=migratedLanguage09.UseThirdLanguage=false;var savedCancellation09=PlanStore.Deserialize(PlanStore.Serialize(migratedLanguage09,true));Check(Input09(Xml(savedCancellation09)).Split(';').Length==1,"explicit saved extra-language cancellation retained");
p=Plan();Extra09(p);p.UseSecondLanguage=p.UseThirdLanguage=true;p.ComputerNameMode=ComputerNamingSettings.Script;p.ComputerNameScript=ComputerNamingSettings.Example;Recommendations.Apply(p,"wired");Check(!p.UseSecondLanguage&&!p.UseThirdLanguage&&p.ComputerNameMode==ComputerNamingSettings.Script&&p.ComputerNameScript==ComputerNamingSettings.Example,"recommendation resets optional input groups while preserving naming identity");p.UseSecondLanguage=true;Check(Recommendations.Differences(p).Any(d=>d.Property==nameof(InstallPlan.UseSecondLanguage)),"extra input group appears in recommendation differences");
viewModel.Plan=Plan();viewModel.Preview="stale09";viewModel.Plan.ComputerNameMode=ComputerNamingSettings.Prompt;Check(viewModel.IsPromptComputerName&&!viewModel.IsFixedComputerName&&viewModel.Preview!="stale09"&&Recommendations.InteractiveSteps(viewModel.Plan).Contains("计算机名输入"),"dialog naming updates controls, summary and interaction requirements");viewModel.Plan.ComputerNameMode=ComputerNamingSettings.Script;Check(viewModel.IsScriptComputerName&&Recommendations.MissingInputs(viewModel.Plan).Contains("计算机命名脚本"),"script naming requires explicit script input");Extra09(viewModel.Plan);viewModel.Plan.UseSecondLanguage=true;Check(viewModel.IsSecondLanguage&&viewModel.LanguagePreferencesSummary.Contains("第二组")&&viewModel.HelpTips["extralanguages"].Body.Contains("第二组"),"extra input groups update summary/help/controls");
Console.WriteLine($"Final V0.9 total: {count} tests passed.");
// V0.10: four optional native protection/permission settings, disabled by default.
void SetSecurity10(InstallPlan plan,int mask){for(int i=0;i<SecurityOptions.Items.Count;i++)typeof(InstallPlan).GetProperty(SecurityOptions.Items[i].Property)!.SetValue(plan,(mask&(1<<i))!=0);}
Check(SecurityOptions.Items.All(i=>!SecurityOptions.Selected(InstallPlan.New(),i)&&!SecurityOptions.Selected(InstallPlan.NewCustom(),i)),"V0.10 security overrides default off in both starting modes");
for(int mask10=0;mask10<16;mask10++){
 p=Plan();SetSecurity10(p,mask10);var xml10=Xml(p);var states10=SecurityOptions.Inspect(XmlMerge.Read(xml10));
 foreach(var item10 in SecurityOptions.Items)Check(states10[item10.Property].Enabled==SecurityOptions.Selected(p,item10)&&!states10[item10.Property].Ambiguous,"native security choice recognized independently: "+mask10+item10.Property);
 foreach(bool metadata10 in new[]{false,true}){
  var input10=metadata10?xml10:WithoutSnapshot(xml10);var loaded10=XmlPlanImport.Import(input10,g);
  Check(SecurityOptions.Items.All(i=>SecurityOptions.Selected(loaded10,i)==SecurityOptions.Selected(p,i))&&SameXml(input10,Xml(loaded10)),"security choices restore actual payload and preserve XML");
  foreach(var item10 in SecurityOptions.Items.Where(i=>SecurityOptions.Selected(p,i))){var single10=XmlPlanImport.Import(input10,g);typeof(InstallPlan).GetProperty(item10.Property)!.SetValue(single10,false);var after10=SecurityOptions.Inspect(XmlMerge.Read(Xml(single10)));Check(!after10[item10.Property].Enabled&&SecurityOptions.Items.Where(i=>i!=item10).All(i=>after10[i.Property].Enabled==SecurityOptions.Selected(p,i)),"one security option removed without affecting others");}
 }
 foreach(bool secrets10 in new[]{false,true}){var loaded10=PlanStore.Deserialize(PlanStore.Serialize(p,secrets10));Check(SecurityOptions.Items.All(i=>SecurityOptions.Selected(loaded10,i)==SecurityOptions.Selected(p,i)),"security choices persist in either credential mode");}
}
foreach(string arch10 in new[]{"amd64","arm64","x86"})foreach(bool hidden10 in new[]{false,true}){
 p=Plan();SetSecurity10(p,15);p.Architecture=arch10;p.HidePowerShellWindows=hidden10;var xml10=Xml(p);Check(SecurityOptions.Inspect(XmlMerge.Read(xml10)).Values.All(s=>s.Enabled&&!s.Ambiguous),"security groups recognize architecture and window style: "+arch10+hidden10);
 var native10=new Schneegans.Unattend.UnattendGenerator().GenerateXml(Schneegans.Unattend.Configuration.Default with{ProcessorArchitectures=System.Collections.Immutable.ImmutableHashSet.Create(Enum.Parse<Schneegans.Unattend.ProcessorArchitecture>(arch10)),DisableSac=true,DisableSmartScreen=true,DisableCoreIsolation=true,HardenSystemDriveAcl=true,HidePowerShellWindows=hidden10}).OuterXml;var imported10=XmlPlanImport.Import(native10,g);Check(SecurityOptions.Items.All(i=>SecurityOptions.Selected(imported10,i))&&SameXml(native10,Xml(imported10)),"native upstream security XML imports without application metadata");
}
p=Plan();SetSecurity10(p,15);var allSecurity10=Xml(p);var allDoc10=XmlMerge.Read(allSecurity10);var firstStage10=allDoc10.SelectNodes("//*[local-name()='File']")!.Cast<XmlElement>().Single(f=>f.GetAttribute("path").EndsWith("\\Specialize.ps1")).InnerText;var defaultStage10=allDoc10.SelectNodes("//*[local-name()='File']")!.Cast<XmlElement>().Single(f=>f.GetAttribute("path").EndsWith("\\DefaultUser.ps1")).InnerText;
Check(firstStage10.Contains("VerifiedAndReputablePolicyState")&&firstStage10.Contains("EnableVirtualizationBasedSecurity")&&firstStage10.Contains("icacls.exe C:\\")&&firstStage10.Contains("HideSystray")&&defaultStage10.Contains("SmartScreenPuaEnabled"),"security options retain native specialize/default-user stages and tray side effect");
Check(!DecodedXml(allSecurity10).Contains("Set-MpPreference")&&!DecodedXml(allSecurity10).Contains("DisableRealtimeMonitoring")&&!DecodedXml(allSecurity10).Contains("Disable-WindowsOptionalFeature"),"security choices do not add antivirus-engine or Windows-feature removal commands");
foreach(var item10 in SecurityOptions.Items)foreach(string change10 in new[]{"altered","duplicate","conditional","wrongStage","noExtraction","noInvocation"}){
 var d10=XmlMerge.Read(WithoutSnapshot(allSecurity10));var files10=d10.SelectNodes("//*[local-name()='File']")!.Cast<XmlElement>().ToArray();var specialize10=files10.Single(f=>f.GetAttribute("path").EndsWith("\\Specialize.ps1"));var stage10=files10.Single(f=>f.GetAttribute("path").EndsWith(item10.Property==nameof(InstallPlan.DisableSmartScreen)?"\\DefaultUser.ps1":"\\Specialize.ps1"));
 // Match the original tab-indented block while retaining its stage wrapper.
 var candidates10=System.Text.RegularExpressions.Regex.Matches(stage10.InnerText.Replace("\r\n","\n"),@"(?m)^\t\{\n(?<body>(?:\t\t[^\n]*\n)*)\t\};\n").Cast<System.Text.RegularExpressions.Match>().ToArray();var target10=candidates10.First(m=>item10.Markers.Any(marker=>m.Groups["body"].Value.Contains(marker,StringComparison.OrdinalIgnoreCase)));string raw10=target10.Value;
 if(change10=="altered")stage10.InnerText=stage10.InnerText.Replace(raw10,raw10.Replace("/d 0", "/d 1").Replace("/remove:g","/grant").Replace("/d \"Off\"","/d \"Warn\""));
 else if(change10=="duplicate")stage10.InnerText=stage10.InnerText.Replace(raw10,raw10+raw10);
 else if(change10=="conditional")stage10.InnerText=stage10.InnerText.Replace(raw10,raw10.Replace("\t\t", "\t\tif ($false) { ").Replace(";\n", "; }\n"));
 else if(change10=="wrongStage"){stage10.InnerText=stage10.InnerText.Replace(raw10,"");var elsewhere10=d10.CreateElement("File",d10.DocumentElement!.NamespaceURI);elsewhere10.SetAttribute("path",@"C:\Windows\Setup\Scripts\UnknownSecurity10.ps1");elsewhere10.InnerText=raw10;files10[0].ParentNode!.AppendChild(elsewhere10);}
 else if(change10=="noExtraction")d10.SelectSingleNode("//*[local-name()='ExtractScript']")!.InnerText="Write-Output 'not extracted';";
 else foreach(XmlElement path10 in d10.SelectNodes("//*[local-name()='Path']")!)if(path10.InnerText.Contains("-File")&&path10.InnerText.Contains("Specialize.ps1"))path10.InnerText=path10.InnerText.Replace("Specialize.ps1","UnusedSecurity10.ps1");
 var unknown10=XmlPlanImport.Import(d10.OuterXml,g);Check(!SecurityOptions.Selected(unknown10,item10)&&SecurityOptions.Inspect(d10)[item10.Property].Ambiguous&&SameXml(d10.OuterXml,Xml(unknown10)),"unknown security payload preserved without false recognition: "+item10.Property+change10);typeof(InstallPlan).GetProperty(item10.Property)!.SetValue(unknown10,true);Reject(unknown10,"unknown security payload cannot be overwritten");
}
var legacySecurity10=XmlPlanImport.Import(allSecurity10,g);SetSecurity10(legacySecurity10,0);var baselineSecurity10=PlanStore.Deserialize(PlanStore.Serialize(legacySecurity10,true));baselineSecurity10.ImportedXml=null;legacySecurity10.ImportedXml!.BaselineXml=Encoding.UTF8.GetString(g.GenerateStandalone(baselineSecurity10));legacySecurity10.ComputerName="PENDINGSEC10";var legacyJson10=System.Text.Json.Nodes.JsonNode.Parse(PlanStore.Serialize(legacySecurity10,true))!.AsObject();foreach(var item10 in SecurityOptions.Items)legacyJson10.Remove(item10.Property);var migratedSecurity10=PlanStore.Deserialize(legacyJson10.ToJsonString());Check(SecurityOptions.Items.All(i=>SecurityOptions.Selected(migratedSecurity10,i))&&migratedSecurity10.ComputerName=="PENDINGSEC10"&&migratedSecurity10.ImportedXml!.Blockers.Length==0,"legacy security JSON migrates native states while preserving unrelated edits");var migratedOutput10=Xml(migratedSecurity10);Check(Node(migratedOutput10,"//u:ComputerName")=="PENDINGSEC10"&&SecurityOptions.Inspect(XmlMerge.Read(migratedOutput10)).Values.All(s=>s.Enabled),"legacy security baseline supports export with pending edits");SetSecurity10(migratedSecurity10,0);var cancelledSecurity10=PlanStore.Deserialize(PlanStore.Serialize(migratedSecurity10,true));Check(SecurityOptions.Inspect(XmlMerge.Read(Xml(cancelledSecurity10))).Values.All(s=>!s.Enabled&&!s.Ambiguous),"explicit saved security cancellation survives reload and removes known payloads");
p=Plan();Recommendations.Apply(p,"wired");SetSecurity10(p,15);Check(SecurityOptions.Items.All(i=>Recommendations.Differences(p).Any(d=>d.Property==i.Property)),"security overrides appear in recommendation differences");Recommendations.Restore(p,nameof(InstallPlan.DisableSac));Check(!p.DisableSac&&p.DisableSmartScreen,"single recommendation restore only clears chosen security override");Recommendations.Apply(p,"wifi");Check(SecurityOptions.Items.All(i=>!SecurityOptions.Selected(p,i)),"full recommendation keeps native security defaults");
viewModel.Plan=Plan();viewModel.Preview="stale10";viewModel.Plan.DisableCoreIsolation=true;Check(viewModel.Preview!="stale10"&&viewModel.ConfigurationSummary.Contains("关闭 VBS")&&viewModel.HelpTips["coreisolation"].Body.Contains("关闭此项"),"security toggle updates help, summary and invalidates preview");Check(SecurityOptions.Description(nameof(InstallPlan.DisableSac)).Contains("重置")&&SecurityOptions.Description(nameof(InstallPlan.DisableSmartScreen)).Contains("托盘")&&SecurityOptions.Description(nameof(InstallPlan.HardenSystemDriveAcl)).Contains("不递归"),"security help explains recovery, tray side effect and permission scope");var groupedSecurity10=UnattendLocal.Desktop.SummaryPresentation.FromText(Recommendations.Summary(viewModel.Plan));Check(groupedSecurity10.Sections.Single(s=>s.Kind=="protection").Rows.Any(row=>row.SourceLabel=="核心隔离"),"security summary belongs to protection group");
Console.WriteLine($"Final V0.10 total: {count} tests passed.");
// V0.11: retain the window scroll fix; native configuration-set and AppLocker integration.
Check(!InstallPlan.New().UseAppLocker&&!InstallPlan.New().UseConfigurationSet&&!InstallPlan.NewCustom().UseAppLocker&&!InstallPlan.NewCustom().UseConfigurationSet,"V0.11 optional deployment settings default off");
Check(AppControlSettings.ReadPolicy(AppControlSettings.Example).DocumentElement!.Name=="AppLockerPolicy"&&AppControlSettings.Example.Contains("AuditOnly"),"AppLocker example is schema-valid audit-only policy");
foreach(string bad11 in new[]{"", "<wrong/>","<AppLockerPolicy/>","<AppLockerPolicy Version='1'><RuleCollection Type='Exe' EnforcementMode='Invalid'/></AppLockerPolicy>","<!DOCTYPE AppLockerPolicy [<!ENTITY value SYSTEM 'file:///private'>]><AppLockerPolicy Version='1'>&value;</AppLockerPolicy>","<?instruction unsafe?><AppLockerPolicy Version='1'/>",new string('x',AppControlSettings.MaxLength+1)}){
 try{_=AppControlSettings.ReadPolicy(bad11);throw new Exception("unexpected valid policy");}catch(ArgumentException){Check(true,"invalid, external-entity, processing-instruction or oversized policy rejected");}
}
p=Plan();p.AppLockerPolicyXml="inactive invalid";Check(!Xml(p).Contains("AppLockerPolicy.xml"),"inactive invalid AppLocker input does not affect generation");p.UseAppLocker=true;Reject(p,"enabled invalid AppLocker cannot generate");p.AppLockerPolicyXml="";Reject(p,"blank policy is not silently replaced by example");Check(Recommendations.MissingInputs(p).Contains("AppLocker 策略 XML"),"AppLocker missing policy appears in required inputs");
foreach(string arch11 in new[]{"amd64","arm64","x86"})foreach(bool hidden11 in new[]{false,true})foreach(int mask11 in Enumerable.Range(0,4)){
 p=Plan();p.Architecture=arch11;p.HidePowerShellWindows=hidden11;p.UseAppLocker=(mask11&1)!=0;p.UseConfigurationSet=(mask11&2)!=0;p.AppLockerPolicyXml=AppControlSettings.Example;
 var xml11=Xml(p);var d11=XmlMerge.Read(xml11);Check(AppControlSettings.Inspect(d11).Enabled==p.UseAppLocker&&!AppControlSettings.Inspect(d11).Ambiguous,"AppLocker recognizes native pipeline: "+arch11+hidden11+mask11);Check(ConfigurationSetSettings.Inspect(d11).Enabled==p.UseConfigurationSet&&!ConfigurationSetSettings.Inspect(d11).Ambiguous,"configuration-set flag remains independent");
 Check(Node(xml11,"//u:settings[@pass='windowsPE']//u:UseConfigurationSet")==p.UseConfigurationSet.ToString().ToLowerInvariant(),"configuration-set flag generated in windowsPE");
 foreach(bool metadata11 in new[]{false,true}){
  var input11=metadata11?xml11:WithoutSnapshot(xml11);var loaded11=XmlPlanImport.Import(input11,g);Check(loaded11.UseAppLocker==p.UseAppLocker&&loaded11.UseConfigurationSet==p.UseConfigurationSet&&SameXml(input11,Xml(loaded11)),"V0.11 settings restore actual XML and preserve content");loaded11.UseAppLocker=!loaded11.UseAppLocker;loaded11.AppLockerPolicyXml=AppControlSettings.Example;loaded11.UseConfigurationSet=!loaded11.UseConfigurationSet;var changed11=Xml(loaded11);Check(AppControlSettings.Inspect(XmlMerge.Read(changed11)).Enabled==!p.UseAppLocker&&ConfigurationSetSettings.Inspect(XmlMerge.Read(changed11)).Enabled==!p.UseConfigurationSet,"V0.11 settings can be added/removed from imported XML");
 }
 foreach(bool secrets11 in new[]{false,true}){var saved11=PlanStore.Deserialize(PlanStore.Serialize(p,secrets11));Check(saved11.UseAppLocker==p.UseAppLocker&&saved11.UseConfigurationSet==p.UseConfigurationSet&&saved11.AppLockerPolicyXml==p.AppLockerPolicyXml,"policy XML and toggles retain in both JSON credential modes");}
 if(p.UseAppLocker){var preview11=GeneratorService.Preview(Encoding.UTF8.GetBytes(xml11));Check(!preview11.Contains("d3b66501-7907-4ad5-9ad6-165630844817")&&preview11.Contains("AppLocker 策略 XML 已整体遮蔽"),"preview hides whole AppLocker policy including identities and paths");}
}
p=Plan();p.UseAppLocker=true;p.UseConfigurationSet=true;p.AppLockerPolicyXml=AppControlSettings.Example;var allDeployment11=Xml(p);Check(DecodedXml(allDeployment11).Contains("Set-AppLockerPolicy -XmlPolicy")&&!DecodedXml(allDeployment11).Contains(" -Merge"),"AppLocker preserves native policy replacement rather than silently merging");
var enforced11=AppControlSettings.Example.Replace("AuditOnly","Enabled");p.AppLockerPolicyXml=enforced11;Check(AppControlSettings.Inspect(XmlMerge.Read(Xml(p))).Xml.Contains("EnforcementMode=\"Enabled\""),"enforced policy is preserved without silently converting to audit");p.AppLockerPolicyXml=AppControlSettings.Example;
foreach(bool hide11 in new[]{false,true}){var native11=new Schneegans.Unattend.UnattendGenerator().GenerateXml(Schneegans.Unattend.Configuration.Default with{UseConfigurationSet=true,AppLockerSettings=new Schneegans.Unattend.ConfigureAppLockerSettings(AppControlSettings.Example),HidePowerShellWindows=hide11}).OuterXml;var imported11=XmlPlanImport.Import(native11,g);Check(imported11.UseConfigurationSet&&imported11.UseAppLocker&&SameXml(native11,Xml(imported11)),"raw native AppLocker/configuration-set XML import without application metadata");}
foreach(string mutation11 in new[]{"noPolicy","duplicatePolicy","badPolicy","alteredCommand","conditional","wrongStage","noExtraction","noInvocation"}){
 var d11=XmlMerge.Read(WithoutSnapshot(allDeployment11));var f11=d11.SelectNodes("//*[local-name()='File']")!.Cast<XmlElement>().ToArray();var policy11=f11.Single(f=>f.GetAttribute("path").EndsWith("AppLockerPolicy.xml"));var specialize11=f11.Single(f=>f.GetAttribute("path").EndsWith("\\Specialize.ps1"));
 if(mutation11=="noPolicy")policy11.ParentNode!.RemoveChild(policy11);
 else if(mutation11=="duplicatePolicy")policy11.ParentNode!.AppendChild(policy11.CloneNode(true));
 else if(mutation11=="badPolicy")policy11.InnerText="<AppLockerPolicy/>";
 else if(mutation11=="alteredCommand")specialize11.InnerText=specialize11.InnerText.Replace("Set-AppLockerPolicy -XmlPolicy","Set-AppLockerPolicy -Merge -XmlPolicy");
 else if(mutation11=="conditional")specialize11.InnerText=specialize11.InnerText.Replace("Set-AppLockerPolicy -XmlPolicy", "if ($false) { Set-AppLockerPolicy -XmlPolicy").Replace("AppLockerPolicy.xml';","AppLockerPolicy.xml'; }");
 else if(mutation11=="wrongStage")specialize11.SetAttribute("path",@"C:\Windows\Setup\Scripts\UncalledPolicy11.ps1");
 else if(mutation11=="noExtraction")d11.SelectSingleNode("//*[local-name()='ExtractScript']")!.InnerText="Write-Output 'not extracted';";
 else foreach(XmlElement call11 in d11.SelectNodes("//*[local-name()='Path']")!)if(call11.InnerText.Contains("-File")&&call11.InnerText.Contains("Specialize.ps1"))call11.InnerText=call11.InnerText.Replace("Specialize.ps1","Uncalled11.ps1");
 var imported11=XmlPlanImport.Import(d11.OuterXml,g);Check(!imported11.UseAppLocker&&AppControlSettings.Inspect(d11).Ambiguous&&SameXml(d11.OuterXml,Xml(imported11)),"unknown AppLocker original preserved: "+mutation11);imported11.UseAppLocker=true;imported11.AppLockerPolicyXml=AppControlSettings.Example;Reject(imported11,"unknown AppLocker cannot be overwritten by new controls");
}
var scriptedSet11=XmlMerge.Read(WithoutSnapshot(Xml(Plan())));var scriptOwner11=scriptedSet11.SelectSingleNode("//*[local-name()='File']")!;scriptOwner11.InnerText+="\nWrite-Output 'ROBOCOPY_ARGS';";var scriptedPlan11=XmlPlanImport.Import(scriptedSet11.OuterXml,g);Check(!scriptedPlan11.UseConfigurationSet&&ConfigurationSetSettings.Inspect(scriptedSet11).Ambiguous&&SameXml(scriptedSet11.OuterXml,Xml(scriptedPlan11)),"custom configuration-set copy script preserved without false editable flag");scriptedPlan11.UseConfigurationSet=true;Reject(scriptedPlan11,"custom PE configuration copy script cannot be controlled by simple flag");
var legacyDeployment11=XmlPlanImport.Import(allDeployment11,g);legacyDeployment11.UseAppLocker=legacyDeployment11.UseConfigurationSet=false;legacyDeployment11.AppLockerPolicyXml="";var oldDeploymentBase11=PlanStore.Deserialize(PlanStore.Serialize(legacyDeployment11,true));oldDeploymentBase11.ImportedXml=null;legacyDeployment11.ImportedXml!.BaselineXml=Encoding.UTF8.GetString(g.GenerateStandalone(oldDeploymentBase11));legacyDeployment11.ComputerName="PENDING11";var legacyDeploymentJson11=System.Text.Json.Nodes.JsonNode.Parse(PlanStore.Serialize(legacyDeployment11,true))!.AsObject();foreach(string name11 in new[]{nameof(InstallPlan.UseAppLocker),nameof(InstallPlan.AppLockerPolicyXml),nameof(InstallPlan.UseConfigurationSet)})legacyDeploymentJson11.Remove(name11);var migratedDeployment11=PlanStore.Deserialize(legacyDeploymentJson11.ToJsonString());Check(migratedDeployment11.UseAppLocker&&migratedDeployment11.UseConfigurationSet&&migratedDeployment11.ImportedXml!.Blockers.Length==0&&Node(Xml(migratedDeployment11),"//u:ComputerName")=="PENDING11","legacy imported deployment choices migrate baselines and preserve pending edits");migratedDeployment11.UseAppLocker=migratedDeployment11.UseConfigurationSet=false;var savedOff11=PlanStore.Deserialize(PlanStore.Serialize(migratedDeployment11,true));Check(!AppControlSettings.Inspect(XmlMerge.Read(Xml(savedOff11))).Enabled&&!ConfigurationSetSettings.Inspect(XmlMerge.Read(Xml(savedOff11))).Enabled,"explicit cancellation survives save and removes only known deployment settings");
p=Plan();Recommendations.Apply(p,"wired");p.UseAppLocker=p.UseConfigurationSet=true;p.AppLockerPolicyXml=AppControlSettings.Example;Check(Recommendations.Differences(p).Any(d=>d.Property==nameof(InstallPlan.UseAppLocker))&&Recommendations.Differences(p).Any(d=>d.Property==nameof(InstallPlan.UseConfigurationSet)),"deployment choices appear in recommendation differences");Recommendations.Apply(p,"wifi");Check(!p.UseAppLocker&&!p.UseConfigurationSet&&p.AppLockerPolicyXml==AppControlSettings.Example,"recommendations disable deployment overrides and retain personal policy text");
viewModel.Plan=Plan();viewModel.Preview="old11";viewModel.Plan.UseAppLocker=true;Check(viewModel.Preview!="old11"&&viewModel.ConfigurationSummary.Contains("策略 XML 待补充")&&viewModel.HelpTips["applocker"].Body.Contains("可能替换"),"AppLocker updates help, summary, validation hints and invalidates preview");viewModel.Plan.UseConfigurationSet=true;Check(viewModel.HelpTips["configset"].Body.Contains("另行")&&viewModel.ConfigurationSummary.Contains("$OEM$ 配置集"),"configuration-set help and summary are linked");
Console.WriteLine($"Final V0.11 total: {count} tests passed.");

// V0.12: search uses static metadata, batch operations keep hidden choices, copies never execute by accident.
var search12=UnattendLocal.Desktop.SettingsSearchCatalog.All;
Check(search12.Count>200&&search12.All(r=>r.Title.Length>0&&r.PageIndex>=0&&r.PageIndex<15&&r.Target.Length>0),"search catalog covers all setting pages");
foreach(var term12 in new[]{"$OEM$","UseConfigurationSet","configuration set","AppLocker","BitLocker","自动登录","ComputerName","DeleteJunctions","静音","RemovePaint","PowerShell","粘滞键"})
 Check(UnattendLocal.Desktop.SettingsSearchCatalog.Search(term12).Count>0,"search finds bilingual aliases: "+term12);
Check(UnattendLocal.Desktop.SettingsSearchCatalog.Search("＄ＯＥＭ＄").Any(r=>r.Aliases.Contains("UseConfigurationSet")),"full width search normalization");
Check(UnattendLocal.Desktop.SettingsSearchCatalog.Search("OEM 配置集").Any(r=>r.Page=="镜像与分区"),"search requires all keywords and identifies page");
Check(UnattendLocal.Desktop.SettingsSearchCatalog.Search("nonexistent-setting-unique-12").Count==0,"search empty state");
Check(UnattendLocal.Desktop.SettingsSearchCatalog.Search("   ").Count==search12.Count,"blank search shows metadata catalog");
Check(UnattendLocal.Desktop.SettingsSearchCatalog.Search("$OEM$").First().Aliases.Contains("UseConfigurationSet"),"direct setting label ranks ahead of description matches");
viewModel.Plan=Plan();viewModel.Plan.Users[0].Password="PrivateSearch12!";viewModel.Plan.ComputerNameScript="return 'PrivateScript12';";var beforeSearch12=PlanStore.Serialize(viewModel.Plan,true);_=UnattendLocal.Desktop.SettingsSearchCatalog.Search("Password");Check(PlanStore.Serialize(viewModel.Plan,true)==beforeSearch12&&!search12.Any(r=>(r.Title+r.Description+r.Aliases).Contains("PrivateSearch12"))&&UnattendLocal.Desktop.SettingsSearchCatalog.Search("PrivateScript12").Count==0,"search neither reads private values nor mutates plan");
viewModel.Plan=Plan();viewModel.AppRemovalFilter="Paint";var shown12=viewModel.FilteredAppRemovalChoices.Select(c=>c.Id).ToArray();var hidden12=viewModel.AppRemovalChoices.First(c=>!shown12.Contains(c.Id));hidden12.IsSelected=true;viewModel.Preview="old-batch12";viewModel.SelectFilteredApps(true);Check(shown12.Length>0&&shown12.All(id=>viewModel.Plan.RemovedApps.Contains(id))&&hidden12.IsSelected&&viewModel.Preview!="old-batch12","batch selects matching items and invalidates preview without changing hidden choices");viewModel.SelectFilteredApps(false);Check(shown12.All(id=>!viewModel.Plan.RemovedApps.Contains(id))&&hidden12.IsSelected,"batch clear retains hidden selection");viewModel.AppRemovalFilter="no-match-12";var beforeApps12=string.Join(',',viewModel.Plan.RemovedApps);viewModel.SelectFilteredApps(true);Check(string.Join(',',viewModel.Plan.RemovedApps)==beforeApps12,"empty filter result batch is no-op");viewModel.AppRemovalFilter="";viewModel.SelectFilteredApps(false);Check(viewModel.Plan.RemovedApps.Count==0,"empty query intentionally covers all items");
var batchRestore12=PlanStore.Deserialize(PlanStore.Serialize(viewModel.Plan,true));Check(batchRestore12.RemovedApps.Count==0,"batch choices use existing plan persistence");
foreach(var phase12 in new[]{"System","DefaultUser","FirstLogon","UserOnce"}){
 p=Plan();var source12=new ScriptEntry{Phase=phase12,Type="Ps1",Content="Write-Output 'copy-test-12';",Enabled=true};p.Scripts.Add(source12);var copy12=CustomScripts.Duplicate(p,source12);
 Check(p.Scripts.IndexOf(copy12)==1&&!ReferenceEquals(copy12,source12)&&!copy12.Enabled&&copy12.Content==source12.Content&&copy12.Phase==phase12&&copy12.Type==source12.Type,"script copy preserves values, follows source and starts disabled: "+phase12);
 copy12.Content="Write-Output 'copy-edited-12';";Check(source12.Content!="Write-Output 'copy-edited-12';"&&!DecodedXml(Xml(p)).Contains("copy-edited-12"),"copy independent edits and disabled generation");
 var restore12=PlanStore.Deserialize(PlanStore.Serialize(p,true));Check(restore12.Scripts.Count==2&&!restore12.Scripts[1].Enabled&&restore12.Scripts[1].Content==copy12.Content,"copied script saves unchanged while disabled");
}
p=Plan();var removedScript12=new ScriptEntry{Type="Js",Content="legacy",Enabled=false};p.Scripts.Add(removedScript12);Check(!CustomScripts.Duplicate(p,removedScript12).Enabled,"legacy unsupported script copies stay disabled");
p=Plan();p.Scripts.Add(new ScriptEntry());while(p.Scripts.Count<60)CustomScripts.Duplicate(p,p.Scripts[0]);try{CustomScripts.Duplicate(p,p.Scripts[0]);Check(false,"copy limit");}catch(ArgumentException){Check(p.Scripts.Count==60,"script copy honors existing 60 row limit");}
try{CustomScripts.Duplicate(p,new ScriptEntry());Check(false,"foreign script");}catch(ArgumentException){Check(true,"script copy rejects foreign row");}
viewModel.Plan=Plan();Check(viewModel.DuplicateSelectedScript()==null,"copy without selection is no-op");var originalScript12=new ScriptEntry{Content="Write-Output 'vm12';"};viewModel.Plan.Scripts.Add(originalScript12);viewModel.SelectedScript=originalScript12;viewModel.Preview="old-copy12";var copiedVm12=viewModel.DuplicateSelectedScript();Check(ReferenceEquals(viewModel.SelectedScript,copiedVm12)&&copiedVm12?.Enabled==false&&viewModel.Preview!="old-copy12","view model selects disabled copy and invalidates preview");

string ScriptBody12(string text,string stage)=>XmlMerge.Read(text).SelectNodes("//*[local-name()='File']")!.Cast<XmlElement>().Single(f=>f.GetAttribute("path").EndsWith(stage,StringComparison.Ordinal)).InnerText;
Check(!InstallPlan.New().DeleteJunctions&&!InstallPlan.NewCustom().DeleteJunctions&&!PlanStore.Deserialize("{\"SchemaVersion\":1,\"Users\":[]}").DeleteJunctions,"junction removal defaults off in new and old plans");
foreach(var arch12 in new[]{"amd64","arm64","x86"})foreach(bool hide12 in new[]{false,true}){
 p=Plan();p.Architecture=arch12;p.HidePowerShellWindows=hide12;p.DeleteJunctions=true;var junctionXml12=Xml(p);var inspected12=JunctionSettings.Inspect(XmlMerge.Read(junctionXml12))[nameof(InstallPlan.DeleteJunctions)];Check(inspected12.Enabled&&!inspected12.Ambiguous,"native junction stages recognized: "+arch12+hide12);

 foreach(var stage12 in new[]{"FirstLogon.ps1","UserOnce.ps1"})Check(ScriptBody12(junctionXml12,stage12).Contains("ReparsePoint")&&ScriptBody12(junctionXml12,stage12).Contains("Remove-Item -Force -Recurse -Verbose"),"junction payload kept in native stage "+stage12);
 foreach(bool snapshot12 in new[]{false,true}){
  var input12=snapshot12?junctionXml12:WithoutSnapshot(junctionXml12);var restored12=XmlPlanImport.Import(input12,g);Check(restored12.DeleteJunctions&&SameXml(input12,Xml(restored12)),"junction import retains editable native stages");restored12.DeleteJunctions=false;Check(!DecodedXml(Xml(restored12)).Contains("ReparsePoint"),"junction removal can be cancelled after import");
 }
 foreach(bool secrets12 in new[]{false,true})Check(PlanStore.Deserialize(PlanStore.Serialize(p,secrets12)).DeleteJunctions,"junction choice survives both JSON modes");
}
p=Plan();p.DeleteJunctions=true;var enabledJunction12=WithoutSnapshot(Xml(p));
foreach(var mutation12 in new[]{"changed","duplicate","wrong-stage","no-call","no-extraction","no-user-registration"}){
 var doc12=XmlMerge.Read(enabledJunction12);var file12=doc12.SelectNodes("//*[local-name()='File']")!.Cast<XmlElement>().Single(f=>f.GetAttribute("path").EndsWith("FirstLogon.ps1"));
 if(mutation12=="changed")file12.InnerText=file12.InnerText.Replace("Remove-Item -Force -Recurse -Verbose","Remove-Item -WhatIf");
 else if(mutation12=="duplicate")file12.ParentNode!.AppendChild(file12.CloneNode(true));
 else if(mutation12=="wrong-stage")file12.SetAttribute("path",file12.GetAttribute("path").Replace("FirstLogon.ps1","ChangedPhase12.ps1"));
 else if(mutation12=="no-extraction"){var extract12=doc12.SelectSingleNode("//*[local-name()='ExtractScript']")!;extract12.ParentNode!.RemoveChild(extract12);}
 else if(mutation12=="no-user-registration"){var default12=doc12.SelectNodes("//*[local-name()='File']")!.Cast<XmlElement>().Single(f=>f.GetAttribute("path").EndsWith("DefaultUser.ps1"));default12.InnerText=default12.InnerText.Replace("UserOnce.ps1","UncalledUser12.ps1");}
 else foreach(XmlElement call12 in doc12.SelectNodes("//*[local-name()='CommandLine']")!)if(call12.InnerText.Contains("FirstLogon.ps1"))call12.InnerText=call12.InnerText.Replace("FirstLogon.ps1","Uncalled12.ps1");
 var imported12=XmlPlanImport.Import(doc12.OuterXml,g);Check(!imported12.DeleteJunctions&&JunctionSettings.Inspect(doc12)[nameof(InstallPlan.DeleteJunctions)].Ambiguous&&SameXml(doc12.OuterXml,Xml(imported12)),"unknown junction flow kept intact: "+mutation12);imported12.DeleteJunctions=true;Reject(imported12,"unknown junction flow blocks overlay: "+mutation12);
}
var legacy12=XmlPlanImport.Import(enabledJunction12,g);legacy12.DeleteJunctions=false;var legacyBase12=PlanStore.Deserialize(PlanStore.Serialize(legacy12,true));legacyBase12.ImportedXml=null;legacy12.ImportedXml!.BaselineXml=Encoding.UTF8.GetString(g.GenerateStandalone(legacyBase12));legacy12.ComputerName="PENDING12";var legacyJson12=System.Text.Json.Nodes.JsonNode.Parse(PlanStore.Serialize(legacy12,true))!.AsObject();legacyJson12.Remove(nameof(InstallPlan.DeleteJunctions));var migrated12=PlanStore.Deserialize(legacyJson12.ToJsonString());Check(migrated12.DeleteJunctions&&migrated12.ImportedXml!.Blockers.Length==0&&Node(Xml(migrated12),"//u:ComputerName")=="PENDING12","legacy junction baseline migration retains pending edits");migrated12.DeleteJunctions=false;Check(!PlanStore.Deserialize(PlanStore.Serialize(migrated12,true)).DeleteJunctions&&!DecodedXml(Xml(migrated12)).Contains("ReparsePoint"),"explicit saved cancellation survives migration");
p=Plan();p.DeleteJunctions=true;Check(Recommendations.Differences(p).Any(d=>d.Property==nameof(InstallPlan.DeleteJunctions)),"junction choice appears in recommendation differences");Recommendations.Restore(p,nameof(InstallPlan.DeleteJunctions));Check(!p.DeleteJunctions,"single recommendation restores junction default");p.DeleteJunctions=true;Recommendations.Apply(p,"wired");Check(!p.DeleteJunctions,"full recommendation restores junction default");viewModel.Plan=Plan();viewModel.Preview="old12";viewModel.Plan.DeleteJunctions=true;Check(viewModel.Preview!="old12"&&viewModel.ConfigurationSummary.Contains("兼容联接点")&&viewModel.HelpTips["junctions"].Body.Contains("ReparsePoint"),"junction summary, help and preview stay linked");
Console.WriteLine($"V0.12 total: {count} checks passed.");

// V0.13: source-form declarations never stand in for actual payloads or installed-app inventory.
string WithForm13(string input,string query,bool slash=true){var doc13=XmlMerge.Read(WithoutSnapshot(input));foreach(XmlComment comment13 in doc13.SelectNodes("//comment()")!.Cast<XmlComment>().Where(c=>c.Value?.StartsWith("https://schneegans.de/windows/unattend-generator")==true).ToArray())comment13.ParentNode!.RemoveChild(comment13);doc13.DocumentElement!.PrependChild(doc13.CreateComment("https://schneegans.de/windows/unattend-generator"+(slash?"/":"")+"?"+query));return doc13.OuterXml;}
bool ParameterNotice13(InstallPlan input,string key)=>input.ImportedXml!.Notices.Any(n=>n.Contains("参数："+key+"；",StringComparison.Ordinal));
var allAppPlan13=Plan();foreach(var item13 in AppRemovalSettings.Items)allAppPlan13.RemovedApps.Add(item13.Id);var allAppXml13=WithoutSnapshot(Xml(allAppPlan13));var allAppQuery13=string.Join('&',AppRemovalSettings.Items.Select(i=>i.Id+"=true"));
foreach(bool slash13 in new[]{false,true}){
 var input13=WithForm13(allAppXml13,allAppQuery13,slash13);var loaded13=XmlPlanImport.Import(input13,g);
 Check(loaded13.RemovedApps.Order().SequenceEqual(allAppPlan13.RemovedApps.Order())&&!loaded13.ImportedXml!.AppRemovalReadOnly,"supported app parameters backed by scripts restore all choices, either URL spelling");
 Check(SameXml(input13,Xml(loaded13)),"form diagnostics preserve actual app-removal XML");
 foreach(var item13 in AppRemovalSettings.Items)Check(!ParameterNotice13(loaded13,item13.Id),"implemented app is not labeled unsupported: "+item13.Id);
}
var benign13=WithForm13(Xml(Plan()),"LockKeysMode=Skip&PartitionMode=Interactive&DiskAssertionMode=Skip&InstallFromMode=Automatic&WdacMode=Skip&CompactOsMode=Default");var benignPlan13=XmlPlanImport.Import(benign13,g);Check(new[]{"LockKeysMode","PartitionMode","DiskAssertionMode","InstallFromMode","WdacMode","CompactOsMode"}.All(key=>!ParameterNotice13(benignPlan13,key)),"inactive/default mode aliases are not reported as missing features");Check(benignPlan13.LockKeyMode==KeyboardLockSettings.Default&&benignPlan13.DiskMode==InstallationSettings.Manual,"inactive mode comments do not change actual settings");
p=Plan();p.CompactOsMode=CompactOsSettings.Enabled;var compact13=WithForm13(Xml(p),"CompactOsMode=Always");var compactPlan13=XmlPlanImport.Import(compact13,g);Check(compactPlan13.CompactOsMode==CompactOsSettings.Enabled&&!ParameterNotice13(compactPlan13,"CompactOsMode")&&!compactPlan13.ImportedXml!.Notices.Any(n=>n.StartsWith("CompactOS 生成参数")),"CompactOS alias is reconciled with actual native field");
var contradictory13=XmlPlanImport.Import(WithForm13(Xml(Plan()),"RemoveCalculator=true&CompactOsMode=Always"),g);Check(!contradictory13.RemovedApps.Contains("RemoveCalculator")&&contradictory13.ImportedXml!.Notices.Any(n=>n.StartsWith("应用移除生成参数与实际脚本不一致")),"comment-only app selection is not applied and discrepancy stays visible");Check(contradictory13.CompactOsMode==CompactOsSettings.Default&&contradictory13.ImportedXml!.Notices.Any(n=>n.StartsWith("CompactOS 生成参数与实际安装配置不一致")),"comment-only CompactOS does not override actual installation fields");
var unsupported13=XmlPlanImport.Import(WithForm13(Xml(Plan()),"RemoveFutureApp13=true&PartitionMode=FutureMode13&AccountPassword0=private13&FutureUnsupported13=true"),g);Check(ParameterNotice13(unsupported13,"RemoveFutureApp13")&&ParameterNotice13(unsupported13,"PartitionMode")&&ParameterNotice13(unsupported13,"FutureUnsupported13")&&unsupported13.ImportedXml!.Notices.All(n=>!n.Contains("private13")&&!n.Contains("FutureMode13")),"unknown active settings remain visible by name without disclosing values");
var duplicateForm13=XmlPlanImport.Import(WithForm13(Xml(Plan()),"RemoveCalculator=true&RemoveCalculator=false"),g);Check(duplicateForm13.ImportedXml!.Notices.Any(n=>n.Contains("重复项：RemoveCalculator")),"duplicate implemented parameters retain ambiguity notice");
var invalidBool13=XmlPlanImport.Import(WithForm13(Xml(Plan()),"RemoveCalculator=unknown13"),g);Check(invalidBool13.ImportedXml!.Notices.Any(n=>n.Contains("取值无法识别"))&&!invalidBool13.RemovedApps.Any(),"invalid app boolean is reported without applying it");
var oldScript13=WithForm13(allAppXml13,allAppQuery13).Replace("RemovePackage.ps1","RemovePackages.ps1").Replace("RemoveCapability.ps1","RemoveCapabilities.ps1").Replace("RemoveFeature.ps1","RemoveFeatures.ps1");var oldPlan13=XmlPlanImport.Import(oldScript13,g);Check(oldPlan13.ImportedXml!.AppRemovalReadOnly&&oldPlan13.ImportedXml.Notices.Any(n=>n.Contains("原文件的生成参数声明选择了 60 项"))&&!ParameterNotice13(oldPlan13,"RemoveCalculator"),"different removal script format is labeled preserved/read-only rather than feature unimplemented");Check(SameXml(oldScript13,Xml(oldPlan13))&&AppRemovalSettings.Selection(oldPlan13).Contains("原 XML 移除脚本保留"),"readonly source scripts still export intact and summary explains preservation");
viewModel.Plan=oldPlan13;Check(!viewModel.CanEditAppRemoval,"unrecognized source disables app checkbox editing");try{viewModel.SelectFilteredApps(true);Check(false,"readonly batch");}catch(ArgumentException){Check(true,"readonly source rejects batch selection");}
var savedReadOnly13=PlanStore.Deserialize(PlanStore.Serialize(oldPlan13,true));Check(savedReadOnly13.ImportedXml!.AppRemovalReadOnly&&SameXml(oldScript13,Xml(savedReadOnly13)),"read-only state derives from source after save/reload");
var stale13=XmlPlanImport.Import(compact13,g);stale13.ComputerName="PENDING13";stale13.ImportedXml!.Notices=stale13.ImportedXml.Notices.Concat(new[]{"界面尚未实现或无法完整还原的生成参数：RemoveCalculator；原 XML 保留。","界面尚未实现或无法完整还原的生成参数：CompactOsMode；原 XML 保留。","生成参数暂不能完全还原：LockKeysMode；相关 XML 原文保留。","界面尚未实现或无法完整还原的生成参数：UnknownLegacy13；原 XML 保留。"}).ToArray();
// Add corresponding original declarations while preserving pending plan changes and baseline.
var staleSource13=XmlMerge.Read(stale13.ImportedXml.SourceXml);var formComment13=staleSource13.SelectNodes("//comment()")!.Cast<XmlComment>().First(c=>c.Value?.StartsWith("https://schneegans.de")==true);formComment13.Value+="&RemoveCalculator=false&LockKeysMode=Skip";stale13.ImportedXml.SourceXml=staleSource13.OuterXml;var staleBaseline13=stale13.ImportedXml.BaselineXml;var migrated13=PlanStore.Deserialize(PlanStore.Serialize(stale13,true));Check(migrated13.ComputerName=="PENDING13"&&migrated13.ImportedXml!.BaselineXml==staleBaseline13&&!ParameterNotice13(migrated13,"RemoveCalculator")&&!ParameterNotice13(migrated13,"CompactOsMode")&&!ParameterNotice13(migrated13,"LockKeysMode")&&ParameterNotice13(migrated13,"UnknownLegacy13"),"old saved plans remove only stale recognized notices without losing pending edits or unknown notes");
var importVm13=new UnattendLocal.Desktop.MainViewModel();importVm13.Plan=Plan();importVm13.Plan.Users[0].Password="KeepCurrent13!";importVm13.Plan.ComputerName="CURRENT13";importVm13.Preview="retain-preview13";var beforePlan13=importVm13.Plan;var beforeJson13=PlanStore.Serialize(beforePlan13,true);int confirmations13=0;
Check(!importVm13.ImportXml(allAppXml13,()=>{confirmations13++;return false;})&&confirmations13==1&&ReferenceEquals(beforePlan13,importVm13.Plan)&&PlanStore.Serialize(importVm13.Plan,true)==beforeJson13&&importVm13.Preview=="retain-preview13"&&importVm13.Status.Contains("已取消 XML 导入"),"declining import preserves current plan, credentials, preview and gives cancellation feedback");
try{_=importVm13.ImportXml("<unattend>",()=>{confirmations13++;return true;});Check(false,"invalid import");}catch(Exception failure13)when(failure13 is ArgumentException or XmlException){Check(confirmations13==1&&ReferenceEquals(beforePlan13,importVm13.Plan)&&PlanStore.Serialize(importVm13.Plan,true)==beforeJson13&&importVm13.Status.Contains("XML 导入失败"),"parse failure leaves current configuration unchanged and never asks replacement confirmation");}
Check(importVm13.ImportXml(allAppXml13,()=>true)&&!ReferenceEquals(beforePlan13,importVm13.Plan)&&importVm13.Plan.RemovedApps.Count==AppRemovalSettings.Items.Count&&importVm13.Status.StartsWith("XML 已导入"),"accepted import commits candidate and reports success");
Check(importVm13.ImportXml(oldScript13,()=>true)&&!importVm13.CanEditAppRemoval&&importVm13.Plan.ImportedXml!.SourceXml.Length>0,"accepting different script format retains full source and readonly feedback");
Console.WriteLine($"V0.13 total: {count} checks passed.");

// V0.14: strictly recognized historical app-removal flow and atomic migration.
string Legacy14(InstallPlan plan)=>LegacyRemovalFixture.FromCurrent(Xml(plan));
string Body14(XmlDocument d,string name)=>d.SelectNodes("//*[local-name()='File']")!.Cast<XmlElement>().Single(f=>f.GetAttribute("path").EndsWith("\\"+name,StringComparison.Ordinal)).InnerText;
XmlElement File14(XmlDocument d,string name)=>d.SelectNodes("//*[local-name()='File']")!.Cast<XmlElement>().Single(f=>f.GetAttribute("path").EndsWith("\\"+name,StringComparison.Ordinal));
foreach(var item14 in AppRemovalSettings.Items){
 p=Plan();p.RemovedApps.Add(item14.Id);var old14=Legacy14(p);var imported14=XmlPlanImport.Import(old14,g);
 Check(!imported14.ImportedXml!.AppRemovalReadOnly&&imported14.RemovedApps.SequenceEqual(new[]{item14.Id}),"legacy actual single selection: "+item14.Id);
 Check(SameXml(old14,Xml(imported14)),"legacy unedited roundtrip: "+item14.Id);
 imported14.RemovedApps.Clear();var cleared14=Xml(imported14);var clearState14=AppRemovalSettings.Inspect(XmlMerge.Read(cleared14));
 Check(!clearState14.Ambiguous&&clearState14.Selected.Length==0&&!cleared14.Contains("RemovePackages.ps1")&&!cleared14.Contains("RemoveCapabilities.ps1")&&!cleared14.Contains("RemoveFeatures.ps1"),"legacy removal can be cleared without leftover helper calls: "+item14.Id);
 string extra14=item14.Id=="RemoveCalculator"?"RemovePhotos":"RemoveCalculator";imported14.RemovedApps.Add(item14.Id);imported14.RemovedApps.Add(extra14);var added14=Xml(imported14);var addedState14=AppRemovalSettings.Inspect(XmlMerge.Read(added14));
 Check(!addedState14.Ambiguous&&addedState14.Selected.Order().SequenceEqual(new[]{item14.Id,extra14}.Order()),"legacy add converts complete recognized flow: "+item14.Id);
}
foreach(var arch14 in new[]{"amd64","arm64","x86"})foreach(bool hide14 in new[]{false,true}){
 p=Plan();p.Architecture=arch14;p.HidePowerShellWindows=hide14;p.RemovedApps=new(AppRemovalSettings.Items.Select(i=>i.Id));p.Scripts.Add(new(){Enabled=true,Phase="System",Type="Ps1",Content="Write-Output 'preserve-extra14';"});
 var old14=Legacy14(p);var imported14=XmlPlanImport.Import(old14,g);Check(imported14.RemovedApps.Count==60&&!imported14.ImportedXml!.AppRemovalReadOnly,"legacy all items and active pipeline: "+arch14+hide14);
 Check(SameXml(old14,Xml(imported14)),"legacy all items unchanged exact tree: "+arch14+hide14);
 var saved14=PlanStore.Deserialize(PlanStore.Serialize(imported14,true));Check(saved14.RemovedApps.Count==60&&!saved14.ImportedXml!.AppRemovalReadOnly&&SameXml(old14,Xml(saved14)),"legacy saved plan retains recognized selection and source: "+arch14+hide14);
 imported14.RemovedApps.Remove("RemoveOneDrive");imported14.RemovedApps.Remove("RemoveMediaFeatures");var edited14=Xml(imported14);var editedPlan14=XmlPlanImport.Import(edited14,g);
 Check(editedPlan14.RemovedApps.Count==58&&!editedPlan14.RemovedApps.Contains("RemoveOneDrive")&&!editedPlan14.RemovedApps.Contains("RemoveMediaFeatures")&&edited14.Contains("preserve-extra14"),"legacy edit removes selectors and all custom side effects while preserving extra command: "+arch14+hide14);
 Check(!edited14.Contains("OneDriveSetup")&&!edited14.Contains("MediaPlayback")&&SameXml(edited14,Xml(editedPlan14)),"legacy edited output reimports without residual commands: "+arch14+hide14);
}
p=Plan();p.RemovedApps=new(new[]{"RemoveCalculator","RemoveMathInputPanel","RemoveRecall","RemoveOneDrive","RemoveCopilot","RemoveTeams","RemoveXboxApps"});var mixedLegacy14=Legacy14(p);var legacyDoc14=XmlMerge.Read(mixedLegacy14);
var corruptions14=new Dictionary<string,Action<XmlDocument>>{
 ["modified helper"]=d=>File14(d,"RemovePackages.ps1").InnerText+="\nWrite-Output 'changed';",
 ["unknown selector"]=d=>File14(d,"RemovePackages.ps1").InnerText=Body14(d,"RemovePackages.ps1").Replace("'Microsoft.WindowsCalculator';","'Unknown.App14';"),
 ["duplicate selector"]=d=>File14(d,"RemovePackages.ps1").InnerText=Body14(d,"RemovePackages.ps1").Replace("'Microsoft.WindowsCalculator';","'Microsoft.WindowsCalculator';\n\t'Microsoft.WindowsCalculator';"),
 ["missing helper"]=d=>File14(d,"RemovePackages.ps1").ParentNode!.RemoveChild(File14(d,"RemovePackages.ps1")),
 ["duplicate helper"]=d=>File14(d,"RemovePackages.ps1").ParentNode!.AppendChild(File14(d,"RemovePackages.ps1").CloneNode(true)),
 ["missing call"]=d=>File14(d,"Specialize.ps1").InnerText=Body14(d,"Specialize.ps1").Replace("& 'C:\\Windows\\Setup\\Scripts\\RemovePackages.ps1';","Write-Output 'skipped';"),
 ["conditional call"]=d=>File14(d,"Specialize.ps1").InnerText=Body14(d,"Specialize.ps1").Replace("& 'C:\\Windows\\Setup\\Scripts\\RemovePackages.ps1';","if($false){ & 'C:\\Windows\\Setup\\Scripts\\RemovePackages.ps1'; }"),
 ["duplicate call"]=d=>File14(d,"Specialize.ps1").InnerText=Body14(d,"Specialize.ps1").Replace("& 'C:\\Windows\\Setup\\Scripts\\RemovePackages.ps1';","& 'C:\\Windows\\Setup\\Scripts\\RemovePackages.ps1';\n\t\t& 'C:\\Windows\\Setup\\Scripts\\RemovePackages.ps1';"),
 ["inactive stage"]=d=>{foreach(var n in d.SelectNodes("//*[local-name()='Path']")!.Cast<XmlElement>().Where(n=>n.InnerText.Contains("-File \"C:\\Windows\\Setup\\Scripts\\Specialize.ps1\"")))n.InnerText="cmd.exe /c echo skipped";},
 ["missing extraction"]=d=>{var n=d.SelectSingleNode("//*[local-name()='ExtractScript']")!;n.ParentNode!.RemoveChild(n);},
 ["changed wrapper"]=d=>File14(d,"Specialize.ps1").InnerText=Body14(d,"Specialize.ps1").Replace("& $script;","if($false){ & $script; }"),
 ["changed one drive"]=d=>File14(d,"Specialize.ps1").InnerText=Body14(d,"Specialize.ps1").Replace(" -ErrorAction 'Continue';"," -ErrorAction 'SilentlyContinue';"),
 ["missing default hive chain"]=d=>{foreach(var n in d.SelectNodes("//*[local-name()='Path']")!.Cast<XmlElement>().Where(n=>n.InnerText.Contains("reg.exe load")))n.InnerText="cmd.exe /c echo skipped";},
 ["missing user registration"]=d=>File14(d,"DefaultUser.ps1").InnerText=Body14(d,"DefaultUser.ps1").Replace("UnattendedSetup","ChangedRegistration14"),
};
foreach(var case14 in corruptions14){var bad14=(XmlDocument)legacyDoc14.CloneNode(true);case14.Value(bad14);var badPlan14=XmlPlanImport.Import(bad14.OuterXml,g);Check(badPlan14.ImportedXml!.AppRemovalReadOnly,"legacy unsafe flow remains read-only: "+case14.Key);badPlan14.RemovedApps.Add("RemovePhotos");Reject(badPlan14,"legacy unsafe flow cannot be overwritten: "+case14.Key);}
// Simulate V0.13's partial view/baseline without embedding any private XML.
var oldSaved14=XmlPlanImport.Import(mixedLegacy14,g);oldSaved14.RemovedApps.Clear();oldSaved14.ImportedXml!.AppRemovalImportVersion=0;oldSaved14.ImportedXml.AppRemovalReadOnly=true;
var beforeMigration14=PlanStore.Deserialize(PlanStore.Serialize(Plan(),true));beforeMigration14.RemovedApps.Clear();oldSaved14.ImportedXml.BaselineXml=Encoding.UTF8.GetString(g.GenerateStandalone(beforeMigration14));
oldSaved14.ComputerName="PENDING14";oldSaved14.ImportedXml.Notices=new[]{AppRemovalSettings.ReadOnlyDescription,"原文件的生成参数声明选择了 7 项应用移除；这是原文件的选择记录，不代表目标系统实际安装了这些应用。原脚本保留，清单暂不可编辑。","原文保留，未完全还原到界面：RemovePackages.ps1","retain-unknown14"};
var upgraded14=PlanStore.Deserialize(PlanStore.Serialize(oldSaved14,true));Check(upgraded14.RemovedApps.Count==7&&upgraded14.ComputerName=="PENDING14"&&!upgraded14.ImportedXml!.AppRemovalReadOnly&&upgraded14.ImportedXml.AppRemovalImportVersion==2,"V0.13 saved plan upgrades selection without losing pending name");
Check(upgraded14.ImportedXml!.Notices.Contains("retain-unknown14")&&upgraded14.ImportedXml.Notices.Contains(AppRemovalSettings.LegacyDescription)&&!upgraded14.ImportedXml.Notices.Contains(AppRemovalSettings.ReadOnlyDescription)&&!upgraded14.ImportedXml.Notices.Any(n=>n.Contains("清单暂不可编辑")),"legacy migration refreshes only derived stale notices");
var upgradedXml14=Xml(upgraded14);Check(Node(upgradedXml14,"//u:ComputerName")=="PENDING14"&&Body14(XmlMerge.Read(upgradedXml14),"RemovePackages.ps1")==Body14(legacyDoc14,"RemovePackages.ps1"),"migration changes pending name while preserving untouched legacy scripts");
Check(PlanStore.Deserialize(PlanStore.Serialize(upgraded14,true)).RemovedApps.SequenceEqual(upgraded14.RemovedApps),"legacy upgrade is idempotent");
var unsafeSaved14=PlanStore.Serialize(oldSaved14,true);oldSaved14.RemovedApps.Add("RemovePhotos");var blockedUpgrade14=PlanStore.Deserialize(PlanStore.Serialize(oldSaved14,true));Check(blockedUpgrade14.RemovedApps.Contains("RemovePhotos")&&blockedUpgrade14.ImportedXml!.Blockers.Length>0,"legacy migration preserves conflicting pending selection and blocks export");Reject(blockedUpgrade14,"ambiguous old edit cannot be silently rebased");
var conflictMigration14=System.Text.Json.JsonSerializer.Deserialize<InstallPlan>(unsafeSaved14)!;conflictMigration14.ImportedXml!.BaselineXml=conflictMigration14.ImportedXml.BaselineXml.Replace("Specialize.ps1","AlteredStage14.ps1");var blockedBaseline14=PlanStore.Deserialize(PlanStore.Serialize(conflictMigration14,true));Check(blockedBaseline14.ImportedXml!.Blockers.Length>0,"legacy baseline migration conflict is explicit");


// Extra commands before/between known removal blocks and historical selector order.
p=Plan();p.RemovedApps=new(new[]{"RemoveCalculator","RemoveMathInputPanel","RemoveMediaFeatures","RemoveOneDrive","RemoveCopilot","RemoveTeams","RemoveDevHome","RemoveOutlook"});
var prefixed14=XmlMerge.Read(Legacy14(p));var stage14=File14(prefixed14,"Specialize.ps1");
stage14.InnerText=stage14.InnerText.Replace("$scripts = @(\n","$scripts = @(\n\t{\n\t\tWrite-Output 'preserve-before14';\n\t};\n");
stage14.InnerText=stage14.InnerText.Replace("\t\t& 'C:\\Windows\\Setup\\Scripts\\RemovePackages.ps1';\n\t};\n","\t\t& 'C:\\Windows\\Setup\\Scripts\\RemovePackages.ps1';\n\t};\n\t{\n\t\tWrite-Output 'preserve-between14';\n\t};\n");
var cap14=File14(prefixed14,"RemovePackages.ps1");var selectors14=System.Text.RegularExpressions.Regex.Match(cap14.InnerText,@"(?<=\$selectors = @\(\n)(?:\t'[^'\n]+';\n)+");cap14.InnerText=cap14.InnerText.Remove(selectors14.Index,selectors14.Length).Insert(selectors14.Index,string.Join('\n',selectors14.Value.TrimEnd('\n').Split('\n').Reverse())+"\n");
var withPrefix14=XmlPlanImport.Import(prefixed14.OuterXml,g);Check(!withPrefix14.ImportedXml!.AppRemovalReadOnly&&withPrefix14.RemovedApps.Count==8,"legacy extra prefix and selector order remain fully recognized");
Check(SameXml(prefixed14.OuterXml,Xml(withPrefix14)),"unchanged historical selector order and extra prefix stay intact");
withPrefix14.RemovedApps.Clear();var prefixCleared14=Xml(withPrefix14);Check(AppRemovalSettings.Inspect(XmlMerge.Read(prefixCleared14)) is {Selected.Length:0,Ambiguous:false}&&prefixCleared14.Contains("preserve-before14")&&prefixCleared14.Contains("preserve-between14"),"clear all legacy removals preserves an extra leading command");
withPrefix14.RemovedApps=new(new[]{"RemoveCalculator","RemoveTeams"});var prefixChanged14=Xml(withPrefix14);Check(AppRemovalSettings.Inspect(XmlMerge.Read(prefixChanged14)).Selected.Order().SequenceEqual(new[]{"RemoveCalculator","RemoveTeams"}.Order())&&prefixChanged14.Contains("preserve-before14")&&prefixChanged14.Contains("preserve-between14"),"legacy partial edit canonicalizes selector order and preserves leading command");
withPrefix14.ComputerName="COMBINED14";withPrefix14.DisableFastStartup=!withPrefix14.DisableFastStartup;var combined14=Xml(withPrefix14);Check(Node(combined14,"//u:ComputerName")=="COMBINED14"&&combined14.Contains("preserve-before14"),"legacy app and unrelated setting changes combine without losing extra command");
Console.WriteLine($"V0.14 total: {count} checks passed.");

// V0.15: built-in Administrator uses the fixed generator's first-logon flow.
string AdminValue15(string text,string parent,string suffix){string value=Node(text,"//u:"+parent+"/u:Value")??"";return Node(text,"//u:"+parent+"/u:PlainText")=="false"?Encoding.Unicode.GetString(Convert.FromBase64String(value))[..^suffix.Length]:value;}
InstallPlan Builtin15(){var result=Plan();result.UseBuiltinAdministrator=true;result.BuiltinAdministratorPassword="Builtin15_密码<&'!";return result;}
Check(!InstallPlan.New().UseBuiltinAdministrator&&FirstLogonSettings.Mode(InstallPlan.New())==FirstLogonSettings.Own,"new/recommended plans keep first-created-admin default");
foreach(var arch15 in new[]{"amd64","arm64","x86"})foreach(bool encoded15 in new[]{true,false})foreach(var users15 in new[]{"admin","standard","none"})foreach(bool empty15 in new[]{false,true}){
 var plan15=Builtin15();plan15.Architecture=arch15;plan15.ObscurePasswords=encoded15;plan15.AllowEmptyBuiltinAdministratorPassword=empty15;if(empty15)plan15.BuiltinAdministratorPassword="";if(users15=="none")plan15.Users.Clear();else if(users15=="standard")plan15.Users[0].Administrator=false;
 string tag15=arch15+encoded15+users15+empty15;string output15=Xml(plan15);var state15=FirstLogonSettings.Inspect(XmlMerge.Read(output15));
 Check(Node(output15,"//u:AutoLogon/u:Username")=="Administrator"&&Node(output15,"//u:AutoLogon/u:LogonCount")=="1"&&Node(output15,"//u:AutoLogon/u:Enabled")=="true","builtin autologon XML: "+tag15);
 Check(AdminValue15(output15,"AdministratorPassword","AdministratorPassword")==plan15.BuiltinAdministratorPassword&&AdminValue15(output15,"AutoLogon/u:Password","Password")==plan15.BuiltinAdministratorPassword,"builtin password encoded with correct field-specific suffix: "+tag15);
 Check(state15.Known&&state15.Password==plan15.BuiltinAdministratorPassword&&state15.Obscure==encoded15&&Node(output15,"//u:LocalAccount/u:Name")!="Administrator"&&DecodedXml(output15).Contains(FirstLogonSettings.ResetCommand),"builtin flow and reset recognized without extra administrator account: "+tag15);
 var preview15=GeneratorService.Preview(g.Generate(plan15));Check(!preview15.Contains("Builtin15_")&&!preview15.Contains(Convert.ToBase64String(Encoding.Unicode.GetBytes(plan15.BuiltinAdministratorPassword+"AdministratorPassword")))&&Node(preview15[(preview15.IndexOf('\n')+1)..],"//u:AdministratorPassword/u:Value")=="[密码已遮蔽]","preview masks administrator and autologon fields: "+tag15);
 foreach(bool snapshot15 in new[]{true,false}){string original15=snapshot15?output15:WithoutSnapshot(output15);var back15=XmlPlanImport.Import(original15,g);Check(FirstLogonSettings.IsBuiltin(back15)&&back15.BuiltinAdministratorPassword==plan15.BuiltinAdministratorPassword&&back15.AllowEmptyBuiltinAdministratorPassword==empty15&&!back15.ImportedXml!.FirstLogonReadOnly&&SameXml(original15,Xml(back15)),"builtin XML restores actual password and unchanged tree: "+tag15+snapshot15);}
 var saved15=PlanStore.Deserialize(PlanStore.Serialize(plan15,true));Check(FirstLogonSettings.IsBuiltin(saved15)&&saved15.BuiltinAdministratorPassword==plan15.BuiltinAdministratorPassword,"builtin opt-in secret save/reload: "+tag15);
 var hidden15=PlanStore.Serialize(plan15,false);var omitted15=PlanStore.Deserialize(hidden15);Check(!hidden15.Contains("Builtin15_")&&!hidden15.Contains(Convert.ToBase64String(Encoding.Unicode.GetBytes("Builtin15_密码<&'!")))&&omitted15.NeedsBuiltinAdministratorPassword==!empty15&&omitted15.BuiltinAdministratorPassword=="","builtin secret omission and reentry flag: "+tag15);
}
p=Builtin15();p.BuiltinAdministratorPassword="";Reject(p,"builtin empty password requires explicit consent");p.AllowEmptyBuiltinAdministratorPassword=true;Check(FirstLogonSettings.Inspect(XmlMerge.Read(Xml(p))).Known,"explicit empty builtin password remains supported");p.BuiltinAdministratorPassword="Conflict15!";Reject(p,"builtin empty consent and actual password conflict");p.AllowEmptyBuiltinAdministratorPassword=false;p.NeedsBuiltinAdministratorPassword=true;Reject(p,"omitted builtin password needs reentry");p.NeedsBuiltinAdministratorPassword=false;p.ComputerName="ADMINISTRATOR";Reject(p,"builtin computer/account name collision");
p=Builtin15();p.AutoLogon=false;p.KeepSensitiveFiles=true;Check(!Xml(p).Contains("AdministratorPassword")&&!Xml(p).Contains("<AutoLogon>"),"inactive builtin choice does not enable account when autologon is off");p.AccountMode="安装时创建本地账户";p.AutoLogon=true;Check(!Xml(p).Contains("AdministratorPassword"),"interactive accounts ignore retained builtin choice");p=Plan();p.Users.Clear();Reject(p,"zero users still rejected for normal first-logon mode");
var vm15=new UnattendLocal.Desktop.MainViewModel();vm15.Plan=Builtin15();vm15.SelectedFirstLogon=FirstLogonSettings.None;Check(!vm15.Plan.AutoLogon&&!vm15.Plan.UseBuiltinAdministrator&&!vm15.IsBuiltinAdministrator,"first-logon UI none selection updates flags");vm15.SelectedFirstLogon=FirstLogonSettings.Builtin;Check(vm15.Plan.AutoLogon&&vm15.Plan.UseBuiltinAdministrator&&vm15.IsBuiltinAdministrator&&vm15.CanEditFirstLogon,"first-logon UI builtin selection updates flags");vm15.SelectedFirstLogon=FirstLogonSettings.Own;Check(vm15.Plan.AutoLogon&&!vm15.Plan.UseBuiltinAdministrator,"first-logon UI own selection restores existing behavior");
p=Builtin15();var secret15=p.BuiltinAdministratorPassword;Check(Recommendations.Differences(p).Any(d=>d.Property==nameof(InstallPlan.UseBuiltinAdministrator)),"recommendation differences include builtin activation");Recommendations.Apply(p,"wired");Check(!p.UseBuiltinAdministrator&&p.BuiltinAdministratorPassword==secret15,"recommendations use ordinary admin while retaining inactive typed password");
Check(Recommendations.MissingInputs(Builtin15()).Contains("未发现空缺")&&Recommendations.Summary(Builtin15()).Contains(FirstLogonSettings.Builtin)&&!Recommendations.Summary(Builtin15()).Contains("Builtin15_"),"summary reports builtin mode without credentials");p=Builtin15();p.BuiltinAdministratorPassword="";Check(Recommendations.MissingInputs(p).Contains("内置 Administrator"),"builtin missing secret is included in pending inputs");
var native15=WithoutSnapshot(Xml(Builtin15()));
foreach(var bad15 in new[]{"count","domain","mismatch","reset","resetduplicate","extract","stage","duplicate"}){
 var badDoc15=XmlMerge.Read(native15);var n15=new XmlNamespaceManager(badDoc15.NameTable);n15.AddNamespace("u",XmlMerge.Namespace);
 switch(bad15){
 case "count":badDoc15.SelectSingleNode("//u:AutoLogon/u:LogonCount",n15)!.InnerText="2";break;
 case "domain":var domain15=badDoc15.CreateElement("Domain",XmlMerge.Namespace);domain15.InnerText="EXAMPLE";badDoc15.SelectSingleNode("//u:AutoLogon",n15)!.AppendChild(domain15);break;
 case "mismatch":badDoc15.SelectSingleNode("//u:AdministratorPassword/u:Value",n15)!.InnerText=Convert.ToBase64String(Encoding.Unicode.GetBytes("Other15!AdministratorPassword"));break;
 case "reset":File14(badDoc15,"FirstLogon.ps1").InnerText=Body14(badDoc15,"FirstLogon.ps1").Replace(FirstLogonSettings.ResetCommand,"Write-Output 'no reset';");break;
 case "resetduplicate":File14(badDoc15,"FirstLogon.ps1").InnerText=Body14(badDoc15,"FirstLogon.ps1").Replace(FirstLogonSettings.ResetCommand,FirstLogonSettings.ResetCommand+"\n\t\t"+FirstLogonSettings.ResetCommand);break;
 case "extract":var extract15=badDoc15.SelectSingleNode("//*[local-name()='ExtractScript']")!;extract15.ParentNode!.RemoveChild(extract15);break;
 case "stage":foreach(var call15 in badDoc15.SelectNodes("//u:CommandLine",n15)!.Cast<XmlElement>().Where(n=>n.InnerText.Contains("FirstLogon.ps1")))call15.InnerText="cmd.exe /c echo skipped";break;
 case "duplicate":var auto15=badDoc15.SelectSingleNode("//u:AutoLogon",n15)!;auto15.ParentNode!.AppendChild(auto15.CloneNode(true));break;
 }
 var unknown15=XmlPlanImport.Import(badDoc15.OuterXml,g);Check(unknown15.ImportedXml!.FirstLogonReadOnly&&unknown15.ImportedXml.Notices.Contains(FirstLogonSettings.ReadOnlyDescription),"unsupported builtin flow is preserved/read-only: "+bad15);Check(SameXml(badDoc15.OuterXml,Xml(unknown15)),"unsupported builtin flow unchanged export: "+bad15);unknown15.AutoLogon=!unknown15.AutoLogon;Reject(unknown15,"unsupported builtin flow cannot change login: "+bad15);
}
var malformed15=XmlMerge.Read(native15);malformed15.SelectSingleNode("//*[local-name()='AdministratorPassword']/*[local-name()='Value']")!.InnerText="not-base64";try{XmlPlanImport.Import(malformed15.OuterXml,g);Check(false,"bad builtin credential");}catch(ArgumentException){Check(true,"invalid builtin password encoding rejects import");}
try{XmlPlanImport.Import(GeneratorService.Preview(g.Generate(Builtin15())).Split('\n',2)[1],g);Check(false,"redacted builtin preview");}catch(ArgumentException){Check(true,"redacted builtin preview rejects import");}
var editable15=XmlPlanImport.Import(native15,g);editable15.UseBuiltinAdministrator=false;var own15=Xml(editable15);Check(Node(own15,"//u:AutoLogon/u:Username")==editable15.Users.First(u=>u.Administrator).Name&&!own15.Contains("AdministratorPassword"),"supported imported builtin mode can switch to ordinary admin");editable15.AutoLogon=false;editable15.KeepSensitiveFiles=true;Check(Node(Xml(editable15),"//u:AutoLogon/u:Enabled")==null,"supported imported builtin mode can disable autologon");
var oldBuiltin15=XmlPlanImport.Import(native15,g);oldBuiltin15.ImportedXml!.FirstLogonImportVersion=0;oldBuiltin15.ComputerName="PENDING15";var oldBack15=PlanStore.Deserialize(PlanStore.Serialize(oldBuiltin15,true));Check(oldBack15.ComputerName=="PENDING15"&&oldBack15.ImportedXml!.FirstLogonReadOnly&&oldBack15.ImportedXml.Blockers.Any(n=>n.Contains("重新导入原 XML")),"older saved builtin workflow requests reimport and preserves pending edits");Reject(oldBack15,"old unsupported builtin plan cannot silently change login");
var privateSearch15="HiddenBuiltinSearch15!";vm15.Plan=Builtin15();vm15.Plan.BuiltinAdministratorPassword=privateSearch15;Check(UnattendLocal.Desktop.SettingsSearchCatalog.Search("Administrator").Any(r=>r.Target=="SettingAnchor073")&&UnattendLocal.Desktop.SettingsSearchCatalog.Search(privateSearch15).Count==0,"builtin search locates mode without reading secret values");


var collision15=Builtin15();collision15.Users[0].Password=collision15.BuiltinAdministratorPassword;var collisionDoc15=XmlMerge.Read(WithoutSnapshot(Xml(collision15)));collisionDoc15.SelectSingleNode("//*[local-name()='AutoLogon']/*[local-name()='LogonCount']")!.InnerText="2";var collisionPlan15=XmlPlanImport.Import(collisionDoc15.OuterXml,g);collisionPlan15.Users[0].Password="ChangedUser15!";Reject(collisionPlan15,"read-only builtin flow cannot inherit a changed ordinary-account password");
var collisionName15=XmlPlanImport.Import(collisionDoc15.OuterXml,g);collisionName15.Users[0].Name="ChangedUser15";Reject(collisionName15,"read-only builtin flow cannot inherit a changed login username");
var collisionDisplay15=XmlPlanImport.Import(collisionDoc15.OuterXml,g);collisionDisplay15.Users[0].DisplayName="Display only 15";Check(Xml(collisionDisplay15).Contains("Display only 15"),"read-only login permits unrelated display-name edits");


vm15.Plan=XmlPlanImport.Import(collisionDoc15.OuterXml,g);var preservedMode15=vm15.SelectedFirstLogon;vm15.SelectedFirstLogon=FirstLogonSettings.Builtin;Check(!vm15.CanEditFirstLogon&&vm15.SelectedFirstLogon.Contains("只读")&&vm15.FirstLogonModes.Contains(preservedMode15)&&!vm15.Plan.UseBuiltinAdministrator,"read-only UI shows preserved source mode and refuses mode edits");
count+=StaticNetworkTests.Run(g);
count+=ImportCompatibilityTests.Run(g);
count+=AutoPartitionTests.Run(g);
count+=ScriptFileTests.Run(g);
count+=SetupDriverTests.Run(g);
Console.WriteLine($"V1.3 total: {count} checks passed.");

count+=SetupDynamicUpdateTests.Run(g);
Console.WriteLine($"V1.4 total: {count} checks passed.");

count+=RegisteredInformationTests.Run(g);
Console.WriteLine($"V1.5 total: {count} checks passed.");

count+=SettingsSearchTests.Run();
Console.WriteLine($"V1.6 total: {count} checks passed.");

count+=NativeDeploymentTests.Run(g);
count+=VersionDisplayTests.Run();
Console.WriteLine($"V1.7 total: {count} checks passed.");

count+=UiImprovementTests.Run(g);
count+=ColorPickerHotfixTests.Run();
count+=NetworkEnhancementTests.Run(g);
count+=DualStackNetworkTests.Run(g);
count+=NetworkDeploymentTests.Run(g);
count+=HelpLayoutTests.Run(g);
count+=UnifiedSubnetTests.Run(g);
count+=ErrorLocationTests.Run(g);
Console.WriteLine($"V1.11 total: {count} checks passed.");

count+=ImportEnhancementTests.Run(g);
Console.WriteLine($"V1.12 total: {count} checks passed.");

count+=SecurityAuditFixTests.Run(g);
Console.WriteLine($"V1.12.1 final total: {count} tests passed.");

count+=AnswerFileExportTests.Run(g);
Console.WriteLine($"V1.13 final total: {count} tests passed.");

count+=AdvancedXmlTests.Run(g);
Console.WriteLine($"V1.14 final total: {count} tests passed.");

count+=NativePreferencesTests.Run(g);
Console.WriteLine($"V1.15 final total: {count} tests passed.");
count+=FileReliabilityTests.Run();
Console.WriteLine($"V1.16 final total: {count} tests passed.");

count+=ScriptTemplateTests.Run(g);
Console.WriteLine($"V1.17 final total: {count} tests passed.");

count+=ScriptEnhancementTests.Run(g);
Console.WriteLine($"V1.18 final total: {count} tests passed.");

Console.WriteLine($"V1.19 final total: {count} tests passed.");

try{count+=LocalizationTests.Run(g);}catch(Exception failure){Console.Error.WriteLine(failure.GetType().FullName);Console.Error.WriteLine(failure.Message);Environment.ExitCode=1;return;}
Console.WriteLine($"V1.20 final total: {count} tests passed.");

count+=DeploymentTuningTests.Run(g);
Console.WriteLine($"V1.21 final total: {count} tests passed.");

Console.WriteLine($"V1.22 final total: {count} tests passed.");

Console.WriteLine($"V1.23 final total: {count} tests passed.");

count+=V124SecurityAndNamingTests.Run(g);
Console.WriteLine($"V1.24 final total: {count} tests passed.");

count+=GeneratedPeTests.Run(g);Console.WriteLine($"V1.25 final total: {count} tests passed.");Console.WriteLine($"V1.26 final total: {count} tests passed.");

count+=PeSafetyTests.Run(g);count+=V128SecurityTests.Run(g);count+=PreviewLocalizationTests.Run(g);Console.WriteLine($"V1.28 final total: {count} tests passed.");

count+=V129GuidanceTests.Run(g);count+=PePreflightTests.Run(g);count+=KeyModeCompatibilityTests.Run(g);count+=PeImportCompatibilityTests.Run(g);Console.WriteLine($"V1.29 final total: {count} tests passed.");
