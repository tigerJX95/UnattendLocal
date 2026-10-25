using System.Text;
using System.Text.Json.Nodes;
using Schneegans.Unattend;
using UnattendLocal.Core;
using UnattendLocal.Desktop;

static class DeploymentTuningTests
{
    public static int Run(GeneratorService generator)
    {
        int count=0;
        void Check(bool condition,string name){if(!condition)throw new Exception("V1.21: "+name);count++;}
        string Xml(InstallPlan p)=>Encoding.UTF8.GetString(generator.Generate(p));
        foreach(var arch in new[]{"amd64","arm64","x86"})
        {
            var p=QuickPresets.Create(QuickPresets.Minimal);p.Architecture=arch;
            Check(!p.DisableNewNtfsShortNames&&!p.UseCustomPageFile,"opt-in defaults");
            var none=Xml(p);
            Check(!none.Contains("NtfsDisable8dot3NameCreation")&&!none.Contains("Win32_PageFileSetting"),"default no tuning scripts");
            p.DisableNewNtfsShortNames=true;p.UseCustomPageFile=true;p.PageFileInitialMiB=4096;p.PageFileMaximumMiB=8192;
            var scripts=DeploymentTuningSettings.Build(p).Scripts.ToArray();
            Check(scripts.Length==2&&scripts.All(s=>s.Phase==ScriptPhase.System),"system phase independent of automatic logon");
            Check(!scripts.Any(s=>s.Content.Contains("strip")||s.Content.Contains("Remove-CimInstance")),"no stripping names or removing other page files");
            Check(scripts.Any(s=>s.Content.Contains("C:\\pagefile.sys")&&s.Content.Contains("[uint32]8192")),"single Windows path separator and typed size");
            var json=PlanStore.Deserialize(PlanStore.Serialize(p,true));
            Check(json.DisableNewNtfsShortNames&&json.UseCustomPageFile&&json.PageFileInitialMiB==4096&&json.PageFileMaximumMiB==8192,"JSON persistence");
            var xml=Xml(p);var restored=XmlPlanImport.Import(xml,generator);
            Check(restored.DisableNewNtfsShortNames&&restored.UseCustomPageFile&&restored.PageFileMaximumMiB==8192,"own XML restoration");
            Check(restored.ImportedXml!.Blockers.Length==0,"own XML editable");
            Check(Xml(restored).Contains("Win32_PageFileSetting"),"restored XML preserves scripts");
            restored.UseCustomPageFile=false;restored.DisableNewNtfsShortNames=false;
            var disabled=Xml(restored);
            Check(!disabled.Contains("Win32_PageFileSetting")&&!disabled.Contains("NtfsDisable8dot3NameCreation"),"removal after own XML import");
        }
        foreach(var pair in new[]{(15,4096),(4096,2048),(16,1048577),(int.MinValue,int.MaxValue)})
        {
            var p=QuickPresets.Create(QuickPresets.Minimal);p.UseCustomPageFile=true;p.PageFileInitialMiB=pair.Item1;p.PageFileMaximumMiB=pair.Item2;
            try{generator.Generate(p);throw new Exception("invalid size accepted");}
            catch(ConfigurationValidationException e){Check(ConfigurationErrorLocation.FromException(e)!=null,"invalid size has UI location");}
            p.UseCustomPageFile=false;generator.Generate(p);Check(true,"inactive numeric values ignored");
        }
        foreach(var pair in new[]{(16,16),(1048576,1048576),(2048,4096)})
        {
            var p=QuickPresets.Create(QuickPresets.Minimal);p.UseCustomPageFile=true;p.PageFileInitialMiB=pair.Item1;p.PageFileMaximumMiB=pair.Item2;
            generator.Generate(p);Check(true,"valid size bounds");
        }
        var old=JsonNode.Parse(PlanStore.Serialize(QuickPresets.Create(QuickPresets.Minimal),true))!;
        foreach(var key in new[]{"DisableNewNtfsShortNames","UseCustomPageFile","PageFileInitialMiB","PageFileMaximumMiB"})old.AsObject().Remove(key);
        var migrated=PlanStore.Deserialize(old.ToJsonString());
        Check(!migrated.UseCustomPageFile&&!migrated.DisableNewNtfsShortNames&&migrated.PageFileInitialMiB==2048,"old JSON keeps safe defaults");
        var custom=QuickPresets.Create(QuickPresets.Minimal);custom.DisableNewNtfsShortNames=true;
        custom.Scripts.Add(new ScriptEntry{Enabled=true,Phase="System",Type="Ps1",Content="Write-Output 'V121-preserved'"});
        Check(Xml(custom).Contains("V121-preserved"),"custom scripts preserved");
        foreach(var target in new[]{"NtfsShortNamesEditor","CustomPageFileEditor","PageFileInitialEditor","PageFileMaximumEditor"})
            Check(SettingsSearchCatalog.All.Any(s=>s.Target==target),"search and numeric validation anchor");
        var summary=DeploymentTuningSettings.Summary(custom);
        Check(!System.Text.RegularExpressions.Regex.IsMatch(UnattendLocal.Localization.UiText.EnglishText(summary), "[\\u4e00-\\u9fff]"),"English summary translates all labels");
        return count;
    }
}
