using System.Xml;
using System.Text.RegularExpressions;
namespace UnattendLocal.Core;

public static class CompactOsSettings {
 public const string Default="使用 Windows 默认设置", Enabled="启用 CompactOS（压缩系统文件）", Disabled="禁用 CompactOS（不压缩系统文件）";
 private const string Ns="urn:schemas-microsoft-com:unattend";
 public record SourceState(string Mode,bool ScriptControlled,bool Known);
 public static void Validate(InstallPlan p){
  if(p.CompactOsMode is not (Default or Enabled or Disabled))throw new ArgumentException(global::UnattendLocal.Localization.UiText.Source("text.ccd3d2099025"));
  if(p.CompactOsMode!=Default&&p.Scripts?.Any(s=>s.Enabled&&Regex.IsMatch(s.Content,@"\bcompact(?:\.exe)?\s+/CompactOS\s*:\s*(always|never)\b",RegexOptions.IgnoreCase,TimeSpan.FromSeconds(1)))==true)throw new ArgumentException(global::UnattendLocal.Localization.UiText.Source("text.36e368573827"));
 }
 public static bool CanEdit(InstallPlan p)=>p.ImportedXml?.CompactOsScriptControlled!=true;
 public static string ModeSummary(InstallPlan p){
  if(p.ImportedXml is {CompactOsScriptControlled:true} data)return data.CompactOsSourceKnown?(data.CompactOsSourceMode==Enabled?global::UnattendLocal.Localization.UiText.Source("text.8072410d481b"):global::UnattendLocal.Localization.UiText.Source("text.aeed9b0d0e0e")):global::UnattendLocal.Localization.UiText.Source("text.b37c0d8efaf5");
  return p.CompactOsMode switch {Default=>p.ImportedXml==null?global::UnattendLocal.Localization.UiText.Source("text.e8dbcfc17a43"):global::UnattendLocal.Localization.UiText.Source("text.32e8cdb88dd3"),Enabled=>global::UnattendLocal.Localization.UiText.Source("text.3e26eb1f1503"),Disabled=>global::UnattendLocal.Localization.UiText.Source("text.32e8e71f5354"),_=>global::UnattendLocal.Localization.UiText.Source("text.70f5443b76d7")};
 }
 public static string Summary(InstallPlan p)=>"CompactOS："+ModeSummary(p);
 // The documented Windows Setup setting is independent of disk selection and destructive operations.
 public static void Apply(XmlDocument doc,XmlNamespaceManager ns,InstallPlan p){
  Validate(p);if(p.CompactOsMode==Default)return;
  var setups=doc.SelectNodes("//u:settings[@pass='windowsPE']/u:component[@name='Microsoft-Windows-Setup']",ns)!;
  if(setups.Count==0)throw new ArgumentException(global::UnattendLocal.Localization.UiText.Source("text.ec84055b605a"));
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
  if(nodes.Count>1)throw new ArgumentException(global::UnattendLocal.Localization.UiText.Source("text.ab859330367e"));
  string native=Default;
  if(nodes.Count==1)native=nodes[0]!.InnerText.Trim() switch {"true" or "1"=>Enabled,"false" or "0"=>Disabled,_=>throw new ArgumentException(global::UnattendLocal.Localization.UiText.Source("text.d093f96888cf"))};
  var copies=doc.SelectNodes("//*[local-name()='PEScriptCopy']")!.Cast<XmlElement>().ToArray();
  var payloads=copies.Length>0?copies.Select(e=>e.InnerText):doc.SelectNodes("//*[local-name()='File' or local-name()='Path']")!.Cast<XmlElement>().Select(e=>e.InnerText);
  var applyLines=payloads.SelectMany(s=>s.Replace("\r\n","\n").Split('\n')).Where(line=>Regex.IsMatch(line,@"\bdism(?:\.exe)?\s+/Apply-Image\b",RegexOptions.IgnoreCase,TimeSpan.FromSeconds(1))).ToArray();
  bool otherCommand=Regex.IsMatch(doc.InnerText,@"\bcompact(?:\.exe)?\s+/CompactOS\s*:\s*(always|never)\b",RegexOptions.IgnoreCase,TimeSpan.FromSeconds(1));
  bool scripted=copies.Length>0||applyLines.Length>0||otherCommand;
  if(scripted&&nodes.Count>0)throw new ArgumentException(global::UnattendLocal.Localization.UiText.Source("text.83a108bf06ce"));
  if(!scripted)return new(native,false,true);
  if(applyLines.Length==1&&!otherCommand)return new(Regex.IsMatch(applyLines[0],@"(?:^|\s)/Compact(?=\s|$)",RegexOptions.IgnoreCase,TimeSpan.FromSeconds(1))?Enabled:Default,true,true);
  return new(Default,true,false);
 }
 public static SourceState ReadIntoPlan(XmlDocument doc,InstallPlan p,List<string> notes){
  var state=Inspect(doc);p.CompactOsMode=state.Mode;
  if(state.ScriptControlled)notes.Add(global::UnattendLocal.Localization.UiText.Source("text.f2017b8b6b55"));
  return state;
 }
 public static void UpdateImportState(ImportedXml data,SourceState state){data.CompactOsScriptControlled=state.ScriptControlled;data.CompactOsSourceMode=state.Mode;data.CompactOsSourceKnown=state.Known;}
 public static void ValidateImported(InstallPlan p,XmlDocument source){
  Validate(p);var state=Inspect(source);
  if(state.ScriptControlled&&p.CompactOsMode!=state.Mode)throw new ArgumentException(global::UnattendLocal.Localization.UiText.Source("text.3d06300483e9"));
 }
 public static void NormalizeLoaded(InstallPlan p,bool fieldWasPresent){
  if(p.ImportedXml is not {} data||data.SourceXml.Length==0||data.OriginalOmitted)return;
  var state=Inspect(XmlMerge.Read(data.SourceXml));UpdateImportState(data,state);
  if(fieldWasPresent)return;
  // A pre40 JSON may contain an XML that pre39 retained without understanding CompactOS.
  p.CompactOsMode=state.Mode;
  if(data.BaselineXml.Length>0&&state.Mode!=Default){var baseline=XmlMerge.Read(data.BaselineXml);var ns=new XmlNamespaceManager(baseline.NameTable);ns.AddNamespace("u",Ns);Apply(baseline,ns,p);data.BaselineXml=baseline.OuterXml;}
  if(state.ScriptControlled&&!data.Notices.Any(n=>n.Contains(global::UnattendLocal.Localization.UiText.Source("text.ffca52313ae7"))))data.Notices=[..data.Notices,global::UnattendLocal.Localization.UiText.Source("text.5e008a22bb75")];
 }
}
