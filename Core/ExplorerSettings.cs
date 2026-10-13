using Schneegans.Unattend;
namespace UnattendLocal.Core;
public static class ExplorerSettings {
 public const string Default="使用 Windows 默认设置";
 public const string ProtectedOnly="隐藏受保护的操作系统文件";
 public const string ShowAll="显示全部文件";
 public static HideModes HiddenFiles(string mode)=>mode switch {
  Default=>HideModes.Hidden,ProtectedOnly=>HideModes.HiddenSystem,ShowAll=>HideModes.None,
  _=>throw new ArgumentException("请选择有效的资源管理器隐藏文件显示方式。")
 };
 public static string Summary(InstallPlan p)=>$"资源管理器：{p.HideFilesMode}；显示扩展名 {On(p.ShowFileExtensions)}\n资源管理器操作：经典右键菜单 {On(p.ClassicContextMenu)}；隐藏悬停说明 {On(p.HideInfoTip)}；打开此电脑 {On(p.LaunchToThisPC)}\n任务栏结束任务：{On(p.ShowEndTask)}";
 private static string On(bool value)=>value?"开启":"关闭";
}
