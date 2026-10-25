using System.Text.Json.Serialization;
using Schneegans.Unattend;
namespace UnattendLocal.Core;
public class ScriptEntry : Observable {
 private string name="";
 public string Name {get=>name;set{value??="";CustomScripts.ValidateName(value);Set(ref name,value);Notify(nameof(Label));}}
 private bool enabled=true;public bool Enabled {get=>enabled;set=>Set(ref enabled,value);}
 private string phase="System";public string Phase {get=>phase;set{Set(ref phase,value);Notify(nameof(Hint));Notify(nameof(Example));Notify(nameof(Label));}}
 private string type="Ps1";public string Type {get=>type;set{Set(ref type,value);Notify(nameof(Example));Notify(nameof(Label));Notify(nameof(Hint));Notify(nameof(IsRemovedType));}}
 private string content="";public string Content {get=>content;set=>Set(ref content,value);}
 [JsonIgnore] public bool IsRemovedType=>string.Equals(Type,"Js",StringComparison.OrdinalIgnoreCase);
 [JsonIgnore] public string Label=>global::UnattendLocal.Localization.UiText.Concat(new object?[] { (string.IsNullOrWhiteSpace(Name)?"":Name.Trim()+" · "), (IsRemovedType?global::UnattendLocal.Localization.UiText.Source("text.f16e63ea9048"):$"{CustomScripts.PhaseLabel(Phase)} · .{Type.ToLowerInvariant()}") },new bool[] { true, false });
 [JsonIgnore] public string Hint=>IsRemovedType?global::UnattendLocal.Localization.UiText.Source("text.d88a757877d2"):Phase switch {
  "System"=>global::UnattendLocal.Localization.UiText.Source("text.df68c33e673d"),
  "DefaultUser"=>global::UnattendLocal.Localization.UiText.Source("text.f0518d6f658d"),
  "FirstLogon"=>global::UnattendLocal.Localization.UiText.Source("text.99cfd7ccc574"),
  "UserOnce"=>global::UnattendLocal.Localization.UiText.Source("text.57f0b5c7ffba"),
  _=>global::UnattendLocal.Localization.UiText.Source("text.c04c267d85a1")};
 [JsonIgnore] public string Example=>Phase=="System"&&Type=="Cmd"?CustomScripts.DirectoryExample:Phase=="FirstLogon"&&Type=="Ps1"?CustomScripts.NetworkExample:global::UnattendLocal.Localization.UiText.Source("text.24458f3c07fe");
}
public static class CustomScripts {
 public const int MaxNameLength=80;
 public static void ValidateName(string name){
  if(name.Length>MaxNameLength||name.Any(char.IsControl))throw new ArgumentException(global::UnattendLocal.Localization.UiText.Source("text.92900c89549e"));
  try{System.Xml.XmlConvert.VerifyXmlChars(name);}catch(System.Xml.XmlException){throw new ArgumentException(global::UnattendLocal.Localization.UiText.Source("text.9e72b6eb398c"));}
 }
 public const string DirectoryExample="setx.exe DIRCMD \"/A /O:GN /C /N\" /m";
 public const string NetworkExample="Get-NetConnectionProfile -InterfaceAlias 'Ethernet*' | Set-NetConnectionProfile -NetworkCategory 'Private';";
 public static string PhaseLabel(string phase)=>phase switch {"System"=>global::UnattendLocal.Localization.UiText.Source("text.c46f6d22bea9"),"DefaultUser"=>global::UnattendLocal.Localization.UiText.Source("text.3db15e52c093"),"FirstLogon"=>global::UnattendLocal.Localization.UiText.Source("text.82f0d8457c07"),"UserOnce"=>global::UnattendLocal.Localization.UiText.Source("text.69bb08106fb5"),_=>global::UnattendLocal.Localization.UiText.Source("text.8106dad736f1")};
 public static ScriptEntry Duplicate(InstallPlan p,ScriptEntry source){
  int index=p.Scripts.IndexOf(source);if(index<0)throw new ArgumentException(global::UnattendLocal.Localization.UiText.Source("text.02e7f6d78d69"));
  if(p.Scripts.Count>=60)throw new ArgumentException(global::UnattendLocal.Localization.UiText.Source("text.bca97a516f08"));
  var copy=new ScriptEntry{Name=source.Name,Enabled=false,Phase=source.Phase,Type=source.Type,Content=source.Content};p.Scripts.Insert(index+1,copy);return copy;
 }
 public static ScriptSettings Build(InstallPlan p){
  if(p.Scripts==null||p.Scripts.Any(s=>s==null)||p.Scripts.Count>60)throw new ArgumentException(global::UnattendLocal.Localization.UiText.Source("text.1cdf8549adec"));
  var result=new List<Script>();
  foreach(var row in p.Scripts.Where(s=>s.Enabled)){
   if(row.IsRemovedType)throw new ArgumentException(global::UnattendLocal.Localization.UiText.Source("text.e6495adccb35"));
   if(!Enum.TryParse<ScriptPhase>(row.Phase,out var phase)||!Enum.IsDefined(phase)||row.Phase!=phase.ToString())throw new ArgumentException(global::UnattendLocal.Localization.UiText.Source("text.fd772a42d227"));
   if(!Enum.TryParse<ScriptType>(row.Type,out var type)||!Enum.IsDefined(type)||row.Type!=type.ToString()||!phase.GetAllowedTypes().Contains(type))throw new ArgumentException(global::UnattendLocal.Localization.UiText.Source("text.92a3d9fff9ef"));
   ScriptFiles.ValidateContent(row.Content);
   if(result.Count(s=>s.Phase==phase)>=15)throw new ArgumentException(global::UnattendLocal.Localization.UiText.Source("text.589016ad3a88"));
   result.Add(new Script(row.Content,phase,type));
  }
  return new ScriptSettings(result,p.RestartExplorerAfterScripts);
 }
 public static string Summary(InstallPlan p)=>global::UnattendLocal.Localization.UiText.Concat(new object?[] { global::UnattendLocal.Localization.UiText.Source("text.9118de80ac7e"), global::UnattendLocal.Localization.UiText.Join("；", new[]{"System","DefaultUser","FirstLogon","UserOnce"}.Select(phase=>global::UnattendLocal.Localization.UiText.Format("message.535ea1099c2f", new string[] { global::UnattendLocal.Localization.UiText.Arg((PhaseLabel(phase)), 0, null), global::UnattendLocal.Localization.UiText.Arg((p.Scripts?.Count(s=>s!=null&&s.Enabled&&s.Phase==phase)??0), 0, null) })), false), global::UnattendLocal.Localization.UiText.Format("message.6eda7f92109e", new string[] { global::UnattendLocal.Localization.UiText.Arg(((p.RestartExplorerAfterScripts?global::UnattendLocal.Localization.UiText.Source("text.8da97ddda990"):global::UnattendLocal.Localization.UiText.Source("text.3fd47edce45b"))), 0, null) }) },new bool[]{false,false,false});
}
