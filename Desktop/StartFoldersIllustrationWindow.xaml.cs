using System.Windows;
namespace UnattendLocal.Desktop;
public partial class StartFoldersIllustrationWindow : Window
{
    public StartFoldersIllustrationWindow() { InitializeComponent();SourceInitialized+=(_,_)=>UiTheme.UpdateTitleBar(this); }
}
