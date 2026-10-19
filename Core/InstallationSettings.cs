using System.Globalization;
using System.Xml;
namespace UnattendLocal.Core;
public static class InstallationSettings {
 public const string Manual="手动选择磁盘与分区", Existing="使用已有分区", Wipe="清空指定磁盘后自动分区";
 private const string Ns="urn:schemas-microsoft-com:unattend",Wcm="http://schemas.microsoft.com/WMIConfig/2002/State";
 public static bool Destructive(InstallPlan p)=>p.DiskMode==DiskScriptSettings.Mode||p.DiskMode==Wipe||(p.DiskMode==Existing&&p.FormatExistingPartition);
 public static string ImageSummary(InstallPlan p)=>p.ImageSelection switch{"按镜像名称"=>$"名称：{p.ImageName}","按镜像索引"=>$"索引：{p.ImageIndex}",_=>"由安装程序和产品密钥决定"};
 public static string DiskSummary(InstallPlan p)=>p.ImportedXml?.AutoPartitionReadOnly==true?"【危险】原 XML 的分区流程未完整识别；须核对原文全部目标与命令，不能按界面默认值推断操作范围。":p.DiskMode switch{
  DiskScriptSettings.Mode=>$"【危险】执行自定义 DiskPart 脚本（可能影响任意磁盘）；随后安装到磁盘 {p.TargetDiskId} / 分区 {p.TargetPartitionId}",
  Existing=>$"磁盘 {p.TargetDiskId} / 分区 {p.TargetPartitionId}；{(p.FormatExistingPartition?"【危险】格式化此分区，删除分区内全部数据":"不格式化；安装仍可能改动原有文件")}",
  Wipe=>$"【危险】清空磁盘 {p.TargetDiskId} 全部分区和数据；{p.DiskLayout} 自动分区；{AutoPartitionSettings.Summary(p)}",
  _=>InstallationReview.ForPlan(p).Description};
 public static void Validate(InstallPlan p){
  if(p.ImageSelection is not ("由安装程序决定" or "按镜像名称" or "按镜像索引"))throw new ArgumentException("请选择有效镜像选择方式。");
  if(p.ImageSelection=="按镜像名称"&&(string.IsNullOrWhiteSpace(p.ImageName)||p.ImageName.Length>256||p.ImageName.Any(char.IsControl)))throw new ArgumentException("请填写安装镜像中的完整名称（不含控制字符）。");
  if(p.ImageSelection=="按镜像索引"&&(p.ImageIndex is null or <1))throw new ArgumentException("镜像索引必须为大于等于 1 的整数。");
  if(p.DiskMode is not (Manual or Existing or Wipe or DiskScriptSettings.Mode))throw new ArgumentException("请选择有效磁盘与分区模式。");
  AutoPartitionSettings.Validate(p);
  if(p.DiskMode==Manual)return;
  if(p.TargetDiskId is null or <0)throw new ArgumentException("请明确填写目标电脑在 Windows PE 中的磁盘编号，从 0 开始。");
  if(p.DiskMode==DiskScriptSettings.Mode){_=DiskScriptSettings.Commands(p.DiskPartScript);if(p.TargetPartitionId is null or <1)throw new ArgumentException("请填写脚本执行后 Windows 目标分区编号，从 1 开始。");if(!p.ConfirmDiskScript)throw new ArgumentException("请核对完整脚本并勾选自定义分区脚本风险确认。");}
  else if(p.DiskMode==Existing){if(p.TargetPartitionId is null or <1)throw new ArgumentException("请明确填写目标分区编号，从 1 开始。");if(p.FormatExistingPartition&&!p.ConfirmFormatPartition)throw new ArgumentException("格式化会删除目标分区数据，请明确勾选格式化风险确认。");}
  else{if(p.RecoverySizeMB<1024||p.RecoverySizeMB>65536)throw new ArgumentException("恢复分区大小应为 1024–65536 MB，并满足实际 WinRE 镜像与维护空间需求。");if(p.Architecture=="arm64"&&p.DiskLayout=="BIOS / MBR")throw new ArgumentException("ARM64 自动分区必须使用 UEFI / GPT。");if(p.DiskLayout is not ("UEFI / GPT" or "BIOS / MBR"))throw new ArgumentException("请选择有效启动与分区类型。");if(!p.ConfirmWipeDisk)throw new ArgumentException("清空磁盘会删除所有分区和数据，请明确勾选清空磁盘风险确认。");}
 }
 private static XmlElement Add(XmlElement parent,string name,string? text=null,bool list=false){var e=parent.OwnerDocument!.CreateElement(name,Ns);if(list){var a=parent.OwnerDocument.CreateAttribute("wcm","action",Wcm);a.Value="add";e.Attributes.Append(a);}if(text!=null)e.InnerText=text;parent.AppendChild(e);return e;}
 private static string Num(int? value)=>value!.Value.ToString(CultureInfo.InvariantCulture);
 public static void Apply(XmlDocument doc,XmlNamespaceManager ns,InstallPlan p){
  Validate(p);
  foreach(XmlElement setup in doc.SelectNodes("//u:settings[@pass='windowsPE']/u:component[@name='Microsoft-Windows-Setup']",ns)!){
   if(p.ImageSelection=="由安装程序决定"&&p.DiskMode==Manual)continue;
   var image=Add(Add(setup,"ImageInstall"),"OSImage");
   if(p.ImageSelection!="由安装程序决定"){var meta=Add(Add(image,"InstallFrom"),"MetaData",list:true);Add(meta,"Key",p.ImageSelection=="按镜像名称"?"/IMAGE/NAME":"/IMAGE/INDEX");Add(meta,"Value",p.ImageSelection=="按镜像名称"?p.ImageName.Trim():Num(p.ImageIndex));}
   if(p.DiskMode!=Manual){var target=Add(image,"InstallTo");Add(target,"DiskID",Num(p.TargetDiskId));Add(target,"PartitionID",p.DiskMode is Existing or DiskScriptSettings.Mode?Num(p.TargetPartitionId):p.DiskLayout=="UEFI / GPT"?"3":"2");Add(image,"InstallToAvailablePartition","false");}
   Add(image,"WillShowUI","OnError");
   if(p.DiskMode==Existing&&p.FormatExistingPartition){var config=Add(setup,"DiskConfiguration");var disk=Add(config,"Disk",list:true);Add(disk,"DiskID",Num(p.TargetDiskId));Add(disk,"WillWipeDisk","false");var part=Add(Add(disk,"ModifyPartitions"),"ModifyPartition",list:true);Add(part,"Order","1");Add(part,"PartitionID",Num(p.TargetPartitionId));Add(part,"Format","NTFS");Add(config,"WillShowUI","OnError");}
   if(p.DiskMode==DiskScriptSettings.Mode){
    var sync=setup.SelectSingleNode("u:RunSynchronous",ns) as XmlElement??Add(setup,"RunSynchronous");
    var order=sync.SelectNodes("u:RunSynchronousCommand/u:Order",ns)!.Cast<XmlNode>().Select(n=>int.Parse(n.InnerText,CultureInfo.InvariantCulture)).DefaultIfEmpty(0).Max()+1;
    foreach(var command in DiskScriptSettings.Commands(p.DiskPartScript)){
     var cmd=Add(sync,"RunSynchronousCommand",list:true);Add(cmd,"Order",(order++).ToString(CultureInfo.InvariantCulture));Add(cmd,"Description","自定义 DiskPart 分区脚本（核对全部命令及安装目标）");Add(cmd,"Path",command);
    }
   }
   if(p.DiskMode==Wipe){
    var sync=setup.SelectSingleNode("u:RunSynchronous",ns) as XmlElement??Add(setup,"RunSynchronous");
    var order=sync.SelectNodes("u:RunSynchronousCommand/u:Order",ns)!.Cast<XmlNode>().Select(n=>int.Parse(n.InnerText,CultureInfo.InvariantCulture)).DefaultIfEmpty(0).Max()+1;
    var cmd=Add(sync,"RunSynchronousCommand",list:true);Add(cmd,"Order",order.ToString(CultureInfo.InvariantCulture));Add(cmd,"Description","清空指定磁盘并创建系统、Windows 与恢复分区");
    Add(cmd,"Path",AutoPartitionSettings.Command(p));
   }
  }
 }
}
