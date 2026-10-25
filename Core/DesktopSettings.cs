using Schneegans.Unattend;
namespace UnattendLocal.Core;
public static class DesktopSettings {
 public const string DefaultIcons="使用 Windows 默认图标",CustomIcons="自定义桌面图标";
 public const string DefaultFolders="使用 Windows 默认文件夹",CustomFolders="自定义开始菜单文件夹";
 public record Item(string Id,string Label,string Property);
 public static IReadOnlyList<Item> Icons {get;}=new Item[]{
  new("ControlPanel",global::UnattendLocal.Localization.UiText.Source("text.69e7a943ab4e"),nameof(InstallPlan.DesktopControlPanel)),new("Desktop",global::UnattendLocal.Localization.UiText.Source("text.3ae8a8a99382"),nameof(InstallPlan.DesktopDesktop)),
  new("Documents",global::UnattendLocal.Localization.UiText.Source("text.2687ccdbb1d2"),nameof(InstallPlan.DesktopDocuments)),new("Downloads",global::UnattendLocal.Localization.UiText.Source("text.4673a2306165"),nameof(InstallPlan.DesktopDownloads)),
  new("Gallery",global::UnattendLocal.Localization.UiText.Source("text.0091a6122f4f"),nameof(InstallPlan.DesktopGallery)),new("Home",global::UnattendLocal.Localization.UiText.Source("text.3e0d676d2d85"),nameof(InstallPlan.DesktopHome)),
  new("Music",global::UnattendLocal.Localization.UiText.Source("text.db9514212493"),nameof(InstallPlan.DesktopMusic)),new("Network",global::UnattendLocal.Localization.UiText.Source("text.97b31b5d63f5"),nameof(InstallPlan.DesktopNetwork)),
  new("Pictures",global::UnattendLocal.Localization.UiText.Source("text.d24c10d37db0"),nameof(InstallPlan.DesktopPictures)),new("RecycleBin",global::UnattendLocal.Localization.UiText.Source("text.ba35dc23b245"),nameof(InstallPlan.DesktopRecycleBin)),
  new("ThisPC",global::UnattendLocal.Localization.UiText.Source("text.7580b1501edb"),nameof(InstallPlan.DesktopThisPC)),new("UserFiles",global::UnattendLocal.Localization.UiText.Source("text.806620421778"),nameof(InstallPlan.DesktopUserFiles)),
  new("Videos",global::UnattendLocal.Localization.UiText.Source("text.c20f7618d330"),nameof(InstallPlan.DesktopVideos))};
 public static IReadOnlyList<Item> Folders {get;}=new Item[]{
  new("Documents",global::UnattendLocal.Localization.UiText.Source("text.2687ccdbb1d2"),nameof(InstallPlan.StartFolderDocuments)),new("Downloads",global::UnattendLocal.Localization.UiText.Source("text.4673a2306165"),nameof(InstallPlan.StartFolderDownloads)),
  new("FileExplorer",global::UnattendLocal.Localization.UiText.Source("text.313caa84bb64"),nameof(InstallPlan.StartFolderFileExplorer)),new("Music",global::UnattendLocal.Localization.UiText.Source("text.db9514212493"),nameof(InstallPlan.StartFolderMusic)),
  new("Network",global::UnattendLocal.Localization.UiText.Source("text.97b31b5d63f5"),nameof(InstallPlan.StartFolderNetwork)),new("PersonalFolder",global::UnattendLocal.Localization.UiText.Source("text.eabd8417709a"),nameof(InstallPlan.StartFolderPersonalFolder)),
  new("Pictures",global::UnattendLocal.Localization.UiText.Source("text.d24c10d37db0"),nameof(InstallPlan.StartFolderPictures)),new("Settings",global::UnattendLocal.Localization.UiText.Source("text.df3d58c7d84b"),nameof(InstallPlan.StartFolderSettings)),
  new("Videos",global::UnattendLocal.Localization.UiText.Source("text.c20f7618d330"),nameof(InstallPlan.StartFolderVideos))};
 public static bool Selected(InstallPlan p,Item item)=>(bool)typeof(InstallPlan).GetProperty(item.Property)!.GetValue(p)!;
 public static void SelectAll(InstallPlan p,bool icons,bool value){foreach(var item in icons?Icons:Folders)typeof(InstallPlan).GetProperty(item.Property)!.SetValue(p,value);}
 public static string Selection(InstallPlan p,bool icons){var names=(icons?Icons:Folders).Where(item=>Selected(p,item)).Select(item=>item.Label).ToList();return names.Count==0?global::UnattendLocal.Localization.UiText.Source("text.6b2b7015d81c"):global::UnattendLocal.Localization.UiText.Join("、", names, false);}
 public static IDesktopIconSettings BuildIcons(UnattendGenerator generator,InstallPlan p)=>p.DesktopIconsMode switch {
  DefaultIcons=>new DefaultDesktopIconSettings(),CustomIcons=>new CustomDesktopIconSettings(Icons.ToDictionary(item=>generator.DesktopIcons[item.Id],item=>Selected(p,item))),
  _=>throw new ArgumentException(global::UnattendLocal.Localization.UiText.Source("text.df433dd8a61c"))
 };
 public static IStartFolderSettings BuildFolders(UnattendGenerator generator,InstallPlan p)=>p.StartFoldersMode switch {
  DefaultFolders=>new DefaultStartFolderSettings(),CustomFolders=>new CustomStartFolderSettings(Folders.ToDictionary(item=>generator.StartFolders[item.Id],item=>Selected(p,item))),
  _=>throw new ArgumentException(global::UnattendLocal.Localization.UiText.Source("text.cb12aad57abb"))
 };
 public static string IconSummary(InstallPlan p)=>p.DesktopIconsMode==CustomIcons?global::UnattendLocal.Localization.UiText.Format("message.924ab5cefe1d", new string[] { global::UnattendLocal.Localization.UiText.Arg((Selection(p,true)), 0, null) }):p.DesktopIconsMode;
 public static string FolderSummary(InstallPlan p)=>p.StartFoldersMode==CustomFolders?global::UnattendLocal.Localization.UiText.Format("message.924ab5cefe1d", new string[] { global::UnattendLocal.Localization.UiText.Arg((Selection(p,false)), 0, null) }):p.StartFoldersMode;
 public static string Summary(InstallPlan p)=>global::UnattendLocal.Localization.UiText.Format("message.8bd00d5c29a8", new string[] { global::UnattendLocal.Localization.UiText.Arg((IconSummary(p)), 0, null), global::UnattendLocal.Localization.UiText.Arg(((p.DeleteEdgeDesktopIcon?global::UnattendLocal.Localization.UiText.Source("text.2f9daa828907"):global::UnattendLocal.Localization.UiText.Source("text.7b09cc9a0f07"))), 0, null), global::UnattendLocal.Localization.UiText.Arg((FolderSummary(p)), 0, null) });
}
