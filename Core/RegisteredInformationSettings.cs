using System.Text.Json;
using System.Xml;
namespace UnattendLocal.Core;

public static class RegisteredInformationSettings {
 private const string Component="Microsoft-Windows-Shell-Setup";
 public static string[] Passes=>["specialize","oobeSystem","offlineServicing","generalize","auditUser"];
 public const string ReadOnlyNotice="原 XML 的 Windows 注册信息存在重复、额外结构或无法完整识别的内容，原文保留；注册信息区域只读，不能用界面默认值覆盖。";
 private const string MigrationBlocker="旧方案的注册信息编辑基准无法可靠迁移，请重新导入原 XML；当前待处理修改保留。";
 public const string Description="分别设置 Windows 的注册所有者和组织名称，默认均不勾选、不写入覆盖。它们是系统注册信息，不创建或重命名登录账户，不更改计算机名、不加入域，也不改变激活和许可证状态。每项最长 256 字符；勾选后留空会明确生成空元素，不勾选则省略该项。新配置在 specialize 阶段设置；完整识别的导入 XML 保留原执行阶段。方案省略密码等凭据时仍保存这些名称，预览也不遮蔽；分享前请按需要处理个人或单位信息。只生成配置，不修改当前电脑。";
 public record Entry(string Value,string Pass);
 public record Source(bool Present,bool Known,Entry? Owner,Entry? Organization);
 private static XmlNamespaceManager Ns(XmlDocument doc){var ns=new XmlNamespaceManager(doc.NameTable);ns.AddNamespace("u",XmlMerge.Namespace);return ns;}
 private static XmlElement[] Nodes(XmlDocument doc)=>doc.SelectNodes("//*[local-name()='RegisteredOwner' or local-name()='RegisteredOrganization']")!.Cast<XmlElement>().ToArray();
 public static bool CanEdit(InstallPlan p)=>p.ImportedXml?.RegisteredInformationReadOnly!=true;
 private static bool Configured(InstallPlan p)=>p.UseRegisteredOwner||p.UseRegisteredOrganization;
 private static void ValidateValue(string? value,string pass,string label){
  if(value==null||value.Length>256||value.Any(char.IsControl)||value!=value.Trim())throw new ArgumentException(label+"最多 256 字符，不能包含控制字符或首尾空白；允许留空以生成空元素。");
  try{XmlConvert.VerifyXmlChars(value);}catch(XmlException){throw new ArgumentException(label+"包含 XML 非法字符，请核对输入。");}
  if(!Passes.Contains(pass))throw new ArgumentException("注册信息的执行阶段无效，请重新导入原 XML。");
 }
 public static void Validate(InstallPlan p){
  if(p.UseRegisteredOwner)ValidateValue(p.RegisteredOwner,p.RegisteredOwnerPass,"注册所有者");
  if(p.UseRegisteredOrganization)ValidateValue(p.RegisteredOrganization,p.RegisteredOrganizationPass,"组织名称");
 }
 private static string PassLabel(string pass)=>pass switch {"specialize"=>"安装阶段","oobeSystem"=>"OOBE 阶段","offlineServicing"=>"离线维护阶段","generalize"=>"常规化阶段","auditUser"=>"审核用户阶段",_=>"未知阶段"};
 public static string Summary(InstallPlan p){
  if(!CanEdit(p))return "Windows 注册信息：原 XML 只读保留，请核对原文。";
  string Text(bool enabled,string? value,string pass)=>!enabled?"不写入覆盖":(value==""?"明确写入空值":value??"待填写")+"（"+PassLabel(pass)+"）";
  return "Windows 注册信息：所有者 "+Text(p.UseRegisteredOwner,p.RegisteredOwner,p.RegisteredOwnerPass)+"；组织 "+Text(p.UseRegisteredOrganization,p.RegisteredOrganization,p.RegisteredOrganizationPass)+"。";
 }
 private static XmlElement GetComponent(XmlDocument doc,string pass,string arch){
  var ns=Ns(doc);var settings=doc.SelectNodes("/u:unattend/u:settings[@pass='"+pass+"']",ns)!.Cast<XmlElement>().ToArray();
  if(settings.Length>1)throw new ArgumentException("注册信息的执行阶段重复，不能可靠生成。");
  XmlElement group;
  if(settings.Length==1)group=settings[0];
  else{
   group=doc.CreateElement("settings",XmlMerge.Namespace);group.SetAttribute("pass",pass);
   var anchor=doc.DocumentElement!.ChildNodes.OfType<XmlElement>().FirstOrDefault(e=>e.NamespaceURI!=XmlMerge.Namespace||e.LocalName!="settings");
   doc.DocumentElement.InsertBefore(group,anchor);
  }
  var components=group.ChildNodes.OfType<XmlElement>().Where(e=>e.NamespaceURI==XmlMerge.Namespace&&e.LocalName=="component"&&e.GetAttribute("name")==Component&&e.GetAttribute("processorArchitecture")==arch).ToArray();
  if(components.Length>1)throw new ArgumentException("注册信息的 Shell-Setup 组件重复，不能可靠生成。");
  if(components.Length==1)return components[0];
  var component=doc.CreateElement("component",XmlMerge.Namespace);
  foreach(var pair in new Dictionary<string,string>{{"name",Component},{"processorArchitecture",arch},{"publicKeyToken","31bf3856ad364e35"},{"language","neutral"},{"versionScope","nonSxS"}})component.SetAttribute(pair.Key,pair.Value);
  group.AppendChild(component);return component;
 }
 public static void Apply(XmlDocument doc,InstallPlan p){
  Validate(p);
  // Retain imported non-default phase containers so removing one known field
  // never requires deleting its component and any unknown sibling settings.
  foreach(string pass in new[]{p.RegisteredOwnerPass,p.RegisteredOrganizationPass}.Where(pass=>pass!="specialize"&&Passes.Contains(pass)).Distinct())GetComponent(doc,pass,p.Architecture);
  if(!Configured(p))return;
  if(Nodes(doc).Length>0)throw new ArgumentException("生成基准已有注册信息，不能重复添加。");
  void Add(string name,string value,string pass){var e=doc.CreateElement(name,XmlMerge.Namespace);e.InnerText=value;GetComponent(doc,pass,p.Architecture).AppendChild(e);}
  if(p.UseRegisteredOwner)Add("RegisteredOwner",p.RegisteredOwner,p.RegisteredOwnerPass);
  if(p.UseRegisteredOrganization)Add("RegisteredOrganization",p.RegisteredOrganization,p.RegisteredOrganizationPass);
 }
 public static Source Inspect(XmlDocument doc){
  var nodes=Nodes(doc);var unknown=new Source(nodes.Length>0,false,null,null);
  if(nodes.Length==0)return new(false,true,null,null);
  if(nodes.GroupBy(e=>e.LocalName).Any(g=>g.Count()!=1))return unknown;
  Entry? owner=null,organization=null;
  foreach(var node in nodes){
   if(node.NamespaceURI!=XmlMerge.Namespace||node.ChildNodes.OfType<XmlElement>().Any()||node.Attributes.Cast<XmlAttribute>().Any(a=>a.NamespaceURI!="http://www.w3.org/2000/xmlns/"))return unknown;
   if(node.ParentNode is not XmlElement c||c.LocalName!="component"||c.NamespaceURI!=XmlMerge.Namespace||c.GetAttribute("name")!=Component||c.ParentNode is not XmlElement s||s.LocalName!="settings"||s.NamespaceURI!=XmlMerge.Namespace||s.ParentNode!=doc.DocumentElement)return unknown;
   string pass=s.GetAttribute("pass"),arch=c.GetAttribute("processorArchitecture");
   if(!Passes.Contains(pass)||arch is not ("amd64" or "arm64" or "x86"))return unknown;
   if(doc.DocumentElement!.ChildNodes.OfType<XmlElement>().Count(e=>e.NamespaceURI==XmlMerge.Namespace&&e.LocalName=="settings"&&e.GetAttribute("pass")==pass)!=1||s.ChildNodes.OfType<XmlElement>().Count(e=>e.NamespaceURI==XmlMerge.Namespace&&e.LocalName=="component"&&e.GetAttribute("name")==Component&&e.GetAttribute("processorArchitecture")==arch)!=1)return unknown;
   var expected=new Dictionary<string,string>{{"name",Component},{"processorArchitecture",arch},{"publicKeyToken","31bf3856ad364e35"},{"language","neutral"},{"versionScope","nonSxS"}};
   if(c.Attributes.Cast<XmlAttribute>().Where(a=>a.NamespaceURI!="http://www.w3.org/2000/xmlns/").Any(a=>a.NamespaceURI.Length!=0||!expected.TryGetValue(a.Name,out string? value)||a.Value!=value)||expected.Keys.Where(k=>k!="versionScope").Any(k=>!c.HasAttribute(k)))return unknown;
   try{ValidateValue(node.InnerText,pass,node.LocalName);}catch(ArgumentException){return unknown;}
   var entry=new Entry(node.InnerText,pass);if(node.LocalName=="RegisteredOwner")owner=entry;else organization=entry;
  }
  return new(true,true,owner,organization);
 }
 private static void Assign(InstallPlan p,Source source){
  p.UseRegisteredOwner=source.Known&&source.Owner!=null;p.UseRegisteredOrganization=source.Known&&source.Organization!=null;
  if(p.UseRegisteredOwner){p.RegisteredOwner=source.Owner!.Value;p.RegisteredOwnerPass=source.Owner.Pass;}
  if(p.UseRegisteredOrganization){p.RegisteredOrganization=source.Organization!.Value;p.RegisteredOrganizationPass=source.Organization.Pass;}
 }
 public static Source ReadIntoPlan(XmlDocument doc,InstallPlan p,List<string> notes){
  var state=Inspect(doc);Assign(p,state);if(!state.Known)notes.Add(ReadOnlyNotice);return state;
 }
 public static void UpdateImportState(ImportedXml data,Source state){data.RegisteredInformationImportVersion=1;data.RegisteredInformationReadOnly=!state.Known;}
 public static void ValidateImported(InstallPlan p,XmlDocument source){if(Configured(p)&&!Inspect(source).Known)throw new ArgumentException(ReadOnlyNotice);}
 public static void NormalizeLoaded(InstallPlan p,JsonElement root){
  if(p.ImportedXml is not {OriginalOmitted:false} data||data.SourceXml.Length==0)return;
  var source=XmlMerge.Read(data.SourceXml);var state=Inspect(source);
  if(data.RegisteredInformationImportVersion<1&&state is {Present:true,Known:true}){
   string[] fields=[nameof(InstallPlan.UseRegisteredOwner),nameof(InstallPlan.RegisteredOwner),nameof(InstallPlan.RegisteredOwnerPass),nameof(InstallPlan.UseRegisteredOrganization),nameof(InstallPlan.RegisteredOrganization),nameof(InstallPlan.RegisteredOrganizationPass)];
   bool missing=fields.All(f=>!root.TryGetProperty(f,out _));
   if(missing&&data.BaselineXml.Length>0&&Nodes(XmlMerge.Read(data.BaselineXml)).Length==0){
    try{
     var baseline=XmlMerge.Read(data.BaselineXml);var next=new InstallPlan{Architecture=Nodes(source)[0].ParentNode!.Attributes!["processorArchitecture"]!.Value};Assign(next,state);Apply(baseline,next);Assign(p,state);data.BaselineXml=baseline.OuterXml;
    }catch(ArgumentException){data.Blockers=data.Blockers.Append(MigrationBlocker).Distinct().ToArray();}
   }else data.Blockers=data.Blockers.Append(MigrationBlocker).Distinct().ToArray();
  }
  data.Notices=data.Notices.Where(n=>n!=ReadOnlyNotice).ToArray();if(!state.Known)data.Notices=[..data.Notices,ReadOnlyNotice];UpdateImportState(data,state);
 }
}
