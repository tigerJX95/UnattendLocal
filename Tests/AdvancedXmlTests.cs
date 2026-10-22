using System.Text;
using System.Xml;
using UnattendLocal.Core;
using UnattendLocal.Desktop;

static class AdvancedXmlTests
{
    public static int Run(GeneratorService generator)
    {
        int count=0;
        void Check(bool ok,string name){if(!ok)throw new Exception(name);count++;}
        void Reject(Action action,string name){try{action();}catch(ArgumentException){count++;return;}throw new Exception("Expected rejection: "+name);}
        foreach(var kind in new[]{QuickPresets.Minimal,QuickPresets.LocalAccount})
        foreach(var arch in new[]{"amd64","arm64","x86"})
        {
            var plan=QuickPresets.Create(kind);plan.Architecture=arch;
            string xml=Encoding.UTF8.GetString(generator.Generate(plan));
            Check(!xml.Contains("diskpart.exe")&&!xml.Contains("LabConfig")&&!xml.Contains("Remove-AppxPackage"),"preset has no disk wiping, hardware bypass or app removal");
            Check(plan.DiskMode==InstallationSettings.Manual&&!plan.BypassNetwork&&!plan.PreventDeviceEncryption,"preset does not inherit recommendation policy");
            Check(plan.InteractiveLanguage&&plan.TimeZoneMode=="由 Windows 决定","preset preserves installation choices");
            Check(plan.Users.Count==(kind==QuickPresets.Minimal?0:1),"account count matches preset");
            if(kind==QuickPresets.LocalAccount)Check(plan.Users[0].Name=="Example"&&plan.Users[0].Administrator&&plan.Users[0].AllowEmptyPassword&&plan.AutoLogon&&!plan.KeepSensitiveFiles,"upstream example account and logon behavior");
            else Check(!plan.AutoLogon&&plan.KeepSensitiveFiles,"minimal preset avoids automatic logon and cleanup");
            var reloaded=PlanStore.Deserialize(PlanStore.Serialize(plan,true));
            Check(reloaded.CustomStartingPoint==kind&&generator.Generate(reloaded).Length>0,"preset JSON roundtrip");
            var imported=XmlPlanImport.Import(xml,generator);
            Check(imported.ImportedXml!.Blockers.Length==0&&generator.Generate(imported).Length>0,"preset XML roundtrip");
        }
        Reject(()=>QuickPresets.Create("unknown"),"unknown preset");
        foreach(var arch in new[]{"amd64","arm64","x86"})
        {
            var plan=QuickPresets.Create(QuickPresets.Minimal);plan.Architecture=arch;plan.ComputerName="KEEP-NAME";
            var row=new XmlComponentEntry{Enabled=true,Content=AdditionalXmlSettings.Example};plan.XmlComponents.Add(row);
            Reject(()=>generator.Generate(plan),"explicit XML consent required");plan.ConfirmAdditionalXml=true;
            byte[] bytes=generator.Generate(plan);string xml=Encoding.UTF8.GetString(bytes);
            var doc=XmlMerge.Read(xml);var ns=new XmlNamespaceManager(doc.NameTable);ns.AddNamespace("u",XmlMerge.Namespace);
            var oem=doc.SelectSingleNode("/u:unattend/u:settings[@pass='specialize']/u:component[@name='Microsoft-Windows-Shell-Setup']/u:OEMInformation",ns);
            Check(oem?.ParentNode?.Attributes?["processorArchitecture"]?.Value==arch,"additional XML uses selected architecture");
            Check(oem?.SelectSingleNode("u:SupportProvider",ns)?.InnerText=="示例技术支持","OEM example inserted");
            Check(doc.SelectNodes("//u:ComputerName",ns)!.Count>0,"existing generated child survives");
            Check(!GeneratorService.Preview(bytes,plan).Contains("https://example.com/support"),"entire additional content redacted from preview");
            var saved=PlanStore.Deserialize(PlanStore.Serialize(plan,true));
            Check(!saved.ConfirmAdditionalXml&&saved.XmlComponents[0].Content==row.Content,"saved JSON retains content but clears consent");
            var omitted=PlanStore.Deserialize(PlanStore.Serialize(plan,false));
            Check(omitted.XmlComponents[0].Enabled&&omitted.XmlComponents[0].Content.Length==0,"omitted custom XML remains enabled and pending");
            omitted.ConfirmAdditionalXml=true;Reject(()=>generator.Generate(omitted),"omitted custom XML cannot silently export");
            var imported=XmlPlanImport.Import(xml,generator);
            Check(imported.ImportedXml!.Blockers.Length==0&&imported.XmlComponents.Count==1,"own export restores XML editor: rows="+imported.XmlComponents.Count+" blockers="+string.Join(" | ",imported.ImportedXml!.Blockers)+" notices="+string.Join(" | ",imported.ImportedXml.Notices));
            imported.ConfirmAdditionalXml=true;
            Check(generator.Generate(imported).Length>0,"additional XML import re-export");
            imported.XmlComponents[0].Content=row.Content.Replace("示例技术支持","修改后的支持名称");
            imported.ConfirmAdditionalXml=true;
            Check(XmlMerge.Read(Encoding.UTF8.GetString(generator.Generate(imported))).InnerText.Contains("修改后的支持名称"),"imported custom XML can be edited");
            var vm=new MainViewModel{Plan=plan};plan.ConfirmAdditionalXml=true;row.Content=row.Content.Replace("示例技术支持","已修改");
            Check(!plan.ConfirmAdditionalXml,"editing XML invalidates consent");
            plan.ConfirmAdditionalXml=true;plan.XmlComponents.Add(new XmlComponentEntry());
            Check(!plan.ConfirmAdditionalXml,"adding XML invalidates consent");plan.XmlComponents.RemoveAt(1);
            plan.ConfirmAdditionalXml=true;row.Content="<ComputerName>COLLISION</ComputerName>";
            plan.ConfirmAdditionalXml=true;Reject(()=>generator.Generate(plan),"existing top-level child cannot be overwritten");
            row.Content=AdditionalXmlSettings.Example;plan.XmlComponents.Add(new XmlComponentEntry{Enabled=true,Content=row.Content});plan.ConfirmAdditionalXml=true;
            Reject(()=>generator.Generate(plan),"duplicate top-level setting across rows rejected");
        }
        foreach(var bad in new[]{"<component/>","<settings/>","<unattend/>","<a xmlns='urn:other'/>","<a/><a/>","text only","<?test value?><a/>","<!DOCTYPE a [<!ENTITY x SYSTEM 'file:///etc/passwd'>]><a>&x;</a>","<a>"})
            Reject(()=>AdditionalXmlSettings.Parse(new XmlComponentEntry{Content=bad}),"invalid XML fragment");
        Reject(()=>AdditionalXmlSettings.Parse(new XmlComponentEntry{Content=new string('x',AdditionalXmlSettings.MaxLength+1)}),"oversized XML");
        Reject(()=>AdditionalXmlSettings.Parse(new XmlComponentEntry{Content=string.Concat(Enumerable.Repeat("<a>",66))+string.Concat(Enumerable.Repeat("</a>",66))}),"deep XML");
        Reject(()=>AdditionalXmlSettings.Parse(new XmlComponentEntry{Component="not-a-component",Content="<a/>"}),"unknown component");
        Reject(()=>AdditionalXmlSettings.Parse(new XmlComponentEntry{Pass="not-a-pass",Content="<a/>"}),"unknown stage");
        Reject(()=>AdditionalXmlSettings.Parse(new XmlComponentEntry{Component="Microsoft-Windows-Audio-AudioCore",Pass="windowsPE",Content="<a/>"}),"component-stage mismatch");
        var disabled=QuickPresets.Create(QuickPresets.Minimal);disabled.XmlComponents.Add(new XmlComponentEntry{Content="<broken"});
        Check(generator.Generate(disabled).Length>0,"disabled draft does not block generation");
        var initial=new MainViewModel();initial.Plan.ConfirmAdditionalXml=true;initial.Plan.XmlComponents.Add(new XmlComponentEntry());
        Check(!initial.Plan.ConfirmAdditionalXml,"initial plan collection is observed");
        var offline=QuickPresets.Create(QuickPresets.LocalAccount);offline.XmlComponents.Add(new XmlComponentEntry{Enabled=true,Pass="offlineServicing",Content=AdditionalXmlSettings.Example});offline.ConfirmAdditionalXml=true;
        Check(generator.Generate(offline).Length>0,"new stage precedes extensions and validates");
        var privacy=QuickPresets.Create(QuickPresets.Minimal);
        privacy.XmlComponents.Add(new XmlComponentEntry{Enabled=true,Content="<!--OMIT-OUTSIDE-COMMENT-->"+AdditionalXmlSettings.Example});
        privacy.XmlComponents.Add(new XmlComponentEntry{Content="<a>DORMANT-SECRET</a>"});privacy.ConfirmAdditionalXml=true;
        var privacyDoc=XmlMerge.Read(Encoding.UTF8.GetString(generator.Generate(privacy)));
        var snapshot=privacyDoc.SelectNodes("//comment()")!.Cast<XmlComment>().Single(c=>c.Value!.StartsWith("UnattendLocal:v1 ")).Value!;
        var decoded=Encoding.UTF8.GetString(Convert.FromBase64String(snapshot.Split(' ',3)[2]));
        Check(!decoded.Contains("OMIT-OUTSIDE-COMMENT")&&!decoded.Contains("DORMANT-SECRET"),"snapshot contains only applied elements, not dormant drafts or outside comments");
        var plain=XmlMerge.Read(Encoding.UTF8.GetString(generator.Generate(privacy)));foreach(var comment in plain.SelectNodes("//comment()")!.Cast<XmlComment>().ToArray())comment.ParentNode!.RemoveChild(comment);
        var foreign=XmlPlanImport.Import(plain.OuterXml,generator);Check(foreign.ImportedXml!.Blockers.Length==0,"unknown fragment source retains a valid editable baseline");foreign.XmlComponents.Add(new XmlComponentEntry{Enabled=true,Content=AdditionalXmlSettings.Example.Replace("示例技术支持","冲突名称")});foreign.ConfirmAdditionalXml=true;
        Reject(()=>generator.Generate(foreign),"custom XML cannot overwrite unknown imported values");
        Reject(()=>AdditionalXmlSettings.Parse(new XmlComponentEntry{Content="<RegisteredOwner>Duplicate editor</RegisteredOwner>"}),"managed native field cannot have two editors");
        Check(ConfigurationErrorLocation.FromField(nameof(InstallPlan.XmlComponents))?.Setting.Target=="AdditionalXmlSection","validation navigates to advanced editor");
        return count;
    }
}
