using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Xml;
namespace UnattendLocal.Core;

public static class SetupDriverSettings {
 public const string ComponentName="Microsoft-Windows-PnpCustomizationsWinPE";
 private const string Wcm="http://schemas.microsoft.com/WMIConfig/2002/State";
 public static readonly string ReadOnlyNotice=global::UnattendLocal.Localization.UiText.Source("text.b5fde38579c4");
 public static readonly string Description=global::UnattendLocal.Localization.UiText.Source("text.09d2bd1db477");
 public record Source(bool Present,bool Known,string[] Paths);
 public static bool CanEdit(InstallPlan p)=>p.ImportedXml?.SetupDriversReadOnly!=true;
 public static string[] ReadPaths(string? text){
  if(text==null||text.Length>8*261)throw new ArgumentException(global::UnattendLocal.Localization.UiText.Source("text.3879e9cf40c4"));
  var paths=text.Replace("\r\n","\n").Split('\n').Select(s=>s.Trim()).Where(s=>s.Length>0).ToArray();
  if(paths.Length is <1 or >8)throw new ArgumentException(global::UnattendLocal.Localization.UiText.Source("text.976ded37ae46"));
  foreach(string path in paths){
   try{XmlConvert.VerifyXmlChars(path);}catch(XmlException){throw new ArgumentException(global::UnattendLocal.Localization.UiText.Source("text.8f252e2379f0"));}
   bool local=Regex.IsMatch(path,@"\A[A-Za-z]:\\",RegexOptions.CultureInvariant);
   bool unc=Regex.IsMatch(path,@"\A\\\\[^\\]+\\[^\\]+(?:\\|\z)",RegexOptions.CultureInvariant)&&!path.StartsWith(@"\\?\",StringComparison.Ordinal)&&!path.StartsWith(@"\\.\",StringComparison.Ordinal);
   if(path.Length>259||path.Any(char.IsControl)||path.Any(c=>"\"<>|?*%/".Contains(c))||!(local||unc)||path[(local?2:0)..].Contains(':'))throw new ArgumentException(global::UnattendLocal.Localization.UiText.Source("text.70326c292b78"));
   string tail=local?path[3..]:path[2..];
   var segments=tail.Split('\\');
   if(segments.Any(s=>s is "." or ".."||s.EndsWith(' ')||s.EndsWith('.'))||segments.Take(Math.Max(0,segments.Length-1)).Any(s=>s.Length==0))throw new ArgumentException(global::UnattendLocal.Localization.UiText.Source("text.d2bcc2a89d68"));
   if(new[]{".inf",".sys",".cat",".exe",".zip",".cab"}.Any(ext=>path.EndsWith(ext,StringComparison.OrdinalIgnoreCase)))throw new ArgumentException(global::UnattendLocal.Localization.UiText.Source("text.265840c4c6b2"));
  }
  if(paths.Select(p=>p.TrimEnd('\\')).Distinct(StringComparer.OrdinalIgnoreCase).Count()!=paths.Length)throw new ArgumentException(global::UnattendLocal.Localization.UiText.Source("text.b9d859c034b2"));
  return paths;
 }
 public static void Validate(InstallPlan p){if(p.UseSetupDrivers)_=ConfigurationValidationException.Read(nameof(InstallPlan.SetupDriverPaths),()=>ReadPaths(p.SetupDriverPaths));}
 public static string Summary(InstallPlan p){
  if(!CanEdit(p))return global::UnattendLocal.Localization.UiText.Source("text.ed00aa20ef3d");
  if(!p.UseSetupDrivers)return global::UnattendLocal.Localization.UiText.Source("text.17fc7d101b65");
  try{return global::UnattendLocal.Localization.UiText.Concat(new object?[] { global::UnattendLocal.Localization.UiText.Source("text.3a38633ed97d"), global::UnattendLocal.Localization.UiText.Join("；", ReadPaths(p.SetupDriverPaths), false) },new bool[]{false,false});}catch(ArgumentException){return global::UnattendLocal.Localization.UiText.Source("text.ccf787bfe82c");}
 }
 private static XmlNamespaceManager Ns(XmlDocument doc){var ns=new XmlNamespaceManager(doc.NameTable);ns.AddNamespace("u",XmlMerge.Namespace);return ns;}
 private static XmlElement[] Nodes(XmlDocument doc)=>doc.SelectNodes("//u:component[@name='"+ComponentName+"']",Ns(doc))!.Cast<XmlElement>().ToArray();
 internal static bool CustomPe(XmlDocument doc)=>doc.SelectNodes("//*[local-name()='PEScriptCopy']")!.Count>0||doc.SelectNodes("//u:settings[@pass='windowsPE']/u:component[@name='Microsoft-Windows-Setup']/u:RunSynchronous/u:RunSynchronousCommand/u:Path",Ns(doc))!.Cast<XmlElement>().Any(p=>Regex.IsMatch(p.InnerText,@"\bdism(?:\.exe)?\s+/Apply-Image\b|[A-Za-z]:\\[^\r\n]*\.cmd\b",RegexOptions.IgnoreCase,TimeSpan.FromSeconds(1)));
 public static void Apply(XmlDocument doc,XmlNamespaceManager ns,InstallPlan p){
  if(!p.UseSetupDrivers)return;
  var paths=ReadPaths(p.SetupDriverPaths);
  if(Nodes(doc).Length>0||CustomPe(doc))throw new ArgumentException(global::UnattendLocal.Localization.UiText.Source("text.07d912e9c10a"));
  var settings=doc.SelectSingleNode("/u:unattend/u:settings[@pass='windowsPE']",ns) as XmlElement??throw new ArgumentException(global::UnattendLocal.Localization.UiText.Source("text.56bb25e09e18"));
  XmlElement Add(XmlNode parent,string name,string? value=null){var e=doc.CreateElement(name,XmlMerge.Namespace);if(value!=null)e.InnerText=value;parent.AppendChild(e);return e;}
  var component=Add(settings,"component");
  foreach(var pair in new Dictionary<string,string>{{"name",ComponentName},{"processorArchitecture",p.Architecture},{"publicKeyToken","31bf3856ad364e35"},{"language","neutral"},{"versionScope","nonSxS"}})component.SetAttribute(pair.Key,pair.Value);
  var list=Add(component,"DriverPaths");
  for(int i=0;i<paths.Length;i++){
   var row=Add(list,"PathAndCredentials");
   foreach(var pair in new Dictionary<string,string>{{"action","add"},{"keyValue",(i+1).ToString(CultureInfo.InvariantCulture)}}){var a=doc.CreateAttribute("wcm",pair.Key,Wcm);a.Value=pair.Value;row.Attributes.Append(a);}
   Add(row,"Path",paths[i]);
  }
 }
 public static Source Inspect(XmlDocument doc){
  var nodes=Nodes(doc);var unknown=new Source(nodes.Length>0,false,[]);
  if(CustomPe(doc))return unknown;
  if(nodes.Length==0)return new(false,true,[]);
  if(nodes.Length!=1)return unknown;
  try{
   var c=nodes[0];if(c.ParentNode is not XmlElement s||s.LocalName!="settings"||s.NamespaceURI!=XmlMerge.Namespace||s.ParentNode!=doc.DocumentElement||s.GetAttribute("pass")!="windowsPE")return unknown;
   string arch=c.GetAttribute("processorArchitecture");if(arch is not ("amd64" or "arm64" or "x86"))return unknown;
   var paths=c.SelectNodes("u:DriverPaths/u:PathAndCredentials/u:Path",Ns(doc))!.Cast<XmlElement>().Select(e=>e.InnerText).ToArray();
   var p=new InstallPlan{UseSetupDrivers=true,SetupDriverPaths=global::UnattendLocal.Localization.UiText.Join("\n", paths, false),Architecture=arch};
   var clean=XmlMerge.Read("<unattend xmlns='"+XmlMerge.Namespace+"'><settings pass='windowsPE'/></unattend>");Apply(clean,Ns(clean),p);
   if(!XmlMerge.Equal(c,Nodes(clean).Single()))return unknown;
   return new(true,true,ReadPaths(p.SetupDriverPaths));
  }catch(Exception error)when(error is ArgumentException or InvalidOperationException){return unknown;}
 }
 private static void Assign(InstallPlan p,Source source){p.UseSetupDrivers=source.Present&&source.Known;if(p.UseSetupDrivers)p.SetupDriverPaths=global::UnattendLocal.Localization.UiText.Join("\r\n", source.Paths, false);}
 public static Source ReadIntoPlan(XmlDocument doc,InstallPlan p,List<string> notes){var source=Inspect(doc);Assign(p,source);if(!source.Known)notes.Add(ReadOnlyNotice);return source;}
 public static void UpdateImportState(ImportedXml data,Source source){data.SetupDriversReadOnly=!source.Known;data.SetupDriversImportVersion=1;}
 public static void ValidateImported(InstallPlan p,XmlDocument source){if(p.UseSetupDrivers&&!Inspect(source).Known)throw new ArgumentException(ReadOnlyNotice);}
 public static void NormalizeLoaded(InstallPlan p,JsonElement root){
  if(p.ImportedXml is not {OriginalOmitted:false} data||data.SourceXml.Length==0)return;
  var source=XmlMerge.Read(data.SourceXml);var state=Inspect(source);
  if(data.SetupDriversImportVersion<1&&state is {Present:true,Known:true}){
   bool missing=!root.TryGetProperty(nameof(InstallPlan.UseSetupDrivers),out _)&&!root.TryGetProperty(nameof(InstallPlan.SetupDriverPaths),out _);
   if(missing&&data.BaselineXml.Length>0&&Nodes(XmlMerge.Read(data.BaselineXml)).Length==0){
    try{var baseline=XmlMerge.Read(data.BaselineXml);var next=new InstallPlan{UseSetupDrivers=true,SetupDriverPaths=global::UnattendLocal.Localization.UiText.Join("\r\n", state.Paths, false),Architecture=Nodes(source).Single().GetAttribute("processorArchitecture")};Apply(baseline,Ns(baseline),next);Assign(p,state);data.BaselineXml=baseline.OuterXml;}
    catch(ArgumentException){data.Blockers=data.Blockers.Append(global::UnattendLocal.Localization.UiText.Source("text.2162a51b3de4")).Distinct().ToArray();}
   }else data.Blockers=data.Blockers.Append(global::UnattendLocal.Localization.UiText.Source("text.2162a51b3de4")).Distinct().ToArray();
  }
  data.Notices=data.Notices.Where(n=>n!=global::UnattendLocal.Localization.UiText.Concat(new object?[] { global::UnattendLocal.Localization.UiText.Source("text.8af4bbf685c1"), ComponentName },new bool[]{false,true})&&n!=ReadOnlyNotice).ToArray();
  if(!state.Known)data.Notices=[..data.Notices,ReadOnlyNotice];UpdateImportState(data,state);
 }
}
