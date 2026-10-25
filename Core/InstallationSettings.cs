using System.Globalization;
using System.Xml;
namespace UnattendLocal.Core;
public static class InstallationSettings {
 public const string Manual="手动选择磁盘与分区", Existing="使用已有分区", Wipe="清空指定磁盘后自动分区";
 private const string Ns="urn:schemas-microsoft-com:unattend",Wcm="http://schemas.microsoft.com/WMIConfig/2002/State";
 public static bool Destructive(InstallPlan p)=>p.DiskMode==DiskScriptSettings.Mode||p.DiskMode==Wipe||(p.DiskMode==Existing&&p.FormatExistingPartition);
 public static string ImageSummary(InstallPlan p)=>p.ImageSelection switch{"按镜像名称"=>global::UnattendLocal.Localization.UiText.Format("message.fe34f7d7e8a1", new string[] { global::UnattendLocal.Localization.UiText.Arg((p.ImageName), 0, null) }),"按镜像索引"=>global::UnattendLocal.Localization.UiText.Format("message.7d589eb1b750", new string[] { global::UnattendLocal.Localization.UiText.Arg((p.ImageIndex), 0, null) }),_=>global::UnattendLocal.Localization.UiText.Source("text.30e0abc2d5fa")};
 public static string DiskSummary(InstallPlan p)=>p.ImportedXml?.AutoPartitionReadOnly==true?global::UnattendLocal.Localization.UiText.Source("text.f9a7957eefb2"):p.DiskMode switch{
  DiskScriptSettings.Mode=>global::UnattendLocal.Localization.UiText.Format("message.1d4d6f86f39f", new string[] { global::UnattendLocal.Localization.UiText.Arg((p.TargetDiskId), 0, null), global::UnattendLocal.Localization.UiText.Arg((p.TargetPartitionId), 0, null) }),
  Existing=>global::UnattendLocal.Localization.UiText.Format("message.af5082351004", new string[] { global::UnattendLocal.Localization.UiText.Arg((p.TargetDiskId), 0, null), global::UnattendLocal.Localization.UiText.Arg((p.TargetPartitionId), 0, null), global::UnattendLocal.Localization.UiText.Arg(((p.FormatExistingPartition?global::UnattendLocal.Localization.UiText.Source("text.b0a7cad8a1ce"):global::UnattendLocal.Localization.UiText.Source("text.7c952fe6423c"))), 0, null) }),
  Wipe=>global::UnattendLocal.Localization.UiText.Format("message.8813669039c9", new string[] { global::UnattendLocal.Localization.UiText.Arg((p.TargetDiskId), 0, null), global::UnattendLocal.Localization.UiText.Arg((p.DiskLayout), 0, null), global::UnattendLocal.Localization.UiText.Arg((AutoPartitionSettings.Summary(p)), 0, null) }),
  _=>InstallationReview.ForPlan(p).Description};
 public static void Validate(InstallPlan p){
  if(p.ImageSelection is not ("由安装程序决定" or "按镜像名称" or "按镜像索引"))throw new ArgumentException(global::UnattendLocal.Localization.UiText.Source("text.952b481ef960"));
  if(p.ImageSelection==global::UnattendLocal.Localization.UiText.Source("text.5965e61aee4c")&&(string.IsNullOrWhiteSpace(p.ImageName)||p.ImageName.Length>256||p.ImageName.Any(char.IsControl)))throw new ArgumentException(global::UnattendLocal.Localization.UiText.Source("text.b5fc6be12e60"));
  if(p.ImageSelection==global::UnattendLocal.Localization.UiText.Source("text.9cbbdbe44e5d")&&(p.ImageIndex is null or <1))throw new ArgumentException(global::UnattendLocal.Localization.UiText.Source("text.02ec7f74b065"));
  if(p.DiskMode is not (Manual or Existing or Wipe or DiskScriptSettings.Mode))throw new ArgumentException(global::UnattendLocal.Localization.UiText.Source("text.a99d66c2bc1b"));
  AutoPartitionSettings.Validate(p);
  if(p.DiskMode==Manual)return;
  if(p.TargetDiskId is null or <0)throw new ArgumentException(global::UnattendLocal.Localization.UiText.Source("text.100b513995e2"));
  if(p.DiskMode==DiskScriptSettings.Mode){_=DiskScriptSettings.Commands(p.DiskPartScript);if(p.TargetPartitionId is null or <1)throw new ArgumentException(global::UnattendLocal.Localization.UiText.Source("text.91a517af2517"));if(!p.ConfirmDiskScript)throw new ArgumentException(global::UnattendLocal.Localization.UiText.Source("text.0aff66257286"));}
  else if(p.DiskMode==Existing){if(p.TargetPartitionId is null or <1)throw new ArgumentException(global::UnattendLocal.Localization.UiText.Source("text.1a0a3842b624"));if(p.FormatExistingPartition&&!p.ConfirmFormatPartition)throw new ArgumentException(global::UnattendLocal.Localization.UiText.Source("text.b7d2a16f1004"));}
  else{if(p.RecoverySizeMB<1024||p.RecoverySizeMB>65536)throw new ArgumentException(global::UnattendLocal.Localization.UiText.Source("text.917fb84d9bab"));if(p.Architecture=="arm64"&&p.DiskLayout=="BIOS / MBR")throw new ArgumentException(global::UnattendLocal.Localization.UiText.Source("text.a30b550e7619"));if(p.DiskLayout is not ("UEFI / GPT" or "BIOS / MBR"))throw new ArgumentException(global::UnattendLocal.Localization.UiText.Source("text.c701cb471f61"));if(!p.ConfirmWipeDisk)throw new ArgumentException(global::UnattendLocal.Localization.UiText.Source("text.7fc02b9199fe"));}
 }
 private static XmlElement Add(XmlElement parent,string name,string? text=null,bool list=false){var e=parent.OwnerDocument!.CreateElement(name,Ns);if(list){var a=parent.OwnerDocument.CreateAttribute("wcm","action",Wcm);a.Value="add";e.Attributes.Append(a);}if(text!=null)e.InnerText=text;parent.AppendChild(e);return e;}
 private static string Num(int? value)=>value!.Value.ToString(CultureInfo.InvariantCulture);
 public static void Apply(XmlDocument doc,XmlNamespaceManager ns,InstallPlan p){
  Validate(p);
  foreach(XmlElement setup in doc.SelectNodes("//u:settings[@pass='windowsPE']/u:component[@name='Microsoft-Windows-Setup']",ns)!){
   if(p.ImageSelection==global::UnattendLocal.Localization.UiText.Source("text.cc65b6582544")&&p.DiskMode==Manual)continue;
   var image=Add(Add(setup,"ImageInstall"),"OSImage");
   if(p.ImageSelection!=global::UnattendLocal.Localization.UiText.Source("text.cc65b6582544")){var meta=Add(Add(image,"InstallFrom"),"MetaData",list:true);Add(meta,"Key",p.ImageSelection==global::UnattendLocal.Localization.UiText.Source("text.5965e61aee4c")?"/IMAGE/NAME":"/IMAGE/INDEX");Add(meta,"Value",p.ImageSelection==global::UnattendLocal.Localization.UiText.Source("text.5965e61aee4c")?p.ImageName.Trim():Num(p.ImageIndex));}
   if(p.DiskMode!=Manual){var target=Add(image,"InstallTo");Add(target,"DiskID",Num(p.TargetDiskId));Add(target,"PartitionID",p.DiskMode is Existing or DiskScriptSettings.Mode?Num(p.TargetPartitionId):p.DiskLayout=="UEFI / GPT"?"3":"2");Add(image,"InstallToAvailablePartition","false");}
   Add(image,"WillShowUI","OnError");
   if(p.DiskMode==Existing&&p.FormatExistingPartition){var config=Add(setup,"DiskConfiguration");var disk=Add(config,"Disk",list:true);Add(disk,"DiskID",Num(p.TargetDiskId));Add(disk,"WillWipeDisk","false");var part=Add(Add(disk,"ModifyPartitions"),"ModifyPartition",list:true);Add(part,"Order","1");Add(part,"PartitionID",Num(p.TargetPartitionId));Add(part,"Format","NTFS");Add(config,"WillShowUI","OnError");}
   if(p.DiskMode==DiskScriptSettings.Mode){
    var sync=setup.SelectSingleNode("u:RunSynchronous",ns) as XmlElement??Add(setup,"RunSynchronous");
    var order=sync.SelectNodes("u:RunSynchronousCommand/u:Order",ns)!.Cast<XmlNode>().Select(n=>int.Parse(n.InnerText,CultureInfo.InvariantCulture)).DefaultIfEmpty(0).Max()+1;
    foreach(var command in DiskScriptSettings.Commands(p.DiskPartScript)){
     var cmd=Add(sync,"RunSynchronousCommand",list:true);Add(cmd,"Order",(order++).ToString(CultureInfo.InvariantCulture));Add(cmd,"Description",global::UnattendLocal.Localization.UiText.Source("text.03d70d7e084c"));Add(cmd,"Path",command);
    }
   }
   if(p.DiskMode==Wipe){
    var sync=setup.SelectSingleNode("u:RunSynchronous",ns) as XmlElement??Add(setup,"RunSynchronous");
    var order=sync.SelectNodes("u:RunSynchronousCommand/u:Order",ns)!.Cast<XmlNode>().Select(n=>int.Parse(n.InnerText,CultureInfo.InvariantCulture)).DefaultIfEmpty(0).Max()+1;
    var cmd=Add(sync,"RunSynchronousCommand",list:true);Add(cmd,"Order",order.ToString(CultureInfo.InvariantCulture));Add(cmd,"Description",global::UnattendLocal.Localization.UiText.Source("text.62dac1bd190f"));
    Add(cmd,"Path",AutoPartitionSettings.Command(p));
   }
  }
 }
}
