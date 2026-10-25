using System.Text.Json;
using System.IO;
using System.Text;
using UnattendLocal.Core;
using UnattendLocal.Localization;
namespace UnattendLocal.Desktop;
public record UiDisplayPreferences(string Theme="Light",string Language="zh-CN");
public static class UiPreferences
{
 private static string DefaultPath=>Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"UnattendLocal","ui-preferences.json");
 public static UiDisplayPreferences LoadState(string? path=null){
  try{
   path??=DefaultPath;if(!File.Exists(path))return new();
   using var doc=JsonDocument.Parse(TextFileImport.Read(path,4096,"UI preferences file is too large."));
   if(doc.RootElement.ValueKind!=JsonValueKind.Object)return new();
   string theme=doc.RootElement.TryGetProperty("theme",out var t)&&t.ValueKind==JsonValueKind.String&&t.GetString()=="Dark"?"Dark":"Light";
   string language=doc.RootElement.TryGetProperty("language",out var l)&&l.ValueKind==JsonValueKind.String&&UiText.IsSupported(l.GetString())?l.GetString()!:"zh-CN";
   return new(theme,language);
  }catch(Exception ex)when(ex is IOException or UnauthorizedAccessException or JsonException or ArgumentException or NotSupportedException){return new();}
 }
 public static string Load(string? path=null)=>LoadState(path).Theme;
 public static string LoadLanguage(string? path=null)=>LoadState(path).Language;
 public static bool Save(string theme,string? path=null)=>theme is "Light" or "Dark"&&Write(new(theme,LoadState(path).Language),path);
 public static bool SaveLanguage(string language,string? path=null)=>UiText.IsSupported(language)&&Write(new(LoadState(path).Theme,language),path);
 private static bool Write(UiDisplayPreferences state,string? path){
  try{path=Path.GetFullPath(path??DefaultPath);Directory.CreateDirectory(Path.GetDirectoryName(path)!);AtomicFile.WriteAllText(path,JsonSerializer.Serialize(new{theme=state.Theme,language=state.Language}),new UTF8Encoding(false,true));return true;}
  catch(Exception ex)when(ex is IOException or UnauthorizedAccessException or ArgumentException or NotSupportedException){return false;}
 }
}
