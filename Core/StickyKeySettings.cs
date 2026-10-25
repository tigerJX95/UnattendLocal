using Schneegans.Unattend;
namespace UnattendLocal.Core;
public static class StickyKeySettings {
 public const string Default="使用 Windows 默认设置", Disabled="禁用粘滞键", Custom="自定义粘滞键";
 public static string? ValidationError(InstallPlan p)=>p.StickyKeysMode is Default or Disabled or Custom?null:global::UnattendLocal.Localization.UiText.Source("text.3ddacc7dfad5");
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
  if(ValidationError(p) is {} error)return global::UnattendLocal.Localization.UiText.Concat(new object?[] { global::UnattendLocal.Localization.UiText.Source("text.acbe63375dc8"), error },new bool[]{false,false});
  if(p.StickyKeysMode==Default)return global::UnattendLocal.Localization.UiText.Source("text.7e27516a75d4");
  if(p.StickyKeysMode==Disabled)return global::UnattendLocal.Localization.UiText.Source("text.fca5844259ec");
  var names=new List<string>();
  if(p.StickyHotKeyActive)names.Add(global::UnattendLocal.Localization.UiText.Source("text.8acf9b351153"));
  if(p.StickyHotKeySound)names.Add(global::UnattendLocal.Localization.UiText.Source("text.99c64bfd2faa"));
  if(p.StickyIndicator)names.Add(global::UnattendLocal.Localization.UiText.Source("text.b39a7c9836dc"));
  if(p.StickyAudibleFeedback)names.Add(global::UnattendLocal.Localization.UiText.Source("text.d70d17b0f29c"));
  if(p.StickyTriState)names.Add(global::UnattendLocal.Localization.UiText.Source("text.37be147529e9"));
  if(p.StickyTwoKeysOff)names.Add(global::UnattendLocal.Localization.UiText.Source("text.434b4e7fe0f3"));
  return names.Count==0?global::UnattendLocal.Localization.UiText.Source("text.748918e7e4a4"):global::UnattendLocal.Localization.UiText.Concat(new object?[] { global::UnattendLocal.Localization.UiText.Source("text.6abf1f6a609b"), global::UnattendLocal.Localization.UiText.Join("、", names, false) },new bool[]{false,false});
 }
 public static string Summary(InstallPlan p)=>global::UnattendLocal.Localization.UiText.Concat(new object?[] { global::UnattendLocal.Localization.UiText.Source("text.3b729177008f"), ModeSummary(p) },new bool[]{false,false});
}
