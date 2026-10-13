using Schneegans.Unattend;
namespace UnattendLocal.Core;
public static class KeyboardLockSettings {
 public const string Default="使用 Windows 默认状态", Custom="自定义初始状态";
 public static ILockKeySettings Build(InstallPlan plan)=>plan.LockKeyMode switch {
  Default=>new SkipLockKeySettings(),
  Custom=>new ConfigureLockKeySettings(Key(plan.InitialCapsLockOn,plan.IgnoreCapsLock),Key(plan.InitialNumLockOn,plan.IgnoreNumLock),Key(plan.InitialScrollLockOn,plan.IgnoreScrollLock)),
  _=>throw new ArgumentException("请选择有效的锁定键初始状态设置方式。")
 };
 private static LockKeySetting Key(bool enabled,bool ignore)=>new(enabled?LockKeyInitial.On:LockKeyInitial.Off,ignore?LockKeyBehavior.Ignore:LockKeyBehavior.Toggle);
 private static string Behavior(bool ignore)=>ignore?"忽略按键":"正常切换";
 private static string State(bool enabled)=>enabled?"开启":"关闭";
 public static string ModeSummary(InstallPlan plan)=>plan.LockKeyMode switch {
  Default=>"Windows 默认状态（不写入键盘状态覆盖）",
  Custom=>$"Num Lock {State(plan.InitialNumLockOn)}；Caps Lock {State(plan.InitialCapsLockOn)}；Scroll Lock {State(plan.InitialScrollLockOn)}；按键行为：Num Lock {Behavior(plan.IgnoreNumLock)}，Caps Lock {Behavior(plan.IgnoreCapsLock)}，Scroll Lock {Behavior(plan.IgnoreScrollLock)}",
  _=>"锁定键设置方式无效，请重新选择"
 };
 public static string CurrentKey(InstallPlan plan,bool enabled,bool ignore=false)=>plan.LockKeyMode==Default?"当前：Windows 默认状态，保留的开关不参与生成。":plan.LockKeyMode==Custom?$"当前：初始{State(enabled)}，{Behavior(ignore)}。":"当前：锁定键设置方式无效，请重新选择。";
 public static string Summary(InstallPlan plan)=>"键盘初始状态："+ModeSummary(plan);
}
