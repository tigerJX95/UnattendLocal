using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Xml;
namespace UnattendLocal.Core;

public static class SetupDriverSettings {
 public const string ComponentName="Microsoft-Windows-PnpCustomizationsWinPE";
 private const string Wcm="http://schemas.microsoft.com/WMIConfig/2002/State";
 public const string ReadOnlyNotice="原 XML 的驱动配置含凭据、额外字段、不同结构或自定义 PE 流程，原文保留；安装驱动目录区域只读，不能用界面默认值覆盖。";
 public const string Description="默认关闭。启用后在 windowsPE 阶段通过 Windows Setup 原生 DriverPaths 配置额外驱动目录；每行一个，最多 8 个，单条最长 259 字符。本版支持目标 PE 中可访问的盘符绝对目录或不带凭据的 UNC 目录，不读取当前电脑、不复制驱动文件、不运行驱动安装程序。请准备已解压且包含 INF 及配套文件的驱动包，匹配目标 Windows、架构与签名要求。PE 盘符可能变化；网络路径要求 PE 已具备网卡驱动、网络和访问权限，不能依靠网络上的网卡驱动解决访问该网络本身的条件。修改开关或目录会取消磁盘危险操作确认，需重新核对。不会自动匹配介质盘符或注入 boot.wim。自定义 PE 的驱动脚本不由本区域控制。";
 public record Source(bool Present,bool Known,string[] Paths);
 public static bool CanEdit(InstallPlan p)=>p.ImportedXml?.SetupDriversReadOnly!=true;
 public static string[] ReadPaths(string? text){
  if(text==null||text.Length>8*261)throw new ArgumentException("驱动目录列表为空或过长；最多 8 行，每个目录最长 259 字符。");
  var paths=text.Replace("\r\n","\n").Split('\n').Select(s=>s.Trim()).Where(s=>s.Length>0).ToArray();
  if(paths.Length is <1 or >8)throw new ArgumentException("请每行填写一个驱动目录，最多 8 个。");
  foreach(string path in paths){
   try{XmlConvert.VerifyXmlChars(path);}catch(XmlException){throw new ArgumentException("驱动目录包含 XML 非法字符，请核对输入。");}
   bool local=Regex.IsMatch(path,@"\A[A-Za-z]:\\",RegexOptions.CultureInvariant);
   bool unc=Regex.IsMatch(path,@"\A\\\\[^\\]+\\[^\\]+(?:\\|\z)",RegexOptions.CultureInvariant)&&!path.StartsWith(@"\\?\",StringComparison.Ordinal)&&!path.StartsWith(@"\\.\",StringComparison.Ordinal);
   if(path.Length>259||path.Any(char.IsControl)||path.Any(c=>"\"<>|?*%/".Contains(c))||!(local||unc)||path[(local?2:0)..].Contains(':'))throw new ArgumentException("驱动目录须为最长 259 字符的盘符绝对路径或 UNC 目录；不支持相对路径、变量、通配符、引号或设备路径。");
   string tail=local?path[3..]:path[2..];
   var segments=tail.Split('\\');
   if(segments.Any(s=>s is "." or ".."||s.EndsWith(' ')||s.EndsWith('.'))||segments.Take(Math.Max(0,segments.Length-1)).Any(s=>s.Length==0))throw new ArgumentException("驱动目录不能含相对层级、重复分隔符或以空格/点结尾的目录名。");
   if(new[]{".inf",".sys",".cat",".exe",".zip",".cab"}.Any(ext=>path.EndsWith(ext,StringComparison.OrdinalIgnoreCase)))throw new ArgumentException("请填写已解压 INF 驱动所在的目录，不要填写单个驱动文件、安装程序或压缩包。");
  }
  if(paths.Select(p=>p.TrimEnd('\\')).Distinct(StringComparer.OrdinalIgnoreCase).Count()!=paths.Length)throw new ArgumentException("驱动目录不能重复（忽略大小写和末尾反斜线）。");
  return paths;
 }
 public static void Validate(InstallPlan p){if(p.UseSetupDrivers)_=ReadPaths(p.SetupDriverPaths);}
 public static string Summary(InstallPlan p){
  if(!CanEdit(p))return "安装驱动目录：原 XML 配置只读保留，请核对原文，不按界面默认值推断。";
  if(!p.UseSetupDrivers)return "安装驱动目录：不写入额外的原生目录设置。";
  try{return "安装驱动目录："+string.Join("；",ReadPaths(p.SetupDriverPaths));}catch(ArgumentException){return "安装驱动目录：已启用，目录待配置。";}
 }
 private static XmlNamespaceManager Ns(XmlDocument doc){var ns=new XmlNamespaceManager(doc.NameTable);ns.AddNamespace("u",XmlMerge.Namespace);return ns;}
 private static XmlElement[] Nodes(XmlDocument doc)=>doc.SelectNodes("//u:component[@name='"+ComponentName+"']",Ns(doc))!.Cast<XmlElement>().ToArray();
 internal static bool CustomPe(XmlDocument doc)=>doc.SelectNodes("//*[local-name()='PEScriptCopy']")!.Count>0||doc.SelectNodes("//u:settings[@pass='windowsPE']/u:component[@name='Microsoft-Windows-Setup']/u:RunSynchronous/u:RunSynchronousCommand/u:Path",Ns(doc))!.Cast<XmlElement>().Any(p=>Regex.IsMatch(p.InnerText,@"\bdism(?:\.exe)?\s+/Apply-Image\b|[A-Za-z]:\\[^\r\n]*\.cmd\b",RegexOptions.IgnoreCase,TimeSpan.FromSeconds(1)));
 public static void Apply(XmlDocument doc,XmlNamespaceManager ns,InstallPlan p){
  if(!p.UseSetupDrivers)return;
  var paths=ReadPaths(p.SetupDriverPaths);
  if(Nodes(doc).Length>0||CustomPe(doc))throw new ArgumentException("生成基准已有驱动组件或自定义 PE 流程，不能重复添加安装驱动目录。");
  var settings=doc.SelectSingleNode("/u:unattend/u:settings[@pass='windowsPE']",ns) as XmlElement??throw new ArgumentException("缺少 windowsPE 安装阶段，不能配置驱动目录。");
  XmlElement Add(XmlNode parent,string name,string? value=null){var e=doc.CreateElement(name,XmlMerge.Namespace);if(value!=null)e.InnerText=value;parent.AppendChild(e);return e;}
  var component=Add(settings,"component");
  foreach(var pair in new Dictionary<string,string>{{"name",ComponentName},{"processorArchitecture",p.Architecture},{"publicKeyToken","31bf3856ad364e35"},{"language","neutral"},{"versionScope","nonSxS"}})component.SetAttribute(pair.Key,pair.Value);
  var list=Add(component,"DriverPaths");
  for(int i=0;i<paths.Length;i++){
   var row=Add(list,"PathAndCredentials");
   foreach(var pair in new Dictionary<string,string>{{"action","add"},{"keyValue",(i+1).ToString(CultureInfo.InvariantCulture)}}){var a=doc.CreateAttribute("wcm",pair.Key,Wcm);a.Value=pair.Value;row.Attributes.Append(a);}
   Add(row,"Path",paths[i]);
  }
 }
 public static Source Inspect(XmlDocument doc){
  var nodes=Nodes(doc);var unknown=new Source(nodes.Length>0,false,[]);
  if(CustomPe(doc))return unknown;
  if(nodes.Length==0)return new(false,true,[]);
  if(nodes.Length!=1)return unknown;
  try{
   var c=nodes[0];if(c.ParentNode is not XmlElement s||s.LocalName!="settings"||s.NamespaceURI!=XmlMerge.Namespace||s.ParentNode!=doc.DocumentElement||s.GetAttribute("pass")!="windowsPE")return unknown;
   string arch=c.GetAttribute("processorArchitecture");if(arch is not ("amd64" or "arm64" or "x86"))return unknown;
   var paths=c.SelectNodes("u:DriverPaths/u:PathAndCredentials/u:Path",Ns(doc))!.Cast<XmlElement>().Select(e=>e.InnerText).ToArray();
   var p=new InstallPlan{UseSetupDrivers=true,SetupDriverPaths=string.Join("\n",paths),Architecture=arch};
   var clean=XmlMerge.Read("<unattend xmlns='"+XmlMerge.Namespace+"'><settings pass='windowsPE'/></unattend>");Apply(clean,Ns(clean),p);
   if(!XmlMerge.Equal(c,Nodes(clean).Single()))return unknown;
   return new(true,true,ReadPaths(p.SetupDriverPaths));
  }catch(Exception error)when(error is ArgumentException or InvalidOperationException){return unknown;}
 }
 private static void Assign(InstallPlan p,Source source){p.UseSetupDrivers=source.Present&&source.Known;if(p.UseSetupDrivers)p.SetupDriverPaths=string.Join("\r\n",source.Paths);}
 public static Source ReadIntoPlan(XmlDocument doc,InstallPlan p,List<string> notes){var source=Inspect(doc);Assign(p,source);if(!source.Known)notes.Add(ReadOnlyNotice);return source;}
 public static void UpdateImportState(ImportedXml data,Source source){data.SetupDriversReadOnly=!source.Known;data.SetupDriversImportVersion=1;}
 public static void ValidateImported(InstallPlan p,XmlDocument source){if(p.UseSetupDrivers&&!Inspect(source).Known)throw new ArgumentException(ReadOnlyNotice);}
 public static void NormalizeLoaded(InstallPlan p,JsonElement root){
  if(p.ImportedXml is not {OriginalOmitted:false} data||data.SourceXml.Length==0)return;
  var source=XmlMerge.Read(data.SourceXml);var state=Inspect(source);
  if(data.SetupDriversImportVersion<1&&state is {Present:true,Known:true}){
   bool missing=!root.TryGetProperty(nameof(InstallPlan.UseSetupDrivers),out _)&&!root.TryGetProperty(nameof(InstallPlan.SetupDriverPaths),out _);
   if(missing&&data.BaselineXml.Length>0&&Nodes(XmlMerge.Read(data.BaselineXml)).Length==0){
    try{var baseline=XmlMerge.Read(data.BaselineXml);var next=new InstallPlan{UseSetupDrivers=true,SetupDriverPaths=string.Join("\r\n",state.Paths),Architecture=Nodes(source).Single().GetAttribute("processorArchitecture")};Apply(baseline,Ns(baseline),next);Assign(p,state);data.BaselineXml=baseline.OuterXml;}
    catch(ArgumentException){data.Blockers=data.Blockers.Append("旧方案的安装驱动编辑基准无法可靠迁移，请重新导入原 XML；当前待处理修改保留。").Distinct().ToArray();}
   }else data.Blockers=data.Blockers.Append("旧方案的安装驱动编辑基准无法可靠迁移，请重新导入原 XML；当前待处理修改保留。").Distinct().ToArray();
  }
  data.Notices=data.Notices.Where(n=>n!="原文保留，未实现的组件："+ComponentName&&n!=ReadOnlyNotice).ToArray();
  if(!state.Known)data.Notices=[..data.Notices,ReadOnlyNotice];UpdateImportState(data,state);
 }
}
