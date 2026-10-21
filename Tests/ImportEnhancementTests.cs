using System.Text;
using System.Text.Json.Nodes;
using System.Xml;
using UnattendLocal.Core;
using UnattendLocal.Desktop;

// Synthetic fixtures only; imported scripts are never executed.
static class ImportEnhancementTests {
 const string DetailedLog="""
      $str = $script.ToString().Trim() -replace '\s+', ' ';
      $max = 100;
      if( $str.Length -le $max ) {
        $str;
      } else {
        $str.Substring( 0, $max - 1 ) + '…';
      }
""";
 const string CompactLog="      $script.ToString().Trim() -replace '\\s+', ' ' -replace '^(.{99})(.+)$', '$1…';";
 public static int Run(GeneratorService g){
  int count=0;
  void Check(bool ok,string name){if(!ok)throw new Exception(name);count++;Console.WriteLine("PASS V1.12 "+name);}
  void Reject(Action action,string name){try{action();}catch(Exception e)when(e is ArgumentException or System.Text.Json.JsonException){Check(true,name);return;}throw new Exception("Expected rejection: "+name);}
  InstallPlan Seed(){var p=InstallPlan.New();p.Users[0].Password="SyntheticOnly112!";p.AllowUnsupportedUpgrade=true;p.RemovedApps=new(["RemoveOneDrive","RemoveCalculator","RemoveInternetExplorer","RemoveMediaFeatures"]);p.DisableFastStartup=true;p.DisableSac=true;p.Scripts.Add(new(){Phase="UserOnce",Type="Ps1",Content="Write-Output 'Synthetic user-once fixture112';"});return p;}
  string Output(InstallPlan p)=>Encoding.UTF8.GetString(g.Generate(p));
  XmlElement Stage(XmlDocument d,string name="Specialize.ps1")=>d.SelectNodes("//*[local-name()='File']")!.Cast<XmlElement>().Single(f=>f.GetAttribute("path").EndsWith("\\"+name,StringComparison.Ordinal));
  string Source(bool legacy,bool detailed){
   var p=Seed();var doc=XmlMerge.Read(legacy?LegacyRemovalFixture.FromCurrent(Output(p)):Output(p));
   foreach(XmlComment c in doc.SelectNodes("//comment()")!.Cast<XmlComment>().ToArray())c.ParentNode!.RemoveChild(c);
   if(detailed)Stage(doc).InnerText=Stage(doc).InnerText.Replace(CompactLog,DetailedLog,StringComparison.Ordinal);
   return doc.OuterXml;
  }
  bool Same(string a,string b)=>XmlMerge.Equal(XmlMerge.Read(a).DocumentElement,XmlMerge.Read(b).DocumentElement);
  foreach(bool legacy in new[]{false,true})foreach(bool detailed in new[]{false,true}){
   string source=Source(legacy,detailed);var p=XmlPlanImport.Import(source,g);string label=legacy+"/"+detailed;
   Check(p.AllowUnsupportedUpgrade&&!p.ImportedXml!.LegacyUpgradeReadOnly,"legacy MoSetup restored "+label);
   Check(!p.BypassTPMCheck&&!p.BypassSecureBootCheck&&!p.BypassRAMCheck,"upgrade compatibility independent of PE checks "+label);
   Check(p.RemovedApps.Order().SequenceEqual(Seed().RemovedApps.Order())&&p.DisableSac&&p.DisableFastStartup,"equivalent functional settings mapped "+label);
   Check(!p.ImportedXml!.Notices.Any(n=>n.Contains("Specialize.ps1")),"equivalent scripts do not imply missing functions "+label);
   Check(Same(source,Output(p)),"untouched export preserves original XML and logging "+label);
   var saved=PlanStore.Deserialize(PlanStore.Serialize(p,true));Check(saved.AllowUnsupportedUpgrade&&Same(source,Output(saved)),"JSON roundtrip keeps source and upgrade setting "+label);
   p.AllowUnsupportedUpgrade=false;var output=Output(p);Check(!Stage(XmlMerge.Read(output)).InnerText.Contains(LegacyUpgradeSettings.Marker)&&!XmlPlanImport.Import(output,g).AllowUnsupportedUpgrade,"disabling changes exact source command "+label);
   Check(Stage(XmlMerge.Read(output)).InnerText.Contains(detailed?"$max = 100;":"^(.{99})(.+)$"),"upgrade edit retains source logging "+label);
   p=XmlPlanImport.Import(source,g);p.RemovedApps.Remove("RemoveCalculator");output=Output(p);var edited=XmlPlanImport.Import(output,g);
   Check(edited.AllowUnsupportedUpgrade&&!edited.RemovedApps.Contains("RemoveCalculator")&&edited.RemovedApps.Contains("RemoveOneDrive"),"app edit converts only recognized legacy removal "+label);
   p=XmlPlanImport.Import(source,g);p.DisableFastStartup=false;output=Output(p);Check(XmlPlanImport.Import(output,g).AllowUnsupportedUpgrade&&!XmlPlanImport.Import(output,g).DisableFastStartup,"unrelated setting edit retains compatibility command "+label);
  }
  {
   string source=Source(true,true);var p=XmlPlanImport.Import(source,g);var old=JsonNode.Parse(PlanStore.Serialize(p,true))!.AsObject();old.Remove(nameof(p.AllowUnsupportedUpgrade));
   var before=Seed();before.AllowUnsupportedUpgrade=false;old["ImportedXml"]!["BaselineXml"]=Output(before);old["ImportedXml"]!["Notices"]=new JsonArray("原文保留，未完全还原到界面：Specialize.ps1","retain-custom-notice112");old["ComputerName"]="PENDING112";
   var loaded=PlanStore.Deserialize(old.ToJsonString());Check(loaded.AllowUnsupportedUpgrade&&loaded.ComputerName=="PENDING112","old JSON baseline migration preserves pending edits");
   Check(loaded.ImportedXml!.Notices.Contains("retain-custom-notice112")&&!loaded.ImportedXml.Notices.Any(n=>n.StartsWith("原文保留，未完全还原")),"stale generic notice refreshed without removing unrelated notices");
   loaded.AllowUnsupportedUpgrade=false;Check(!Stage(XmlMerge.Read(Output(loaded))).InnerText.Contains(LegacyUpgradeSettings.Marker),"migrated baseline supports disabling command");
  }
  foreach(string mutation in new[]{"value","phase","duplicate","wrapper","invocation","custom-payload"}){
   var doc=XmlMerge.Read(Source(false,false));var stage=Stage(doc);string text=stage.InnerText;
   switch(mutation){
    case "value":stage.InnerText=text.Replace("/v "+LegacyUpgradeSettings.Marker+" /t REG_DWORD /d 1","/v "+LegacyUpgradeSettings.Marker+" /t REG_DWORD /d 0");break;
    case "phase":stage.InnerText=text.Replace("\t{\n\t\t"+LegacyUpgradeSettings.Command+"\n\t};\n","");var first=Stage(doc,"FirstLogon.ps1");first.InnerText=first.InnerText.Replace("$scripts = @(\n","$scripts = @(\n\t{\n\t\t"+LegacyUpgradeSettings.Command+"\n\t};\n");break;
    case "duplicate":stage.InnerText=text.Replace(LegacyUpgradeSettings.Command,LegacyUpgradeSettings.Command+"\n\t\t"+LegacyUpgradeSettings.Command);break;
    case "wrapper":stage.InnerText=text.Replace("& $script;","if( $false ) { & $script; }");break;
    case "invocation":foreach(XmlElement e in doc.SelectNodes("//*[local-name()='Path']")!)if(e.InnerText.EndsWith("-File \"C:\\Windows\\Setup\\Scripts\\Specialize.ps1\""))e.InnerText="cmd.exe /c echo not-invoked112";break;
    case "custom-payload":var f=doc.CreateElement("File",stage.NamespaceURI);f.SetAttribute("path",@"C:\Windows\Setup\Scripts\unattend-ff.ps1");f.InnerText="Write-Output '"+LegacyUpgradeSettings.Marker+"';";stage.ParentNode!.AppendChild(f);break;
   }
   var p=XmlPlanImport.Import(doc.OuterXml,g);
   Check(!p.AllowUnsupportedUpgrade&&p.ImportedXml!.LegacyUpgradeReadOnly,"different semantics/pipeline is not mapped: "+mutation);
   Check(Same(doc.OuterXml,Output(p)),"unrecognized original retained: "+mutation);
   p.AllowUnsupportedUpgrade=true;Reject(()=>Output(p),"ambiguous command cannot be overwritten: "+mutation);
  }
  foreach(string stageName in new[]{"Specialize.ps1","DefaultUser.ps1","FirstLogon.ps1","UserOnce.ps1"}){
   var doc=XmlMerge.Read(Source(false,true));var stage=Stage(doc,stageName);stage.InnerText=stage.InnerText.Replace("$scripts = @(\n","$scripts = @(\n\t{\n\t\tWrite-Output 'private-token-112';\n\t};\n");
   var p=XmlPlanImport.Import(doc.OuterXml,g);var notes=p.ImportedXml!.Notices;
   Check(notes.Any(n=>n.Contains(stageName)&&n.Contains("命令块"))&&notes.All(n=>!n.Contains("private-token-112")),"specific difference location without leaking command values: "+stageName);
   Check(Same(doc.OuterXml,Output(p)),"unknown command retained intact: "+stageName);
  }
  var vm=new MainViewModel();var current=vm.Plan;vm.Preview="preserve-preview112";int confirms=0;
  Reject(()=>vm.ImportJson("{\"pinnedList\":[]}",()=>{confirms++;return true;}),"start menu JSON rejected as plan");
  Check(confirms==0&&ReferenceEquals(current,vm.Plan)&&vm.Preview=="preserve-preview112","invalid JSON never asks to replace or changes current plan");
  foreach(string json in new[]{"{}","[]","null","{\"SchemaVersion\":2}","{\"SchemaVersion\":\"1\"}","{\"SchemaVersion\":1,\"SchemaVersion\":1}","{\"SchemaVersion\":1,\"Users\":null}"})Reject(()=>vm.ImportJson(json,()=>true),"invalid schema rejected: "+json);
  foreach(string field in new[]{"ComputerName","StaticIPv4Mac","AccentColorHex"}){
   Reject(()=>vm.ImportJson("{\"SchemaVersion\":1,\""+field+"\":null}",()=>{confirms++;return true;}),"null string rejected before replacing: "+field);
   Check(ReferenceEquals(current,vm.Plan)&&confirms==0,"null value cannot partially load: "+field);
  }
  string valid=PlanStore.Serialize(Seed(),false);
  Check(!vm.ImportJson(valid,()=>false)&&ReferenceEquals(current,vm.Plan)&&vm.Preview=="preserve-preview112"&&vm.Status.Contains("已取消"),"cancel JSON leaves complete current state");
  Check(vm.ImportJson(valid,()=>true)&&!ReferenceEquals(current,vm.Plan)&&vm.Plan.Users[0].NeedsPassword&&vm.Plan.AllowUnsupportedUpgrade&&!vm.IsLoadingPlan,"JSON loads and keeps omitted-credential reminders");
  current=vm.Plan;Reject(()=>vm.ImportXml("<WLANProfile/>",()=>{confirms++;return true;}),"wrong XML type rejected");Check(ReferenceEquals(current,vm.Plan)&&confirms==0,"wrong XML leaves state and does not confirm");
  string directory=Path.Combine(Path.GetTempPath(),"unattend112-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(directory);
  try{
   string xml=Path.Combine(directory,"answer.XML"),json=Path.Combine(directory,"plan.Json");File.WriteAllText(xml,Source(true,true),Encoding.Unicode);File.WriteAllText(json,valid,new UTF8Encoding(true));
   Check(PlanFileImport.Kind(xml)==PlanFileKind.AnswerXml&&PlanFileImport.Kind(json)==PlanFileKind.JsonPlan,"extensions recognized without case sensitivity");
   Check(PlanFileImport.SingleFile([xml])==xml&&XmlPlanImport.Import(PlanFileImport.Read(xml),g).AllowUnsupportedUpgrade,"UTF16 XML BOM supported");
   Check(PlanStore.Deserialize(PlanFileImport.Read(json)).Users[0].NeedsPassword,"UTF8 JSON BOM supported");
   Reject(()=>PlanFileImport.SingleFile([]),"empty file drop rejected");Reject(()=>PlanFileImport.SingleFile([xml,json]),"multiple files rejected atomically");Reject(()=>PlanFileImport.SingleFile([directory]),"folder rejected");Reject(()=>PlanFileImport.SingleFile([Path.Combine(directory,"missing.xml")]),"missing file rejected");Reject(()=>PlanFileImport.Kind("script.ps1"),"script file not interpreted as configuration");
   foreach(var pair in new[]{(xml,PlanFileImport.MaxXmlBytes),(json,PlanFileImport.MaxJsonBytes)}){using(var f=new FileStream(pair.Item1,FileMode.Create)){f.SetLength(pair.Item2+1);}Reject(()=>PlanFileImport.Read(pair.Item1),"oversized file rejected before reading "+Path.GetExtension(pair.Item1));}
  }finally{Directory.Delete(directory,true);}
  Check(SettingsSearchCatalog.All.Any(x=>x.Target=="LegacyUpgradeCheck"&&x.Aliases.Contains(LegacyUpgradeSettings.Marker)),"compatibility setting searchable");
  return count;
 }
}
