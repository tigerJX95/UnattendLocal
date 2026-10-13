using Schneegans.Unattend;
namespace UnattendLocal.Core;
public static class StickyKeySettings {
 public const string Default="使用 Windows 默认设置", Disabled="禁用粘滞键", Custom="自定义粘滞键";
 public static string? ValidationError(InstallPlan p)=>p.StickyKeysMode is Default or Disabled or Custom?null:"请选择有效的粘滞键设置方式";
 public static HashSet<StickyKeys> Flags(InstallPlan p){
  var flags=new HashSet<StickyKeys>();
  if(p.StickyHotKeyActive)flags.Add(StickyKeys.HotKeyActive);
  if(p.StickyHotKeySound)flags.Add(StickyKeys.HotKeySound);
  if(p.StickyIndicator)flags.Add(StickyKeys.Indicator);
  if(p.StickyAudibleFeedback)flags.Add(StickyKeys.AudibleFeedback);
  if(p.StickyTriState)flags.Add(StickyKeys.TriState);
  if(p.StickyTwoKeysOff)flags.Add(StickyKeys.TwoKeysOff);
  return flags;
 }
 public static IStickyKeysSettings Build(InstallPlan p)=>p.StickyKeysMode switch {
  Default=>new DefaultStickyKeysSettings(),Disabled=>new DisabledStickyKeysSettings(),
  Custom=>new CustomStickyKeysSettings(Flags(p)),_=>throw new ArgumentException(ValidationError(p)+"。")
 };
 public static string ModeSummary(InstallPlan p){
  if(ValidationError(p) is {} error)return "配置无效："+error;
  if(p.StickyKeysMode==Default)return "Windows 默认设置（不写入覆盖）";
  if(p.StickyKeysMode==Disabled)return "禁用粘滞键及连续五次 Shift 快捷方式";
  var names=new List<string>();
  if(p.StickyHotKeyActive)names.Add("连续五次 Shift 切换");
  if(p.StickyHotKeySound)names.Add("快捷切换提示音");
  if(p.StickyIndicator)names.Add("任务栏图标");
  if(p.StickyAudibleFeedback)names.Add("按下/释放快捷键提示音");
  if(p.StickyTriState)names.Add("连续两次锁定快捷键");
  if(p.StickyTwoKeysOff)names.Add("同时按两键关闭");
  return names.Count==0?"自定义：六项选项均关闭":"自定义："+string.Join("、",names);
 }
 public static string Summary(InstallPlan p)=>"粘滞键："+ModeSummary(p);
}
