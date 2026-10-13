using System.Text;using System.Xml;using UnattendLocal.Core;
int count=0;var g=new GeneratorService();
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
Check(Recommendations.InteractiveSteps(p)=="磁盘与分区","custom summary accurately discloses remaining deliberate installation interaction");
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
var webXml=XmlMerge.Read(WithoutSnapshot(Xml(Plan())));webXml.DocumentElement!.PrependChild(webXml.CreateComment("https://schneegans.de/windows/unattend-generator/?EffectsMode=Custom&FontSmoothing=true&DisableUac=true&AccountPassword0=do-not-show-this&WallpaperMode=Default"));
var webPlan=XmlPlanImport.Import(webXml.OuterXml,g);Check(webPlan.ImportedXml!.Notices.Any(n=>n.Contains("DisableUac"))&&webPlan.ImportedXml.Notices.All(n=>!n.Contains("do-not-show-this")),"website unimplemented parameters identified by name only");
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
 Check(officialPlan.ImportedXml!.Notices.Any(n=>n.Contains("DisableUac"))&&officialPlan.ImportedXml.Blockers.Length==0,"official unimplemented UAC setting is reported and retained");
 Check(SameXml(officialXml,Xml(officialPlan)),"official website XML unchanged roundtrip preserves unknown UAC command");
 officialPlan.ComputerName="OfficialEdit";var officialEdited=Xml(officialPlan);
 Check(officialEdited.Contains("OfficialEdit")&&DecodedXml(officialEdited).Contains("EnableLUA")&&DecodedXml(officialEdited).Contains("LongPathsEnabled"),"official XML supported edit retains unimplemented protection command");
}
Check(!SameXml("<unattend xmlns='urn:schemas-microsoft-com:unattend'><A><B><C/><D/></B></A></unattend>","<unattend xmlns='urn:schemas-microsoft-com:unattend'><A><B><C/></B><D/></A></unattend>"),"XML integrity distinguishes different parent relationships");

var externalJs=XmlMerge.Read(WithoutSnapshot(Xml(Plan())));var externalFirst=(XmlElement)externalJs.SelectSingleNode("//*[local-name()='File' and contains(@path,'FirstLogon.ps1')]")!;externalFirst.InnerText+="\ncscript.exe C:\\legacy.js;\n";var externalJsPlan=XmlPlanImport.Import(externalJs.OuterXml,g);Check(externalJsPlan.ImportedXml!.Blockers.Any(n=>n.Contains("JScript")),"external JS invocation also cannot bypass removed script support");
var outsidePeDisk=XmlMerge.Read(WithoutSnapshot(Xml(Plan())));var outsideFirst=(XmlElement)outsidePeDisk.SelectSingleNode("//*[local-name()='File' and contains(@path,'FirstLogon.ps1')]")!;outsideFirst.InnerText+="\nFormat-Volume -DriveLetter Z;\n";Check(XmlPlanImport.Import(outsidePeDisk.OuterXml,g).ImportedXml!.RequiresDiskReview,"disk operation outside PE still requires original-scope review");
Console.WriteLine($"Final pre39 total: {count} tests passed.");
