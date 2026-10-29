using System.Text.RegularExpressions;
using UnattendLocal.Core;
using UnattendLocal.Desktop;
using UnattendLocal.Localization;

static class V129GuidanceTests
{
 public static int Run(GeneratorService generator){
  int count=0;var previous=UiText.Language;
  void Check(bool ok,string label){if(!ok)throw new Exception("V1.29 guidance: "+label);count++;}
  InstallPlan Plan(){var p=QuickPresets.Create(QuickPresets.LocalAccount);p.DiskMode=InstallationSettings.Wipe;p.TargetDiskId=0;p.DiskLayout="UEFI / GPT";p.ConfirmWipeDisk=true;return p;}
  void Invalid(Action<InstallPlan> change,string field,string target){
   var p=Plan();change(p);string before=PlanStore.Serialize(p,true);bool wipe=p.ConfirmWipeDisk,format=p.ConfirmFormatPartition,script=p.ConfirmDiskScript;
   try{InstallationSettings.Validate(p);throw new Exception("accepted "+field);}catch(ConfigurationValidationException error){
    Check(error.Field==field,"typed error for "+field);
    var location=ConfigurationErrorLocation.FromException(error);
    Check(location?.Setting.Target==target && location.Setting.PageIndex==3,"exact disk setting for "+field);
    Check(before==PlanStore.Serialize(p,true) && wipe==p.ConfirmWipeDisk && format==p.ConfirmFormatPartition && script==p.ConfirmDiskScript,"lookup does not change configuration or consent");
    foreach(var language in new[]{"zh-CN","en-US"}){UiText.SetLanguage(language);Check(language!="en-US"||!Regex.IsMatch(UiText.Translate(location!.Path),"[\u4e00-\u9fff]"),"localized destination for "+field);}
   }
  }
  try{
   var importedRisk=XmlPlanImport.Import(System.Text.Encoding.UTF8.GetString(generator.Generate(Plan())),generator);
   importedRisk.ConfirmWipeDisk=true;
   try{generator.Generate(importedRisk);throw new Exception("Missing imported XML consent was accepted.");}
   catch(ConfigurationValidationException error){
    var location=ConfigurationErrorLocation.FromException(error);
    Check(error.Field==nameof(InstallPlan.ConfirmImportedXmlRisk),"import risk consent has its own validation field");
    Check(location?.Setting.Target=="ConfirmImportedXmlRiskCheck"&&location.Setting.PageIndex==0,"import risk navigates to the exact checkbox");
    foreach(var language in new[]{"zh-CN","en-US"}){
     UiText.SetLanguage(language);
     Check(UiText.Translate(location!.ActionText)==(language=="zh-CN"?"前往确认":"Go to confirmation"),"confirmation action is explicit in both languages");
     Check(language!="en-US"||!Regex.IsMatch(UiText.Translate(location.Path),"[\u4e00-\u9fff]"),"English confirmation destination has no Chinese");
    }
    Check(!importedRisk.ConfirmImportedXmlRisk,"locating a missing consent never grants it");
   }
   Invalid(p=>p.ConfirmWipeDisk=false,nameof(InstallPlan.ConfirmWipeDisk),"ConfirmWipeDiskCheck");
   Invalid(p=>{p.UseGeneratedPe=true;p.ConfirmWipeDisk=false;},nameof(InstallPlan.ConfirmWipeDisk),"ConfirmWipeDiskCheck");
   Invalid(p=>{p.DiskMode=InstallationSettings.Existing;p.TargetPartitionId=1;p.FormatExistingPartition=true;p.ConfirmFormatPartition=false;},nameof(InstallPlan.ConfirmFormatPartition),"ConfirmFormatPartitionCheck");
   Invalid(p=>{p.DiskMode=DiskScriptSettings.Mode;p.DiskPartScript="select disk 0\r\nclean";p.TargetPartitionId=1;p.ConfirmDiskScript=false;},nameof(InstallPlan.ConfirmDiskScript),"ConfirmDiskScriptCheck");
   Invalid(p=>p.TargetDiskId=null,nameof(InstallPlan.TargetDiskId),"SettingAnchor062");
   Invalid(p=>{p.DiskMode=InstallationSettings.Existing;p.TargetPartitionId=0;},nameof(InstallPlan.TargetPartitionId),"SettingAnchor063");
   Invalid(p=>p.RecoverySizeMB=100,nameof(InstallPlan.RecoverySizeMB),"SettingAnchor067");
   Invalid(p=>p.SystemPartitionSizeMB=1,nameof(InstallPlan.SystemPartitionSizeMB),"SystemPartitionSizeEditor");
   Invalid(p=>{p.ImageSelection="按镜像索引";p.ImageIndex=0;},nameof(InstallPlan.ImageIndex),"SettingAnchor057");
   Invalid(p=>{p.ImageSelection="按镜像名称";p.ImageName="";},nameof(InstallPlan.ImageName),"SettingAnchor056");
   Invalid(p=>{p.DiskMode=DiskScriptSettings.Mode;p.DiskPartScript="";},nameof(InstallPlan.DiskPartScript),"DiskPartExampleEditor");
   var vm=new MainViewModel();vm.Plan=Plan();vm.Plan.ImageSelection="按镜像索引";vm.Plan.ImageIndex=1;vm.Plan.EditionMode=UiText.Source("text.cc706914a2ad");vm.Plan.EditionId="pro";vm.Plan.CustomProductKey="SAVED-DRAFT";
   string[] normalDescriptions=[vm.EditionInstallationDescription,vm.ActivationKeyDescription];
   var notified=new HashSet<string>();vm.PropertyChanged+=(_,e)=>notified.Add(e.PropertyName??"");vm.Plan.UseGeneratedPe=true;
   Check(vm.Plan.EditionMode==UiText.Source("text.cc706914a2ad")&&vm.Plan.EditionId=="pro"&&vm.Plan.CustomProductKey=="SAVED-DRAFT","enabling PE preserves existing selections and drafts");
   Check(notified.Contains(nameof(vm.EditionInstallationDescription))&&notified.Contains(nameof(vm.ActivationKeyDescription)),"PE toggle refreshes both guidance bindings");
   foreach(string mode in new[]{UiText.Source("text.cc706914a2ad"),UiText.Source("text.2a5194b6a242"),UiText.Source("text.42a0cbd1b22f")}){
    vm.Plan.EditionMode=mode;
    try{GeneratedPeSettings.Validate(vm.Plan);throw new Exception("PE accepted conflicting key mode");}catch(ConfigurationValidationException e){Check(e.Field==nameof(InstallPlan.EditionMode),"conflicting key mode remains rejected");}
    Check(vm.Plan.EditionMode==mode,"guidance does not normalize a conflicting mode");
   }
   vm.Plan.EditionMode=UiText.Source("text.5056d73b13ec");vm.Plan.BypassHardware=vm.Plan.BypassTPMCheck=vm.Plan.BypassSecureBootCheck=vm.Plan.BypassRAMCheck=false;vm.Plan.ConfirmWipeDisk=true;
   var baseline=generator.Generate(vm.Plan);
   foreach(var language in new[]{"zh-CN","en-US"}){
    UiText.SetLanguage(language);
    foreach(string text in new[]{vm.EditionHint,vm.EditionInstallationDescription,vm.ActivationKeyDescription})Check(language!="en-US"||!Regex.IsMatch(UiText.Translate(text),"[\u4e00-\u9fff]"),"bilingual PE guidance");
    foreach(string id in new[]{"edition","sku","installkey","activation"})foreach(var part in vm.HelpTips[id].Sections)Check(language!="en-US"||!Regex.IsMatch(UiText.Translate(part.Text),"[\u4e00-\u9fff]"),"bilingual dynamic key help");
    Check(generator.Generate(vm.Plan).SequenceEqual(baseline),"guidance leaves exported deployment bytes unchanged");
   }
   vm.Plan.UseGeneratedPe=false;
   Check(vm.EditionInstallationDescription==normalDescriptions[0]&&vm.ActivationKeyDescription==normalDescriptions[1],"disabling PE restores ordinary Setup wording");
   Check(vm.Plan.CustomProductKey=="SAVED-DRAFT"&&vm.Plan.EditionId=="pro","switching paths retains saved inputs");
  }finally{UiText.SetLanguage(previous);}
  return count;
 }
}
