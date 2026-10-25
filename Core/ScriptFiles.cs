using System.Text;
using System.Xml;
namespace UnattendLocal.Core;

// Text transfer only. Never runs a script or changes its execution stage.
public static class ScriptFiles {
 public const int MaxCharacters=2*1024*1024, MaxBytes=MaxCharacters*4;
 private static readonly UTF8Encoding Utf8=new(false,true);
 public static string TypeForFile(string fileName)=>Path.GetExtension(fileName).ToLowerInvariant() switch {
  ".ps1"=>"Ps1", ".cmd"=>"Cmd", ".reg"=>"Reg", ".vbs"=>"Vbs",
  _=>throw new ArgumentException(global::UnattendLocal.Localization.UiText.Source("text.4556a2b9cfe9"))
 };
 public static string Extension(string type)=>type switch {"Ps1"=>".ps1","Cmd"=>".cmd","Reg"=>".reg","Vbs"=>".vbs",_=>throw new ArgumentException(global::UnattendLocal.Localization.UiText.Source("text.686b6dddcd8d"))};
 public static void ValidateContent(string? text){
  if(string.IsNullOrWhiteSpace(text)||text.Length>MaxCharacters)throw new ArgumentException(global::UnattendLocal.Localization.UiText.Source("text.7bc37898b17e"));
  try{XmlConvert.VerifyXmlChars(text);}catch(XmlException){throw new ArgumentException(global::UnattendLocal.Localization.UiText.Source("text.f85c107c7b7d"));}
 }
 public static ScriptEntry Decode(string fileName,ReadOnlySpan<byte> bytes){
  string type=TypeForFile(fileName);
  if(bytes.Length>MaxBytes)throw new ArgumentException(global::UnattendLocal.Localization.UiText.Source("text.76f6fc512cd8"));
  Encoding encoding=Utf8;int skip=0;
  if(bytes.StartsWith(new byte[]{0xff,0xfe,0,0})||bytes.StartsWith(new byte[]{0,0,0xfe,0xff}))throw new ArgumentException(global::UnattendLocal.Localization.UiText.Source("text.294133e2ea71"));
  if(bytes.StartsWith(new byte[]{0xef,0xbb,0xbf}))skip=3;
  else if(bytes.StartsWith(new byte[]{0xff,0xfe})){encoding=new UnicodeEncoding(false,true,true);skip=2;}
  else if(bytes.StartsWith(new byte[]{0xfe,0xff})){encoding=new UnicodeEncoding(true,true,true);skip=2;}
  string text;
  try{text=encoding.GetString(bytes[skip..]);}
  catch(DecoderFallbackException){throw new ArgumentException(global::UnattendLocal.Localization.UiText.Source("text.337ef682c494"));}
  ValidateContent(text);
  return new ScriptEntry{Enabled=false,Phase="System",Type=type,Content=text};
 }
 public static ScriptEntry Read(string path){
  _=TypeForFile(path);
  using var file=new FileStream(path,FileMode.Open,FileAccess.Read,FileShare.Read);
  if(file.Length>MaxBytes)throw new ArgumentException(global::UnattendLocal.Localization.UiText.Source("text.76f6fc512cd8"));
  var bytes=new byte[(int)file.Length];file.ReadExactly(bytes);
  if(file.ReadByte()!=-1)throw new ArgumentException(global::UnattendLocal.Localization.UiText.Source("text.c193d308007b"));
  return Decode(path,bytes);
 }
 public static ScriptEntry Append(InstallPlan plan,ScriptEntry candidate){
  if(plan.Scripts.Count>=60)throw new ArgumentException(global::UnattendLocal.Localization.UiText.Source("text.846f88a32070"));
  if(candidate.Enabled||candidate.Phase!="System")throw new ArgumentException(global::UnattendLocal.Localization.UiText.Source("text.9b5d0f8bd3a1"));
  _=Extension(candidate.Type);ValidateContent(candidate.Content);
  // Clone so callers cannot mutate the candidate after committing it.
  var row=new ScriptEntry{Name=candidate.Name,Enabled=false,Phase="System",Type=candidate.Type,Content=candidate.Content};plan.Scripts.Add(row);return row;
 }
 public static byte[] Encode(ScriptEntry row,string destination){
  if(TypeForFile(destination)!=row.Type)throw new ArgumentException(global::UnattendLocal.Localization.UiText.Source("text.320f87732423"));
  ValidateContent(row.Content);string text=row.Content;
  if(row.Type=="Reg"&&!text.StartsWith("Windows Registry Editor Version 5.00",StringComparison.Ordinal))text="Windows Registry Editor Version 5.00\r\n\r\n"+text;
  ValidateContent(text);
  Encoding encoding=row.Type switch {"Reg" or "Vbs"=>new UnicodeEncoding(false,true,true),"Ps1"=>new UTF8Encoding(true,true),_=>Utf8};
  return [..encoding.GetPreamble(),..encoding.GetBytes(text)];
 }
 public static void Write(ScriptEntry row,string destination){
  AtomicFile.WriteAllBytes(destination,Encode(row,destination));
 }
 public static string EncodingHint(string type)=>type switch {"Cmd"=>global::UnattendLocal.Localization.UiText.Source("text.d9746ee69618"),"Reg"=>global::UnattendLocal.Localization.UiText.Source("text.8dbfc788e3b3"),"Vbs"=>global::UnattendLocal.Localization.UiText.Source("text.eff0f766b82e"),"Ps1"=>global::UnattendLocal.Localization.UiText.Source("text.3f635552ca60"),_=>global::UnattendLocal.Localization.UiText.Source("text.d5d96b3cf5bf")};
}
