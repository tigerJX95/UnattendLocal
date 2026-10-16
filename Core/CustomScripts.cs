using System.Text.Json.Serialization;
using Schneegans.Unattend;
namespace UnattendLocal.Core;
public class ScriptEntry : Observable {
 private bool enabled=true;public bool Enabled {get=>enabled;set=>Set(ref enabled,value);}
 private string phase="System";public string Phase {get=>phase;set{Set(ref phase,value);Notify(nameof(Hint));Notify(nameof(Example));Notify(nameof(Label));}}
 private string type="Ps1";public string Type {get=>type;set{Set(ref type,value);Notify(nameof(Example));Notify(nameof(Label));Notify(nameof(Hint));Notify(nameof(IsRemovedType));}}
 private string content="";public string Content {get=>content;set=>Set(ref content,value);}
 [JsonIgnore] public bool IsRemovedType=>string.Equals(Type,"Js",StringComparison.OrdinalIgnoreCase);
 [JsonIgnore] public string Label=>IsRemovedType?"JScript（.js）已停止支持，请移除或手动改写":$"{CustomScripts.PhaseLabel(Phase)} · .{Type.ToLowerInvariant()}";
 [JsonIgnore] public string Hint=>IsRemovedType?"JScript（.js）已停止支持。旧脚本内容保留并停用；请移除或手动改写，不会自动转换格式。":Phase switch {
  "System"=>"系统安装（specialize）：以 SYSTEM 身份执行，在创建用户账户之前。",
  "DefaultUser"=>"默认用户配置：在 System 后、创建账户前执行。默认用户注册表已挂载为 HKU\\DefaultUser，勿使用 HKCU 修改模板。",
  "FirstLogon"=>"全机首次登录：首个用户登录时执行一次。管理员首次登录时通常具有提升权限；不会自动开启自动登录。",
  "UserOnce"=>"每个用户首次登录：通过默认用户 RunOnce 执行，在各用户上下文中运行；不能保证桌面已完全加载，也不自动提升权限。",
  _=>"请选择有效执行阶段。"};
 [JsonIgnore] public string Example=>Phase=="System"&&Type=="Cmd"?CustomScripts.DirectoryExample:Phase=="FirstLogon"&&Type=="Ps1"?CustomScripts.NetworkExample:"填写脚本内容；空白不会载入任何示例。\nDefaultUser 注册表前缀：\n.cmd：HKU\\DefaultUser\\…\n.ps1：Registry::HKU\\DefaultUser\\…\n.reg：[HKEY_USERS\\DefaultUser\\…]";
}
public static class CustomScripts {
 public const string DirectoryExample="setx.exe DIRCMD \"/A /O:GN /C /N\" /m";
 public const string NetworkExample="Get-NetConnectionProfile -InterfaceAlias 'Ethernet*' | Set-NetConnectionProfile -NetworkCategory 'Private';";
 public static string PhaseLabel(string phase)=>phase switch {"System"=>"系统安装时","DefaultUser"=>"默认用户配置","FirstLogon"=>"全机首次登录","UserOnce"=>"每个用户首次登录",_=>"未知阶段"};
 public static ScriptEntry Duplicate(InstallPlan p,ScriptEntry source){
  int index=p.Scripts.IndexOf(source);if(index<0)throw new ArgumentException("请选择当前方案中的脚本。");
  if(p.Scripts.Count>=60)throw new ArgumentException("最多 60 条脚本。");
  var copy=new ScriptEntry{Enabled=false,Phase=source.Phase,Type=source.Type,Content=source.Content};p.Scripts.Insert(index+1,copy);return copy;
 }
 public static ScriptSettings Build(InstallPlan p){
  if(p.Scripts==null||p.Scripts.Any(s=>s==null)||p.Scripts.Count>60)throw new ArgumentException("自定义脚本列表无效，最多 60 条。");
  var result=new List<Script>();
  foreach(var row in p.Scripts.Where(s=>s.Enabled)){
   if(row.IsRemovedType)throw new ArgumentException("JScript（.js）已停止支持；请停用、移除或手动改写该脚本后再导出。");
   if(!Enum.TryParse<ScriptPhase>(row.Phase,out var phase)||!Enum.IsDefined(phase)||row.Phase!=phase.ToString())throw new ArgumentException("请选择有效脚本阶段。");
   if(!Enum.TryParse<ScriptType>(row.Type,out var type)||!Enum.IsDefined(type)||row.Type!=type.ToString()||!phase.GetAllowedTypes().Contains(type))throw new ArgumentException("请选择有效脚本格式；DefaultUser 仅支持 CMD、PowerShell 和 REG。");
   if(string.IsNullOrWhiteSpace(row.Content)||row.Content.Length>2*1024*1024||row.Content.Any(c=>!System.Xml.XmlConvert.IsXmlChar(c)))throw new ArgumentException("启用的脚本必须填写内容，且不超过 2 Mi 字符、不含 XML 非法字符。");
   if(result.Count(s=>s.Phase==phase)>=15)throw new ArgumentException("每个执行阶段最多启用 15 条脚本。");
   result.Add(new Script(row.Content,phase,type));
  }
  return new ScriptSettings(result,p.RestartExplorerAfterScripts);
 }
 public static string Summary(InstallPlan p)=>"自定义脚本："+string.Join("；",new[]{"System","DefaultUser","FirstLogon","UserOnce"}.Select(phase=>$"{PhaseLabel(phase)} {p.Scripts?.Count(s=>s!=null&&s.Enabled&&s.Phase==phase)??0} 条"))+$"；脚本后重启资源管理器 {(p.RestartExplorerAfterScripts?"开启":"关闭")}";
}
