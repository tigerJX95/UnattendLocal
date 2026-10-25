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
  _=>throw new ArgumentException(global::UnattendLocal.Localization.UiText.Source("text.aabce4911fd9"))
 };
 public static ITaskbarIcons Taskbar(InstallPlan p)=>p.TaskbarIconsMode switch {
  "使用默认图标"=>new DefaultTaskbarIcons(),"移除全部固定图标"=>new EmptyTaskbarIcons(),CustomXml=>new CustomTaskbarIcons(p.TaskbarIconsXml),
  _=>throw new ArgumentException(global::UnattendLocal.Localization.UiText.Source("text.85247d1d172b"))
 };
 public static IStartTilesSettings Tiles(InstallPlan p)=>p.StartTilesMode switch {
  "使用默认磁贴"=>new DefaultStartTilesSettings(),"移除全部磁贴"=>new EmptyStartTilesSettings(),CustomXml=>new CustomStartTilesSettings(p.StartTilesXml),
  _=>throw new ArgumentException(global::UnattendLocal.Localization.UiText.Source("text.fdf8af4926c0"))
 };
 public static IStartPinsSettings Pins(InstallPlan p)=>p.StartPinsMode switch {
  "使用默认固定应用"=>new DefaultStartPinsSettings(),"移除全部固定应用"=>new EmptyStartPinsSettings(),CustomJson=>new CustomStartPinsSettings(p.StartPinsJson),
  _=>throw new ArgumentException(global::UnattendLocal.Localization.UiText.Source("text.84c334f5145f"))
 };
 public static void Validate(InstallPlan p){
  _=Search(p.TaskbarSearch);_=Taskbar(p);_=Tiles(p);_=Pins(p);
  if(p.TaskbarIconsMode==CustomXml)ValidateDocument("taskbar",p.TaskbarIconsXml);
  if(p.StartTilesMode==CustomXml)ValidateDocument("tiles",p.StartTilesXml);
  if(p.StartPinsMode==CustomJson)ValidateDocument("pins",p.StartPinsJson);
 }
 public static void ValidateDocument(string kind,string content){
  string label=kind switch {"taskbar"=>global::UnattendLocal.Localization.UiText.Source("text.0ec7d9ab7696"),"tiles"=>global::UnattendLocal.Localization.UiText.Source("text.eaff4584199a"),"pins"=>global::UnattendLocal.Localization.UiText.Source("text.b4ab0abe0171"),_=>throw new ArgumentException(global::UnattendLocal.Localization.UiText.Source("text.041423d2ccdd"))};
  if(string.IsNullOrWhiteSpace(content))throw new ArgumentException(global::UnattendLocal.Localization.UiText.Format("message.74459b09a4f3", new string[] { global::UnattendLocal.Localization.UiText.Arg((label), 0, null) }));
  if(content.Length>MaxDocumentLength)throw new ArgumentException(global::UnattendLocal.Localization.UiText.Format("message.95c65fa6f9fe", new string[] { global::UnattendLocal.Localization.UiText.Arg((label), 0, null) }));
  try {
   if(kind=="pins"){
    using var json=JsonDocument.Parse(content);
    if(json.RootElement.ValueKind!=JsonValueKind.Object||!json.RootElement.TryGetProperty("pinnedList",out var list)||list.ValueKind!=JsonValueKind.Array)throw new ArgumentException(global::UnattendLocal.Localization.UiText.Source("text.600f223d467d"));
    foreach(var item in list.EnumerateArray()){
     if(item.ValueKind!=JsonValueKind.Object)throw new ArgumentException(global::UnattendLocal.Localization.UiText.Source("text.125c1476143d"));
     var valid=false;
     foreach(var key in new[]{"desktopAppLink","desktopAppId","packagedAppId"})if(item.TryGetProperty(key,out var value)){
      if(value.ValueKind!=JsonValueKind.String||string.IsNullOrWhiteSpace(value.GetString()))throw new ArgumentException(global::UnattendLocal.Localization.UiText.Format("message.45812d9f6ec8", new string[] { global::UnattendLocal.Localization.UiText.Arg((key), 0, null) }));valid=true;
     }
     if(item.TryGetProperty("secondaryTile",out var secondary)&&secondary.ValueKind==JsonValueKind.Object)valid=true;
     if(!valid)throw new ArgumentException(global::UnattendLocal.Localization.UiText.Source("text.b94a23698ae6"));
    }
    return;
   }
   var doc=new XmlDocument{XmlResolver=null};
   using(var reader=XmlReader.Create(new StringReader(content),new XmlReaderSettings{DtdProcessing=DtdProcessing.Prohibit,XmlResolver=null,MaxCharactersInDocument=MaxDocumentLength}))doc.Load(reader);
   if(doc.DocumentElement is not {} root||root.LocalName!="LayoutModificationTemplate"||root.NamespaceURI!=LayoutNamespace||root.GetAttribute("Version")!="1")throw new ArgumentException(global::UnattendLocal.Localization.UiText.Source("text.b80f9377c968"));
   var ns=new XmlNamespaceManager(doc.NameTable);ns.AddNamespace("l",LayoutNamespace);ns.AddNamespace("d",FullNamespace);ns.AddNamespace("t",TaskbarNamespace);
   string path=kind=="taskbar"?"/l:LayoutModificationTemplate/l:CustomTaskbarLayoutCollection/d:TaskbarLayout/t:TaskbarPinList":"/l:LayoutModificationTemplate/l:DefaultLayoutOverride/l:StartLayoutCollection/d:StartLayout";
   if(doc.SelectNodes(path,ns)!.Count==0)throw new ArgumentException(kind=="taskbar"?global::UnattendLocal.Localization.UiText.Source("text.8d70528e96bf"):global::UnattendLocal.Localization.UiText.Source("text.f3b632b8ca1c"));
   if(kind=="taskbar"){
    foreach(XmlElement collection in doc.SelectNodes("/l:LayoutModificationTemplate/l:CustomTaskbarLayoutCollection",ns)!)if(collection.HasAttribute("PinListPlacement")&&collection.GetAttribute("PinListPlacement") is not ("Replace" or "Append"))throw new ArgumentException(global::UnattendLocal.Localization.UiText.Source("text.5e221570ad42"));
    foreach(XmlNode listNode in doc.SelectNodes(path,ns)!)foreach(XmlNode node in listNode.ChildNodes)if(node is XmlElement item){
     if(item.NamespaceURI!=TaskbarNamespace||item.LocalName is not ("DesktopApp" or "UWA" or "SecondaryTile"))throw new ArgumentException(global::UnattendLocal.Localization.UiText.Source("text.f5dc691c21a2"));
     if(item.LocalName=="DesktopApp"&&string.IsNullOrWhiteSpace(item.GetAttribute("DesktopApplicationID"))&&string.IsNullOrWhiteSpace(item.GetAttribute("DesktopApplicationLinkPath")))throw new ArgumentException(global::UnattendLocal.Localization.UiText.Source("text.4117dbecd956"));
     if(item.LocalName=="UWA"&&string.IsNullOrWhiteSpace(item.GetAttribute("AppUserModelID")))throw new ArgumentException(global::UnattendLocal.Localization.UiText.Source("text.c52d1557a7d6"));
    }
   }
  }catch(Exception e)when(e is XmlException or JsonException or ArgumentException){throw new ArgumentException(global::UnattendLocal.Localization.UiText.Format("message.c1000c804395", new string[] { global::UnattendLocal.Localization.UiText.Arg((label), 0, null), global::UnattendLocal.Localization.UiText.Arg((e.Message), 0, null) }),e);}
 }
 public static string Summary(InstallPlan p)=>global::UnattendLocal.Localization.UiText.Format("message.6ad1298890c0", new string[] { global::UnattendLocal.Localization.UiText.Arg((p.TaskbarSearch), 0, null), global::UnattendLocal.Localization.UiText.Arg((p.TaskbarIconsMode), 0, null), global::UnattendLocal.Localization.UiText.Arg((On(p.DisableWidgets)), 0, null), global::UnattendLocal.Localization.UiText.Arg((On(p.LeftTaskbar)), 0, null), global::UnattendLocal.Localization.UiText.Arg((On(p.HideTaskViewButton)), 0, null), global::UnattendLocal.Localization.UiText.Arg((On(p.DisableBingResults)), 0, null), global::UnattendLocal.Localization.UiText.Arg((On(p.ShowAllTrayIcons)), 0, null), global::UnattendLocal.Localization.UiText.Arg((p.StartTilesMode), 0, null), global::UnattendLocal.Localization.UiText.Arg((p.StartPinsMode), 0, null) });
 private static string On(bool value)=>value?global::UnattendLocal.Localization.UiText.Source("text.8da97ddda990"):global::UnattendLocal.Localization.UiText.Source("text.3fd47edce45b");
}
