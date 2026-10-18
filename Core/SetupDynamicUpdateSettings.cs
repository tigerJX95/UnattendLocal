using System.Text.Json;
using System.Xml;
namespace UnattendLocal.Core;

public static class SetupDynamicUpdateSettings {
 public const string Default="遵循安装程序默认";
 public const string Enabled="启用动态更新";
 public const string Disabled="禁用动态更新";
 public const string UiDefault="遵循默认显示行为";
 public const string UiOnError="仅出错时显示";
 public const string UiNever="不显示更新界面";
 public const string UiAlways="始终显示更新界面";
 public static string[] Modes=>[Default,Enabled,Disabled];
 public static string[] UiModes=>[UiDefault,UiOnError,UiNever,UiAlways];
 public const string ReadOnlyNotice="原 XML 的动态更新配置或 PE 流程未完整识别，原文保留；动态更新区域只读，不能用界面默认值覆盖。";
 public const string Description="默认不写入覆盖。此项控制 Windows Setup 安装阶段的动态更新，可获取新的安装文件、驱动及其他安装所需文件；实际支持及联网行为取决于目标安装程序、介质、网络和组织策略。启用不保证能取得更新，也不替代安装后的 Windows Update；禁用仅影响此安装阶段，不关闭安装后的更新服务。界面显示行为可独立设置；默认不写入 WillShowUI，微软文档的默认值为仅出错时显示。不显示更新界面不保证隐藏严重错误，也不自动处理错误。更改动态更新方式后需重新确认危险磁盘操作。不会更改当前电脑，不会配置 PE 网络或访问凭据。自定义 PE/DISM 安装流程只读保留，不由本区域覆盖。";
 private const string Component="Microsoft-Windows-Setup";
 private const string MigrationBlocker="旧方案的动态更新编辑基准无法可靠迁移，请重新导入原 XML；当前待处理修改保留。";
 public record Source(bool Present,bool Known,string Mode,string UiMode);
 private static XmlNamespaceManager Ns(XmlDocument doc){var ns=new XmlNamespaceManager(doc.NameTable);ns.AddNamespace("u",XmlMerge.Namespace);return ns;}
 private static XmlElement[] Nodes(XmlDocument doc)=>doc.SelectNodes("//*[local-name()='DynamicUpdate']")!.Cast<XmlElement>().ToArray();
 private static XmlElement[] SetupComponents(XmlDocument doc)=>doc.SelectNodes("//u:component[@name='"+Component+"']",Ns(doc))!.Cast<XmlElement>().ToArray();
 public static bool CanEdit(InstallPlan p)=>p.ImportedXml?.SetupDynamicUpdateReadOnly!=true;
 private static bool Configured(InstallPlan p)=>p.SetupDynamicUpdateMode!=Default||p.SetupDynamicUpdateUi!=UiDefault;
 public static void Validate(InstallPlan p){
  if(!Modes.Contains(p.SetupDynamicUpdateMode)||!UiModes.Contains(p.SetupDynamicUpdateUi))throw new ArgumentException("请选择有效的安装阶段动态更新及界面显示方式。");
 }
 private static string? UiValue(string mode)=>mode switch {UiDefault=>null,UiOnError=>"OnError",UiNever=>"Never",UiAlways=>"Always",_=>throw new ArgumentException("动态更新界面显示方式无效。")};
 public static string Summary(InstallPlan p)=>!CanEdit(p)?"安装阶段动态更新：原 XML 只读保留，请核对原文。":"安装阶段动态更新："+p.SetupDynamicUpdateMode+"；"+p.SetupDynamicUpdateUi+"。";
 public static void Apply(XmlDocument doc,XmlNamespaceManager ns,InstallPlan p){
  Validate(p);if(!Configured(p))return;
  if(Nodes(doc).Length>0||SetupDriverSettings.CustomPe(doc))throw new ArgumentException("已有动态更新配置或自定义 PE 流程，不能重复添加动态更新设置。");
  var components=SetupComponents(doc);
  if(components.Length!=1||components[0].ParentNode is not XmlElement settings||settings.ParentNode!=doc.DocumentElement||settings.GetAttribute("pass")!="windowsPE"||components[0].GetAttribute("processorArchitecture")!=p.Architecture)throw new ArgumentException("缺少唯一匹配的 Windows Setup / windowsPE 组件，不能配置动态更新。");
  var node=doc.CreateElement("DynamicUpdate",XmlMerge.Namespace);components[0].PrependChild(node);
  void Add(string name,string value){var e=doc.CreateElement(name,XmlMerge.Namespace);e.InnerText=value;node.AppendChild(e);}
  if(p.SetupDynamicUpdateMode!=Default)Add("Enable",p.SetupDynamicUpdateMode==Enabled?"true":"false");
  if(UiValue(p.SetupDynamicUpdateUi) is {} ui)Add("WillShowUI",ui);
 }
 public static Source Inspect(XmlDocument doc){
  var nodes=Nodes(doc);var unknown=new Source(nodes.Length>0,false,Default,UiDefault);
  if(SetupDriverSettings.CustomPe(doc))return unknown;
  if(nodes.Length==0)return new(false,true,Default,UiDefault);
  if(nodes.Length!=1)return unknown;
  var node=nodes[0];var components=SetupComponents(doc);
  if(node.NamespaceURI!=XmlMerge.Namespace||components.Length!=1||node.ParentNode!=components[0])return unknown;
  var component=components[0];
  if(component.ParentNode is not XmlElement s||s.LocalName!="settings"||s.NamespaceURI!=XmlMerge.Namespace||s.ParentNode!=doc.DocumentElement||s.GetAttribute("pass")!="windowsPE")return unknown;
  string arch=component.GetAttribute("processorArchitecture");if(arch is not ("amd64" or "arm64" or "x86"))return unknown;
  var metadata=new Dictionary<string,string>{{"name",Component},{"processorArchitecture",arch},{"publicKeyToken","31bf3856ad364e35"},{"language","neutral"},{"versionScope","nonSxS"}};
  var attrs=component.Attributes.Cast<XmlAttribute>().Where(a=>a.NamespaceURI!="http://www.w3.org/2000/xmlns/").ToArray();
  if(attrs.Length!=metadata.Count||attrs.Any(a=>a.NamespaceURI.Length!=0||!metadata.TryGetValue(a.Name,out var v)||a.Value!=v))return unknown;
  var children=node.ChildNodes.OfType<XmlElement>().ToArray();
  if(children.Length is <1 or >2||children.Any(c=>c.NamespaceURI!=XmlMerge.Namespace)||children.Select(c=>c.LocalName).Distinct().Count()!=children.Length)return unknown;
  string mode=Default,uiMode=UiDefault;
  foreach(var c in children){
   if(c.LocalName=="Enable")mode=c.InnerText.Trim() switch {"true"=>Enabled,"false"=>Disabled,_=>""};
   else if(c.LocalName=="WillShowUI")uiMode=c.InnerText.Trim() switch {"OnError"=>UiOnError,"Never"=>UiNever,"Always"=>UiAlways,_=>""};
   else return unknown;
  }
  if(mode==""||uiMode=="")return unknown;
  var clean=XmlMerge.Read("<unattend xmlns='"+XmlMerge.Namespace+"'><settings pass='windowsPE'><component name='Microsoft-Windows-Setup' processorArchitecture='"+arch+"'/></settings></unattend>");
  Apply(clean,Ns(clean),new InstallPlan{Architecture=arch,SetupDynamicUpdateMode=mode,SetupDynamicUpdateUi=uiMode});
  if(!XmlMerge.Equal(node,Nodes(clean).Single()))return unknown;
  return new(true,true,mode,uiMode);
 }
 private static void Assign(InstallPlan p,Source state){p.SetupDynamicUpdateMode=state.Known?state.Mode:Default;p.SetupDynamicUpdateUi=state.Known?state.UiMode:UiDefault;}
 public static Source ReadIntoPlan(XmlDocument doc,InstallPlan p,List<string> notes){var state=Inspect(doc);Assign(p,state);if(!state.Known)notes.Add(ReadOnlyNotice);return state;}
 public static void UpdateImportState(ImportedXml data,Source state){data.SetupDynamicUpdateReadOnly=!state.Known;data.SetupDynamicUpdateImportVersion=1;}
 public static void ValidateImported(InstallPlan p,XmlDocument source){if(Configured(p)&&!Inspect(source).Known)throw new ArgumentException(ReadOnlyNotice);}
 public static void NormalizeLoaded(InstallPlan p,JsonElement root){
  if(p.ImportedXml is not {OriginalOmitted:false} data||data.SourceXml.Length==0)return;
  var source=XmlMerge.Read(data.SourceXml);var state=Inspect(source);
  if(data.SetupDynamicUpdateImportVersion<1&&state is {Present:true,Known:true}){
   bool missing=!root.TryGetProperty(nameof(InstallPlan.SetupDynamicUpdateMode),out _)&&!root.TryGetProperty(nameof(InstallPlan.SetupDynamicUpdateUi),out _);
   if(missing&&data.BaselineXml.Length>0&&Nodes(XmlMerge.Read(data.BaselineXml)).Length==0){
    try{
     var baseline=XmlMerge.Read(data.BaselineXml);
     var next=new InstallPlan{Architecture=SetupComponents(source).Single().GetAttribute("processorArchitecture"),SetupDynamicUpdateMode=state.Mode,SetupDynamicUpdateUi=state.UiMode};
     Apply(baseline,Ns(baseline),next);Assign(p,state);data.BaselineXml=baseline.OuterXml;
    }catch(ArgumentException){data.Blockers=data.Blockers.Append(MigrationBlocker).Distinct().ToArray();}
   }else data.Blockers=data.Blockers.Append(MigrationBlocker).Distinct().ToArray();
  }
  data.Notices=data.Notices.Where(n=>n!=ReadOnlyNotice).ToArray();
  if(!state.Known)data.Notices=[..data.Notices,ReadOnlyNotice];UpdateImportState(data,state);
 }
}
