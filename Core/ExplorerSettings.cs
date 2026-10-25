using Schneegans.Unattend;
namespace UnattendLocal.Core;
public static class ExplorerSettings {
 public const string Default="使用 Windows 默认设置";
 public const string ProtectedOnly="隐藏受保护的操作系统文件";
 public const string ShowAll="显示全部文件";
 public static HideModes HiddenFiles(string mode)=>mode switch {
  Default=>HideModes.Hidden,ProtectedOnly=>HideModes.HiddenSystem,ShowAll=>HideModes.None,
  _=>throw new ArgumentException(global::UnattendLocal.Localization.UiText.Source("text.503c16ca2604"))
 };
 public static string Summary(InstallPlan p)=>global::UnattendLocal.Localization.UiText.Format("message.604344d85161", new string[] { global::UnattendLocal.Localization.UiText.Arg((p.HideFilesMode), 0, null), global::UnattendLocal.Localization.UiText.Arg((On(p.ShowFileExtensions)), 0, null), global::UnattendLocal.Localization.UiText.Arg((On(p.ClassicContextMenu)), 0, null), global::UnattendLocal.Localization.UiText.Arg((On(p.HideInfoTip)), 0, null), global::UnattendLocal.Localization.UiText.Arg((On(p.LaunchToThisPC)), 0, null), global::UnattendLocal.Localization.UiText.Arg((On(p.ShowEndTask)), 0, null) });
 private static string On(bool value)=>value?global::UnattendLocal.Localization.UiText.Source("text.8da97ddda990"):global::UnattendLocal.Localization.UiText.Source("text.3fd47edce45b");
}
