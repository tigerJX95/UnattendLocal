using Schneegans.Unattend;
namespace UnattendLocal.Core;
public static class DesktopSettings {
 public const string DefaultIcons="使用 Windows 默认图标",CustomIcons="自定义桌面图标";
 public const string DefaultFolders="使用 Windows 默认文件夹",CustomFolders="自定义开始菜单文件夹";
 public record Item(string Id,string Label,string Property);
 public static IReadOnlyList<Item> Icons {get;}=new Item[]{
  new("ControlPanel","控制面板",nameof(InstallPlan.DesktopControlPanel)),new("Desktop","桌面文件夹",nameof(InstallPlan.DesktopDesktop)),
  new("Documents","文档",nameof(InstallPlan.DesktopDocuments)),new("Downloads","下载",nameof(InstallPlan.DesktopDownloads)),
  new("Gallery","图库",nameof(InstallPlan.DesktopGallery)),new("Home","主页",nameof(InstallPlan.DesktopHome)),
  new("Music","音乐",nameof(InstallPlan.DesktopMusic)),new("Network","网络",nameof(InstallPlan.DesktopNetwork)),
  new("Pictures","图片",nameof(InstallPlan.DesktopPictures)),new("RecycleBin","回收站",nameof(InstallPlan.DesktopRecycleBin)),
  new("ThisPC","此电脑",nameof(InstallPlan.DesktopThisPC)),new("UserFiles","用户的文件",nameof(InstallPlan.DesktopUserFiles)),
  new("Videos","视频",nameof(InstallPlan.DesktopVideos))};
 public static IReadOnlyList<Item> Folders {get;}=new Item[]{
  new("Documents","文档",nameof(InstallPlan.StartFolderDocuments)),new("Downloads","下载",nameof(InstallPlan.StartFolderDownloads)),
  new("FileExplorer","文件资源管理器",nameof(InstallPlan.StartFolderFileExplorer)),new("Music","音乐",nameof(InstallPlan.StartFolderMusic)),
  new("Network","网络",nameof(InstallPlan.StartFolderNetwork)),new("PersonalFolder","个人文件夹",nameof(InstallPlan.StartFolderPersonalFolder)),
  new("Pictures","图片",nameof(InstallPlan.StartFolderPictures)),new("Settings","设置",nameof(InstallPlan.StartFolderSettings)),
  new("Videos","视频",nameof(InstallPlan.StartFolderVideos))};
 public static bool Selected(InstallPlan p,Item item)=>(bool)typeof(InstallPlan).GetProperty(item.Property)!.GetValue(p)!;
 public static void SelectAll(InstallPlan p,bool icons,bool value){foreach(var item in icons?Icons:Folders)typeof(InstallPlan).GetProperty(item.Property)!.SetValue(p,value);}
 public static string Selection(InstallPlan p,bool icons){var names=(icons?Icons:Folders).Where(item=>Selected(p,item)).Select(item=>item.Label).ToList();return names.Count==0?"全部隐藏":string.Join("、",names);}
 public static IDesktopIconSettings BuildIcons(UnattendGenerator generator,InstallPlan p)=>p.DesktopIconsMode switch {
  DefaultIcons=>new DefaultDesktopIconSettings(),CustomIcons=>new CustomDesktopIconSettings(Icons.ToDictionary(item=>generator.DesktopIcons[item.Id],item=>Selected(p,item))),
  _=>throw new ArgumentException("请选择有效的桌面图标设置方式。")
 };
 public static IStartFolderSettings BuildFolders(UnattendGenerator generator,InstallPlan p)=>p.StartFoldersMode switch {
  DefaultFolders=>new DefaultStartFolderSettings(),CustomFolders=>new CustomStartFolderSettings(Folders.ToDictionary(item=>generator.StartFolders[item.Id],item=>Selected(p,item))),
  _=>throw new ArgumentException("请选择有效的开始菜单文件夹设置方式。")
 };
 public static string IconSummary(InstallPlan p)=>p.DesktopIconsMode==CustomIcons?$"自定义：{Selection(p,true)}":p.DesktopIconsMode;
 public static string FolderSummary(InstallPlan p)=>p.StartFoldersMode==CustomFolders?$"自定义：{Selection(p,false)}":p.StartFoldersMode;
 public static string Summary(InstallPlan p)=>$"系统桌面图标：{IconSummary(p)}\nEdge 桌面快捷方式：{(p.DeleteEdgeDesktopIcon?"删除":"保留 Windows 默认行为")}\n开始菜单电源按钮旁文件夹：{FolderSummary(p)}（仅 Windows 11）";
}
