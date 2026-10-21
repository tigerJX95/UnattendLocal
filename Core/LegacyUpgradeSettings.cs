using System.Text;
using System.Text.Json;
using System.Xml;
namespace UnattendLocal.Core;

// Recognize the complete historical MoSetup command independently of PE LabConfig.
public static class LegacyUpgradeSettings {
 public const string Marker="AllowUpgradesWithUnsupportedTPMOrCPU";
 public const string Command="reg.exe add \"HKLM\\SYSTEM\\Setup\\MoSetup\" /v AllowUpgradesWithUnsupportedTPMOrCPU /t REG_DWORD /d 1 /f;";
 public const string Description="兼容旧版 XML 的 MoSetup 设置：在目标系统安装阶段写入 AllowUpgradesWithUnsupportedTPMOrCPU=1。与 Windows PE 阶段的 TPM、安全启动、内存检查选项独立，不会联动勾选。这是旧版升级兼容设置，不保证当前或后续 Windows 安装程序采用，也不能补足硬件或 CPU 指令集要求。默认关闭；取消仅从本次导出配置中移除已识别命令，不删除已安装系统的注册表值。本工具不在当前电脑执行命令。";
 public record State(bool Enabled,bool Ambiguous);
 public static State Inspect(XmlDocument source){
  var doc=XmlImportPaths.Inspection(source);
  var lines=doc.SelectNodes("//*[local-name()='File' or local-name()='Path' or local-name()='CommandLine']")!.Cast<XmlElement>()
   .SelectMany(e=>e.InnerText.Replace("\r\n","\n").Split('\n')).Where(l=>l.Contains(Marker,StringComparison.OrdinalIgnoreCase)).ToArray();
  bool enabled=lines.Length==1&&lines[0].Trim()==Command&&AdditionalSetupSettings.KnownBlocks(doc,"Specialize.ps1").Count(b=>b==Command)==1;
  return new(enabled,lines.Length>0&&!enabled);
 }
 public static void ReadIntoPlan(XmlDocument doc,InstallPlan p,List<string> notes){
  var state=Inspect(doc);p.AllowUnsupportedUpgrade=state.Enabled;
  if(state.Ambiguous)notes.Add("MoSetup 旧版升级兼容命令的参数、执行阶段或调用链未完整识别；原文已保留，此项只读，不能用界面开关覆盖。");
 }
 public static void Apply(XmlDocument doc,InstallPlan p){
  if(!p.AllowUnsupportedUpgrade)return;
  var file=doc.SelectNodes("//*[local-name()='File']")!.Cast<XmlElement>().Single(f=>f.GetAttribute("path")==@"C:\Windows\Setup\Scripts\Specialize.ps1");
  const string start="$scripts = @(\n";string text=file.InnerText.Replace("\r\n","\n");
  int at=text.IndexOf(start,StringComparison.Ordinal);
  if(at<0)throw new ArgumentException("找不到安装脚本命令列表，不能生成 MoSetup 兼容设置。");
  file.InnerText=text.Insert(at+start.Length,"\t{\n\t\t"+Command+"\n\t};\n");
 }
 public static void ValidateImported(InstallPlan p,XmlDocument source){
  if(Inspect(source).Ambiguous&&p.AllowUnsupportedUpgrade)throw new ArgumentException("MoSetup 原命令未完整识别，不能使用兼容开关覆盖；请在原 XML 核对后重新导入。");
 }
 public static void NormalizeLoaded(InstallPlan p,JsonElement json){
  if(p.ImportedXml is not {OriginalOmitted:false} data||data.SourceXml.Length==0||data.BaselineXml.Length==0)return;
  var state=Inspect(XmlMerge.Read(data.SourceXml));data.LegacyUpgradeReadOnly=state.Ambiguous;
  if(json.TryGetProperty(nameof(InstallPlan.AllowUnsupportedUpgrade),out _))return;
  if(!state.Enabled)return;
  var before=JsonSerializer.Deserialize<InstallPlan>(JsonSerializer.Serialize(p))!;
  before.ImportedXml=null;before.ConfirmWipeDisk=before.ConfirmFormatPartition=before.ConfirmDiskScript=true;
  var after=JsonSerializer.Deserialize<InstallPlan>(JsonSerializer.Serialize(before))!;after.AllowUnsupportedUpgrade=true;
  try{
   var g=new GeneratorService();
   data.BaselineXml=XmlMerge.Apply(data.BaselineXml,Encoding.UTF8.GetString(g.GenerateImportBaseline(before)),XmlMerge.Read(Encoding.UTF8.GetString(g.GenerateImportBaseline(after)))).OuterXml;
   p.AllowUnsupportedUpgrade=true;
  }catch(Exception e)when(e is ArgumentException or Schneegans.Unattend.ConfigurationException or XmlException){
   data.Blockers=data.Blockers.Append("旧方案的 MoSetup 兼容设置无法可靠迁移，请重新导入原 XML。").Distinct().ToArray();
  }
 }
}
