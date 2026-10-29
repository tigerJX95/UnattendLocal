using System.Text.RegularExpressions;
using Schneegans.Unattend;
namespace UnattendLocal.Core;

public static class PePreflight
{
 public static string ImageCheckerSource(){
  using var stream=typeof(PePreflight).Assembly.GetManifestResourceStream("UnattendLocal.Core.Resources.PeImageCheck.vbs")!;
  using var reader=new StreamReader(stream);return reader.ReadToEnd();
 }
 public static string ImageCheckerWriter()=>">X:\\UL-imagecheck.vbs (\r\n"+
  string.Join("\r\n",EchoProcessor.Process(ImageCheckerSource().Split('\n')))+"\r\n)\r\n";
 public static string Keyboard(string script){
  var matches=Regex.Matches(script,@"(?m)^wpeutil\.exe SetKeyboardLayout [^\r\n]+");
  if(matches.Count>1)throw new InvalidOperationException("Unexpected PE keyboard command count.");
  if(matches.Count==0)return script;
  var line=matches[0];string argument=line.Value["wpeutil.exe SetKeyboardLayout ".Length..];
  string replacement=Regex.IsMatch(argument,@"\A[0-9a-fA-F]{4}:[0-9a-fA-F]{8}\z")
   ? line.Value+" || call :print \"Warning: PE keyboard layout could not be changed.\""
   : "call :print \"PE input method is not a basic keyboard layout; keeping the current PE layout.\"";
  return script.Remove(line.Index,line.Length).Insert(line.Index,replacement);
 }
}
