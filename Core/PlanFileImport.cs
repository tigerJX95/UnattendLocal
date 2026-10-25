using System.Text.Json;
namespace UnattendLocal.Core;

public enum PlanFileKind { AnswerXml, JsonPlan }
public static class PlanFileImport {
 public const long MaxXmlBytes=32L*1024*1024;
 public const long MaxJsonBytes=128L*1024*1024;
 public static PlanFileKind Kind(string path)=>Path.GetExtension(path).ToLowerInvariant() switch {
  ".xml"=>PlanFileKind.AnswerXml,".json"=>PlanFileKind.JsonPlan,
  _=>throw new ArgumentException(global::UnattendLocal.Localization.UiText.Source("text.84ec86cb41c2"))};
 public static string SingleFile(string[] paths){
  if(paths.Length!=1)throw new ArgumentException(global::UnattendLocal.Localization.UiText.Source("text.d0cd57d196a6"));
  string path=paths[0];_=Kind(path);
  if(!File.Exists(path)||Directory.Exists(path))throw new ArgumentException(global::UnattendLocal.Localization.UiText.Source("text.a3277e2189ea"));
  return path;
 }
 public static string Read(string path){
  var kind=Kind(path);long limit=kind==PlanFileKind.AnswerXml?MaxXmlBytes:MaxJsonBytes;
  return TextFileImport.Read(path,limit,TooLarge(kind).Message);
 }
 private static ArgumentException TooLarge(PlanFileKind kind)=>new(kind==PlanFileKind.AnswerXml?global::UnattendLocal.Localization.UiText.Source("text.876a545bfc0f"):global::UnattendLocal.Localization.UiText.Source("text.e14fca26996c"));
 public static void ValidateJsonPlan(JsonElement root){
  if(root.ValueKind!=JsonValueKind.Object||!root.TryGetProperty(nameof(InstallPlan.SchemaVersion),out var schema)||schema.ValueKind!=JsonValueKind.Number||!schema.TryGetInt32(out var version))
   throw new ArgumentException(global::UnattendLocal.Localization.UiText.Source("text.17bfa4c5a9ff"));
  if(version!=1)throw new ArgumentException(global::UnattendLocal.Localization.UiText.Source("text.b8956bfee619"));
  if(root.EnumerateObject().GroupBy(p=>p.Name,StringComparer.OrdinalIgnoreCase).Any(g=>g.Count()>1))throw new ArgumentException(global::UnattendLocal.Localization.UiText.Source("text.6dca4c64395d"));
 }
 public static void ValidateForDisplay(InstallPlan plan){
  void Strings(object value){
   foreach(var property in value.GetType().GetProperties().Where(p=>p.PropertyType==typeof(string)&&p.CanWrite))
    if(property.GetValue(value)==null)throw new ArgumentException(global::UnattendLocal.Localization.UiText.Concat(new object?[] { global::UnattendLocal.Localization.UiText.Source("text.44f2db91a447"), property.Name, global::UnattendLocal.Localization.UiText.Source("text.73b5601ce749") },new bool[]{false,true,false}));
  }
  Strings(plan);foreach(var row in plan.XmlComponents)Strings(row);foreach(var user in plan.Users)Strings(user);foreach(var script in plan.Scripts)Strings(script);
  if(plan.ImportedXml is {} data){Strings(data);if(data.Notices.Any(n=>n==null)||data.Blockers.Any(n=>n==null))throw new ArgumentException(global::UnattendLocal.Localization.UiText.Source("text.65d971214b02"));}
 }
}
