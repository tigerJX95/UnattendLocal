using Schneegans.Unattend;
namespace UnattendLocal.Core;
public static class KeyboardLockSettings {
 public const string Default="使用 Windows 默认状态", Custom="自定义初始状态";
 public static ILockKeySettings Build(InstallPlan plan)=>plan.LockKeyMode switch {
  Default=>new SkipLockKeySettings(),
  Custom=>new ConfigureLockKeySettings(Key(plan.InitialCapsLockOn,plan.IgnoreCapsLock),Key(plan.InitialNumLockOn,plan.IgnoreNumLock),Key(plan.InitialScrollLockOn,plan.IgnoreScrollLock)),
  _=>throw new ArgumentException(global::UnattendLocal.Localization.UiText.Source("text.c0d053a1e805"))
 };
 private static LockKeySetting Key(bool enabled,bool ignore)=>new(enabled?LockKeyInitial.On:LockKeyInitial.Off,ignore?LockKeyBehavior.Ignore:LockKeyBehavior.Toggle);
 private static string Behavior(bool ignore)=>ignore?global::UnattendLocal.Localization.UiText.Source("text.d8ac41175069"):global::UnattendLocal.Localization.UiText.Source("text.57bfaf6dd207");
 private static string State(bool enabled)=>enabled?global::UnattendLocal.Localization.UiText.Source("text.8da97ddda990"):global::UnattendLocal.Localization.UiText.Source("text.3fd47edce45b");
 public static string ModeSummary(InstallPlan plan)=>plan.LockKeyMode switch {
  Default=>global::UnattendLocal.Localization.UiText.Source("text.942afa1c61f7"),
  Custom=>global::UnattendLocal.Localization.UiText.Format("message.087dc43b85c0", new string[] { global::UnattendLocal.Localization.UiText.Arg((State(plan.InitialNumLockOn)), 0, null), global::UnattendLocal.Localization.UiText.Arg((State(plan.InitialCapsLockOn)), 0, null), global::UnattendLocal.Localization.UiText.Arg((State(plan.InitialScrollLockOn)), 0, null), global::UnattendLocal.Localization.UiText.Arg((Behavior(plan.IgnoreNumLock)), 0, null), global::UnattendLocal.Localization.UiText.Arg((Behavior(plan.IgnoreCapsLock)), 0, null), global::UnattendLocal.Localization.UiText.Arg((Behavior(plan.IgnoreScrollLock)), 0, null) }),
  _=>global::UnattendLocal.Localization.UiText.Source("text.0d337880927b")
 };
 public static string CurrentKey(InstallPlan plan,bool enabled,bool ignore=false)=>plan.LockKeyMode==Default?global::UnattendLocal.Localization.UiText.Source("text.a8aff96b644a"):plan.LockKeyMode==Custom?global::UnattendLocal.Localization.UiText.Format("message.68937e40f8c2", new string[] { global::UnattendLocal.Localization.UiText.Arg((State(enabled)), 0, null), global::UnattendLocal.Localization.UiText.Arg((Behavior(ignore)), 0, null) }):global::UnattendLocal.Localization.UiText.Source("text.5027aeb09681");
 public static string Summary(InstallPlan plan)=>global::UnattendLocal.Localization.UiText.Concat(new object?[] { global::UnattendLocal.Localization.UiText.Source("text.343aa0252eae"), ModeSummary(plan) },new bool[]{false,false});
}
