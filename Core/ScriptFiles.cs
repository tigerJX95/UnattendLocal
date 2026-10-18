using System.Text;
using System.Xml;
namespace UnattendLocal.Core;

// Text transfer only. Never runs a script or changes its execution stage.
public static class ScriptFiles {
 public const int MaxCharacters=2*1024*1024, MaxBytes=MaxCharacters*4;
 private static readonly UTF8Encoding Utf8=new(false,true);
 public static string TypeForFile(string fileName)=>Path.GetExtension(fileName).ToLowerInvariant() switch {
  ".ps1"=>"Ps1", ".cmd"=>"Cmd", ".reg"=>"Reg", ".vbs"=>"Vbs",
  _=>throw new ArgumentException("仅支持 .ps1、.cmd、.reg、.vbs 脚本文件；不支持 JScript（.js）。")
 };
 public static string Extension(string type)=>type switch {"Ps1"=>".ps1","Cmd"=>".cmd","Reg"=>".reg","Vbs"=>".vbs",_=>throw new ArgumentException("请选择支持的脚本格式；JScript（.js）不能另存。")};
 public static void ValidateContent(string? text){
  if(string.IsNullOrWhiteSpace(text)||text.Length>MaxCharacters)throw new ArgumentException("脚本内容不能为空，且不能超过 2 Mi 字符。");
  try{XmlConvert.VerifyXmlChars(text);}catch(XmlException){throw new ArgumentException("脚本包含无法嵌入 XML 的字符，可能不是文本文件；请核对内容和编码。");}
 }
 public static ScriptEntry Decode(string fileName,ReadOnlySpan<byte> bytes){
  string type=TypeForFile(fileName);
  if(bytes.Length>MaxBytes)throw new ArgumentException("脚本文件超过 8 MiB 编码读取上限。");
  Encoding encoding=Utf8;int skip=0;
  if(bytes.StartsWith(new byte[]{0xff,0xfe,0,0})||bytes.StartsWith(new byte[]{0,0,0xfe,0xff}))throw new ArgumentException("暂不支持 UTF-32；请将脚本另存为 UTF-8 或带 BOM 的 UTF-16 后导入。");
  if(bytes.StartsWith(new byte[]{0xef,0xbb,0xbf}))skip=3;
  else if(bytes.StartsWith(new byte[]{0xff,0xfe})){encoding=new UnicodeEncoding(false,true,true);skip=2;}
  else if(bytes.StartsWith(new byte[]{0xfe,0xff})){encoding=new UnicodeEncoding(true,true,true);skip=2;}
  string text;
  try{text=encoding.GetString(bytes[skip..]);}
  catch(DecoderFallbackException){throw new ArgumentException("脚本编码无法识别。支持 UTF-8（可带 BOM）或带 BOM 的 UTF-16；请先转换 ANSI / GBK 等文件，程序不会猜测编码。");}
  ValidateContent(text);
  return new ScriptEntry{Enabled=false,Phase="System",Type=type,Content=text};
 }
 public static ScriptEntry Read(string path){
  _=TypeForFile(path);
  using var file=new FileStream(path,FileMode.Open,FileAccess.Read,FileShare.Read);
  if(file.Length>MaxBytes)throw new ArgumentException("脚本文件超过 8 MiB 编码读取上限。");
  var bytes=new byte[(int)file.Length];file.ReadExactly(bytes);
  if(file.ReadByte()!=-1)throw new ArgumentException("脚本文件在读取期间发生变化，请重新导入。");
  return Decode(path,bytes);
 }
 public static ScriptEntry Append(InstallPlan plan,ScriptEntry candidate){
  if(plan.Scripts.Count>=60)throw new ArgumentException("最多 60 条脚本；本次文件未导入。");
  if(candidate.Enabled||candidate.Phase!="System")throw new ArgumentException("文件导入条目必须默认停用，并从 System 阶段开始。");
  _=Extension(candidate.Type);ValidateContent(candidate.Content);
  // Clone so callers cannot mutate the candidate after committing it.
  var row=new ScriptEntry{Enabled=false,Phase="System",Type=candidate.Type,Content=candidate.Content};plan.Scripts.Add(row);return row;
 }
 public static byte[] Encode(ScriptEntry row,string destination){
  if(TypeForFile(destination)!=row.Type)throw new ArgumentException("另存文件的扩展名必须与当前脚本格式一致；请先在界面转换格式或更正文件名。");
  ValidateContent(row.Content);string text=row.Content;
  if(row.Type=="Reg"&&!text.StartsWith("Windows Registry Editor Version 5.00",StringComparison.Ordinal))text="Windows Registry Editor Version 5.00\r\n\r\n"+text;
  ValidateContent(text);
  Encoding encoding=row.Type switch {"Reg" or "Vbs"=>new UnicodeEncoding(false,true,true),"Ps1"=>new UTF8Encoding(true,true),_=>Utf8};
  return [..encoding.GetPreamble(),..encoding.GetBytes(text)];
 }
 public static void Write(ScriptEntry row,string destination){
  var bytes=Encode(row,destination);
  string full=Path.GetFullPath(destination),temporary=Path.Combine(Path.GetDirectoryName(full)!,".unattend-script-"+Guid.NewGuid().ToString("N")+".tmp");
  try{
   using(var output=new FileStream(temporary,FileMode.CreateNew,FileAccess.Write,FileShare.None)){output.Write(bytes);output.Flush(true);}
   File.Move(temporary,full,true);
  }finally{if(File.Exists(temporary))File.Delete(temporary);}
 }
 public static string EncodingHint(string type)=>type switch {"Cmd"=>"UTF-8（无 BOM）；CMD 中文需目标代码页匹配，不自动插入 chcp 命令。","Reg"=>"UTF-16 LE（带 BOM）；缺少标准 REG 文件头时，仅在另存文件中补齐。","Vbs"=>"UTF-16 LE（带 BOM）。","Ps1"=>"UTF-8（带 BOM），兼容 Windows PowerShell 5.1 中文脚本。",_=>"请选择支持的脚本格式。"};
}
