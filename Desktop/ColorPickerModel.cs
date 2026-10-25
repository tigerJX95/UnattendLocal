using System.Globalization;
using UnattendLocal.Core;
namespace UnattendLocal.Desktop;

// An isolated draft: cancelling a dialog never writes to an InstallPlan.
public sealed class ColorPickerModel : Observable
{
    private int red, green, blue;
    private double hue, saturation, brightness;
    private string hex = "#0078D4";
    private bool useWindowsDefault;
    public bool UseWindowsDefault { get=>useWindowsDefault; set { useWindowsDefault=value; Refresh(); } }
    public bool CanAccept => UseWindowsDefault || IsValid;
    public ColorPickerModel(string initial) { Hex = ColorSettings.TryNormalize(initial, out var valid) ? valid : "#0078D4"; }
    public string Hex { get => hex; set {
        useWindowsDefault=false; hex = value ?? "";
        if (ColorSettings.TryNormalize(hex, out var normalized)) {
            hex = normalized; int rgb = int.Parse(hex.AsSpan(1), NumberStyles.HexNumber, CultureInfo.InvariantCulture);
            red = rgb >> 16; green = rgb >> 8 & 255; blue = rgb & 255;
            (hue, saturation, brightness) = ToHsv(red, green, blue, hue);
        }
        Refresh();
    } }
    public string ColorHex => $"#{red:X2}{green:X2}{blue:X2}";
    public string HueHex { get { var (r,g,b)=FromHsv(hue,1,1); return $"#{r:X2}{g:X2}{b:X2}"; } }
    public bool IsValid => ColorSettings.TryNormalize(hex, out _);
    public string ValidationMessage => UseWindowsDefault ? global::UnattendLocal.Localization.UiText.Source("text.a8db5364e534") : IsValid ? global::UnattendLocal.Localization.UiText.Source("text.e81fc282cfdf") : global::UnattendLocal.Localization.UiText.Source("text.8b18c272cf7b");
    public int Red { get => red; set { if (red == value) return; red = Math.Clamp(value,0,255); FromRgb(); } }
    public int Green { get => green; set { if (green == value) return; green = Math.Clamp(value,0,255); FromRgb(); } }
    public int Blue { get => blue; set { if (blue == value) return; blue = Math.Clamp(value,0,255); FromRgb(); } }
    public double Hue { get => hue; set { if (!double.IsFinite(value)) return; hue = Math.Clamp(value,0,360); FromSelection(); } }
    public double Saturation => saturation;
    public double Brightness => brightness;
    public void SelectSaturationValue(double s, double v) { if (!double.IsFinite(s) || !double.IsFinite(v)) return; saturation = Math.Clamp(s,0,1); brightness = Math.Clamp(v,0,1); FromSelection(); }
    // Selecting the spectrum chooses its visible, fully saturated color;
    // the two-dimensional plane can then refine saturation and brightness.
    public void SelectHuePosition(double position) {
        if (!double.IsFinite(position)) return;
        hue = Math.Clamp(position,0,1)*360;
        saturation = brightness = 1;
        FromSelection();
    }
    private void FromSelection() { useWindowsDefault=false; (red,green,blue) = FromHsv(hue,saturation,brightness); hex = ColorHex; Refresh(); }
    private void FromRgb() { useWindowsDefault=false; (hue,saturation,brightness) = ToHsv(red,green,blue,hue); hex = ColorHex; Refresh(); }
    private void Refresh() { foreach (string name in new[]{nameof(Hex),nameof(ColorHex),nameof(HueHex),nameof(IsValid),nameof(UseWindowsDefault),nameof(CanAccept),nameof(ValidationMessage),nameof(Red),nameof(Green),nameof(Blue),nameof(Hue),nameof(Saturation),nameof(Brightness)}) Notify(name); }
    public static (int R,int G,int B) FromHsv(double h,double s,double v) {
        double c=v*s, x=c*(1-Math.Abs((h%360)/60%2-1)), m=v-c;
        var (r,g,b)=(int)((h%360)/60) switch {0=>(c,x,0d),1=>(x,c,0d),2=>(0d,c,x),3=>(0d,x,c),4=>(x,0d,c),_=>(c,0d,x)};
        int Byte(double n)=>(int)Math.Clamp(Math.Round((n+m)*255,MidpointRounding.AwayFromZero),0,255);
        return (Byte(r),Byte(g),Byte(b));
    }
    private static (double H,double S,double V) ToHsv(int r,int g,int b,double previousHue) {
        double rd=r/255d, gd=g/255d, bd=b/255d, max=Math.Max(rd,Math.Max(gd,bd)), min=Math.Min(rd,Math.Min(gd,bd)), diff=max-min;
        double h=diff==0?previousHue:max==rd?60*((gd-bd)/diff%6):max==gd?60*((bd-rd)/diff+2):60*((rd-gd)/diff+4);
        if(h<0)h+=360;
        return (h,max==0?0:diff/max,max);
    }
}
