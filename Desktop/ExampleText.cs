using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
namespace UnattendLocal.Desktop;
// Example text remains separate from the editor's Text binding and saved configuration.
public static class ExampleText {
 public static readonly DependencyProperty ValueProperty=DependencyProperty.RegisterAttached("Value",typeof(string),typeof(ExampleText),new PropertyMetadata("",(_,_)=>CommandManager.InvalidateRequerySuggested()));
 public static void SetValue(DependencyObject target,string value)=>target.SetValue(ValueProperty,value);
 public static string GetValue(DependencyObject target)=>(string)target.GetValue(ValueProperty);
 public static readonly DependencyProperty CopyStatusProperty=DependencyProperty.RegisterAttached("CopyStatus",typeof(string),typeof(ExampleText),new PropertyMetadata(""));
 public static string GetCopyStatus(DependencyObject target)=>(string)target.GetValue(CopyStatusProperty);
 public static readonly DependencyProperty HasCopyErrorProperty=DependencyProperty.RegisterAttached("HasCopyError",typeof(bool),typeof(ExampleText),new PropertyMetadata(false));
 public static bool GetHasCopyError(DependencyObject target)=>(bool)target.GetValue(HasCopyErrorProperty);
 public static void SetHasCopyError(DependencyObject target,bool value)=>target.SetValue(HasCopyErrorProperty,value);

 public static ICommand CopyCommand {get;}=new CopyExampleCommand();
 private static readonly ConditionalWeakTable<TextBox,Feedback> feedback=new();
 private sealed class Feedback {
  private readonly TextBox editor;private readonly DispatcherTimer timer;
  public Feedback(TextBox editor){this.editor=editor;timer=new DispatcherTimer(DispatcherPriority.Background,editor.Dispatcher){Interval=TimeSpan.FromSeconds(3)};timer.Tick+=(_,_)=>Clear();editor.Unloaded+=(_,_)=>Clear();}
  public void Show(string message,bool error=false){timer.Stop();editor.SetValue(CopyStatusProperty,message);SetHasCopyError(editor,error);timer.Start();}
  private void Clear(){timer.Stop();editor.SetValue(CopyStatusProperty,"");SetHasCopyError(editor,false);}
 }
 private sealed class CopyExampleCommand:ICommand {
  public event EventHandler? CanExecuteChanged {add=>CommandManager.RequerySuggested+=value;remove=>CommandManager.RequerySuggested-=value;}
  public bool CanExecute(object? parameter)=>parameter is TextBox editor&&!string.IsNullOrEmpty(GetValue(editor));
  public void Execute(object? parameter){
   if(parameter is not TextBox editor||!CanExecute(editor))return;
   try{Clipboard.SetText(GetValue(editor));feedback.GetValue(editor,e=>new Feedback(e)).Show(global::UnattendLocal.Localization.UiText.Source("text.8f6f8d979c98"));}
   catch(Exception ex) when(ex is System.Runtime.InteropServices.ExternalException or System.Threading.ThreadStateException){feedback.GetValue(editor,e=>new Feedback(e)).Show(global::UnattendLocal.Localization.UiText.Source("text.7bf5dea678e3"),true);}
  }
 }
}
