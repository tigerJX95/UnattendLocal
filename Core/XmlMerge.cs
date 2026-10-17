using System.Text;
using System.Xml;
namespace UnattendLocal.Core;

// Apply only changes between the imported editable baseline and the newly generated document.
// Unchanged source content survives. Ambiguous or conflicting replacements fail atomically.
public static class XmlMerge {
 public const string Namespace="urn:schemas-microsoft-com:unattend";
 public const int MaxLength=16*1024*1024;
 public static XmlDocument Read(string text){
  if(string.IsNullOrWhiteSpace(text)||text.Length>MaxLength)throw new ArgumentException("应答 XML 为空或超过 16 Mi 字符。");
  var doc=new XmlDocument{XmlResolver=null};
  using(var reader=XmlReader.Create(new StringReader(text),new XmlReaderSettings{DtdProcessing=DtdProcessing.Prohibit,XmlResolver=null,MaxCharactersInDocument=MaxLength,MaxCharactersFromEntities=0}))doc.Load(reader);
  if(doc.DocumentElement is not {} root||root.LocalName!="unattend"||root.NamespaceURI!=Namespace)throw new ArgumentException("请选择 Windows 应答 XML（unattend 根元素及正确命名空间），不能导入布局 XML 或遮蔽后的预览。");
  if(doc.SelectNodes("//processing-instruction()")!.Count>0)throw new ArgumentException("暂不支持包含处理指令的应答 XML。");
  var pending=new Stack<(XmlElement Element,int Depth)>();pending.Push((root,1));int count=0;
  while(pending.Count>0){var item=pending.Pop();if(item.Depth>128||++count>50000)throw new ArgumentException("XML 层级或项目过多，不能可靠处理。");foreach(var child in item.Element.ChildNodes.OfType<XmlElement>())pending.Push((child,item.Depth+1));}
  return doc;
 }
 public static string Identity(XmlElement e){
  string id=e.NamespaceURI+"|"+e.LocalName;
  if(e.HasAttribute("keyValue","http://schemas.microsoft.com/WMIConfig/2002/State"))id+="|keyValue="+e.GetAttribute("keyValue","http://schemas.microsoft.com/WMIConfig/2002/State");
  foreach(var a in new[]{"pass","name","processorArchitecture","path"})if(e.HasAttribute(a))id+="|"+a+"="+e.GetAttribute(a);
  if(e.LocalName is "RunSynchronousCommand" or "SynchronousCommand" or "ModifyPartition" or "CreatePartition")id+="|Order="+e.ChildNodes.OfType<XmlElement>().FirstOrDefault(n=>n.LocalName=="Order")?.InnerText;
  if(e.LocalName is "LocalAccount")id+="|Name="+e.ChildNodes.OfType<XmlElement>().FirstOrDefault(n=>n.LocalName=="Name")?.InnerText;
  if(e.LocalName is "Disk")id+="|DiskID="+e.ChildNodes.OfType<XmlElement>().FirstOrDefault(n=>n.LocalName=="DiskID")?.InnerText;
  if(e.LocalName is "MetaData")id+="|Key="+e.ChildNodes.OfType<XmlElement>().FirstOrDefault(n=>n.LocalName=="Key")?.InnerText;
  return id;
 }
 private static Dictionary<string,XmlElement> Children(XmlElement e){
  var result=new Dictionary<string,XmlElement>();
  foreach(var child in e.ChildNodes.OfType<XmlElement>())if(!result.TryAdd(Identity(child),child))throw new ArgumentException("XML 存在重复或无法区分的项目："+e.LocalName+" / "+child.LocalName+"；为避免错误合并，未导出。");
  return result;
 }
 private static Dictionary<string,string> Attributes(XmlElement e)=>e.Attributes.Cast<XmlAttribute>().Where(a=>a.NamespaceURI!="http://www.w3.org/2000/xmlns/").ToDictionary(a=>a.NamespaceURI+"|"+a.LocalName,a=>a.Value);
 public static string Canonical(XmlElement e){
  var sb=new StringBuilder();sb.Append('{').Append(e.NamespaceURI).Append('}').Append(e.LocalName).Append('[');
  foreach(var a in Attributes(e).OrderBy(a=>a.Key))sb.Append(a.Key.Length).Append(':').Append(a.Key).Append('=').Append(a.Value.Length).Append(':').Append(a.Value);
  sb.Append(']');
  var children=e.ChildNodes.OfType<XmlElement>().ToArray();
  if(children.Length==0)sb.Append(Normalize(e.InnerText).Length).Append(':').Append(Normalize(e.InnerText));
  else foreach(var child in children)sb.Append(Canonical(child));
  return sb.Append("</>").ToString();
 }
 public static bool Equal(XmlElement? a,XmlElement? b)=>a==null?b==null:b!=null&&Canonical(a)==Canonical(b);
 private static string Normalize(string text)=>text.Replace("\r\n","\n").Replace('\r','\n').Trim();
 private static Exception Conflict(string path)=>new ArgumentException("XML 修改与原有内容存在冲突，未导出："+path+"。原文件未修改；请核对原 XML，或减少本次修改后重试。");
 public static XmlDocument Apply(string source,string baseline,XmlDocument updated)=>Apply(source,baseline,updated,null);
 internal static XmlDocument Apply(string source,string baseline,XmlDocument updated,Func<string,string,string,string,string?>? scriptMerge){
  var original=Read(source);var before=Read(baseline);Merge(original.DocumentElement!,before.DocumentElement!,updated.DocumentElement!,"unattend",scriptMerge);return original;
 }
 private static void Merge(XmlElement target,XmlElement before,XmlElement after,string path,Func<string,string,string,string,string?>? scriptMerge){
  if(Equal(before,after))return;
  var oldAttrs=Attributes(before);var newAttrs=Attributes(after);
  foreach(var key in oldAttrs.Keys.Union(newAttrs.Keys)){
   oldAttrs.TryGetValue(key,out var oldValue);newAttrs.TryGetValue(key,out var newValue);if(oldValue==newValue)continue;
   var split=key.IndexOf('|');var space=key[..split];var name=key[(split+1)..];var current=target.HasAttribute(name,space)?target.GetAttribute(name,space):null;
   if(current!=oldValue&&current!=newValue)throw Conflict(path+"/@"+name);
   if(newValue==null)target.RemoveAttribute(name,space);else target.SetAttribute(name,space,newValue);
  }
  var old=Children(before);var next=Children(after);var actual=Children(target);
  if(old.Count==0&&next.Count==0){
   if(Normalize(before.InnerText)==Normalize(after.InnerText))return;
   if(actual.Count!=0)throw Conflict(path);
   if(Normalize(target.InnerText)==Normalize(after.InnerText))return;
   if(Normalize(target.InnerText)==Normalize(before.InnerText)){target.InnerText=after.InnerText;return;}
   if(target.LocalName=="File"&&target.GetAttribute("path").EndsWith(".ps1",StringComparison.OrdinalIgnoreCase)){
    target.InnerText=scriptMerge?.Invoke(target.GetAttribute("path"),target.InnerText,before.InnerText,after.InnerText) ?? MergeScript(target.InnerText,before.InnerText,after.InnerText,path);return;
   }
   throw Conflict(path);
  }
  foreach(var key in old.Keys.Union(next.Keys)){
   old.TryGetValue(key,out var b);next.TryGetValue(key,out var a);actual.TryGetValue(key,out var t);var childPath=path+" / "+(a??b)!.LocalName;
   if((a??b)!.HasAttribute("path"))childPath+=" ("+(a??b)!.GetAttribute("path")+")";
   else if((a??b)!.HasAttribute("name"))childPath+=" ("+(a??b)!.GetAttribute("name")+")";
   if(Equal(b,a))continue;
   if(b==null){
    if(t!=null){if(!Equal(t,a))throw Conflict(childPath);continue;}
    // Insert relative to generated neighbours to preserve Windows component order.
    var following=a!.NextSibling;XmlElement? anchor=null;
    while(following!=null){if(following is XmlElement f&&actual.TryGetValue(Identity(f),out anchor))break;following=following.NextSibling;}
    var node=target.OwnerDocument!.ImportNode(a,true);if(anchor==null)target.AppendChild(node);else target.InsertBefore(node,anchor);continue;
   }
   if(a==null){if(t==null)continue;if(!Equal(t,b))throw Conflict(childPath);target.RemoveChild(t);continue;}
   if(t==null)throw Conflict(childPath);Merge(t,b,a,childPath,scriptMerge);
  }
 }
 internal static string MergeScript(string source,string baseline,string updated,string path){
  var b=Normalize(baseline).Split('\n');var a=Normalize(updated).Split('\n');var s=Normalize(source).Split('\n').ToList();
  if((long)b.Length*a.Length>4_000_000)throw Conflict(path+"（脚本变更过大）");
  var lcs=new int[b.Length+1,a.Length+1];
  for(int i=b.Length-1;i>=0;i--)for(int j=a.Length-1;j>=0;j--)lcs[i,j]=b[i]==a[j]?lcs[i+1,j+1]+1:Math.Max(lcs[i+1,j],lcs[i,j+1]);
  var hunks=new List<(int Start,int End,List<string> Replacement)>();int x=0,y=0;
  while(x<b.Length||y<a.Length){
   if(x<b.Length&&y<a.Length&&b[x]==a[y]){x++;y++;continue;}
   int start=x;var replacement=new List<string>();
   while(x<b.Length||y<a.Length){
    if(x<b.Length&&y<a.Length&&b[x]==a[y])break;
    if(y<a.Length&&(x==b.Length||lcs[x,y+1]>=lcs[x+1,y]))replacement.Add(a[y++]);else x++;
   }
   hunks.Add((start,x,replacement));
  }
  // Every edit needs an exact unique match including surrounding unchanged lines.
  foreach(var h in hunks.AsEnumerable().Reverse()){
   int left=Math.Max(0,h.Start-2),right=Math.Min(b.Length,h.End+2);var needle=b[left..right];var matches=new List<int>();
   for(int i=0;i+needle.Length<=s.Count;i++)if(s.Skip(i).Take(needle.Length).SequenceEqual(needle))matches.Add(i);
   if(matches.Count!=1)throw Conflict(path+"（脚本上下文无法唯一匹配）");
   int at=matches[0]+h.Start-left;s.RemoveRange(at,h.End-h.Start);s.InsertRange(at,h.Replacement);
  }
  return string.Join('\n',s);
 }
}
