using Schneegans.Unattend;
namespace UnattendLocal.Core;
public static class AccountLockoutPolicy {
 public const string Default="使用 Windows 默认策略", Disabled="禁用账户锁定", Custom="自定义账户锁定策略";
 public static bool IsDisabled(InstallPlan p)=>p.AccountLockoutMode==Disabled||(p.AccountLockoutMode==Custom&&p.LockoutThreshold==0);
 public static string? ValidationError(InstallPlan p){
  if(p.AccountLockoutMode is Default or Disabled)return null;
  if(p.AccountLockoutMode!=Custom)return "请选择有效的账户锁定策略";
  if(p.LockoutThreshold is null or <0 or >999)return "账户锁定失败次数须为 0–999";
  if(p.LockoutResetMinutes is null or <1 or >99999)return "失败计数重置等待时间须为 1–99999 分钟";
  if(p.LockoutDurationMinutes is null or <1 or >99999)return "锁定持续时间须为 1–99999 分钟";
  if(p.LockoutResetMinutes>p.LockoutDurationMinutes)return "失败计数重置等待时间不得大于锁定持续时间";
  return null;
 }
 public static ILockoutSettings Build(InstallPlan p){
  if(ValidationError(p) is {} error)throw new ArgumentException(error+"。");
  return p.AccountLockoutMode switch {Default=>new DefaultLockoutSettings(),Disabled=>new DisableLockoutSettings(),Custom=>new CustomLockoutSettings(p.LockoutThreshold,p.LockoutDurationMinutes,p.LockoutResetMinutes),_=>throw new ArgumentException("账户锁定策略无效。")};
 }
 public static string ModeSummary(InstallPlan p){
  if(ValidationError(p) is {} error)return "配置无效："+error;
  return p.AccountLockoutMode switch {Default=>"Windows 默认策略（不写入策略覆盖）",Disabled=>"已禁用：不按登录失败次数锁定账户",Custom when p.LockoutThreshold==0=>"自定义失败次数为 0：已禁用锁定，时间参数不触发锁定",Custom=>$"失败 {p.LockoutThreshold} 次后锁定；失败计数重置等待 {p.LockoutResetMinutes} 分钟；锁定 {p.LockoutDurationMinutes} 分钟后自动解锁",_=>"策略无效"};
 }
 public static string Summary(InstallPlan p)=>"账户锁定策略："+ModeSummary(p);
}
