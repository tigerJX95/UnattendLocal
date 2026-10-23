using System.Text;
using System.Text.Json.Nodes;
using System.Xml;
using UnattendLocal.Core;
using UnattendLocal.Desktop;

static class NativePreferencesTests
{
    public static int Run(GeneratorService generator)
    {
        int count=0;
        void Check(bool value,string name){if(!value)throw new Exception(name);count++;}
        void Reject(Action action,string name){try{action();}catch(ArgumentException){count++;return;}throw new Exception("Expected rejection: "+name);}
        XmlDocument Generate(InstallPlan p)=>XmlMerge.Read(Encoding.UTF8.GetString(generator.Generate(p)));
        string? Text(XmlDocument d,NativePreferences.Setting s)=>d.SelectSingleNode("//*[local-name()='"+s.Element+"']")?.InnerText;
        void Set(InstallPlan p,NativePreferences.Setting s,bool on){if(s.Power)p.PowerPlanMode=on?NativePreferences.HighPerformance:NativePreferences.Default;else p.ErrorReportingMode=on?NativePreferences.WerDisabled:NativePreferences.Default;}
        XmlDocument Plain(XmlDocument d){foreach(XmlComment c in d.SelectNodes("//comment()")!.Cast<XmlComment>().ToArray())c.ParentNode!.RemoveChild(c);return d;}
        foreach(var arch in new[]{"amd64","arm64","x86"})
        {
            var p=QuickPresets.Create(QuickPresets.Minimal);p.Architecture=arch;
            var none=Generate(p);Check(Text(none,NativePreferences.Power)==null&&Text(none,NativePreferences.Wer)==null,"default emits neither setting");
            foreach(var mode in NativePreferences.PowerModes.Where(m=>m!=NativePreferences.Default))
            {
                p.PowerPlanMode=mode;p.CustomPowerPlanGuid="6b79ac3b-2222-4444-8888-abcdefabcdef";
                var doc=Generate(p);var value=Text(doc,NativePreferences.Power);
                Check(value==NativePreferences.Value(p,NativePreferences.Power),"power mode writes selected GUID");
                var node=(XmlElement)doc.SelectSingleNode("//*[local-name()='PreferredPlan']")!;
                Check(node.ParentNode!.Attributes!["processorArchitecture"]!.Value==arch&&node.ParentNode.ParentNode!.Attributes!["pass"]!.Value=="specialize","power architecture and stage");
                var imported=XmlPlanImport.Import(doc.OuterXml,generator);
                Check(imported.ImportedXml!.Blockers.Length==0&&Text(Generate(imported),NativePreferences.Power)==value,"power own XML roundtrip");
                imported.PowerPlanMode=NativePreferences.Default;
                Check(Text(Generate(imported),NativePreferences.Power)==null,"power can be removed after import");
            }
            p.PowerPlanMode=NativePreferences.Default;
            foreach(var mode in NativePreferences.WerModes)
            {
                p.ErrorReportingMode=mode;var doc=Generate(p);
                Check(Text(doc,NativePreferences.Wer)==NativePreferences.Value(p,NativePreferences.Wer),"WER explicit zero one and default");
                var imported=XmlPlanImport.Import(doc.OuterXml,generator);Check(imported.ImportedXml!.Blockers.Length==0,"WER import baseline valid");
                imported.ErrorReportingMode=mode==NativePreferences.WerEnabled?NativePreferences.WerDisabled:NativePreferences.WerEnabled;
                Check(Text(Generate(imported),NativePreferences.Wer)==NativePreferences.Value(imported,NativePreferences.Wer),"WER import editing");
                imported.ErrorReportingMode=NativePreferences.Default;Check(Text(Generate(imported),NativePreferences.Wer)==null,"WER removal");
            }
        }
        foreach(var s in NativePreferences.All)
        {
            var p=QuickPresets.Create(QuickPresets.Minimal);Set(p,s,true);var original=Plain(Generate(p));
            var component=(XmlElement)original.SelectSingleNode("//*[local-name()='"+s.Element+"']")!.ParentNode!;
            var other=original.CreateElement(s.Power?"UnknownPowerSibling":"DefaultConsent",XmlMerge.Namespace);other.InnerText="1";component.AppendChild(other);
            var imported=XmlPlanImport.Import(original.OuterXml,generator);
            Check(imported.ImportedXml!.Blockers.Length==0,"native field with untouched sibling imports");
            Set(imported,s,false);var cleared=Generate(imported);
            Check(Text(cleared,s)==null&&cleared.SelectSingleNode("//*[local-name()='"+other.LocalName+"']")?.InnerText=="1","removing own field retains sibling");
            var absent=(XmlDocument)original.CloneNode(true);var setting=absent.SelectSingleNode("//*[local-name()='"+s.Element+"']")!;setting.ParentNode!.RemoveChild(setting);
            var adding=XmlPlanImport.Import(absent.OuterXml,generator);Set(adding,s,true);
            var added=Generate(adding);Check(Text(added,s)!=null&&added.SelectSingleNode("//*[local-name()='"+other.LocalName+"']")!=null,"add scalar into preserved component without losing sibling");
            foreach(var variant in new[]{"unknown","duplicate","stage","attributes","nested","namespace"})
            {
                var altered=(XmlDocument)original.CloneNode(true);var n=(XmlElement)altered.SelectSingleNode("//*[local-name()='"+s.Element+"']")!;
                if(variant=="unknown")n.InnerText="unsupported";
                if(variant=="duplicate")n.ParentNode!.AppendChild(n.CloneNode(true));
                if(variant=="stage")((XmlElement)n.ParentNode!.ParentNode!).SetAttribute("pass","generalize");
                if(variant=="attributes")n.SetAttribute("unexpected","value");
                if(variant=="nested")n.InnerXml="<Nested xmlns='"+XmlMerge.Namespace+"'/>";
                if(variant=="namespace"){var foreign=altered.CreateElement(s.Element,"urn:unknown");foreign.InnerText=n.InnerText;n.ParentNode!.ReplaceChild(foreign,n);}
                var unknown=XmlPlanImport.Import(altered.OuterXml,generator);
                Check(!NativePreferences.CanEdit(unknown,s)&&unknown.ImportedXml!.Blockers.Length==0,"unknown variant read-only "+variant);
                Check(Text(Generate(unknown),s)==Text(altered,s),"unknown variant preserved "+variant);
                Set(unknown,s,true);Reject(()=>generator.Generate(unknown),"unknown variant cannot overwrite "+variant);
            }
            var saved=XmlPlanImport.Import(original.OuterXml,generator);
            var json=JsonNode.Parse(PlanStore.Serialize(saved,true))!;json.AsObject().Remove(nameof(InstallPlan.PowerPlanMode));json.AsObject().Remove(nameof(InstallPlan.CustomPowerPlanGuid));json.AsObject().Remove(nameof(InstallPlan.ErrorReportingMode));
            var data=json[nameof(InstallPlan.ImportedXml)]!;data.AsObject().Remove(nameof(ImportedXml.NativePreferencesImportVersion));
            var baseline=XmlMerge.Read(data[nameof(ImportedXml.BaselineXml)]!.GetValue<string>());
            foreach(XmlElement c in baseline.SelectNodes("//*[local-name()='component' and @name='"+s.Component+"']")!.Cast<XmlElement>().ToArray())c.ParentNode!.RemoveChild(c);
            data[nameof(ImportedXml.BaselineXml)]=baseline.OuterXml;
            var upgraded=PlanStore.Deserialize(json.ToJsonString());
            Check(upgraded.ImportedXml!.Blockers.Length==0&&Text(Generate(upgraded),s)==Text(original,s),"V1.14 JSON baseline migrates actual XML");
            Set(upgraded,s,false);Check(Text(Generate(upgraded),s)==null,"migrated setting is editable");
            // Explicit new fields in an inconsistent old baseline must not be silently adopted.
            json[s.Field]=NativePreferences.Default;
            var guarded=PlanStore.Deserialize(json.ToJsonString());Check(guarded.ImportedXml!.Blockers.Length>0,"ambiguous migration stops export");
        }
        var upper=QuickPresets.Create(QuickPresets.Minimal);upper.PowerPlanMode=NativePreferences.Custom;upper.CustomPowerPlanGuid=NativePreferences.BalancedGuid.ToUpperInvariant();
        var upperImport=XmlPlanImport.Import(Plain(Generate(upper)).OuterXml,generator);
        Check(Text(Generate(upperImport),NativePreferences.Power)==upper.CustomPowerPlanGuid,"GUID spelling preserved");upperImport.PowerPlanMode=NativePreferences.Default;Check(Text(Generate(upperImport),NativePreferences.Power)==null,"uppercase imported GUID removes cleanly");
        var extra=QuickPresets.Create(QuickPresets.Minimal);extra.XmlComponents.Add(new XmlComponentEntry{Enabled=true,Component=NativePreferences.Wer.Component,Content="<DisableWER>1</DisableWER>"});extra.ConfirmAdditionalXml=true;
        var legacy=XmlPlanImport.Import(Generate(extra).OuterXml,generator);legacy.ConfirmAdditionalXml=true;
        Check(legacy.ErrorReportingMode==NativePreferences.Default&&!NativePreferences.CanEdit(legacy,NativePreferences.Wer)&&Text(Generate(legacy),NativePreferences.Wer)=="1","V1.14 extra XML retains ownership");
        legacy.ErrorReportingMode=NativePreferences.WerEnabled;Reject(()=>generator.Generate(legacy),"dual editors reject overwrite");Check(NativePreferences.CanEdit(legacy,NativePreferences.Wer),"conflicting mode remains clearable in UI");
        legacy.ErrorReportingMode=NativePreferences.Default;legacy.XmlComponents[0].Enabled=false;legacy.ErrorReportingMode=NativePreferences.WerEnabled;Check(Text(Generate(legacy),NativePreferences.Wer)=="0","explicit handoff from extra XML to native editor");
        var legacyJson=JsonNode.Parse(PlanStore.Serialize(XmlPlanImport.Import(Generate(extra).OuterXml,generator),true))!;
        foreach(var name in new[]{nameof(InstallPlan.PowerPlanMode),nameof(InstallPlan.ErrorReportingMode),nameof(InstallPlan.CustomPowerPlanGuid)})legacyJson.AsObject().Remove(name);
        legacyJson[nameof(InstallPlan.ImportedXml)]!.AsObject().Remove(nameof(ImportedXml.NativePreferencesImportVersion));
        var kept=PlanStore.Deserialize(legacyJson.ToJsonString());kept.ConfirmAdditionalXml=true;
        Check(kept.ErrorReportingMode==NativePreferences.Default&&Text(Generate(kept),NativePreferences.Wer)=="1","old JSON extra editor is not adopted by native editor");
        legacyJson[nameof(InstallPlan.XmlComponents)]![0]![nameof(XmlComponentEntry.Enabled)]=false;
        var pending=PlanStore.Deserialize(legacyJson.ToJsonString());
        Check(Text(Generate(pending),NativePreferences.Wer)==null,"old pending extra disable is not resurrected during migration");
        foreach(var s in NativePreferences.All)
        {
            var empty=QuickPresets.Create(QuickPresets.Minimal);Set(empty,s,true);var doc=Plain(Generate(empty));var n=doc.SelectSingleNode("//*[local-name()='"+s.Element+"']")!;n.ParentNode!.RemoveChild(n);
            var importedEmpty=XmlPlanImport.Import(doc.OuterXml,generator);
            Check(Generate(importedEmpty).SelectNodes("//*[local-name()='component' and @name='"+s.Component+"']")!.Count==1,"unrelated original empty component remains preserved");
        }
        foreach(var value in new[]{"","{}",Guid.Empty.ToString(),NativePreferences.BalancedGuid+" ","cmd.exe /c whoami"})
        {var bad=QuickPresets.Create(QuickPresets.Minimal);bad.PowerPlanMode=NativePreferences.Custom;bad.CustomPowerPlanGuid=value;Reject(()=>generator.Generate(bad),"invalid GUID rejected");}
        var invalid=QuickPresets.Create(QuickPresets.Minimal);invalid.ErrorReportingMode="invalid";Reject(()=>generator.Generate(invalid),"unknown WER mode");
        Check(ConfigurationErrorLocation.FromField(nameof(InstallPlan.CustomPowerPlanGuid))?.Setting.Target=="PowerPlanGuidEditor","GUID error navigation");
        var vm=new MainViewModel();vm.Plan.PowerPlanMode=NativePreferences.Custom;Check(vm.IsCustomPowerPlan&&vm.PowerPlanSummary.Contains("自定义"),"editor visibility refreshes");
        Check(DiagnosticText.IsErrorSummary(vm.PowerPlanSummary)&&!DiagnosticText.IsErrorSummary("首选电源计划：原 XML 只读保留，请核对原文。"),"pending GUID is red while read-only information is ordinary");
        return count;
    }
}
