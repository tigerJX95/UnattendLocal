using System.Windows;
using System.Windows.Markup;
using UnattendLocal.Localization;
namespace UnattendLocal.Desktop;
public partial class App : Application
{
 internal static string? LanguageWarning { get; private set; }
 protected override void OnStartup(StartupEventArgs e)
 {
  var directory=System.IO.Path.Combine(System.AppContext.BaseDirectory,"Languages");
  var manifest=System.IO.Path.Combine(directory,"languages.json");
  if(System.IO.File.Exists(manifest)&&!UiText.LoadLanguageManifest(manifest,out _))LanguageWarning="language.manifestRejected";
  foreach(var item in UiText.Languages){var pack=System.IO.Path.Combine(directory,item.Code+".json");if(System.IO.File.Exists(pack)&&!UiText.LoadExternalPack(item.Code,pack,out _))LanguageWarning="language.packRejected";}
  UiText.SetLanguage(UiPreferences.LoadLanguage());
  EventManager.RegisterClassHandler(typeof(Window),FrameworkElement.LoadedEvent,new RoutedEventHandler((sender,_)=>{if(sender is Window window)window.Language=XmlLanguage.GetLanguage(UiText.Language);}));
  UiTheme.Apply(UiPreferences.Load(),false);base.OnStartup(e);
 }
}
