using System.Text.Json;
using System.Xml;
using Schneegans.Unattend;
namespace UnattendLocal.Core;

// Presentation settings use the fixed upstream generator; custom documents are validated before mapping.
public static class ShellSettings {
 public const string LayoutNamespace="http://schemas.microsoft.com/Start/2014/LayoutModification";
 public const string FullNamespace="http://schemas.microsoft.com/Start/2014/FullDefaultLayout";
 public const string TaskbarNamespace="http://schemas.microsoft.com/Start/2014/TaskbarLayout";
 public const string StartNamespace="http://schemas.microsoft.com/Start/2014/StartLayout";
 public const string CustomXml="自定义 XML";
 public const string CustomJson="自定义 JSON";
 public const int MaxDocumentLength=2*1024*1024;
 public const string TaskbarExample="""
<LayoutModificationTemplate xmlns="http://schemas.microsoft.com/Start/2014/LayoutModification" xmlns:defaultlayout="http://schemas.microsoft.com/Start/2014/FullDefaultLayout" xmlns:taskbar="http://schemas.microsoft.com/Start/2014/TaskbarLayout" Version="1">
  <CustomTaskbarLayoutCollection PinListPlacement="Replace">
    <defaultlayout:TaskbarLayout>
      <taskbar:TaskbarPinList>
        <taskbar:DesktopApp DesktopApplicationID="Microsoft.Windows.Explorer" />
        <taskbar:UWA AppUserModelID="windows.immersivecontrolpanel_cw5n1h2txyewy!microsoft.windows.immersivecontrolpanel" />
        <taskbar:DesktopApp DesktopApplicationLinkPath="%APPDATA%\Microsoft\Windows\Start Menu\Programs\Windows PowerShell\Windows PowerShell.lnk" />
      </taskbar:TaskbarPinList>
    </defaultlayout:TaskbarLayout>
  </CustomTaskbarLayoutCollection>
</LayoutModificationTemplate>
""";
 public const string TilesExample="""
<LayoutModificationTemplate xmlns="http://schemas.microsoft.com/Start/2014/LayoutModification" xmlns:defaultlayout="http://schemas.microsoft.com/Start/2014/FullDefaultLayout" xmlns:start="http://schemas.microsoft.com/Start/2014/StartLayout" Version="1">
  <LayoutOptions StartTileGroupCellWidth="6" />
  <DefaultLayoutOverride>
    <StartLayoutCollection>
      <defaultlayout:StartLayout GroupCellWidth="6">
        <start:Group Name="">
          <start:Tile Size="2x2" Column="4" Row="0" AppUserModelID="windows.immersivecontrolpanel_cw5n1h2txyewy!microsoft.windows.immersivecontrolpanel" />
          <start:DesktopApplicationTile Size="2x2" Column="0" Row="0" DesktopApplicationLinkPath="%ALLUSERSPROFILE%\Microsoft\Windows\Start Menu\Programs\Microsoft Edge.lnk" />
          <start:DesktopApplicationTile Size="2x2" Column="2" Row="0" DesktopApplicationLinkPath="%APPDATA%\Microsoft\Windows\Start Menu\Programs\System Tools\File Explorer.lnk" />
        </start:Group>
      </defaultlayout:StartLayout>
    </StartLayoutCollection>
  </DefaultLayoutOverride>
</LayoutModificationTemplate>
""";
 public const string PinsExample="""
{
  "pinnedList": [
    { "desktopAppLink": "%ALLUSERSPROFILE%\\Microsoft\\Windows\\Start Menu\\Programs\\Microsoft Edge.lnk" },
    { "desktopAppLink": "%APPDATA%\\Microsoft\\Windows\\Start Menu\\Programs\\File Explorer.lnk" },
    { "packagedAppId": "windows.immersivecontrolpanel_cw5n1h2txyewy!microsoft.windows.immersivecontrolpanel" }
  ]
}
""";
 public static TaskbarSearchMode Search(string mode)=>mode switch {
  "完整搜索框"=>TaskbarSearchMode.Box,"图标和文字"=>TaskbarSearchMode.Label,"仅图标"=>TaskbarSearchMode.Icon,"隐藏"=>TaskbarSearchMode.Hide,
  _=>throw new ArgumentException("请选择有效的任务栏搜索样式。")
 };
 public static ITaskbarIcons Taskbar(InstallPlan p)=>p.TaskbarIconsMode switch {
  "使用默认图标"=>new DefaultTaskbarIcons(),"移除全部固定图标"=>new EmptyTaskbarIcons(),CustomXml=>new CustomTaskbarIcons(p.TaskbarIconsXml),
  _=>throw new ArgumentException("请选择有效的任务栏固定图标模式。")
 };
 public static IStartTilesSettings Tiles(InstallPlan p)=>p.StartTilesMode switch {
  "使用默认磁贴"=>new DefaultStartTilesSettings(),"移除全部磁贴"=>new EmptyStartTilesSettings(),CustomXml=>new CustomStartTilesSettings(p.StartTilesXml),
  _=>throw new ArgumentException("请选择有效的 Windows 10 开始菜单磁贴模式。")
 };
 public static IStartPinsSettings Pins(InstallPlan p)=>p.StartPinsMode switch {
  "使用默认固定应用"=>new DefaultStartPinsSettings(),"移除全部固定应用"=>new EmptyStartPinsSettings(),CustomJson=>new CustomStartPinsSettings(p.StartPinsJson),
  _=>throw new ArgumentException("请选择有效的 Windows 11 开始菜单固定应用模式。")
 };
 public static void Validate(InstallPlan p){
  _=Search(p.TaskbarSearch);_=Taskbar(p);_=Tiles(p);_=Pins(p);
  if(p.TaskbarIconsMode==CustomXml)ValidateDocument("taskbar",p.TaskbarIconsXml);
  if(p.StartTilesMode==CustomXml)ValidateDocument("tiles",p.StartTilesXml);
  if(p.StartPinsMode==CustomJson)ValidateDocument("pins",p.StartPinsJson);
 }
 public static void ValidateDocument(string kind,string content){
  string label=kind switch {"taskbar"=>"任务栏图标 XML","tiles"=>"Windows 10 磁贴 XML","pins"=>"Windows 11 固定应用 JSON",_=>throw new ArgumentException("布局类型无效。")};
  if(string.IsNullOrWhiteSpace(content))throw new ArgumentException($"请填写或导入{label}。");
  if(content.Length>MaxDocumentLength)throw new ArgumentException($"{label}过大，请使用不超过 2 Mi 字符的布局文件。");
  try {
   if(kind=="pins"){
    using var json=JsonDocument.Parse(content);
    if(json.RootElement.ValueKind!=JsonValueKind.Object||!json.RootElement.TryGetProperty("pinnedList",out var list)||list.ValueKind!=JsonValueKind.Array)throw new ArgumentException("根对象必须包含 pinnedList 数组。");
    foreach(var item in list.EnumerateArray()){
     if(item.ValueKind!=JsonValueKind.Object)throw new ArgumentException("pinnedList 中的每项必须是应用对象。");
     var valid=false;
     foreach(var key in new[]{"desktopAppLink","desktopAppId","packagedAppId"})if(item.TryGetProperty(key,out var value)){
      if(value.ValueKind!=JsonValueKind.String||string.IsNullOrWhiteSpace(value.GetString()))throw new ArgumentException($"{key} 必须是非空字符串。");valid=true;
     }
     if(item.TryGetProperty("secondaryTile",out var secondary)&&secondary.ValueKind==JsonValueKind.Object)valid=true;
     if(!valid)throw new ArgumentException("应用对象须包含 desktopAppLink、desktopAppId、packagedAppId 或 secondaryTile。");
    }
    return;
   }
   var doc=new XmlDocument{XmlResolver=null};
   using(var reader=XmlReader.Create(new StringReader(content),new XmlReaderSettings{DtdProcessing=DtdProcessing.Prohibit,XmlResolver=null,MaxCharactersInDocument=MaxDocumentLength}))doc.Load(reader);
   if(doc.DocumentElement is not {} root||root.LocalName!="LayoutModificationTemplate"||root.NamespaceURI!=LayoutNamespace||root.GetAttribute("Version")!="1")throw new ArgumentException("根元素必须为 Version=\"1\" 的 LayoutModificationTemplate，并使用 Microsoft 布局命名空间。");
   var ns=new XmlNamespaceManager(doc.NameTable);ns.AddNamespace("l",LayoutNamespace);ns.AddNamespace("d",FullNamespace);ns.AddNamespace("t",TaskbarNamespace);
   string path=kind=="taskbar"?"/l:LayoutModificationTemplate/l:CustomTaskbarLayoutCollection/d:TaskbarLayout/t:TaskbarPinList":"/l:LayoutModificationTemplate/l:DefaultLayoutOverride/l:StartLayoutCollection/d:StartLayout";
   if(doc.SelectNodes(path,ns)!.Count==0)throw new ArgumentException(kind=="taskbar"?"缺少 CustomTaskbarLayoutCollection / TaskbarLayout / TaskbarPinList。":"缺少 DefaultLayoutOverride / StartLayoutCollection / StartLayout。");
   if(kind=="taskbar"){
    foreach(XmlElement collection in doc.SelectNodes("/l:LayoutModificationTemplate/l:CustomTaskbarLayoutCollection",ns)!)if(collection.HasAttribute("PinListPlacement")&&collection.GetAttribute("PinListPlacement") is not ("Replace" or "Append"))throw new ArgumentException("PinListPlacement 只能为 Replace 或 Append。");
    foreach(XmlNode listNode in doc.SelectNodes(path,ns)!)foreach(XmlNode node in listNode.ChildNodes)if(node is XmlElement item){
     if(item.NamespaceURI!=TaskbarNamespace||item.LocalName is not ("DesktopApp" or "UWA" or "SecondaryTile"))throw new ArgumentException("TaskbarPinList 包含不支持的图标元素。");
     if(item.LocalName=="DesktopApp"&&string.IsNullOrWhiteSpace(item.GetAttribute("DesktopApplicationID"))&&string.IsNullOrWhiteSpace(item.GetAttribute("DesktopApplicationLinkPath")))throw new ArgumentException("DesktopApp 需要应用 ID 或快捷方式路径。");
     if(item.LocalName=="UWA"&&string.IsNullOrWhiteSpace(item.GetAttribute("AppUserModelID")))throw new ArgumentException("UWA 需要 AppUserModelID。");
    }
   }
  }catch(Exception e)when(e is XmlException or JsonException or ArgumentException){throw new ArgumentException($"{label}未通过检查：{e.Message}",e);}
 }
 public static string Summary(InstallPlan p)=>$"任务栏：搜索 {p.TaskbarSearch}；固定图标 {p.TaskbarIconsMode}\n任务栏开关：禁用小组件 {On(p.DisableWidgets)}；左对齐 {On(p.LeftTaskbar)}；隐藏任务视图 {On(p.HideTaskViewButton)}\n搜索与托盘：不显示 Bing 结果 {On(p.DisableBingResults)}；显示全部托盘图标 {On(p.ShowAllTrayIcons)}\n开始菜单：Windows 10 {p.StartTilesMode}；Windows 11 {p.StartPinsMode}";
 private static string On(bool value)=>value?"开启":"关闭";
}
