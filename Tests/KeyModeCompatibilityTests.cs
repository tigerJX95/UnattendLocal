using System.Text;
using System.Xml;
using UnattendLocal.Core;
using UnattendLocal.Desktop;
using UnattendLocal.Localization;

static class KeyModeCompatibilityTests
{
 public static int Run(GeneratorService generator){
  int count=0;string previous=UiText.Language;
  void Check(bool ok,string name){if(!ok)throw new Exception("Key mode compatibility: "+name);count++;}
  string? Read(byte[] bytes,string path){var doc=XmlMerge.Read(Encoding.UTF8.GetString(bytes));return doc.SelectSingleNode(path)?.InnerText;}
  try{
   const string canonical="安装时输入密钥";
   Check(UiText.Source("text.5056d73b13ec")==canonical,"canonical selection identity is unchanged");
   UiText.SetLanguage("zh-CN");Check(UiText.Get("text.5056d73b13ec")=="在 Windows 安装程序中手动输入","Chinese display label is explicit");
   UiText.SetLanguage("en-US");Check(UiText.Get("text.5056d73b13ec")=="Enter manually in Windows Setup","English display label is explicit");
   Check(SettingsSearchCatalog.Search("在 Windows 安装程序中手动输入").Any(x=>x.Target=="SettingAnchor042"),"new Chinese label locates key settings");
   Check(SettingsSearchCatalog.Search("Enter manually in Windows Setup").Any(x=>x.Target=="SettingAnchor042"),"new English label locates key settings");
   Check(SettingsSearchCatalog.Search(canonical).Any(x=>x.Target=="SettingAnchor042"),"old label remains searchable");
   var plan=QuickPresets.Create(QuickPresets.LocalAccount);plan.EditionMode=canonical;
   var original=generator.Generate(plan);
   const string setup="//*[local-name()='settings'][@pass='windowsPE']//*[local-name()='UserData']/*[local-name()='ProductKey']";
   Check(Read(original,setup+"/*[local-name()='WillShowUI']")=="Always","ordinary Windows Setup still requests interactive key entry");
   foreach(string language in new[]{"zh-CN","en-US"}){
    UiText.SetLanguage(language);
    string saved=PlanStore.Serialize(plan,true);
    var restored=PlanStore.Deserialize(saved);
    Check(restored.EditionMode==canonical,"saved selections stay compatible after display rename");
    Check(generator.Generate(restored).SequenceEqual(original),"ordinary Setup export stays identical across languages and JSON reload");
   }
   var loaded=PlanStore.Deserialize("{\"SchemaVersion\":1,\"EditionMode\":\"安装时输入密钥\"}");
   Check(loaded.EditionMode==canonical,"old JSON retains the original selection");
   plan.UseGeneratedPe=true;plan.DiskMode=InstallationSettings.Wipe;plan.DiskLayout="UEFI / GPT";plan.ImageSelection="按镜像索引";plan.ImageIndex=3;plan.TargetDiskId=0;
   plan.BypassHardware=plan.BypassTPMCheck=plan.BypassSecureBootCheck=plan.BypassRAMCheck=false;plan.ConfirmWipeDisk=true;
   var pe=generator.Generate(plan);string script=Read(pe,"//*[local-name()='PEScriptCopy']")!;
   Check(Read(pe,setup)==null,"direct PE deployment has no competing Setup key-entry block");
   Check(script.Contains("UL-imageinfo.txt 3 >X:\\UL-imagecheck.out")&&script.Contains("Selected image edition/SKU:"),"PE reports actual selected image identity");
   Check(script.Contains("/Index:3")&&!script.Contains("/Set-Edition",StringComparison.OrdinalIgnoreCase),"image index controls deployment without changing editions");
   var vm=new MainViewModel{Plan=plan};
   foreach(string language in new[]{"zh-CN","en-US"}){
    UiText.SetLanguage(language);
    Check(UiText.Translate(vm.EditionHint).Contains("PE + DISM"),"dynamic hint explains the direct deployment path");
    Check(UiText.Translate(vm.HelpTips["edition"].Body).Contains("Edition ID"),"help explains actual SKU discovery");
   }
   Check(PePreflight.ImageCheckerSource().Contains("Edition/SKU unavailable; using the explicitly selected index."),"optional edition metadata never selects an index or blocks an otherwise valid image");
   Check(plan.EditionMode==canonical,"display guidance never changes persisted key mode");
   plan.UseActivationKey=true;plan.ActivationProductKey="VK7JG-NPHTM-C97JM-9MPGT-3V66T";plan.ConfirmWipeDisk=true;
   var activated=generator.Generate(plan);
   Check(Read(activated,"//*[local-name()='component'][@name='Microsoft-Windows-Shell-Setup']/*[local-name()='ProductKey']")==plan.ActivationProductKey,"separate activation key path remains intact");
  }finally{UiText.SetLanguage(previous);}
  return count;
 }
}
