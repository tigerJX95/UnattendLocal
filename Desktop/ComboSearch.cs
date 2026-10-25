using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Threading;
using System.Windows.Media;
namespace UnattendLocal.Desktop;

public static class ComboSearch {
 public sealed record Option(object Item,string Label,string SearchText,object DisplayContent,DataTemplate? PresentationTemplate,object? Tip,bool Enabled);
 public static readonly DependencyProperty EnabledProperty=DependencyProperty.RegisterAttached("Enabled",typeof(bool),typeof(ComboSearch),new PropertyMetadata(false,EnabledChanged));
 public static void SetEnabled(DependencyObject d,bool value)=>d.SetValue(EnabledProperty,value);
 public static bool GetEnabled(DependencyObject d)=>(bool)d.GetValue(EnabledProperty);
 public static readonly DependencyProperty ResultsProperty=DependencyProperty.RegisterAttached("Results",typeof(IReadOnlyList<Option>),typeof(ComboSearch),new PropertyMetadata(Array.Empty<Option>()));
 public static IReadOnlyList<Option> GetResults(DependencyObject d)=>(IReadOnlyList<Option>)d.GetValue(ResultsProperty);
 public static readonly DependencyProperty NoMatchesProperty=DependencyProperty.RegisterAttached("NoMatches",typeof(bool),typeof(ComboSearch),new PropertyMetadata(false));
 public static bool GetNoMatches(DependencyObject d)=>(bool)d.GetValue(NoMatchesProperty);
 private static readonly DependencyProperty StateProperty=DependencyProperty.RegisterAttached("State",typeof(State),typeof(ComboSearch));
 private static void EnabledChanged(DependencyObject d,DependencyPropertyChangedEventArgs e){
  if(d is not ComboBox combo)return;
  if((bool)e.NewValue){combo.Loaded+=Loaded;combo.Unloaded+=Unloaded;if(combo.IsLoaded)Start(combo);}
  else{combo.Loaded-=Loaded;combo.Unloaded-=Unloaded;Stop(combo);}
 }
 private static void Loaded(object sender,RoutedEventArgs e)=>Start((ComboBox)sender);
 private static void Unloaded(object sender,RoutedEventArgs e)=>Stop((ComboBox)sender);
 private static void Start(ComboBox combo){Stop(combo);var state=new State(combo);combo.SetValue(StateProperty,state);state.Attach();}
 private static void Stop(ComboBox combo){if(combo.GetValue(StateProperty) is State state)state.Detach();combo.ClearValue(StateProperty);}

