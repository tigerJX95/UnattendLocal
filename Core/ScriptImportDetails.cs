using System.Text.RegularExpressions;
using System.Xml;
namespace UnattendLocal.Core;

// Comparison only; never execute scripts or infer equivalence from keywords.
internal static class ScriptImportDetails {
 private const string Pattern=@"(?m)^\t\{\n(?<body>(?:\t\t[^\n]*\n)*)\t\};\n";
 private static readonly string[] Stages=["Specialize.ps1","DefaultUser.ps1","FirstLogon.ps1","UserOnce.ps1"];
 private static Match[] Blocks(string text)=>Regex.Matches(text.Replace("\r\n","\n"),Pattern,RegexOptions.None,TimeSpan.FromSeconds(1)).Cast<Match>().ToArray();
 // Retain literal/here-string indentation instead of normalizing arbitrary code.
 private static string Normal(string text)=>text.Replace("\r\n","\n").Trim();
 internal static bool Describe(XmlDocument source,XmlElement actual,XmlElement expected,List<string> notes){
  string path=actual.GetAttribute("path"),name=path[(path.LastIndexOf('\\')+1)..];
  if(path!=@"C:\Windows\Setup\Scripts\"+name||!Stages.Contains(name))return false;
  string a=actual.InnerText.Replace("\r\n","\n"),b=expected.InnerText.Replace("\r\n","\n");
  if(Normal(a)==Normal(b))return true;
  var old=Blocks(a);var next=Blocks(b);
  string Skeleton(string text)=>Normal(Regex.Replace(text,Pattern,"",RegexOptions.None,TimeSpan.FromSeconds(1)));
  if(Skeleton(a)!=Skeleton(b)||AdditionalSetupSettings.KnownBlocks(source,name).Length!=old.Length){
   notes.Add($"脚本原文已保留，执行管线或命令块结构未完整识别：{name}；不能仅凭相同关键词认定为界面设置。");return true;
  }
  var remaining=next.Select(m=>Normal(m.Groups["body"].Value)).ToList();int unknown=0;
  for(int i=0;i<old.Length;i++){
   string body=Normal(old[i].Groups["body"].Value);int at=remaining.IndexOf(body);
   if(at>=0){remaining.RemoveAt(at);continue;}
   unknown++;int line=1+a[..old[i].Index].Count(c=>c=='\n');
   string label=body.Contains(LegacyUpgradeSettings.Marker,StringComparison.OrdinalIgnoreCase)?"MoSetup 升级兼容设置":"自定义或未识别命令";
   notes.Add($"脚本原文已保留，部分内容未映射为界面设置：{name}；第 {i+1} 个命令块（比较视图第 {line} 行），{label}，未确认与当前实现等效。行号可能因路径或日志兼容处理与原文略有差异。");
   if(unknown==12){notes.Add($"脚本原文已保留：{name}；命令差异较多，仅列出前 12 项，请在原 XML 核对完整脚本。");break;}
  }
  if(unknown==0){
   if(remaining.Count>0)notes.Add($"脚本原文已保留：{name}；界面生成版本含 {remaining.Count} 个未在原文完整匹配的命令块，相关设置的修改可能发生冲突。");
   else notes.Add($"脚本中的设置已识别：{name}；原命令顺序不同，未修改时保留原顺序。");
  }
  return true;
 }
}
