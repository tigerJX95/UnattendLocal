using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Xml;
namespace UnattendLocal.Core;

public static class AutoPartitionSettings {
 public const string ReadOnlyNotice="自动分区命令未完整识别，原文保留；不能用自动分区控件覆盖，请先在原工具调整后重新导入。";
 private const string File=@"X:\UnattendLocal-diskpart.txt";
 public static int SystemSize(InstallPlan p)=>p.SystemPartitionSizeMB??(p.DiskLayout=="UEFI / GPT"?300:500);
 public static bool CanEdit(InstallPlan p)=>p.ImportedXml?.AutoPartitionReadOnly!=true;
 public static void Validate(InstallPlan p){
  if(p.DiskMode!=InstallationSettings.Wipe)return;
  int minimum=p.DiskLayout=="UEFI / GPT"?300:100;
  if(SystemSize(p)<minimum||SystemSize(p)>8192)throw new ArgumentException($"系统分区大小应为 {minimum}–8192 MB；留空使用当前布局的默认大小（GPT 300 / MBR 500 MB）。更复杂布局请使用自定义分区脚本。");
 }
 public static string Summary(InstallPlan p)=>p.ImportedXml?.AutoPartitionReadOnly==true?"原 XML 的分区流程未完整识别，大小与暂停状态不按界面默认值推断；请核对原文。":p.DiskMode!=InstallationSettings.Wipe?"系统分区大小与分区前暂停仅用于清空指定磁盘后自动分区。":$"系统分区 {SystemSize(p)} MB（{(p.SystemPartitionSizeMB.HasValue?"指定大小":"布局默认")}）；恢复分区 {p.RecoverySizeMB} MB；{(p.PauseBeforeFormatting?"执行 DiskPart 前暂停，按任意键继续":"不额外暂停")}";
 public static string Command(InstallPlan p){
  string Number(int? n)=>n!.Value.ToString(CultureInfo.InvariantCulture);
  var commands=new List<string>{"select disk "+Number(p.TargetDiskId),"clean"};
  if(p.DiskLayout=="UEFI / GPT")commands.AddRange(["convert gpt","create partition efi size="+SystemSize(p).ToString(CultureInfo.InvariantCulture),"format quick fs=fat32 label=System","create partition msr size=16"]);
  else commands.AddRange(["convert mbr","create partition primary size="+SystemSize(p).ToString(CultureInfo.InvariantCulture),"format quick fs=ntfs label=System","active"]);
  commands.AddRange(["create partition primary","format quick fs=ntfs label=Windows",$"shrink minimum={p.RecoverySizeMB.ToString(CultureInfo.InvariantCulture)} desired={p.RecoverySizeMB.ToString(CultureInfo.InvariantCulture)}","create partition primary","format quick fs=ntfs label=Recovery"]);
  if(p.DiskLayout=="UEFI / GPT")commands.AddRange(["set id=de94bba4-06d1-4d40-a16a-bfd50179d6ac","gpt attributes=0x8000000000000001"]);else commands.Add("set id=27");
  string pause=p.PauseBeforeFormatting?$"echo WARNING: Disk {Number(p.TargetDiskId)} will be erased. Verify the target disk before continuing. && pause && ":"";
  return "cmd.exe /c \"("+string.Join(" & ",commands.Select(c=>"echo "+c))+") > "+File+" && "+pause+"diskpart.exe /s "+File+"\"";
 }
 public record Source(bool Mentioned,bool Known,InstallPlan? Values,XmlElement? Path);
 private static XmlNamespaceManager Ns(XmlDocument doc){var n=new XmlNamespaceManager(doc.NameTable);n.AddNamespace("u",XmlMerge.Namespace);return n;}
 public static Source Inspect(XmlDocument doc){
  var ns=Ns(doc);
  var paths=doc.SelectNodes("//u:RunSynchronousCommand/u:Path",ns)!.Cast<XmlElement>().Where(e=>e.InnerText.Contains(File,StringComparison.OrdinalIgnoreCase)&&e.InnerText.Contains("echo clean",StringComparison.OrdinalIgnoreCase)).ToArray();
  if(paths.Length==0)return new(doc.SelectNodes("//*[local-name()='PEScriptCopy']")!.Count>0,false,null,null);
  var node=paths[0];if(paths.Length!=1||node.ParentNode?.ParentNode?.ParentNode is not XmlElement component||component.GetAttribute("name")!="Microsoft-Windows-Setup"||component.ParentNode is not XmlElement settings||settings.GetAttribute("pass")!="windowsPE")return new(true,false,null,node);
  var matches=Regex.Match(node.InnerText,@"echo select disk (?<disk>\d+) & echo clean & echo convert (?<layout>gpt|mbr) & echo create partition (?:efi|primary) size=(?<size>\d+).*?shrink minimum=(?<recovery>\d+) desired=\d+",RegexOptions.CultureInvariant,TimeSpan.FromSeconds(1));
  if(!matches.Success||!int.TryParse(matches.Groups["disk"].Value,out int disk)||!int.TryParse(matches.Groups["size"].Value,out int size)||!int.TryParse(matches.Groups["recovery"].Value,out int recovery))return new(true,false,null,node);
  var p=new InstallPlan{DiskMode=InstallationSettings.Wipe,DiskLayout=matches.Groups["layout"].Value=="gpt"?"UEFI / GPT":"BIOS / MBR",TargetDiskId=disk,RecoverySizeMB=recovery,SystemPartitionSizeMB=size,PauseBeforeFormatting=node.InnerText.Contains("&& pause &&",StringComparison.Ordinal)};
  string expectedPartition=p.DiskLayout=="UEFI / GPT"?"3":"2";
  var targets=component.SelectNodes("u:ImageInstall/u:OSImage/u:InstallTo",ns)!.Cast<XmlElement>().ToArray();
  bool known=node.InnerText==Command(p)&&targets.Length==1&&targets[0].SelectSingleNode("u:DiskID",ns)?.InnerText==disk.ToString(CultureInfo.InvariantCulture)&&targets[0].SelectSingleNode("u:PartitionID",ns)?.InnerText==expectedPartition;
  try{Validate(p);}catch(ArgumentException){known=false;}
  if(size==(p.DiskLayout=="UEFI / GPT"?300:500))p.SystemPartitionSizeMB=null;
  return new(true,known,known?p:null,node);
 }
 public static Source ReadIntoPlan(XmlDocument doc,InstallPlan p,List<string> notes){
  var state=Inspect(doc);
  if(state is {Known:true,Values:{} values}){p.DiskMode=InstallationSettings.Wipe;p.DiskLayout=values.DiskLayout;p.TargetDiskId=values.TargetDiskId;p.RecoverySizeMB=values.RecoverySizeMB;p.SystemPartitionSizeMB=p.SystemPartitionSizeMB==SystemSize(values)?p.SystemPartitionSizeMB:values.SystemPartitionSizeMB;p.PauseBeforeFormatting=values.PauseBeforeFormatting;}
  else if(state.Mentioned)notes.Add(ReadOnlyNotice);
  return state;
 }
 public static void ValidateImported(InstallPlan p,XmlDocument doc){
  var source=Inspect(doc);if(!source.Mentioned||source.Known)return;
  var before=Inspect(XmlMerge.Read(p.ImportedXml!.BaselineXml));
  if(before is {Known:true,Values:{} baseline}){
   if(p.DiskMode!=InstallationSettings.Wipe||p.TargetDiskId!=baseline.TargetDiskId||Command(p)!=Command(baseline))throw new ArgumentException(ReadOnlyNotice);
  }else if(p.DiskMode==InstallationSettings.Wipe)throw new ArgumentException(ReadOnlyNotice);
 }
 public static void NormalizeLoaded(InstallPlan p,JsonElement json){
  if(p.ImportedXml is not {OriginalOmitted:false} data||data.SourceXml.Length==0)return;
  var state=Inspect(XmlMerge.Read(data.SourceXml));data.AutoPartitionReadOnly=state.Mentioned&&!state.Known;
  if(data.AutoPartitionReadOnly&&!data.Notices.Contains(ReadOnlyNotice))data.Notices=[..data.Notices,ReadOnlyNotice];
  if(state is not {Known:true,Values:{} values})return;
  bool sizeMissing=!json.TryGetProperty(nameof(InstallPlan.SystemPartitionSizeMB),out _),pauseMissing=!json.TryGetProperty(nameof(InstallPlan.PauseBeforeFormatting),out _);
  if(!sizeMissing&&!pauseMissing)return;
  if(sizeMissing)p.SystemPartitionSizeMB=values.SystemPartitionSizeMB;if(pauseMissing)p.PauseBeforeFormatting=values.PauseBeforeFormatting;
  if(data.BaselineXml.Length==0)return;
  var baseline=XmlMerge.Read(data.BaselineXml);var old=Inspect(baseline);
  if(old is {Known:true,Values:{} oldValues,Path:{} path}){
   if(sizeMissing)oldValues.SystemPartitionSizeMB=values.SystemPartitionSizeMB;if(pauseMissing)oldValues.PauseBeforeFormatting=values.PauseBeforeFormatting;
   path.InnerText=Command(oldValues);data.BaselineXml=baseline.OuterXml;
  }else if(values.SystemPartitionSizeMB.HasValue||values.PauseBeforeFormatting)data.Blockers=data.Blockers.Append("旧方案的自动分区编辑基准无法可靠迁移，请重新导入原 XML；当前修改已保留。").Distinct().ToArray();
 }
}
