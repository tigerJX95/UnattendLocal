using System.Text;
using System.Text.Json.Nodes;
using System.Xml;
using UnattendLocal.Core;
using UnattendLocal.Desktop;

static class RegisteredInformationTests {
 public static int Run(GeneratorService generator){
  int count=0;
  void Check(bool ok,string message){if(!ok)throw new Exception(message);count++;Console.WriteLine("PASS V1.5 "+message);}
  void Reject(Action action,string message){try{action();}catch(ArgumentException){Check(true,message);return;}throw new Exception("Expected rejection: "+message);}
  InstallPlan Plan(){var p=InstallPlan.New();p.Users[0].Password="SyntheticRegistration1!";return p;}
  string Xml(InstallPlan p)=>Encoding.UTF8.GetString(generator.Generate(p));
  XmlElement[] Nodes(XmlDocument d)=>d.SelectNodes("//*[local-name()='RegisteredOwner' or local-name()='RegisteredOrganization']")!.Cast<XmlElement>().ToArray();
  XmlElement Owner(XmlDocument d)=>Nodes(d).Single(n=>n.LocalName=="RegisteredOwner");
  string Strip(string xml){var d=XmlMerge.Read(xml);foreach(var c in d.SelectNodes("//comment()")!.Cast<XmlComment>().ToArray())c.ParentNode!.RemoveChild(c);return d.OuterXml;}
  var fresh=Plan();Check(!fresh.UseRegisteredOwner&&!fresh.UseRegisteredOrganization&&Nodes(XmlMerge.Read(Xml(fresh))).Length==0,"safe defaults omit both native fields");
  var old=PlanStore.Deserialize("{\"SchemaVersion\":1,\"Users\":[]}");Check(!old.UseRegisteredOwner&&!old.UseRegisteredOrganization,"old standalone plans preserve omission");
  foreach(string arch in new[]{"amd64","arm64","x86"})foreach(string pass in RegisteredInformationSettings.Passes)foreach(int mask in new[]{1,2,3}){
   var p=Plan();p.Architecture=arch;p.UseRegisteredOwner=(mask&1)!=0;p.UseRegisteredOrganization=(mask&2)!=0;p.RegisteredOwner="王小明 & <部署>";p.RegisteredOrganization="研发中心 'A'";p.RegisteredOwnerPass=pass;p.RegisteredOrganizationPass=pass;
   string label=arch+pass+mask;var d=XmlMerge.Read(Xml(p));var nodes=Nodes(d);
   Check(nodes.Length==(mask==3?2:1)&&nodes.All(n=>n.ParentNode!.Attributes!["name"]!.Value=="Microsoft-Windows-Shell-Setup"&&n.ParentNode.Attributes!["processorArchitecture"]!.Value==arch&&n.ParentNode.ParentNode!.Attributes!["pass"]!.Value==pass),"literal native component, architecture and phase "+label);
   Check(nodes.All(n=>n.InnerText==(n.LocalName=="RegisteredOwner"?p.RegisteredOwner:p.RegisteredOrganization)),"Unicode and XML special characters preserved "+label);
   foreach(bool snapshot in new[]{false,true}){
    string source=snapshot?d.OuterXml:Strip(d.OuterXml);var copy=XmlPlanImport.Import(source,generator);
    Check(copy.UseRegisteredOwner==p.UseRegisteredOwner&&copy.UseRegisteredOrganization==p.UseRegisteredOrganization&&!copy.ImportedXml!.RegisteredInformationReadOnly&&(!p.UseRegisteredOwner||copy.RegisteredOwner==p.RegisteredOwner&&copy.RegisteredOwnerPass==pass)&&(!p.UseRegisteredOrganization||copy.RegisteredOrganization==p.RegisteredOrganization&&copy.RegisteredOrganizationPass==pass),"actual source restoration snapshot="+snapshot+label);
    Check(XmlMerge.Equal(XmlMerge.Read(source).DocumentElement,XmlMerge.Read(Xml(copy)).DocumentElement),"unchanged source roundtrip "+snapshot+label);
   }
  }
  var seed=Plan();seed.UseRegisteredOwner=true;seed.UseRegisteredOrganization=true;seed.RegisteredOwner="Original Owner";seed.RegisteredOrganization="Original Org";
  string native=Strip(Xml(seed));
  foreach(bool secrets in new[]{false,true}){
   var copy=PlanStore.Deserialize(PlanStore.Serialize(seed,secrets));Check(copy.UseRegisteredOwner&&copy.UseRegisteredOrganization&&copy.RegisteredOwner==seed.RegisteredOwner&&copy.RegisteredOrganization==seed.RegisteredOrganization,"JSON names retained in either credentials mode "+secrets);
  }
  var edited=XmlPlanImport.Import(native,generator);edited.RegisteredOwner="New Owner";edited.RegisteredOrganization="新组织";edited.ComputerName="EDITREGISTER";
  var back=XmlPlanImport.Import(Xml(edited),generator);Check(back.RegisteredOwner=="New Owner"&&back.RegisteredOrganization=="新组织"&&back.ComputerName=="EDITREGISTER","native fields and unrelated edits roundtrip together");
  edited.UseRegisteredOwner=false;back=XmlPlanImport.Import(Xml(edited),generator);Check(!back.UseRegisteredOwner&&back.UseRegisteredOrganization&&back.RegisteredOrganization=="新组织","owner reset does not remove organization");edited.UseRegisteredOrganization=false;Check(Nodes(XmlMerge.Read(Xml(edited))).Length==0,"reset removes recognized fields");
  var empty=Plan();empty.UseRegisteredOwner=true;empty.UseRegisteredOrganization=true;
  var emptyNodes=Nodes(XmlMerge.Read(Xml(empty)));Check(emptyNodes.Length==2&&emptyNodes.All(n=>n.InnerText==""),"explicit empty values remain present");
  back=XmlPlanImport.Import(Strip(Xml(empty)),generator);Check(back.UseRegisteredOwner&&back.UseRegisteredOrganization&&back.RegisteredOwner==""&&back.RegisteredOrganization=="","empty source distinguished from omitted field");back.RegisteredOwner="Filled later";Check(Owner(XmlMerge.Read(Xml(back))).InnerText=="Filled later","imported empty value can be edited");
  foreach(string field in new[]{"owner","organization"}){
   var p=Plan();if(field=="owner"){p.UseRegisteredOwner=true;p.RegisteredOwner=new string('中',256);}else{p.UseRegisteredOrganization=true;p.RegisteredOrganization=new string('中',256);}
   Check(Nodes(XmlMerge.Read(Xml(p))).Single().InnerText.Length==256,"256 boundary accepted "+field);
   foreach(string? bad in new string?[]{new string('中',257)," leading","trailing ","line\nbreak","tab\tname","bad\0name","bad\ufffe","bad\ud800",null}){
    if(field=="owner")p.RegisteredOwner=bad!;else p.RegisteredOrganization=bad!;
    Reject(()=>Xml(p),"invalid active value rejected "+field);
   }
   if(field=="owner")p.UseRegisteredOwner=false;else p.UseRegisteredOrganization=false;
   Check(Nodes(XmlMerge.Read(Xml(p))).Length==0,"inactive invalid text ignored "+field);
  }
  var inactive=Plan();inactive.RegisteredOwner="Unused private name";inactive.RegisteredOrganization="Unused company";
  var inactiveJson=PlanStore.Deserialize(PlanStore.Serialize(inactive,false));Check(inactiveJson.RegisteredOwner==inactive.RegisteredOwner&&inactiveJson.RegisteredOrganization==inactive.RegisteredOrganization,"JSON retains unchecked inputs for later reuse");
  var note=XmlMerge.Read(Xml(inactive)).SelectNodes("//comment()")!.Cast<XmlComment>().Single(c=>c.Value!.StartsWith("UnattendLocal:v1 ")).Value!;var payload=JsonNode.Parse(Encoding.UTF8.GetString(Convert.FromBase64String(note.Split(' ',3)[2])))!;
  Check(payload[nameof(InstallPlan.RegisteredOwner)]!.GetValue<string>()==""&&payload[nameof(InstallPlan.RegisteredOrganization)]!.GetValue<string>()=="","XML snapshot excludes unchecked personal names");
  var tampered=XmlMerge.Read(Xml(seed));Owner(tampered).InnerText="Actual XML";Check(XmlPlanImport.Import(tampered.OuterXml,generator).RegisteredOwner=="Actual XML","actual XML wins over stale snapshot");
  foreach(string pass in RegisteredInformationSettings.Passes){
   var p=Plan();p.UseRegisteredOwner=true;p.RegisteredOwner="Keep phase";p.RegisteredOwnerPass=pass;
   var d=XmlMerge.Read(Strip(Xml(p)));var parent=Owner(d).ParentNode!;var extra=d.CreateElement("FutureProperty",XmlMerge.Namespace);extra.InnerText="must survive";parent.AppendChild(extra);
   var copy=XmlPlanImport.Import(d.OuterXml,generator);copy.UseRegisteredOwner=false;var output=XmlMerge.Read(Xml(copy));Check(Nodes(output).Length==0&&output.SelectNodes("//*[local-name()='FutureProperty']")!.Cast<XmlElement>().Single().InnerText=="must survive","reset retains unknown component siblings "+pass);
   copy.UseRegisteredOwner=true;copy.RegisteredOwner="Updated";Check(Owner(XmlMerge.Read(Xml(copy))).InnerText=="Updated"&&copy.RegisteredOwnerPass==pass,"edit retains original execution phase "+pass);
  }
  var split=Plan();split.UseRegisteredOwner=true;split.UseRegisteredOrganization=true;split.RegisteredOwner="Separate";split.RegisteredOrganization="Stages";split.RegisteredOwnerPass="generalize";split.RegisteredOrganizationPass="oobeSystem";
  back=XmlPlanImport.Import(Strip(Xml(split)),generator);back.RegisteredOwner="Changed";var splitDoc=XmlMerge.Read(Xml(back));Check(Nodes(splitDoc).Single(n=>n.LocalName=="RegisteredOwner").ParentNode!.ParentNode!.Attributes!["pass"]!.Value=="generalize"&&Nodes(splitDoc).Single(n=>n.LocalName=="RegisteredOrganization").ParentNode!.ParentNode!.Attributes!["pass"]!.Value=="oobeSystem","independent fields retain different phases");
  foreach(string mutation in new[]{"attribute","nested","duplicate","cross-phase","unsupported-phase","foreign-namespace","over-limit","control","leading-space","wrong-parent","component-attribute"}){
   var d=XmlMerge.Read(native);var owner=Owner(d);
   switch(mutation){
    case "attribute":owner.SetAttribute("future","keep");break;
    case "nested":owner.AppendChild(d.CreateElement("Nested",XmlMerge.Namespace));break;
    case "duplicate":owner.ParentNode!.AppendChild(owner.CloneNode(true));break;
    case "cross-phase":d.SelectNodes("//*[local-name()='component']")!.Cast<XmlElement>().Single(c=>c.GetAttribute("name")=="Microsoft-Windows-Shell-Setup"&&c.ParentNode!.Attributes!["pass"]!.Value=="oobeSystem").AppendChild(owner.CloneNode(true));break;
    case "unsupported-phase":owner.ParentNode!.ParentNode!.Attributes!["pass"]!.Value="auditSystem";break;
    case "foreign-namespace":var foreign=d.CreateElement("RegisteredOwner","urn:future");foreign.InnerText="Future owner";owner.ParentNode!.ReplaceChild(foreign,owner);break;
    case "over-limit":owner.InnerText=new string('a',257);break;
    case "control":owner.InnerText="with\nnewline";break;
    case "leading-space":owner.InnerText=" Owner";break;
    case "wrong-parent":d.DocumentElement!.AppendChild(owner);break;
    case "component-attribute":owner.ParentNode!.Attributes!.Append(d.CreateAttribute("future")).Value="keep";break;
   }
   var copy=XmlPlanImport.Import(d.OuterXml,generator);var vm=new MainViewModel{Plan=copy};Check(copy.ImportedXml!.RegisteredInformationReadOnly&&copy.ImportedXml.Notices.Contains(RegisteredInformationSettings.ReadOnlyNotice)&&!vm.CanEditRegisteredInformation&&vm.HasReadonlyRegisteredInformation&&vm.RegisteredInformationSummary.Contains("只读"),"unknown input visibly readonly "+mutation);
   copy.UseRegisteredOwner=true;copy.RegisteredOwner="Overwrite";Reject(()=>Xml(copy),"unknown owner cannot be overwritten "+mutation);copy.UseRegisteredOwner=false;copy.UseRegisteredOrganization=true;copy.RegisteredOrganization="Overwrite";Reject(()=>Xml(copy),"unknown group cannot be overwritten through organization "+mutation);copy.UseRegisteredOrganization=false;
   Check(copy.ImportedXml.SourceXml==XmlMerge.Read(d.OuterXml).OuterXml,"source remains untouched "+mutation);
  }
  var legacy=XmlPlanImport.Import(native,generator);legacy.ComputerName="PENDINGREG";
  JsonObject OldRecord(){
   var root=JsonNode.Parse(PlanStore.Serialize(legacy,true))!.AsObject();foreach(string key in new[]{nameof(InstallPlan.UseRegisteredOwner),nameof(InstallPlan.RegisteredOwner),nameof(InstallPlan.RegisteredOwnerPass),nameof(InstallPlan.UseRegisteredOrganization),nameof(InstallPlan.RegisteredOrganization),nameof(InstallPlan.RegisteredOrganizationPass)})root.Remove(key);
   var data=root[nameof(InstallPlan.ImportedXml)]!.AsObject();data.Remove(nameof(ImportedXml.RegisteredInformationImportVersion));data.Remove(nameof(ImportedXml.RegisteredInformationReadOnly));var baseline=XmlMerge.Read(data[nameof(ImportedXml.BaselineXml)]!.GetValue<string>());foreach(var node in Nodes(baseline))node.ParentNode!.RemoveChild(node);data[nameof(ImportedXml.BaselineXml)]=baseline.OuterXml;return root;
  }
  var oldJson=OldRecord();var migrated=PlanStore.Deserialize(oldJson.ToJsonString());Check(migrated.RegisteredOwner==seed.RegisteredOwner&&migrated.RegisteredOrganization==seed.RegisteredOrganization&&migrated.ComputerName=="PENDINGREG"&&migrated.ImportedXml!.Blockers.Length==0,"legacy migration restores fields while preserving pending edits");migrated.RegisteredOwner="Migrated";Check(XmlPlanImport.Import(Xml(migrated),generator).RegisteredOwner=="Migrated","migrated baseline supports editing");
  oldJson=OldRecord();oldJson[nameof(InstallPlan.Architecture)]="arm64";migrated=PlanStore.Deserialize(oldJson.ToJsonString());Check(migrated.Architecture=="arm64"&&migrated.ComputerName=="PENDINGREG"&&migrated.UseRegisteredOwner,"migration preserves pending architecture");
  oldJson=OldRecord();oldJson[nameof(InstallPlan.UseRegisteredOwner)]=false;migrated=PlanStore.Deserialize(oldJson.ToJsonString());Check(migrated.ImportedXml!.Blockers.Any(b=>b.Contains("注册信息编辑基准"))&&migrated.ComputerName=="PENDINGREG","ambiguous migration blocks without discarding edits");Reject(()=>Xml(migrated),"ambiguous migration export blocked");
  var noOriginal=PlanStore.Deserialize(PlanStore.Serialize(legacy,false));Check(noOriginal.RegisteredOwner==seed.RegisteredOwner&&noOriginal.ImportedXml!.OriginalOmitted,"omitting original XML retains displayed names");Reject(()=>Xml(noOriginal),"omitted original cannot be replaced with defaults");
  var recommendation=Plan();recommendation.UseRegisteredOwner=true;recommendation.RegisteredOwner="User choice";Recommendations.Apply(recommendation,"wired");Check(recommendation.UseRegisteredOwner&&recommendation.RegisteredOwner=="User choice","recommendation preserves registration choice");
  var view=new MainViewModel{Plan=Plan()};view.Plan.ConfirmWipeDisk=true;view.Plan.UseRegisteredOwner=true;view.Plan.RegisteredOwner="Different from account";Check(view.IsRegisteredOwner&&!view.IsRegisteredOrganization&&view.RegisteredInformationSummary.Contains("Different from account")&&view.Plan.ConfirmWipeDisk,"view state refreshes without unrelated disk-consent linkage");
  Check(view.Plan.Users[0].Name=="User"&&view.Plan.ComputerName==""&&!view.Plan.UseBuiltinAdministrator,"registration does not rename or create accounts");
  Check(view.HelpTips.ContainsKey("registeredowner")&&view.HelpTips.ContainsKey("registeredorganization")&&SettingsSearchCatalog.Search("RegisteredOwner").Any()&&SettingsSearchCatalog.Search("组织名称").Any(),"help and search expose both fields");
  Check(Recommendations.Summary(view.Plan).Contains("Windows 注册信息"),"summary contains actual registration selection");
  return count;
 }
}
