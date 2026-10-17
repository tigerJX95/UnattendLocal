using System.Text.RegularExpressions;
using System.Xml;
namespace UnattendLocal.Core;

// Comparison view only. Repeated directory separators are not PowerShell escapes.
// Never rewrite embedded .reg, layout documents, or user-authored script payloads.
internal static class XmlImportPaths {
 private const string DetailedLog="""
$str = $script.ToString().Trim() -replace '\s+', ' ';
$max = 100;
if( $str.Length -le $max ) {
  $str;
} else {
  $str.Substring( 0, $max - 1 ) + '…';
}
""";
 private const string CompactLog="$script.ToString().Trim() -replace '\\s+', ' ' -replace '^(.{99})(.+)$', '$1…';";
 private static readonly Regex DetailedLogPattern=new("(?m)^"+string.Join(@"[ \t]*\r?\n",DetailedLog.Split('\n').Select(l=>@"[ \t]*"+Regex.Escape(l.Trim())))+@"[ \t]*$",RegexOptions.CultureInvariant,TimeSpan.FromSeconds(1));
 private static readonly Regex QuotedPath=new("'(?<path>[A-Za-z]:\\\\[^'\\r\\n]*)'|\"(?<path>[A-Za-z]:\\\\[^\"`$\\r\\n]*)\"",RegexOptions.CultureInvariant,TimeSpan.FromSeconds(1));
 internal static string Path(string value)=>Regex.IsMatch(value,@"\A[A-Za-z]:\\",RegexOptions.CultureInvariant,TimeSpan.FromSeconds(1))?Regex.Replace(value,@"\\{2,}","\\",RegexOptions.None,TimeSpan.FromSeconds(1)):value;
 internal static string Command(string value)=>QuotedPath.Replace(value,m=>m.Value.Replace(m.Groups["path"].Value,Path(m.Groups["path"].Value),StringComparison.Ordinal));
 internal static XmlDocument Inspection(XmlDocument source){
  var doc=(XmlDocument)source.CloneNode(true);
  foreach(XmlElement file in doc.SelectNodes("//*[local-name()='File']")!){
   string path=Path(file.GetAttribute("path"));file.SetAttribute("path",path);
   string name=path[(path.LastIndexOf('\\')+1)..];
   if(path.StartsWith(@"C:\Windows\Setup\Scripts\",StringComparison.OrdinalIgnoreCase)&&name.EndsWith(".ps1",StringComparison.OrdinalIgnoreCase)&&!name.StartsWith("unattend-",StringComparison.OrdinalIgnoreCase)){
    file.InnerText=Command(file.InnerText);
    if(name is "Specialize.ps1" or "DefaultUser.ps1" or "FirstLogon.ps1" or "UserOnce.ps1")file.InnerText=DetailedLogPattern.Replace(file.InnerText,_=>"      "+CompactLog);
   }
  }
  var ns=new XmlNamespaceManager(doc.NameTable);ns.AddNamespace("u",XmlMerge.Namespace);
  foreach(XmlElement command in doc.SelectNodes("//u:RunSynchronousCommand/u:Path | //u:SynchronousCommand/u:CommandLine",ns)!)command.InnerText=Command(command.InnerText);
  return doc;
 }
}
