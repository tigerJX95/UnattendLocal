using System.Windows;
using System.Windows.Input;
using System.Windows.Controls;
namespace UnattendLocal.Desktop;
public partial class ColorPickerWindow : Window
{
    public ColorPickerModel Draft { get; }
    public string SelectedHex => Draft.ColorHex;
    public bool RestoreWindowsDefault => Draft.UseWindowsDefault;
    public ColorPickerWindow(string label,string initial) {
        Draft=new(initial);InitializeComponent();Title="选择"+label;PickerHeading.Text="选择"+label;DataContext=Draft;
        Draft.PropertyChanged+=(_,_)=>UpdateMarker();Loaded+=(_,_)=>UpdateMarker();SourceInitialized+=(_,_)=>UiTheme.UpdateTitleBar(this);
    }
    private void UpdateMarker() { if(ColorPlane==null)return;double x=Draft.Saturation*ColorPlane.ActualWidth,y=(1-Draft.Brightness)*ColorPlane.ActualHeight;Canvas.SetLeft(SelectionRing,x-7);Canvas.SetTop(SelectionRing,y-7);Canvas.SetLeft(SelectionOutline,x-9);Canvas.SetTop(SelectionOutline,y-9); }
    private void Select(MouseEventArgs e) { var p=e.GetPosition(ColorPlane);if(ColorPlane.ActualWidth>0&&ColorPlane.ActualHeight>0)Draft.SelectSaturationValue(p.X/ColorPlane.ActualWidth,1-p.Y/ColorPlane.ActualHeight); }
    private void PlaneDown(object s,MouseButtonEventArgs e) {ColorPlane.Focus();ColorPlane.CaptureMouse();Select(e);e.Handled=true;}
    private void PlaneMove(object s,MouseEventArgs e) {if(ColorPlane.IsMouseCaptured&&e.LeftButton==MouseButtonState.Pressed)Select(e);}
    private void PlaneUp(object s,MouseButtonEventArgs e) {if(ColorPlane.IsMouseCaptured){Select(e);ColorPlane.ReleaseMouseCapture();}}
    private void PlaneSizeChanged(object s,SizeChangedEventArgs e)=>UpdateMarker();
    private void PlaneKeyDown(object s,KeyEventArgs e) {double step=Keyboard.Modifiers.HasFlag(ModifierKeys.Shift)?0.1:0.01;double x=0,y=0;switch(e.Key){case Key.Left:x=-step;break;case Key.Right:x=step;break;case Key.Up:y=step;break;case Key.Down:y=-step;break;default:return;}Draft.SelectSaturationValue(Draft.Saturation+x,Draft.Brightness+y);e.Handled=true;}
    private void AcceptClick(object s,RoutedEventArgs e) {if(Draft.CanAccept)DialogResult=true;}
    private void DefaultClick(object s,RoutedEventArgs e) {Draft.UseWindowsDefault=true;}
}
