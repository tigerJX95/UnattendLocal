using System.Collections.Immutable;
using Schneegans.Unattend;
namespace UnattendLocal.Core;
public static class VisualEffectsSettings {
 public const string Default="使用 Windows 默认设置",Appearance="调整为最佳外观",Performance="调整为最佳性能",Custom="自定义视觉效果";
 public record Item(Effect Effect,string Label,string Description,string Property) {public string HelpId=>"effect"+Effect;}
 public static IReadOnlyList<Item> Items {get;}=new Item[]{
  new(Effect.ControlAnimations,"窗口内的控件与元素动画","显示窗口内部控件及元素的过渡动画。",nameof(InstallPlan.EffectControlAnimations)),
  new(Effect.AnimateMinMax,"最小化和最大化窗口时显示动画","显示窗口最小化、还原和最大化的过渡动画。",nameof(InstallPlan.EffectAnimateMinMax)),
  new(Effect.TaskbarAnimations,"任务栏动画","显示任务栏支持的动画效果。",nameof(InstallPlan.EffectTaskbarAnimations)),
  new(Effect.DWMAeroPeekEnabled,"启用桌面速览（Peek）","启用目标 Windows 版本支持的桌面速览效果。",nameof(InstallPlan.EffectDWMAeroPeekEnabled)),
  new(Effect.MenuAnimation,"菜单淡入或滑动显示","使菜单在出现时淡入或滑动。",nameof(InstallPlan.EffectMenuAnimation)),
  new(Effect.TooltipAnimation,"工具提示淡入或滑动显示","使目标系统的工具提示淡入或滑动，不控制本工具的提示。",nameof(InstallPlan.EffectTooltipAnimation)),
  new(Effect.SelectionFade,"单击后淡出菜单项","使已单击的菜单项以淡出效果消失。",nameof(InstallPlan.EffectSelectionFade)),
  new(Effect.DWMSaveThumbnailEnabled,"保存任务栏缩略图预览","允许保存任务栏窗口缩略图预览。",nameof(InstallPlan.EffectDWMSaveThumbnailEnabled)),
  new(Effect.CursorShadow,"鼠标指针下显示阴影","显示鼠标指针下的阴影。",nameof(InstallPlan.EffectCursorShadow)),
  new(Effect.ListviewShadow,"桌面图标文字显示阴影","显示桌面图标标签文字下的阴影。",nameof(InstallPlan.EffectListviewShadow)),
  new(Effect.ThumbnailsOrIcon,"显示缩略图而非图标","显示图片等文件的缩略图；关闭后以图标显示，不影响文件内容。",nameof(InstallPlan.EffectThumbnailsOrIcon)),
  new(Effect.ListviewAlphaSelect,"显示半透明选择框","拖动选择多个项目时显示半透明选择矩形。",nameof(InstallPlan.EffectListviewAlphaSelect)),
  new(Effect.DragFullWindows,"拖动时显示窗口内容","拖动窗口时显示完整内容；关闭后可能仅显示窗口轮廓。",nameof(InstallPlan.EffectDragFullWindows)),
  new(Effect.ComboBoxAnimation,"下拉框滑动展开","使下拉选择框以滑动效果展开。",nameof(InstallPlan.EffectComboBoxAnimation)),
  new(Effect.FontSmoothing,"平滑屏幕字体边缘","启用屏幕字体边缘平滑。关闭后文字边缘可能显得粗糙；此项不单独配置 ClearType 参数。",nameof(InstallPlan.EffectFontSmoothing)),
  new(Effect.ListBoxSmoothScrolling,"列表框平滑滚动","使支持的列表框以平滑效果滚动。",nameof(InstallPlan.EffectListBoxSmoothScrolling)),
  new(Effect.DropShadow,"窗口下显示阴影","显示窗口下的阴影。",nameof(InstallPlan.EffectDropShadow))};
 public static bool Selected(InstallPlan p,Item item)=>(bool)typeof(InstallPlan).GetProperty(item.Property)!.GetValue(p)!;
 public static void SelectAll(InstallPlan p,bool value){foreach(var item in Items)typeof(InstallPlan).GetProperty(item.Property)!.SetValue(p,value);}
 public static IEffects Build(InstallPlan p)=>p.VisualEffectsMode switch {
  Default=>new DefaultEffects(),Appearance=>new BestAppearanceEffects(),Performance=>new BestPerformanceEffects(),
  Custom=>new CustomEffects(Items.ToImmutableDictionary(item=>item.Effect,item=>Selected(p,item))),
  _=>throw new ArgumentException("请选择有效的视觉效果设置方式。")
 };
 public static string Selection(InstallPlan p){var names=Items.Where(item=>Selected(p,item)).Select(item=>item.Label).ToList();return names.Count==0?"全部关闭":string.Join("、",names);}
 public static string ModeSummary(InstallPlan p)=>p.VisualEffectsMode switch {
  Custom=>$"自定义：启用 {Items.Count(item=>Selected(p,item))} / {Items.Count} 项；{Selection(p)}",
  Appearance=>$"{Appearance}（本组 {Items.Count} 项全部开启）",
  Performance=>$"{Performance}（本组 {Items.Count} 项全部关闭）",
  Default=>$"{Default}（由目标系统选择，不写入覆盖）",
  _=>"视觉效果模式无效，请重新选择"
 };
 public static string ItemState(InstallPlan p,Item item)=>p.VisualEffectsMode switch {
  Custom=>Selected(p,item)?"开启":"关闭",Appearance=>"开启（最佳外观）",Performance=>"关闭（最佳性能）",
  Default=>"由 Windows 决定",_=>"模式无效"
 };
 public static string Summary(InstallPlan p)=>"视觉效果："+ModeSummary(p);
}

