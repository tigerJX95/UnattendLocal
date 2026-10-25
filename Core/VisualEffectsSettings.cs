using System.Collections.Immutable;
using Schneegans.Unattend;
namespace UnattendLocal.Core;
public static class VisualEffectsSettings {
 public const string Default="使用 Windows 默认设置",Appearance="调整为最佳外观",Performance="调整为最佳性能",Custom="自定义视觉效果";
 public record Item(Effect Effect,string Label,string Description,string Property) {public string HelpId=>"effect"+Effect;}
 public static IReadOnlyList<Item> Items {get;}=new Item[]{
  new(Effect.ControlAnimations,global::UnattendLocal.Localization.UiText.Source("text.15ea5ba0c16f"),global::UnattendLocal.Localization.UiText.Source("text.f193d7dfaa8b"),nameof(InstallPlan.EffectControlAnimations)),
  new(Effect.AnimateMinMax,global::UnattendLocal.Localization.UiText.Source("text.1e4ae0d2eda0"),global::UnattendLocal.Localization.UiText.Source("text.1bc382efb093"),nameof(InstallPlan.EffectAnimateMinMax)),
  new(Effect.TaskbarAnimations,global::UnattendLocal.Localization.UiText.Source("text.004ae1b70038"),global::UnattendLocal.Localization.UiText.Source("text.14426766240b"),nameof(InstallPlan.EffectTaskbarAnimations)),
  new(Effect.DWMAeroPeekEnabled,global::UnattendLocal.Localization.UiText.Source("text.3b557192a6f4"),global::UnattendLocal.Localization.UiText.Source("text.406a7b01d800"),nameof(InstallPlan.EffectDWMAeroPeekEnabled)),
  new(Effect.MenuAnimation,global::UnattendLocal.Localization.UiText.Source("text.b8ae38d43e1f"),global::UnattendLocal.Localization.UiText.Source("text.2f267f1c74db"),nameof(InstallPlan.EffectMenuAnimation)),
  new(Effect.TooltipAnimation,global::UnattendLocal.Localization.UiText.Source("text.36b72bd2d41e"),global::UnattendLocal.Localization.UiText.Source("text.b330a009121e"),nameof(InstallPlan.EffectTooltipAnimation)),
  new(Effect.SelectionFade,global::UnattendLocal.Localization.UiText.Source("text.03ef7d4ce42a"),global::UnattendLocal.Localization.UiText.Source("text.919b361bedd2"),nameof(InstallPlan.EffectSelectionFade)),
  new(Effect.DWMSaveThumbnailEnabled,global::UnattendLocal.Localization.UiText.Source("text.d7ae4277dbb4"),global::UnattendLocal.Localization.UiText.Source("text.38a25c7521cb"),nameof(InstallPlan.EffectDWMSaveThumbnailEnabled)),
  new(Effect.CursorShadow,global::UnattendLocal.Localization.UiText.Source("text.de9b112839fc"),global::UnattendLocal.Localization.UiText.Source("text.f832e6fc2ade"),nameof(InstallPlan.EffectCursorShadow)),
  new(Effect.ListviewShadow,global::UnattendLocal.Localization.UiText.Source("text.c41e16074b50"),global::UnattendLocal.Localization.UiText.Source("text.2f2381b489e4"),nameof(InstallPlan.EffectListviewShadow)),
  new(Effect.ThumbnailsOrIcon,global::UnattendLocal.Localization.UiText.Source("text.7e4e4de345b4"),global::UnattendLocal.Localization.UiText.Source("text.826b60317017"),nameof(InstallPlan.EffectThumbnailsOrIcon)),
  new(Effect.ListviewAlphaSelect,global::UnattendLocal.Localization.UiText.Source("text.9c884df9911d"),global::UnattendLocal.Localization.UiText.Source("text.73b7dab60528"),nameof(InstallPlan.EffectListviewAlphaSelect)),
  new(Effect.DragFullWindows,global::UnattendLocal.Localization.UiText.Source("text.e81867904b6c"),global::UnattendLocal.Localization.UiText.Source("text.e39b9303c352"),nameof(InstallPlan.EffectDragFullWindows)),
  new(Effect.ComboBoxAnimation,global::UnattendLocal.Localization.UiText.Source("text.d952a2942dfd"),global::UnattendLocal.Localization.UiText.Source("text.134564f9b868"),nameof(InstallPlan.EffectComboBoxAnimation)),
  new(Effect.FontSmoothing,global::UnattendLocal.Localization.UiText.Source("text.880fb683944b"),global::UnattendLocal.Localization.UiText.Source("text.181182cc1add"),nameof(InstallPlan.EffectFontSmoothing)),
  new(Effect.ListBoxSmoothScrolling,global::UnattendLocal.Localization.UiText.Source("text.ddc2ca6509a9"),global::UnattendLocal.Localization.UiText.Source("text.f04e42b504b1"),nameof(InstallPlan.EffectListBoxSmoothScrolling)),
  new(Effect.DropShadow,global::UnattendLocal.Localization.UiText.Source("text.20018140ddbc"),global::UnattendLocal.Localization.UiText.Source("text.7dbfe4f128d9"),nameof(InstallPlan.EffectDropShadow))};
 public static bool Selected(InstallPlan p,Item item)=>(bool)typeof(InstallPlan).GetProperty(item.Property)!.GetValue(p)!;
 public static void SelectAll(InstallPlan p,bool value){foreach(var item in Items)typeof(InstallPlan).GetProperty(item.Property)!.SetValue(p,value);}
 public static IEffects Build(InstallPlan p)=>p.VisualEffectsMode switch {
  Default=>new DefaultEffects(),Appearance=>new BestAppearanceEffects(),Performance=>new BestPerformanceEffects(),
  Custom=>new CustomEffects(Items.ToImmutableDictionary(item=>item.Effect,item=>Selected(p,item))),
  _=>throw new ArgumentException(global::UnattendLocal.Localization.UiText.Source("text.aa26283859a5"))
 };
 public static string Selection(InstallPlan p){var names=Items.Where(item=>Selected(p,item)).Select(item=>item.Label).ToList();return names.Count==0?global::UnattendLocal.Localization.UiText.Source("text.34f7336d3983"):global::UnattendLocal.Localization.UiText.Join("、", names, false);}
 public static string ModeSummary(InstallPlan p)=>p.VisualEffectsMode switch {
  Custom=>global::UnattendLocal.Localization.UiText.Format("message.ab71419762e1", new string[] { global::UnattendLocal.Localization.UiText.Arg((Items.Count(item=>Selected(p,item))), 0, null), global::UnattendLocal.Localization.UiText.Arg((Items.Count), 0, null), global::UnattendLocal.Localization.UiText.Arg((Selection(p)), 0, null) }),
  Appearance=>global::UnattendLocal.Localization.UiText.Format("message.fe0bf92819e5", new string[] { global::UnattendLocal.Localization.UiText.Arg((Appearance), 0, null), global::UnattendLocal.Localization.UiText.Arg((Items.Count), 0, null) }),
  Performance=>global::UnattendLocal.Localization.UiText.Format("message.5a33907ad9cb", new string[] { global::UnattendLocal.Localization.UiText.Arg((Performance), 0, null), global::UnattendLocal.Localization.UiText.Arg((Items.Count), 0, null) }),
  Default=>global::UnattendLocal.Localization.UiText.Format("message.bee8e2ec18fb", new string[] { global::UnattendLocal.Localization.UiText.Arg((Default), 0, null) }),
  _=>global::UnattendLocal.Localization.UiText.Source("text.e4819894480e")
 };
 public static string ItemState(InstallPlan p,Item item)=>p.VisualEffectsMode switch {
  Custom=>Selected(p,item)?global::UnattendLocal.Localization.UiText.Source("text.8da97ddda990"):global::UnattendLocal.Localization.UiText.Source("text.3fd47edce45b"),Appearance=>global::UnattendLocal.Localization.UiText.Source("text.7ee7b662ad22"),Performance=>global::UnattendLocal.Localization.UiText.Source("text.9bbebf2ec167"),
  Default=>global::UnattendLocal.Localization.UiText.Source("text.499ff33c1078"),_=>global::UnattendLocal.Localization.UiText.Source("text.ad222cd5a4b3")
 };
 public static string Summary(InstallPlan p)=>global::UnattendLocal.Localization.UiText.Concat(new object?[] { global::UnattendLocal.Localization.UiText.Source("text.983a71826e98"), ModeSummary(p) },new bool[]{false,false});
}

