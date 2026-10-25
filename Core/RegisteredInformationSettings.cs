using System.Text.Json;
using System.Xml;
namespace UnattendLocal.Core;

public static class RegisteredInformationSettings {
 private const string Component="Microsoft-Windows-Shell-Setup";
 public static string[] Passes=>["specialize","oobeSystem","offlineServicing","generalize","auditUser"];
 public static readonly string ReadOnlyNotice=global::UnattendLocal.Localization.UiText.Source("text.dc161972f60b");
 private static readonly string MigrationBlocker=global::UnattendLocal.Localization.UiText.Source("text.4b5699e9bc17");
 public static readonly string Description=global::UnattendLocal.Localization.UiText.Source("text.481c408499d2");
 public record Entry(string Value,string Pass);
 public record Source(bool Present,bool Known,Entry? Owner,Entry? Organization);
 private static XmlNamespaceManager Ns(XmlDocument doc){var ns=new XmlNamespaceManager(doc.NameTable);ns.AddNamespace("u",XmlMerge.Namespace);return ns;}
 private static XmlElement[] Nodes(XmlDocument doc)=>doc.SelectNodes("//*[local-name()='RegisteredOwner' or local-name()='RegisteredOrganization']")!.Cast<XmlElement>().ToArray();
 public static bool CanEdit(InstallPlan p)=>p.ImportedXml?.RegisteredInformationReadOnly!=true;
 private static bool Configured(InstallPlan p)=>p.UseRegisteredOwner||p.UseRegisteredOrganization;
 private static void ValidateValue(string? value,string pass,string label){
  if(value==null||value.Length>256||value.Any(char.IsControl)||value!=value.Trim())throw new ArgumentException(global::UnattendLocal.Localization.UiText.Concat(new object?[] { label, global::UnattendLocal.Localization.UiText.Source("text.6e9afcc86d8a") },new bool[]{false,false}));
  try{XmlConvert.VerifyXmlChars(value);}catch(XmlException){throw new ArgumentException(global::UnattendLocal.Localization.UiText.Concat(new object?[] { label, global::UnattendLocal.Localization.UiText.Source("text.dede746a2286") },new bool[]{false,false}));}
  if(!Passes.Contains(pass))throw new ArgumentException(global::UnattendLocal.Localization.UiText.Source("text.c9608b7ac1e1"));
 }
 public static void Validate(InstallPlan p){
  if(p.UseRegisteredOwner)ValidateValue(p.RegisteredOwner,p.RegisteredOwnerPass,global::UnattendLocal.Localization.UiText.Source("text.b36d05aba66e"));
  if(p.UseRegisteredOrganization)ValidateValue(p.RegisteredOrganization,p.RegisteredOrganizationPass,global::UnattendLocal.Localization.UiText.Source("text.3f38e493103b"));
 }
 private static string PassLabel(string pass)=>pass switch {"specialize"=>global::UnattendLocal.Localization.UiText.Source("text.f7ea293ea339"),"oobeSystem"=>global::UnattendLocal.Localization.UiText.Source("text.3e8fe498174e"),"offlineServicing"=>global::UnattendLocal.Localization.UiText.Source("text.a94d7d9ee146"),"generalize"=>global::UnattendLocal.Localization.UiText.Source("text.2edaaa2873a4"),"auditUser"=>global::UnattendLocal.Localization.UiText.Source("text.f009f2a38ac6"),_=>global::UnattendLocal.Localization.UiText.Source("text.8106dad736f1")};
 public static string Summary(InstallPlan p){
  if(!CanEdit(p))return global::UnattendLocal.Localization.UiText.Source("text.366389928c9a");
  string Text(bool enabled,string? value,string pass)=>!enabled?global::UnattendLocal.Localization.UiText.Source("text.23c0a12a1d32"):global::UnattendLocal.Localization.UiText.Concat(new object?[] { (value==""?global::UnattendLocal.Localization.UiText.Source("text.40b78a9c1763"):value??global::UnattendLocal.Localization.UiText.Source("text.4f28ec483981")), "（", PassLabel(pass), "）" },new bool[]{!string.IsNullOrEmpty(value),false,false,false});
  return global::UnattendLocal.Localization.UiText.Concat(new object?[] { global::UnattendLocal.Localization.UiText.Source("text.5d9ac2eb2416"), Text(p.UseRegisteredOwner,p.RegisteredOwner,p.RegisteredOwnerPass), global::UnattendLocal.Localization.UiText.Source("text.1ff180297f96"), Text(p.UseRegisteredOrganization,p.RegisteredOrganization,p.RegisteredOrganizationPass), "。" },new bool[]{false,false,false,false,false});
 }
 private static XmlElement GetComponent(XmlDocument doc,string pass,string arch){
  var ns=Ns(doc);var settings=doc.SelectNodes("/u:unattend/u:settings[@pass='"+pass+"']",ns)!.Cast<XmlElement>().ToArray();
  if(settings.Length>1)throw new ArgumentException(global::UnattendLocal.Localization.UiText.Source("text.ae2c158bff88"));
  XmlElement group;
  if(settings.Length==1)group=settings[0];
  else{
   group=doc.CreateElement("settings",XmlMerge.Namespace);group.SetAttribute("pass",pass);
   var anchor=doc.DocumentElement!.ChildNodes.OfType<XmlElement>().FirstOrDefault(e=>e.NamespaceURI!=XmlMerge.Namespace||e.LocalName!="settings");
   doc.DocumentElement.InsertBefore(group,anchor);
  }
  var components=group.ChildNodes.OfType<XmlElement>().Where(e=>e.NamespaceURI==XmlMerge.Namespace&&e.LocalName=="component"&&e.GetAttribute("name")==Component&&e.GetAttribute("processorArchitecture")==arch).ToArray();
  if(components.Length>1)throw new ArgumentException(global::UnattendLocal.Localization.UiText.Source("text.f2a206cc6205"));
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
  if(Nodes(doc).Length>0)throw new ArgumentException(global::UnattendLocal.Localization.UiText.Source("text.28a016820442"));
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
