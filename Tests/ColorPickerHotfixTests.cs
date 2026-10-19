using UnattendLocal.Desktop;

static class ColorPickerHotfixTests
{
    public static int Run()
    {
        int count=0;
        void Check(bool ok,string label) { if(!ok)throw new Exception(label);count++;Console.WriteLine("PASS V1.8.1 "+label); }
        // A dark, desaturated initial color must not make spectrum clicks appear inert.
        foreach(var (position,expected) in new[]{(0d,"#FF0000"),(1d/6,"#FFFF00"),(2d/6,"#00FF00"),(0.5,"#00FFFF"),(4d/6,"#0000FF"),(5d/6,"#FF00FF"),(1d,"#FF0000")}) {
            var model=new ColorPickerModel("#000000");
            model.SelectHuePosition(position);
            Check(model.ColorHex==expected&&model.Hex==expected&&model.HueHex==expected&&model.Saturation==1&&model.Brightness==1&&model.CanAccept,"spectrum selects displayed color from black "+expected);
        }
        var draft=new ColorPickerModel("#808080");
        draft.SelectHuePosition(0.5);
        Check(draft.Red==0&&draft.Green==255&&draft.Blue==255,"spectrum selection updates RGB from gray");
        draft.SelectSaturationValue(0.5,0.5);
        Check(draft.ColorHex=="#408080","plane can refine the selected hue afterward");
        draft.SelectHuePosition(-2);
        Check(draft.Hue==0&&draft.ColorHex=="#FF0000","drag beyond left edge clamps to red");
        draft.SelectHuePosition(2);
        Check(draft.Hue==360&&draft.ColorHex=="#FF0000","drag beyond right edge retains right marker endpoint");
        foreach(double invalid in new[]{double.NaN,double.PositiveInfinity,double.NegativeInfinity}) {
            double hue=draft.Hue;string hex=draft.Hex;
            draft.SelectHuePosition(invalid);
            Check(draft.Hue==hue&&draft.Hex==hex,"invalid pointer position leaves draft unchanged "+invalid);
        }
        draft.Hex="invalid";draft.UseWindowsDefault=true;draft.SelectHuePosition(1d/3);
        Check(draft.IsValid&&draft.CanAccept&&!draft.UseWindowsDefault&&draft.ColorHex=="#00FF00","spectrum repairs invalid HEX and exits pending default");
        draft.Hex="#289565";
        Check(draft.Red==40&&draft.Green==149&&draft.Blue==101&&draft.Saturation<1&&draft.Brightness<1,"HEX retains shade and saturation without spectrum reselection");
        draft.Red=50;
        Check(draft.ColorHex=="#329565"&&draft.Saturation<1&&draft.Brightness<1,"RGB does not trigger pure spectrum color selection");
        var changes=new HashSet<string>();draft.PropertyChanged+=(_,e)=>changes.Add(e.PropertyName??"");
        draft.SelectHuePosition(2d/3);
        Check(new[]{"Hue","HueHex","ColorHex","Hex","Red","Green","Blue","Saturation","Brightness","CanAccept"}.All(changes.Contains),"spectrum notifies preview, plane, marker and editor bindings");
        var vm=new MainViewModel();
        Check(vm.FilteredAppRemovalChoices.Count()==60&&vm.FilteredAppRemovalChoices.All(c=>!string.IsNullOrWhiteSpace(c.Description)),"all removal items have actual description text for tooltips");
        return count;
    }
}
