using System.Text;
using System.Text.Json.Nodes;
using System.Xml;
using UnattendLocal.Core;
using UnattendLocal.Desktop;

static class SetupDynamicUpdateTests {
 public static int Run(GeneratorService generator){
  int count=0;
  void Check(bool ok,string message){if(!ok)throw new Exception(message);count++;Console.WriteLine("PASS V1.4 "+message);}
  void Reject(Action action,string message){try{action();}catch(ArgumentException){Check(true,message);return;}throw new Exception("Expected rejection: "+message);}
  InstallPlan Plan(){var p=InstallPlan.New();p.Users[0].Password="SyntheticUpdates1!";return p;}
  string Xml(InstallPlan p)=>Encoding.UTF8.GetString(generator.Generate(p));
  XmlElement? Dynamic(XmlDocument d)=>d.SelectSingleNode("//*[local-name()='DynamicUpdate']") as XmlElement;
  XmlElement Setup(XmlDocument d)=>d.SelectNodes("//*[local-name()='component']")!.Cast<XmlElement>().Single(c=>c.GetAttribute("name")=="Microsoft-Windows-Setup");
  string Strip(string xml){var d=XmlMerge.Read(xml);foreach(var c in d.SelectNodes("//comment()")!.Cast<XmlComment>().ToArray())c.ParentNode!.RemoveChild(c);return d.OuterXml;}
  void RemoveDynamic(XmlDocument d){foreach(XmlElement e in d.SelectNodes("//*[local-name()='DynamicUpdate']")!.Cast<XmlElement>().ToArray())e.ParentNode!.RemoveChild(e);}
  var fresh=Plan();Check(Dynamic(XmlMerge.Read(Xml(fresh)))==null,"default adds no native dynamic-update node");
  var old=PlanStore.Deserialize("{\"SchemaVersion\":1,\"Users\":[]}");Check(old.SetupDynamicUpdateMode==SetupDynamicUpdateSettings.Default&&old.SetupDynamicUpdateUi==SetupDynamicUpdateSettings.UiDefault,"old non-imported plans keep omitted native settings");
  foreach(string arch in new[]{"amd64","arm64","x86"})foreach(string mode in SetupDynamicUpdateSettings.Modes)foreach(string ui in SetupDynamicUpdateSettings.UiModes){
   var p=Plan();p.Architecture=arch;p.SetupDynamicUpdateMode=mode;p.SetupDynamicUpdateUi=ui;
   var d=XmlMerge.Read(Xml(p));var node=Dynamic(d);bool configured=mode!=SetupDynamicUpdateSettings.Default||ui!=SetupDynamicUpdateSettings.UiDefault;
   string label=arch+mode+ui;
   Check((node!=null)==configured,"node omission or presence "+label);
   if(node!=null){
    Check(node.ParentNode==Setup(d)&&node.ParentNode!.ParentNode!.Attributes!["pass"]!.Value=="windowsPE"&&Setup(d).GetAttribute("processorArchitecture")==arch,"native component, pass and architecture "+label);
    string? enabled=mode==SetupDynamicUpdateSettings.Default?null:mode==SetupDynamicUpdateSettings.Enabled?"true":"false";
    string? expectedUi=ui switch {SetupDynamicUpdateSettings.UiOnError=>"OnError",SetupDynamicUpdateSettings.UiNever=>"Never",SetupDynamicUpdateSettings.UiAlways=>"Always",_=>null};
    Check(node.ChildNodes.OfType<XmlElement>().FirstOrDefault(e=>e.LocalName=="Enable")?.InnerText==enabled&&node.ChildNodes.OfType<XmlElement>().FirstOrDefault(e=>e.LocalName=="WillShowUI")?.InnerText==expectedUi,"literal Microsoft setting values "+label);
   }
   foreach(bool snapshot in new[]{false,true}){
    string source=snapshot?d.OuterXml:Strip(d.OuterXml);var copy=XmlPlanImport.Import(source,generator);
    Check(copy.SetupDynamicUpdateMode==mode&&copy.SetupDynamicUpdateUi==ui&&!copy.ImportedXml!.SetupDynamicUpdateReadOnly,"actual XML restore, snapshot="+snapshot+label);
    Check(XmlMerge.Equal(XmlMerge.Read(source).DocumentElement,XmlMerge.Read(Xml(copy)).DocumentElement),"unchanged source preserved "+snapshot+label);
   }
  }
  var safety=Plan();safety.ConfirmWipeDisk=true;safety.ConfirmImportedXmlRisk=true;safety.SetupDynamicUpdateMode=SetupDynamicUpdateSettings.Enabled;
  Check(!safety.ConfirmWipeDisk&&!safety.ConfirmImportedXmlRisk,"update mode changes reset dangerous-disk consent");safety.ConfirmWipeDisk=true;safety.SetupDynamicUpdateMode=SetupDynamicUpdateSettings.Enabled;Check(safety.ConfirmWipeDisk,"unchanged mode keeps consent");
  safety.AutoLogon=false;safety.KeepSensitiveFiles=true;Check(!Xml(safety).Contains("<AutoLogon>")&&!safety.DisableWindowsUpdate,"dynamic updates do not force automatic login or installed-system update policy");
  var enabledPlan=Plan();enabledPlan.SetupDynamicUpdateMode=SetupDynamicUpdateSettings.Enabled;enabledPlan.SetupDynamicUpdateUi=SetupDynamicUpdateSettings.UiNever;
  string native=Strip(Xml(enabledPlan));
  foreach(bool secrets in new[]{false,true}){
   var copy=PlanStore.Deserialize(PlanStore.Serialize(enabledPlan,secrets));
   Check(copy.SetupDynamicUpdateMode==SetupDynamicUpdateSettings.Enabled&&copy.SetupDynamicUpdateUi==SetupDynamicUpdateSettings.UiNever,"JSON setting persistence "+secrets);
  }
  var editable=XmlPlanImport.Import(native,generator);editable.SetupDynamicUpdateMode=SetupDynamicUpdateSettings.Disabled;editable.SetupDynamicUpdateUi=SetupDynamicUpdateSettings.UiOnError;editable.ComputerName="EDITUPDATES";
  var edited=XmlPlanImport.Import(Xml(editable),generator);Check(edited.SetupDynamicUpdateMode==SetupDynamicUpdateSettings.Disabled&&edited.SetupDynamicUpdateUi==SetupDynamicUpdateSettings.UiOnError&&edited.ComputerName=="EDITUPDATES","settings and unrelated edits roundtrip together");
  editable.SetupDynamicUpdateMode=SetupDynamicUpdateSettings.Default;editable.SetupDynamicUpdateUi=SetupDynamicUpdateSettings.UiDefault;
  Check(Dynamic(XmlMerge.Read(Xml(editable)))==null&&Xml(editable).Contains("EDITUPDATES"),"reset removes only recognized dynamic updates");
  var added=XmlPlanImport.Import(Strip(Xml(Plan())),generator);added.SetupDynamicUpdateUi=SetupDynamicUpdateSettings.UiAlways;
  var partial=XmlPlanImport.Import(Xml(added),generator);Check(partial.SetupDynamicUpdateMode==SetupDynamicUpdateSettings.Default&&partial.SetupDynamicUpdateUi==SetupDynamicUpdateSettings.UiAlways,"UI-only native setting recognized without inventing Enable");
  var dTamper=XmlMerge.Read(Xml(enabledPlan));Dynamic(dTamper)!.FirstChild!.InnerText="false";
  Check(XmlPlanImport.Import(dTamper.OuterXml,generator).SetupDynamicUpdateMode==SetupDynamicUpdateSettings.Disabled,"modified actual XML wins over stale settings snapshot");
  foreach(string mutation in new[]{"extra-attribute","extra-child","nested-value","duplicate-enable","invalid-enable","empty-ui","duplicate-node","wrong-phase","wrong-parent","foreign-namespace","component-attribute","duplicate-component"}){
   var d=XmlMerge.Read(native);var node=Dynamic(d)!;
   switch(mutation){
    case "extra-attribute":node.SetAttribute("future","preserve");break;
    case "extra-child":node.AppendChild(d.CreateElement("Future",XmlMerge.Namespace));break;
    case "nested-value":node.FirstChild!.AppendChild(d.CreateElement("Nested",XmlMerge.Namespace));break;
    case "duplicate-enable":node.AppendChild(node.FirstChild!.CloneNode(true));break;
    case "invalid-enable":node.FirstChild!.InnerText="1";break;
    case "empty-ui":node.LastChild!.InnerText="";break;
    case "duplicate-node":node.ParentNode!.AppendChild(node.CloneNode(true));break;
    case "wrong-phase":d.SelectNodes("//*[local-name()='settings']")!.Cast<XmlElement>().Single(s=>s.GetAttribute("pass")=="offlineServicing").AppendChild(Setup(d));break;
    case "wrong-parent":d.DocumentElement!.AppendChild(node);break;
    case "foreign-namespace":var foreign=d.CreateElement("DynamicUpdate","urn:future");foreign.InnerXml=node.InnerXml;node.ParentNode!.ReplaceChild(foreign,node);break;
    case "component-attribute":Setup(d).SetAttribute("future","keep");break;
    case "duplicate-component":Setup(d).ParentNode!.AppendChild(Setup(d).CloneNode(true));break;
   }
   string original=d.OuterXml;var copy=XmlPlanImport.Import(original,generator);
   Check(copy.ImportedXml!.SetupDynamicUpdateReadOnly&&copy.ImportedXml.Notices.Contains(SetupDynamicUpdateSettings.ReadOnlyNotice),"unknown input explicitly readonly "+mutation);
   var vmReadonly=new MainViewModel{Plan=copy};Check(!vmReadonly.CanEditSetupDynamicUpdate&&vmReadonly.HasReadonlySetupDynamicUpdate&&vmReadonly.SetupDynamicUpdateSummary.Contains("只读"),"readonly UI and summary "+mutation);
   copy.SetupDynamicUpdateMode=SetupDynamicUpdateSettings.Enabled;Reject(()=>Xml(copy),"unknown config cannot be overwritten "+mutation);
   copy.SetupDynamicUpdateMode=SetupDynamicUpdateSettings.Default;copy.SetupDynamicUpdateUi=SetupDynamicUpdateSettings.UiAlways;Reject(()=>Xml(copy),"unknown config cannot be overwritten through UI-only edit "+mutation);
   copy.SetupDynamicUpdateUi=SetupDynamicUpdateSettings.UiDefault;
   if(mutation is not ("wrong-parent" or "component-attribute")&&copy.ImportedXml.Blockers.Length==0){
    copy.ComputerName="KEEPNATIVE";copy.ConfirmImportedXmlRisk=true;
    var result=XmlMerge.Read(Xml(copy));var before=d.SelectNodes("//*[local-name()='DynamicUpdate']")!.Cast<XmlElement>().ToArray();var after=result.SelectNodes("//*[local-name()='DynamicUpdate']")!.Cast<XmlElement>().ToArray();Check(before.Length==after.Length&&before.Zip(after).All(pair=>XmlMerge.Equal(pair.First,pair.Second)),"unrelated edit preserves all original native nodes "+mutation);
   }
   if(mutation is "wrong-parent" or "component-attribute"){copy.ComputerName="BLOCKINVALID";copy.ConfirmImportedXmlRisk=true;Reject(()=>Xml(copy),"invalid/ambiguous original cannot silently export "+mutation);}
   Check(copy.ImportedXml.SourceXml==XmlMerge.Read(original).OuterXml,"import never rewrites source "+mutation);
  }
  foreach(bool withDynamic in new[]{false,true}){
   var custom=XmlMerge.Read(withDynamic?native:Strip(Xml(Plan())));custom.DocumentElement!.AppendChild(custom.CreateElement("PEScriptCopy","urn:test"));
   var copy=XmlPlanImport.Import(custom.OuterXml,generator);Check(copy.ImportedXml!.SetupDynamicUpdateReadOnly,"custom PE path explicitly readonly "+withDynamic);
   copy.SetupDynamicUpdateMode=SetupDynamicUpdateSettings.Enabled;Reject(()=>Xml(copy),"custom PE cannot receive native override "+withDynamic);
  }
  // Pre-V1.4 imported JSON: recover actual settings and extend only the old baseline.
  var legacy=XmlPlanImport.Import(native,generator);legacy.ComputerName="PENDINGNAME";
  JsonObject OldRecord(){
   var root=JsonNode.Parse(PlanStore.Serialize(legacy,true))!.AsObject();root.Remove(nameof(InstallPlan.SetupDynamicUpdateMode));root.Remove(nameof(InstallPlan.SetupDynamicUpdateUi));
   var data=root[nameof(InstallPlan.ImportedXml)]!.AsObject();data.Remove(nameof(ImportedXml.SetupDynamicUpdateImportVersion));data.Remove(nameof(ImportedXml.SetupDynamicUpdateReadOnly));
   var baseline=XmlMerge.Read(data[nameof(ImportedXml.BaselineXml)]!.GetValue<string>());RemoveDynamic(baseline);data[nameof(ImportedXml.BaselineXml)]=baseline.OuterXml;return root;
  }
  var oldJson=OldRecord();var migrated=PlanStore.Deserialize(oldJson.ToJsonString());
  Check(migrated.ComputerName=="PENDINGNAME"&&migrated.SetupDynamicUpdateMode==SetupDynamicUpdateSettings.Enabled&&migrated.SetupDynamicUpdateUi==SetupDynamicUpdateSettings.UiNever,"legacy migration restores native fields and preserves pending edits");
  migrated.SetupDynamicUpdateMode=SetupDynamicUpdateSettings.Disabled;Check(XmlPlanImport.Import(Xml(migrated),generator).SetupDynamicUpdateMode==SetupDynamicUpdateSettings.Disabled,"legacy baseline supports subsequent editing");
  oldJson=OldRecord();oldJson[nameof(InstallPlan.Architecture)]="arm64";migrated=PlanStore.Deserialize(oldJson.ToJsonString());Check(migrated.Architecture=="arm64"&&migrated.ComputerName=="PENDINGNAME"&&migrated.SetupDynamicUpdateMode==SetupDynamicUpdateSettings.Enabled,"legacy migration preserves pending architecture");
  oldJson=OldRecord();oldJson[nameof(InstallPlan.SetupDynamicUpdateMode)]=SetupDynamicUpdateSettings.Default;
  migrated=PlanStore.Deserialize(oldJson.ToJsonString());Check(migrated.ImportedXml!.Blockers.Any(b=>b.Contains("动态更新编辑基准"))&&migrated.ComputerName=="PENDINGNAME","ambiguous legacy fields block export without losing edits");Reject(()=>Xml(migrated),"ambiguous legacy export blocked");
  var noOriginal=PlanStore.Deserialize(PlanStore.Serialize(legacy,false));Check(noOriginal.ImportedXml!.OriginalOmitted&&noOriginal.SetupDynamicUpdateMode==SetupDynamicUpdateSettings.Enabled,"omitted source keeps setting display");Reject(()=>Xml(noOriginal),"omitted source never replaced by UI defaults");
  var recommended=Plan();recommended.SetupDynamicUpdateMode=SetupDynamicUpdateSettings.Disabled;recommended.SetupDynamicUpdateUi=SetupDynamicUpdateSettings.UiOnError;Recommendations.Apply(recommended,"wired");
  Check(recommended.SetupDynamicUpdateMode==SetupDynamicUpdateSettings.Disabled&&recommended.SetupDynamicUpdateUi==SetupDynamicUpdateSettings.UiOnError,"recommendations preserve explicit setup choices");
  var vm=new MainViewModel{Plan=Plan()};vm.Plan.SetupDynamicUpdateMode=SetupDynamicUpdateSettings.Enabled;vm.Plan.SetupDynamicUpdateUi=SetupDynamicUpdateSettings.UiNever;
  Check(vm.SetupDynamicUpdateSummary.Contains("启用动态更新")&&vm.CanEditSetupDynamicUpdate&&!vm.HasReadonlySetupDynamicUpdate,"view model refreshes state and summary");
  Check(HelpCatalog.ForPage(3).Any(h=>h.Id=="setupdynamicupdate")&&SettingsSearchCatalog.Search("DynamicUpdate").Any(),"help and search include native feature");
  Check(Recommendations.Summary(vm.Plan).Contains("安装阶段动态更新：启用动态更新"),"overall summary includes actual setup choices");
  foreach(var bad in new[]{"", "future"}){var p=Plan();p.SetupDynamicUpdateMode=bad;Reject(()=>Xml(p),"invalid mode blocked");p.SetupDynamicUpdateMode=SetupDynamicUpdateSettings.Default;p.SetupDynamicUpdateUi=bad;Reject(()=>Xml(p),"invalid UI mode blocked");}
  return count;
 }
}
