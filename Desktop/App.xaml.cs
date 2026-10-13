using System.Windows;
namespace UnattendLocal.Desktop;
public partial class App : Application
{
 protected override void OnStartup(StartupEventArgs e){UiTheme.Apply(UiPreferences.Load(),false);base.OnStartup(e);}
}
