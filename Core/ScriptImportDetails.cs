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
   notes.Add(global::UnattendLocal.Localization.UiText.Format("message.5b6b3866d2d6", new string[] { global::UnattendLocal.Localization.UiText.Arg((name), 0, null) }));return true;
  }
  var remaining=next.Select(m=>Normal(m.Groups["body"].Value)).ToList();int unknown=0;
  for(int i=0;i<old.Length;i++){
   string body=Normal(old[i].Groups["body"].Value);int at=remaining.IndexOf(body);
   if(at>=0){remaining.RemoveAt(at);continue;}
   unknown++;int line=1+a[..old[i].Index].Count(c=>c=='\n');
   string label=body.Contains(LegacyUpgradeSettings.Marker,StringComparison.OrdinalIgnoreCase)?global::UnattendLocal.Localization.UiText.Source("text.132f3075f263"):global::UnattendLocal.Localization.UiText.Source("text.93f153db4fc8");
   notes.Add(global::UnattendLocal.Localization.UiText.Format("message.22f9a89a05d3", new string[] { global::UnattendLocal.Localization.UiText.Arg((name), 0, null), global::UnattendLocal.Localization.UiText.Arg((i+1), 0, null), global::UnattendLocal.Localization.UiText.Arg((line), 0, null), global::UnattendLocal.Localization.UiText.Arg((label), 0, null) }));
   if(unknown==12){notes.Add(global::UnattendLocal.Localization.UiText.Format("message.706d6ef5074c", new string[] { global::UnattendLocal.Localization.UiText.Arg((name), 0, null) }));break;}
  }
  if(unknown==0){
   if(remaining.Count>0)notes.Add(global::UnattendLocal.Localization.UiText.Format("message.17f90e8ae554", new string[] { global::UnattendLocal.Localization.UiText.Arg((name), 0, null), global::UnattendLocal.Localization.UiText.Arg((remaining.Count), 0, null) }));
   else notes.Add(global::UnattendLocal.Localization.UiText.Format("message.ce56c5290849", new string[] { global::UnattendLocal.Localization.UiText.Arg((name), 0, null) }));
  }
  return true;
 }
}
