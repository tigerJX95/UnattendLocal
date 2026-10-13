using Schneegans.Unattend;
namespace UnattendLocal.Core;
public static class KeyboardLockSettings {
 public const string Default="使用 Windows 默认状态", Custom="自定义初始状态";
 public static ILockKeySettings Build(InstallPlan plan)=>plan.LockKeyMode switch {
  Default=>new SkipLockKeySettings(),
  Custom=>new ConfigureLockKeySettings(Key(plan.InitialCapsLockOn),Key(plan.InitialNumLockOn),Key(plan.InitialScrollLockOn)),
  _=>throw new ArgumentException("请选择有效的锁定键初始状态设置方式。")
 };
 private static LockKeySetting Key(bool enabled)=>new(enabled?LockKeyInitial.On:LockKeyInitial.Off,LockKeyBehavior.Toggle);
 private static string State(bool enabled)=>enabled?"开启":"关闭";
 public static string ModeSummary(InstallPlan plan)=>plan.LockKeyMode switch {
  Default=>"Windows 默认状态（不写入键盘状态覆盖）",
  Custom=>$"Num Lock {State(plan.InitialNumLockOn)}；Caps Lock {State(plan.InitialCapsLockOn)}；Scroll Lock {State(plan.InitialScrollLockOn)}",
  _=>"锁定键设置方式无效，请重新选择"
 };
 public static string CurrentKey(InstallPlan plan,bool enabled)=>plan.LockKeyMode==Default?"当前：Windows 默认状态，保留的开关不参与生成。":plan.LockKeyMode==Custom?$"当前：初始{State(enabled)}，按键仍可正常切换。":"当前：锁定键设置方式无效，请重新选择。";
 public static string Summary(InstallPlan plan)=>"键盘初始状态："+ModeSummary(plan);
}
