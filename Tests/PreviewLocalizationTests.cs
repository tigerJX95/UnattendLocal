using System.Text;
using System.Text.RegularExpressions;
using UnattendLocal.Core;
using UnattendLocal.Desktop;
using UnattendLocal.Localization;

static class PreviewLocalizationTests
{
 public static int Run(GeneratorService generator){
  int count=0;var previous=UiText.Language;
  void Check(bool ok,string name){if(!ok)throw new Exception("Preview localization: "+name);count++;}
  try{
   const string xml="""
   <unattend xmlns="urn:schemas-microsoft-com:unattend">
    <settings pass="specialize">
     <component name="Microsoft-Windows-Shell-Setup" processorArchitecture="amd64">
      <AutoLogon><Password><Value>PRIVATE-PASSWORD</Value><PlainText>true</PlainText></Password></AutoLogon>
      <ProductKey>PRIVATE-PRODUCT-KEY</ProductKey><RegisteredOwner>中文用户内容</RegisteredOwner>
     </component>
     <component name="Microsoft-Windows-PnpCustomizationsWinPE"><Credentials><Password>PRIVATE-DRIVER</Password></Credentials></component>
     <component name="Microsoft-Windows-UnattendedJoin"><Credentials><Password>PRIVATE-DOMAIN</Password></Credentials><MachinePassword>PRIVATE-MACHINE</MachinePassword><Provisioning><AccountData>PRIVATE-BLOB</AccountData></Provisioning></component>
    </settings>
    <Extensions xmlns="https://schneegans.de/windows/unattend-generator/">
     <File path="C:\Windows\Setup\Scripts\Wifi.xml">PRIVATE-WIFI</File>
     <File path="C:\Windows\Setup\Scripts\AppLockerPolicy.xml">PRIVATE-POLICY</File>
     <File path="C:\Windows\Setup\Scripts\User.ps1">Write-Host '中文用户脚本'</File>
    </Extensions>
   </unattend>
   """;
   var bytes=Encoding.UTF8.GetBytes(xml);
   foreach(string language in new[]{"zh-CN","en-US"}){
    UiText.SetLanguage(language);
    string english=GeneratorService.Preview(bytes,english:true);
    Check(!english.Contains("PRIVATE-"),"all native credential and policy fields stay redacted");
    Check(english.Contains("中文用户内容")&&english.Contains("中文用户脚本"),"raw user text and script remain unchanged");
    Check(!Regex.IsMatch(english.Replace("中文用户内容","").Replace("中文用户脚本",""),"[\u4e00-\u9fff]"),"English authored preview text has no Chinese");
    Check(GeneratorService.Preview(bytes).Contains("[密码已遮蔽]"),"canonical preview remains compatible");
    try{XmlPlanImport.Import(english[english.IndexOf("<unattend",StringComparison.Ordinal)..],generator);throw new Exception("accepted redacted domain XML");}catch(ArgumentException){count++;}
   }
   var model=new MainViewModel();model.Plan.Users[0].Password="PreviewTest123!";
   var exported=generator.Generate(model.Plan);string before=Convert.ToHexString(exported);
   UiText.SetLanguage("zh-CN");model.ShowXmlPreview(exported);
   Check(model.Preview.Contains("预览（"),"Chinese preview labels");
   UiText.SetLanguage("en-US");model.RefreshPreviewLanguage();
   Check(!Regex.IsMatch(model.Preview,"[\u4e00-\u9fff]"),"English view model includes translated disk summary and masks");
   UiText.SetLanguage("zh-CN");model.RefreshPreviewLanguage();
   Check(model.Preview.Contains("[密码已遮蔽]"),"switching back preserves localized preview");
   Check(Convert.ToHexString(generator.Generate(model.Plan))==before,"display localization leaves actual export bytes unchanged");
   model.Preview=UiText.Source("text.92a3dbdf6833");UiText.SetLanguage("en-US");
   Check(!Regex.IsMatch(model.Preview,"[\u4e00-\u9fff]"),"invalidated preview messages are translated");
  }finally{UiText.SetLanguage(previous);}
  return count;
 }
}
