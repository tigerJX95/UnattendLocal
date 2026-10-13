using System.Xml;
using System.Text.RegularExpressions;
namespace UnattendLocal.Core;

public static class CompactOsSettings {
 public const string Default="使用 Windows 默认设置", Enabled="启用 CompactOS（压缩系统文件）", Disabled="禁用 CompactOS（不压缩系统文件）";
 private const string Ns="urn:schemas-microsoft-com:unattend";
 public record SourceState(string Mode,bool ScriptControlled,bool Known);
 public static void Validate(InstallPlan p){
  if(p.CompactOsMode is not (Default or Enabled or Disabled))throw new ArgumentException("请选择有效的 CompactOS 设置方式。");
  if(p.CompactOsMode!=Default&&p.Scripts?.Any(s=>s.Enabled&&Regex.IsMatch(s.Content,@"\bcompact(?:\.exe)?\s+/CompactOS\s*:\s*(always|never)\b",RegexOptions.IgnoreCase,TimeSpan.FromSeconds(1)))==true)throw new ArgumentException("CompactOS 安装设置与自定义脚本中的压缩/解压命令重叠，请只保留一种控制方式后导出。");
 }
 public static bool CanEdit(InstallPlan p)=>p.ImportedXml?.CompactOsScriptControlled!=true;
 public static string ModeSummary(InstallPlan p){
  if(p.ImportedXml is {CompactOsScriptControlled:true} data)return data.CompactOsSourceKnown?(data.CompactOsSourceMode==Enabled?"原 XML 的镜像应用脚本启用 /Compact；保留原参数，暂不可在界面修改":"原 XML 的镜像应用脚本未指定 /Compact；保留原参数，暂不可在界面修改"):"原 XML 使用自定义安装/压缩脚本，状态未完全识别；保留原参数，暂不可在界面修改";
  return p.CompactOsMode switch {Default=>p.ImportedXml==null?"遵循 Windows 安装程序默认行为（不写入覆盖）":"不写入新的压缩覆盖；保留导入 XML 中的原配置",Enabled=>"安装时压缩 Windows 系统文件",Disabled=>"安装时明确使用非 CompactOS 模式",_=>"CompactOS 设置方式无效，请重新选择"};
 }
 public static string Summary(InstallPlan p)=>"CompactOS："+ModeSummary(p);
 // The documented Windows Setup setting is independent of disk selection and destructive operations.
 public static void Apply(XmlDocument doc,XmlNamespaceManager ns,InstallPlan p){
  Validate(p);if(p.CompactOsMode==Default)return;
  var setups=doc.SelectNodes("//u:settings[@pass='windowsPE']/u:component[@name='Microsoft-Windows-Setup']",ns)!;
  if(setups.Count==0)throw new ArgumentException("找不到 Windows Setup 组件，无法写入 CompactOS 安装设置。");
  foreach(XmlElement setup in setups){
   var imageInstall=setup.SelectSingleNode("u:ImageInstall",ns) as XmlElement??Add(setup,"ImageInstall");
   var image=imageInstall.SelectSingleNode("u:OSImage",ns) as XmlElement??Add(imageInstall,"OSImage");
   var compact=image.SelectSingleNode("u:Compact",ns) as XmlElement??Add(image,"Compact");
   compact.InnerText=p.CompactOsMode==Enabled?"true":"false";
  }
 }
 private static XmlElement Add(XmlElement parent,string name){var e=parent.OwnerDocument!.CreateElement(name,Ns);parent.AppendChild(e);return e;}
 public static SourceState Inspect(XmlDocument doc){
  var ns=new XmlNamespaceManager(doc.NameTable);ns.AddNamespace("u",Ns);
  var nodes=doc.SelectNodes("//u:settings[@pass='windowsPE']/u:component[@name='Microsoft-Windows-Setup']/u:ImageInstall/u:OSImage/u:Compact",ns)!;
  if(nodes.Count>1)throw new ArgumentException("XML 含多个 CompactOS 安装设置，暂不能可靠还原。");
  string native=Default;
  if(nodes.Count==1)native=nodes[0]!.InnerText.Trim() switch {"true" or "1"=>Enabled,"false" or "0"=>Disabled,_=>throw new ArgumentException("XML 的 CompactOS 设置必须为 true、false、1 或 0。")};
  var copies=doc.SelectNodes("//*[local-name()='PEScriptCopy']")!.Cast<XmlElement>().ToArray();
  var payloads=copies.Length>0?copies.Select(e=>e.InnerText):doc.SelectNodes("//*[local-name()='File' or local-name()='Path']")!.Cast<XmlElement>().Select(e=>e.InnerText);
  var applyLines=payloads.SelectMany(s=>s.Replace("\r\n","\n").Split('\n')).Where(line=>Regex.IsMatch(line,@"\bdism(?:\.exe)?\s+/Apply-Image\b",RegexOptions.IgnoreCase,TimeSpan.FromSeconds(1))).ToArray();
  bool otherCommand=Regex.IsMatch(doc.InnerText,@"\bcompact(?:\.exe)?\s+/CompactOS\s*:\s*(always|never)\b",RegexOptions.IgnoreCase,TimeSpan.FromSeconds(1));
  bool scripted=copies.Length>0||applyLines.Length>0||otherCommand;
  if(scripted&&nodes.Count>0)throw new ArgumentException("XML 同时含 Windows Setup Compact 设置与自定义镜像应用/Compact 命令，无法判断最终状态；请先核对原 XML。");
  if(!scripted)return new(native,false,true);
  if(applyLines.Length==1&&!otherCommand)return new(Regex.IsMatch(applyLines[0],@"(?:^|\s)/Compact(?=\s|$)",RegexOptions.IgnoreCase,TimeSpan.FromSeconds(1))?Enabled:Default,true,true);
  return new(Default,true,false);
 }
 public static SourceState ReadIntoPlan(XmlDocument doc,InstallPlan p,List<string> notes){
  var state=Inspect(doc);p.CompactOsMode=state.Mode;
  if(state.ScriptControlled)notes.Add("CompactOS 由原 XML 的自定义安装/压缩脚本控制，参数保留。本版仅编辑 Windows Setup 原生设置，暂不能在界面修改该脚本的压缩参数。");
  return state;
 }
 public static void UpdateImportState(ImportedXml data,SourceState state){data.CompactOsScriptControlled=state.ScriptControlled;data.CompactOsSourceMode=state.Mode;data.CompactOsSourceKnown=state.Known;}
 public static void ValidateImported(InstallPlan p,XmlDocument source){
  Validate(p);var state=Inspect(source);
  if(state.ScriptControlled&&p.CompactOsMode!=state.Mode)throw new ArgumentException("CompactOS 由原 XML 的安装/压缩脚本控制；当前不能从界面覆盖该脚本参数，请使用原工具调整后重新导入。");
 }
 public static void NormalizeLoaded(InstallPlan p,bool fieldWasPresent){
  if(p.ImportedXml is not {} data||data.SourceXml.Length==0||data.OriginalOmitted)return;
  var state=Inspect(XmlMerge.Read(data.SourceXml));UpdateImportState(data,state);
  if(fieldWasPresent)return;
  // A pre40 JSON may contain an XML that pre39 retained without understanding CompactOS.
  p.CompactOsMode=state.Mode;
  if(data.BaselineXml.Length>0&&state.Mode!=Default){var baseline=XmlMerge.Read(data.BaselineXml);var ns=new XmlNamespaceManager(baseline.NameTable);ns.AddNamespace("u",Ns);Apply(baseline,ns,p);data.BaselineXml=baseline.OuterXml;}
  if(state.ScriptControlled&&!data.Notices.Any(n=>n.Contains("CompactOS 由")))data.Notices=[..data.Notices,"CompactOS 由原 XML 的安装/压缩脚本控制，原参数保留，暂不可在界面修改。"];
 }
}
