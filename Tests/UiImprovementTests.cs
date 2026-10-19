using System.Text;
using UnattendLocal.Core;
using UnattendLocal.Desktop;

static class UiImprovementTests
{
    public static int Run(GeneratorService service) {
        int count=0;void Check(bool ok,string label){if(!ok)throw new Exception(label);count++;Console.WriteLine("PASS V1.8 "+label);}
        var initial=InstallPlan.New();initial.Users[0].Password="UiTest1!";
        foreach (var (hex,r,g,b) in new[]{("#FF0000",255,0,0),("#00FF00",0,255,0),("#0000FF",0,0,255),("#FFFFFF",255,255,255),("#000000",0,0,0),("#808080",128,128,128),("#0078D4",0,120,212)}) {
            var model=new ColorPickerModel(hex);Check(model.IsValid&&model.Red==r&&model.Green==g&&model.Blue==b&&model.ColorHex==hex,"known HEX to RGB "+hex);
        }
        foreach (int r in new[]{0,1,17,64,127,128,200,254,255}) foreach (int g in new[]{0,1,17,64,127,128,200,254,255}) foreach (int b in new[]{0,1,17,64,127,128,200,254,255}) {
            var model=new ColorPickerModel($"#{r:X2}{g:X2}{b:X2}");
            var rgb=ColorPickerModel.FromHsv(model.Hue,model.Saturation,model.Brightness);
            Check(rgb==(r,g,b),"arbitrary RGB survives HSV selection roundtrip "+model.ColorHex);
        }
        var draft=new ColorPickerModel("#102030");draft.Red=255;draft.Green=128;draft.Blue=0;
        Check(draft.Hex=="#FF8000"&&draft.IsValid,"RGB edits update canonical HEX");
        draft.Hex=" #abcdef ";Check(draft.ColorHex=="#ABCDEF"&&draft.Red==171&&draft.Green==205&&draft.Blue==239,"HEX edits update RGB and normalized field");
        foreach(string value in new[]{"", "red", "#ABC", "#12345G", "#80123456", "#123456;"}){string before=draft.ColorHex;draft.Hex=value;Check(!draft.IsValid&&draft.ColorHex==before,"invalid HEX preserves last preview and disables acceptance "+value);}
        draft.Hue=120;draft.SelectSaturationValue(1,1);Check(draft.ColorHex=="#00FF00"&&draft.IsValid,"visual selection repairs invalid draft without touching plan");
        draft.SelectSaturationValue(5,-1);Check(draft.Saturation==1&&draft.Brightness==0&&draft.ColorHex=="#000000","drag outside color plane clamps safely");
        draft.SelectSaturationValue(0,1);Check(draft.ColorHex=="#FFFFFF","white corner of color plane");
        draft.Hex="bad";draft.UseWindowsDefault=true;Check(draft.CanAccept&&draft.UseWindowsDefault,"default is a pending draft choice even with invalid custom HEX");
        draft.Hex="#123456";Check(!draft.UseWindowsDefault&&draft.CanAccept,"custom color edit leaves default draft mode");
        string keep=draft.ColorHex;draft.Hue=double.NaN;draft.SelectSaturationValue(double.NaN,1);Check(draft.ColorHex==keep,"nonfinite input does not corrupt selection");
        string saved=initial.AccentColorHex;var detached=new ColorPickerModel(saved);detached.Hex="#FF0000";
        Check(initial.AccentColorHex==saved,"cancelled isolated draft does not mutate original InstallPlan");
        var colors=InstallPlan.New();colors.Users[0].Password="UiTest1!";colors.ColorSettingsMode=ColorSettings.Custom;colors.AccentColorHex="#A1B2C3";colors.WallpaperMode=WallpaperSettings.Solid;colors.WallpaperColorHex="#243648";
        var copy=PlanStore.Deserialize(PlanStore.Serialize(colors,true));Check(copy.AccentColorHex==colors.AccentColorHex&&copy.WallpaperColorHex==colors.WallpaperColorHex,"selected colors retain existing JSON format");
        string xml=Encoding.UTF8.GetString(service.Generate(colors));var imported=XmlPlanImport.Import(xml,service);
        Check(imported.AccentColorHex==colors.AccentColorHex&&imported.WallpaperColorHex==colors.WallpaperColorHex,"selected colors retain existing XML import and generator behavior");
        colors.ColorSettingsMode=ColorSettings.Default;colors.WallpaperMode=WallpaperSettings.Default;
        Check(colors.AccentColorHex=="#A1B2C3"&&colors.WallpaperColorHex=="#243648"&&!ColorSettings.IsCustom(colors)&&!WallpaperSettings.IsSolid(colors),"Windows default stops overrides and retains stored color values");
        foreach(string layout in new[]{"UEFI / GPT","BIOS / MBR"}) {
            var p=PlanStore.Deserialize(PlanStore.Serialize(initial,true));p.DiskMode=InstallationSettings.Wipe;p.TargetDiskId=2;p.DiskLayout=layout;p.ConfirmWipeDisk=true;
            byte[] bytes=service.Generate(p);var target=InstallationReview.FromXml(XmlMerge.Read(Encoding.UTF8.GetString(bytes)));
            Check(!target.MayNeedSelection&&target.Description.Contains("已指定安装目标：磁盘 2")&&target.Description.Contains("自动分区"),"automatic partitioning and actual target disclosed "+layout);
            var restored=XmlPlanImport.Import(Encoding.UTF8.GetString(bytes),service);Check(!InstallationReview.ForPlan(restored).MayNeedSelection,"recognized auto-partition import does not claim manual selection "+layout);
            restored.DiskMode=InstallationSettings.Manual;Check(InstallationReview.ForPlan(restored).MayNeedSelection,"cancelling owned imported target restores possible-selection notice "+layout);
        }
        var existing=PlanStore.Deserialize(PlanStore.Serialize(initial,true));existing.DiskMode=InstallationSettings.Existing;existing.TargetDiskId=1;existing.TargetPartitionId=4;
        var existingXml=service.Generate(existing);var existingTarget=InstallationReview.FromXml(XmlMerge.Read(Encoding.UTF8.GetString(existingXml)));
        Check(!existingTarget.MayNeedSelection&&existingTarget.Description.Contains("磁盘 1 / 分区 4")&&!existingTarget.Description.Contains("配置含自动分区操作"),"existing target distinguished from automatic partitioning");
        Check(InstallationReview.ForPlan(initial).MayNeedSelection&&InstallationSettings.DiskSummary(initial).Contains("可能需要"),"unspecified target uses possible rather than mandatory interaction");
        string Wrap(string body)=>"<unattend xmlns='"+XmlMerge.Namespace+"'><settings pass='windowsPE'><component name='Microsoft-Windows-Setup' processorArchitecture='amd64' publicKeyToken='31bf3856ad364e35' language='neutral'><ImageInstall><OSImage>"+body+"</OSImage></ImageInstall></component></settings></unattend>";
        foreach(string truth in new[]{"true","1"}) {
            string source=Wrap("<InstallToAvailablePartition>"+truth+"</InstallToAvailablePartition>");var target=InstallationReview.FromXml(XmlMerge.Read(source));
            Check(!target.MayNeedSelection&&target.Description.Contains("自动选择可用分区"),"native automatic target recognized "+truth);
            var p=new InstallPlan{ImportedXml=new(){SourceXml=source,BaselineXml=Wrap(""),OriginalOmitted=false}};
            Check(!InstallationReview.ForPlan(p).MayNeedSelection&&!Recommendations.InteractiveSteps(p).Contains("磁盘与分区"),"imported raw target overrides misleading manual UI default "+truth);
        }
        foreach(string body in new[]{"<InstallTo><DiskID>0</DiskID></InstallTo>","<InstallTo><DiskID>-1</DiskID><PartitionID>1</PartitionID></InstallTo>","<InstallTo><DiskID>0</DiskID><PartitionID>0</PartitionID></InstallTo>","<InstallToAvailablePartition>yes</InstallToAvailablePartition>","<InstallToAvailablePartition>true</InstallToAvailablePartition><InstallTo><DiskID>0</DiskID><PartitionID>1</PartitionID></InstallTo>"})
            Check(InstallationReview.FromXml(XmlMerge.Read(Wrap(body))).MayNeedSelection,"incomplete or conflicting target never promises automatic installation");
        string explicitSource=Wrap("<InstallTo><DiskID>3</DiskID><PartitionID>2</PartitionID></InstallTo>");var raw=new InstallPlan{ImportedXml=new(){SourceXml=explicitSource,BaselineXml=Wrap(""),OriginalOmitted=false}};
        Check(InstallationReview.ForPlan(raw).Description.Contains("磁盘 3 / 分区 2")&&!InstallationReview.ForPlan(raw).MayNeedSelection,"imported explicit target is not confused with manual editor default");
        var cached=InstallationReview.ForPlan(raw);Check(ReferenceEquals(cached,InstallationReview.ForPlan(raw)),"repeated summary reuses parsed target until source changes");
        raw.ImportedXml!.SourceXml=Wrap("<InstallToAvailablePartition>true</InstallToAvailablePartition>");Check(InstallationReview.ForPlan(raw).Description.Contains("自动选择可用分区"),"changed imported source invalidates target review cache");
        raw.ImportedXml.BaselineXml=explicitSource;Check(InstallationReview.ForPlan(raw).MayNeedSelection,"changed editable baseline invalidates target review cache");
        foreach(string term in new[]{"调色板","RGB","位置示意"})Check(SettingsSearchCatalog.Search(term).Count>0,"new help entries searchable "+term);
        foreach(string target in new[]{"AccentColorPickerButton","WallpaperColorPickerButton","StartFoldersIllustrationButton"})
            Check(SettingsSearchCatalog.Search(target=="StartFoldersIllustrationButton"?"位置示意":"调色板").Count(r=>r.Target==target)==1,"new search destination appears once "+target);
        return count;
    }
}
