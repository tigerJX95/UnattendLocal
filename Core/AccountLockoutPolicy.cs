using Schneegans.Unattend;
namespace UnattendLocal.Core;
public static class AccountLockoutPolicy {
 public const string Default="使用 Windows 默认策略", Disabled="禁用账户锁定", Custom="自定义账户锁定策略";
 public static bool IsDisabled(InstallPlan p)=>p.AccountLockoutMode==Disabled||(p.AccountLockoutMode==Custom&&p.LockoutThreshold==0);
 public static string? ValidationError(InstallPlan p){
  if(p.AccountLockoutMode is Default or Disabled)return null;
  if(p.AccountLockoutMode!=Custom)return global::UnattendLocal.Localization.UiText.Source("text.a53f372a4ed3");
  if(p.LockoutThreshold is null or <0 or >999)return global::UnattendLocal.Localization.UiText.Source("text.dd3fc2512015");
  if(p.LockoutResetMinutes is null or <1 or >99999)return global::UnattendLocal.Localization.UiText.Source("text.991931b30f66");
  if(p.LockoutDurationMinutes is null or <1 or >99999)return global::UnattendLocal.Localization.UiText.Source("text.0fcfa052c65f");
  if(p.LockoutResetMinutes>p.LockoutDurationMinutes)return global::UnattendLocal.Localization.UiText.Source("text.f332e8b5c672");
  return null;
 }
 public static ILockoutSettings Build(InstallPlan p){
  if(ValidationError(p) is {} error)throw new ArgumentException(error+"。");
  return p.AccountLockoutMode switch {Default=>new DefaultLockoutSettings(),Disabled=>new DisableLockoutSettings(),Custom=>new CustomLockoutSettings(p.LockoutThreshold,p.LockoutDurationMinutes,p.LockoutResetMinutes),_=>throw new ArgumentException(global::UnattendLocal.Localization.UiText.Source("text.04637ff6e2a2"))};
 }
 public static string ModeSummary(InstallPlan p){
  if(ValidationError(p) is {} error)return global::UnattendLocal.Localization.UiText.Concat(new object?[] { global::UnattendLocal.Localization.UiText.Source("text.acbe63375dc8"), error },new bool[]{false,false});
  return p.AccountLockoutMode switch {Default=>global::UnattendLocal.Localization.UiText.Source("text.3b777ee41f7f"),Disabled=>global::UnattendLocal.Localization.UiText.Source("text.1714abe2728b"),Custom when p.LockoutThreshold==0=>global::UnattendLocal.Localization.UiText.Source("text.9eee22848280"),Custom=>global::UnattendLocal.Localization.UiText.Format("message.b9b92aa25432", new string[] { global::UnattendLocal.Localization.UiText.Arg((p.LockoutThreshold), 0, null), global::UnattendLocal.Localization.UiText.Arg((p.LockoutResetMinutes), 0, null), global::UnattendLocal.Localization.UiText.Arg((p.LockoutDurationMinutes), 0, null) }),_=>global::UnattendLocal.Localization.UiText.Source("text.b962f2c179ee")};
 }
 public static string Summary(InstallPlan p)=>global::UnattendLocal.Localization.UiText.Concat(new object?[] { global::UnattendLocal.Localization.UiText.Source("text.e45b42cd5e8c"), ModeSummary(p) },new bool[]{false,false});
}
