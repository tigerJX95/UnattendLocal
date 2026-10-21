using System.Text.Json;
namespace UnattendLocal.Core;

public enum PlanFileKind { AnswerXml, JsonPlan }
public static class PlanFileImport {
 public const long MaxXmlBytes=32L*1024*1024;
 public const long MaxJsonBytes=128L*1024*1024;
 public static PlanFileKind Kind(string path)=>Path.GetExtension(path).ToLowerInvariant() switch {
  ".xml"=>PlanFileKind.AnswerXml,".json"=>PlanFileKind.JsonPlan,
  _=>throw new ArgumentException("请选择一个 Windows 应答 XML 或本程序保存的 JSON 配置方案。")};
 public static string SingleFile(string[] paths){
  if(paths.Length!=1)throw new ArgumentException("请每次只拖入一个 XML 或 JSON 文件；本次未导入，当前配置未改变。");
  string path=paths[0];_=Kind(path);
  if(!File.Exists(path)||Directory.Exists(path))throw new ArgumentException("请选择可读取的文件，不能导入文件夹。");
  return path;
 }
 public static string Read(string path){
  var kind=Kind(path);long limit=kind==PlanFileKind.AnswerXml?MaxXmlBytes:MaxJsonBytes;
  using var input=new FileStream(path,FileMode.Open,FileAccess.Read,FileShare.Read);
  if(input.Length>limit)throw TooLarge(kind);
  using var bytes=new MemoryStream();var buffer=new byte[81920];int read;
  while((read=input.Read(buffer))>0){if(bytes.Length+read>limit)throw TooLarge(kind);bytes.Write(buffer,0,read);}
  bytes.Position=0;using var reader=new StreamReader(bytes,new System.Text.UTF8Encoding(false,true),detectEncodingFromByteOrderMarks:true);return reader.ReadToEnd();
 }
 private static ArgumentException TooLarge(PlanFileKind kind)=>new(kind==PlanFileKind.AnswerXml?"应答 XML 文件超过 32 MiB。":"JSON 方案文件超过 128 MiB。");
 public static void ValidateJsonPlan(JsonElement root){
  if(root.ValueKind!=JsonValueKind.Object||!root.TryGetProperty(nameof(InstallPlan.SchemaVersion),out var schema)||schema.ValueKind!=JsonValueKind.Number||!schema.TryGetInt32(out var version))
   throw new ArgumentException("这不是本程序保存的 JSON 配置方案：缺少有效的 SchemaVersion。开始菜单 JSON 等文件请在对应设置中导入。");
  if(version!=1)throw new ArgumentException("不支持此方案版本。");
  if(root.EnumerateObject().GroupBy(p=>p.Name,StringComparer.OrdinalIgnoreCase).Any(g=>g.Count()>1))throw new ArgumentException("JSON 方案含重复字段，无法确定应采用的设置。");
 }
 public static void ValidateForDisplay(InstallPlan plan){
  void Strings(object value){
   foreach(var property in value.GetType().GetProperties().Where(p=>p.PropertyType==typeof(string)&&p.CanWrite))
    if(property.GetValue(value)==null)throw new ArgumentException("方案中的字段不能为 null："+property.Name+"。空文本请使用空字符串。");
  }
  Strings(plan);foreach(var user in plan.Users)Strings(user);foreach(var script in plan.Scripts)Strings(script);
  if(plan.ImportedXml is {} data){Strings(data);if(data.Notices.Any(n=>n==null)||data.Blockers.Any(n=>n==null))throw new ArgumentException("方案中的 XML 导入说明无效。");}
 }
}