 private sealed class State(ComboBox combo){
  private TextBox? input;private ListBox? list;private bool updating,editing,composing,alive;
  private List<Option> options=[];
  private readonly DependencyPropertyDescriptor sourceDescriptor=DependencyPropertyDescriptor.FromProperty(ItemsControl.ItemsSourceProperty,typeof(ComboBox));
  private static readonly string InteractionHint=global::UnattendLocal.Localization.UiText.Source("text.e4c98e0a6fdf");
  private readonly DependencyPropertyDescriptor tipDescriptor=DependencyPropertyDescriptor.FromProperty(FrameworkElement.ToolTipProperty,typeof(ComboBox));
  private readonly DependencyPropertyDescriptor templateDescriptor=DependencyPropertyDescriptor.FromProperty(Control.TemplateProperty,typeof(ComboBox));
  public void Attach(){
   alive=true;combo.ApplyTemplate();ConnectParts();
   combo.SelectionChanged+=SelectionChanged;combo.DropDownOpened+=Opened;combo.DropDownClosed+=Closed;combo.PreviewKeyDown+=KeyDown;
   combo.PreviewMouseLeftButtonDown+=ComboMouse;combo.PreviewMouseWheel+=Wheel;combo.IsKeyboardFocusWithinChanged+=FocusChanged;combo.DataContextChanged+=ContextChanged;
   tipDescriptor.AddValueChanged(combo,TipChanged);sourceDescriptor.AddValueChanged(combo,SourceChanged);templateDescriptor.AddValueChanged(combo,TemplateChanged);
   UnattendLocal.Localization.UiText.LanguageChanged+=LanguageChanged;Reset();
  }
  private void LanguageChanged(object? sender,EventArgs e)=>Queue(Reset);
  public void Detach(){
   alive=false;UnattendLocal.Localization.UiText.LanguageChanged-=LanguageChanged;DisconnectParts();combo.SelectionChanged-=SelectionChanged;combo.DropDownOpened-=Opened;combo.DropDownClosed-=Closed;combo.PreviewKeyDown-=KeyDown;
   combo.PreviewMouseLeftButtonDown-=ComboMouse;combo.PreviewMouseWheel-=Wheel;combo.IsKeyboardFocusWithinChanged-=FocusChanged;combo.DataContextChanged-=ContextChanged;
   tipDescriptor.RemoveValueChanged(combo,TipChanged);sourceDescriptor.RemoveValueChanged(combo,SourceChanged);templateDescriptor.RemoveValueChanged(combo,TemplateChanged);
  }
  private void ConnectParts(){
   input=combo.Template.FindName("SearchInput",combo) as TextBox;list=combo.Template.FindName("SearchResults",combo) as ListBox;
   if(input is null||list is null)return;
   input.TextChanged+=TextChanged;input.GotKeyboardFocus+=InputFocus;
   TextCompositionManager.AddPreviewTextInputStartHandler(input,CompositionStart);
   TextCompositionManager.AddPreviewTextInputHandler(input,CompositionDone);
   list.PreviewMouseLeftButtonDown+=ResultMouse;list.SelectionChanged+=ResultHighlightChanged;
  }
  private void DisconnectParts(){
   if(input is not null){input.TextChanged-=TextChanged;input.GotKeyboardFocus-=InputFocus;TextCompositionManager.RemovePreviewTextInputStartHandler(input,CompositionStart);TextCompositionManager.RemovePreviewTextInputHandler(input,CompositionDone);}
   if(list is not null){list.PreviewMouseLeftButtonDown-=ResultMouse;list.SelectionChanged-=ResultHighlightChanged;}
   input=null;list=null;
  }
  private void TemplateChanged(object? sender,EventArgs e)=>Queue(()=>{DisconnectParts();combo.ApplyTemplate();ConnectParts();Reset();});
  private void TipChanged(object? sender,EventArgs e)=>UpdateTip();
  private void UpdateTip(){if(input is not null)input.ToolTip=combo.ToolTip is HelpTopic tip?tip with {Body=tip.Body+"\n\n"+InteractionHint,Parts=[..tip.Sections,new(InteractionHint)]}:new HelpTopic("combosearch",global::UnattendLocal.Localization.UiText.Source("text.fe323490f04c"),InteractionHint);}
  private void BeginEditing(){if(input is null||combo.IsReadOnly)return;editing=true;input.IsReadOnly=false;input.Focus();input.SelectAll();combo.SetCurrentValue(ComboBox.IsDropDownOpenProperty,true);}
  private static bool Inside(DependencyObject? element,DependencyObject? ancestor){while(element is not null){if(ReferenceEquals(element,ancestor))return true;element=element is Visual or System.Windows.Media.Media3D.Visual3D?VisualTreeHelper.GetParent(element):LogicalTreeHelper.GetParent(element);}return false;}
  private void ComboMouse(object sender,MouseButtonEventArgs e){
   if(input is null||e.OriginalSource is not DependencyObject origin||Inside(origin,list))return;
   var point=e.GetPosition(combo);if(point.X<0||point.Y<0||point.X>combo.ActualWidth||point.Y>combo.ActualHeight)return; // Let ComboBox process outside clicks while it captures the mouse.
   // The arrow keeps normal toggle behavior; all other display-area clicks open the full list.
   var arrow=combo.Template.FindName("DropDownToggle",combo) as ToggleButton;if(Inside(origin,arrow))return;
   if(editing&&!input.IsReadOnly)return; // Native caret/selection behavior once the search editor is active.
   if(e.ClickCount>=2&&!combo.IsReadOnly)BeginEditing();
   else{input.Focus();input.Select(0,0);combo.SetCurrentValue(ComboBox.IsDropDownOpenProperty,true);}
   e.Handled=true;
  }
  private void SourceChanged(object? sender,EventArgs e)=>Queue(Reset);
  private void ContextChanged(object sender,DependencyPropertyChangedEventArgs e)=>Queue(Reset);
  private void Queue(Action action)=>combo.Dispatcher.BeginInvoke(DispatcherPriority.DataBind,new Action(()=>{if(alive)action();}));
  private Option Describe(object item){
   var container=item as ComboBoxItem;var content=container?.Content??item;
   string label=Value(content,string.IsNullOrEmpty(combo.DisplayMemberPath)?LocalizedItems.GetDisplayPath(combo):combo.DisplayMemberPath);
   if(string.IsNullOrEmpty(label))label=Value(content,"Label");
   if(string.IsNullOrEmpty(label))label=content?.ToString()??"";
   string identifier=container?.Tag?.ToString()??Value(item,"Id");
   if(string.IsNullOrEmpty(identifier)&&combo.SelectedValuePath!="Content")identifier=Value(item,combo.SelectedValuePath);
   // Named models use the same label-only presentation as the search input.
   // Other selectors keep their existing templates (including color swatches).
   bool nameOnly=content is UnattendLocal.Core.ScriptTemplate;
   return new(item,label,SearchTextMatcher.SearchText(label,identifier),nameOnly||combo.ItemTemplate is null?label:content!,nameOnly?null:combo.ItemTemplate,container?.ToolTip??combo.ToolTip,container?.IsEnabled??true);
  }
  private static string Value(object? item,string? path){
   if(item is null||string.IsNullOrWhiteSpace(path))return "";
   object? value=item;foreach(var part in path.Split('.')){if(value is null)return "";value=TypeDescriptor.GetProperties(value)[part]?.GetValue(value);}
   return value?.ToString()??"";
  }
  private void Refresh(string query){
   options=combo.Items.Cast<object>().Select(Describe).ToList();
   var matches=options.Where(o=>SearchTextMatcher.Matches(o.SearchText,query)).ToArray();
   combo.SetValue(ResultsProperty,matches);combo.SetValue(NoMatchesProperty,matches.Length==0);
   if(list is not null){var preferred=editing?matches.FirstOrDefault(o=>o.Enabled):matches.FirstOrDefault(o=>ReferenceEquals(o.Item,combo.SelectedItem))??matches.FirstOrDefault(o=>o.Enabled);list.SelectedItem=preferred;if(preferred is not null)list.ScrollIntoView(preferred);}
  }
  private void Reset(){
   if(!alive||input is null)return;updating=true;
   try{editing=false;composing=false;input.IsReadOnly=true;UpdateTip();Refresh("");input.Text=UnattendLocal.Localization.UiText.Translate(options.FirstOrDefault(o=>ReferenceEquals(o.Item,combo.SelectedItem))?.Label??"");}
   finally{updating=false;}
  }
  private void TextChanged(object sender,TextChangedEventArgs e){if(updating||!alive||input is null||composing)return;editing=true;Refresh(input.Text);combo.SetCurrentValue(ComboBox.IsDropDownOpenProperty,true);}
  private void SelectionChanged(object sender,SelectionChangedEventArgs e){if(!updating&&ReferenceEquals(e.OriginalSource,combo))Reset();}
  private void Opened(object? sender,EventArgs e){
   if(input is null)return;if(!editing)Refresh("");
   // Focus stays in the text input so Chinese IME candidates are not replaced.
   if(!composing)Queue(()=>{input?.Focus();if(!editing)input?.Select(0,0);});
  }
  private void Closed(object? sender,EventArgs e){if(!composing)Reset();Queue(()=>{if(combo.IsKeyboardFocusWithin&&!composing)input?.Focus();});}
  private void FocusChanged(object sender,DependencyPropertyChangedEventArgs e){if(!(bool)e.NewValue)Queue(()=>{if(!combo.IsKeyboardFocusWithin&&!combo.IsDropDownOpen){Reset();}});}
  private void InputFocus(object sender,KeyboardFocusChangedEventArgs e){if(!editing&&!composing)input?.Select(0,0);}
  private void CompositionStart(object sender,TextCompositionEventArgs e){if(input is not null&&input.IsReadOnly)BeginEditing();composing=true;}
  private void CompositionDone(object sender,TextCompositionEventArgs e){bool wasComposing=composing;composing=false;if(!wasComposing)return;Queue(()=>{if(input is not null&&input.IsKeyboardFocusWithin){editing=true;Refresh(input.Text);combo.SetCurrentValue(ComboBox.IsDropDownOpenProperty,true);}});}
  private void Commit(Option? option){
   if(option is null||!option.Enabled)return;
   // Only a confirmed original item reaches SelectedValue; never assign the query.
   updating=true;try{combo.SetCurrentValue(Selector.SelectedItemProperty,option.Item);}finally{updating=false;}
   editing=false;combo.SetCurrentValue(ComboBox.IsDropDownOpenProperty,false);Reset();input?.Focus();input?.Select(0,0);
  }
  private static void ResultHighlightChanged(object sender,SelectionChangedEventArgs e)=>e.Handled=true;
  private void ResultMouse(object sender,MouseButtonEventArgs e){if(list is not null&&e.OriginalSource is DependencyObject d&&ItemsControl.ContainerFromElement(list,d) is ListBoxItem {DataContext:Option option}){Commit(option);e.Handled=true;}}
  private void Wheel(object sender,MouseWheelEventArgs e){
   if(combo.IsDropDownOpen)return;
   // Scrolling the settings page must not silently choose another option.
   e.Handled=true;var parent=VisualTreeHelper.GetParent(combo);
   while(parent is not null&&parent is not UIElement)parent=VisualTreeHelper.GetParent(parent);
   if(parent is UIElement element)element.RaiseEvent(new MouseWheelEventArgs(e.MouseDevice,e.Timestamp,e.Delta){RoutedEvent=Mouse.MouseWheelEvent,Source=element});
  }
  private void KeyDown(object sender,KeyEventArgs e){
   if(composing)return;var key=e.Key==Key.System?e.SystemKey:e.Key;
   if((key==Key.F4&&Keyboard.Modifiers==ModifierKeys.None)||(key==Key.Down&&Keyboard.Modifiers==ModifierKeys.Alt)){combo.SetCurrentValue(ComboBox.IsDropDownOpenProperty,!combo.IsDropDownOpen);e.Handled=true;return;}
   if(key==Key.Escape){combo.SetCurrentValue(ComboBox.IsDropDownOpenProperty,false);Reset();input?.Focus();input?.Select(0,0);e.Handled=true;return;}
   if(key==Key.Enter&&combo.IsDropDownOpen){Commit(list?.SelectedItem as Option);e.Handled=true;return;}
   if(key==Key.Tab){combo.SetCurrentValue(ComboBox.IsDropDownOpenProperty,false);Reset();input?.Focus();input?.MoveFocus(new TraversalRequest(Keyboard.Modifiers.HasFlag(ModifierKeys.Shift)?FocusNavigationDirection.Previous:FocusNavigationDirection.Next));e.Handled=true;return;}
   if(key==Key.Space&&input?.IsReadOnly==true){combo.SetCurrentValue(ComboBox.IsDropDownOpenProperty,true);e.Handled=true;return;}
   bool typing=key==Key.ImeProcessed||key is >=Key.A and <=Key.Z||key is >=Key.D0 and <=Key.D9||key is >=Key.NumPad0 and <=Key.Divide||key is >=Key.Oem1 and <=Key.OemBackslash||key is Key.Back or Key.Delete;
   if(input?.IsReadOnly==true&&!combo.IsReadOnly&&((typing&&(Keyboard.Modifiers&(ModifierKeys.Control|ModifierKeys.Alt))==ModifierKeys.None)||(key==Key.V&&Keyboard.Modifiers==ModifierKeys.Control)))BeginEditing();
   if(key is Key.Up or Key.Down){
    bool wasOpen=combo.IsDropDownOpen;combo.SetCurrentValue(ComboBox.IsDropDownOpenProperty,true);
    var entries=GetResults(combo).Where(o=>o.Enabled).ToList();if(entries.Count>0&&list is not null){int index=entries.IndexOf(list.SelectedItem as Option??entries[0]);if(wasOpen)index=Math.Clamp(index+(key==Key.Down?1:-1),0,entries.Count-1);list.SelectedItem=entries[Math.Max(index,0)];list.ScrollIntoView(list.SelectedItem);}
    e.Handled=true;
   }
  }
 }
}
