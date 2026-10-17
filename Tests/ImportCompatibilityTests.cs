using System.Text;
using System.Text.RegularExpressions;
using System.Text.Json.Nodes;
using System.Xml;
using UnattendLocal.Core;
using UnattendLocal.Desktop;

// All payloads are generated here from public templates and invented test data.
static class ImportCompatibilityTests {
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
  int count=0;void Check(bool ok,string name){if(!ok)throw new Exception(name);count++;Console.WriteLine("PASS V1.0.1 "+name);}
  string Xml(InstallPlan p)=>Encoding.UTF8.GetString(g.Generate(p));
  XmlElement FileNode(XmlDocument d,string name)=>d.SelectNodes("//*[local-name()='File']")!.Cast<XmlElement>().Single(f=>f.GetAttribute("path").EndsWith("\\"+name));
  bool SameLayout(string a,string b){var x=new XmlDocument();x.LoadXml(a);var y=new XmlDocument();y.LoadXml(b);return XmlMerge.Equal(x.DocumentElement,y.DocumentElement);}
  InstallPlan Seed(){var p=InstallPlan.New();p.Users[0].Name="ImportFixtureUser";p.Users[0].Password="SyntheticImport1!";p.Users[0].DisplayName="合成测试用户";p.Keyboard="00000804";p.CompactOsMode=CompactOsSettings.Enabled;p.TaskbarSearch="隐藏";p.TaskbarIconsMode="移除全部固定图标";p.StartTilesMode=ShellSettings.CustomXml;p.StartTilesXml=ShellSettings.TilesExample;p.StartPinsMode=ShellSettings.CustomJson;p.StartPinsJson=ShellSettings.PinsExample;p.DisableWpbt=true;p.DisableSystemRestore=true;p.DisableSac=true;p.DisableSmartScreen=true;p.RemovedApps=new(AppRemovalSettings.Items.Select(i=>i.Id));p.Scripts.Add(new(){Phase="System",Type="Ps1",Content="Write-Output 'Synthetic only';\n$path = 'C:\\\\Example\\\\KeepDouble';"});p.Scripts.Add(new(){Phase="DefaultUser",Type="Reg",Content="Windows Registry Editor Version 5.00\n\n[HKEY_USERS\\DefaultUser\\Software\\ImportFixture]\n\"Path\"=\"C:\\\\Example\\\\KeepDouble\""});return p;}
  string Fixture(InstallPlan p,bool legacy,bool doubled){
   var d=XmlMerge.Read(legacy?LegacyRemovalFixture.FromCurrent(Xml(p)):Xml(p));
   foreach(var c in d.SelectNodes("//comment()")!.Cast<XmlComment>().ToArray())c.ParentNode!.RemoveChild(c);
   foreach(XmlElement f in d.SelectNodes("//*[local-name()='File']")!){
    string name=f.GetAttribute("path").Split('\\').Last();
    if(name is "Specialize.ps1" or "DefaultUser.ps1" or "FirstLogon.ps1" or "UserOnce.ps1")f.InnerText=f.InnerText.Replace(CompactLog,DetailedLog,StringComparison.Ordinal);
    if(doubled){f.SetAttribute("path",f.GetAttribute("path").Replace("\\","\\\\"));if(!name.StartsWith("unattend-"))f.InnerText=Regex.Replace(f.InnerText,"['\"](?<p>C:\\\\[^'\"\\r\\n]*)['\"]",m=>m.Value.Replace(m.Groups["p"].Value,m.Groups["p"].Value.Replace("\\","\\\\"),StringComparison.Ordinal));}
   }
   if(doubled)foreach(XmlElement e in d.SelectNodes("//*[local-name()='RunSynchronousCommand']/*[local-name()='Path'] | //*[local-name()='SynchronousCommand']/*[local-name()='CommandLine']")!)e.InnerText=Regex.Replace(e.InnerText,"['\"](?<p>C:\\\\[^'\"\\r\\n]*)['\"]",m=>m.Value.Replace(m.Groups["p"].Value,m.Groups["p"].Value.Replace("\\","\\\\"),StringComparison.Ordinal));
   return d.OuterXml;
  }
  foreach(var arch in new[]{"amd64","x86","arm64"})foreach(bool legacy in new[]{false,true})foreach(bool doubled in new[]{false,true}){
   var seed=Seed();seed.Architecture=arch;string source=Fixture(seed,legacy,doubled);var p=XmlPlanImport.Import(source,g);string label=arch+"/"+legacy+"/"+doubled;
   Check(p.ImageLanguage==seed.ImageLanguage&&p.Locale==seed.Locale&&p.Keyboard==seed.Keyboard,"actual language and keyboard restored "+label);
   Check(p.EditionMode==seed.EditionMode&&p.EditionId==seed.EditionId&&g.GenericInstallationKey(p.EditionId).Length==29,"generic installation key and SKU restored "+label);
   Check(p.Users.Count==1&&p.Users[0].Name==seed.Users[0].Name&&p.Users[0].Password==seed.Users[0].Password&&p.Users[0].DisplayName==seed.Users[0].DisplayName&&p.Users[0].Administrator,"account and encoded password restored "+label);
   Check(p.CompactOsMode==CompactOsSettings.Enabled&&p.TaskbarSearch=="隐藏"&&p.TaskbarIconsMode=="移除全部固定图标","native CompactOS/search/taskbar restored "+label);
   Check(p.StartTilesMode==ShellSettings.CustomXml&&SameLayout(p.StartTilesXml,seed.StartTilesXml)&&JsonNode.DeepEquals(JsonNode.Parse(p.StartPinsJson),JsonNode.Parse(seed.StartPinsJson)),"start layouts restored from payload "+label);
   Check(p.RemovedApps.Count==60&&p.ImportedXml is {AppRemovalReadOnly:false},"all actual removals recognized without URL comment "+label);
   Check(p.DisableWpbt&&p.DisableSystemRestore&&p.DisableSac&&p.DisableSmartScreen,"other feature pipelines recognized "+label);
   Check(p.Scripts.Count==2&&p.Scripts.Select(s=>s.Phase).SequenceEqual(new[]{"System","DefaultUser"})&&p.Scripts.Zip(seed.Scripts).All(t=>t.First.Content==t.Second.Content),"custom commands/phases and escaped payloads preserved "+label);
   Check(p.ImportedXml!.Blockers.Length==0&&XmlMerge.Equal(XmlMerge.Read(source).DocumentElement,XmlMerge.Read(Xml(p)).DocumentElement),"unchanged import/export preserves original XML "+label);
   p=PlanStore.Deserialize(PlanStore.Serialize(p,true));Check(p.Users[0].Password==seed.Users[0].Password&&p.RemovedApps.Count==60&&p.Scripts.Count==2&&XmlMerge.Equal(XmlMerge.Read(source).DocumentElement,XmlMerge.Read(Xml(p)).DocumentElement),"saved plan reload preserves imported settings and source "+label);
  }
  string original=Fixture(Seed(),true,true);
  foreach(string edit in new[]{"computer","account","compact","search","script","remove-app","disable-feature"}){
   var p=XmlPlanImport.Import(original,g);
   switch(edit){case "computer":p.ComputerName="IMPORTEDFIX";break;case "account":p.Users[0].DisplayName="Edited fixture";break;case "compact":p.CompactOsMode=CompactOsSettings.Disabled;break;case "search":p.TaskbarSearch="仅图标";break;case "script":p.Scripts[0].Content="Write-Output 'Edited synthetic script';";break;case "remove-app":p.RemovedApps.Remove("RemoveCalculator");break;case "disable-feature":p.DisableSac=false;break;}
   var output=Xml(p);var again=XmlPlanImport.Import(output,g);
   Check(edit switch{"computer"=>again.ComputerName==p.ComputerName,"account"=>again.Users[0].DisplayName==p.Users[0].DisplayName,"compact"=>again.CompactOsMode==p.CompactOsMode,"search"=>again.TaskbarSearch==p.TaskbarSearch,"script"=>again.Scripts[0].Content==p.Scripts[0].Content,"remove-app"=>!again.RemovedApps.Contains("RemoveCalculator")&&again.RemovedApps.Count==59,_=>!again.DisableSac},"recognized edit survives export/reimport: "+edit);
   Check(again.Scripts[1].Content==Seed().Scripts[1].Content&&again.Users[0].Password==Seed().Users[0].Password,"unrelated credential/REG payload survives: "+edit);
  }
  var unknown=XmlMerge.Read(original);var stage=FileNode(unknown,"Specialize.ps1");stage.InnerText=stage.InnerText.Replace("$max = 100;","$max = 101;");var ro=XmlPlanImport.Import(unknown.OuterXml,g);
  Check(ro.ImportedXml is {AppRemovalReadOnly:true},"altered wrapper stays read-only");ro.ComputerName="KEEPUNKNOWN";var unchanged=XmlMerge.Read(Xml(ro));Check(FileNode(unchanged,"Specialize.ps1").InnerText==stage.InnerText,"unknown wrapper retained under unrelated edit");
  var vm=new MainViewModel();int prompts=0,notifications=0;vm.PropertyChanged+=(_,e)=>{if(e.PropertyName==nameof(MainViewModel.Plan)){notifications++;if(!vm.IsLoadingPlan)prompts++;}};
  Check(vm.ImportXml(original,()=>true)&&notifications==1&&prompts==0&&!vm.IsLoadingPlan,"plan notification runs inside load guard and does not request reset");
  Check(vm.Plan.Users.Count==1&&vm.SelectedUser==vm.Plan.Users[0]&&vm.SelectedScript==vm.Plan.Scripts[0]&&vm.AppRemovalChoices.Count(c=>c.IsSelected)==60&&vm.IsGenericEdition&&vm.SelectedEditionKey.Length==29,"loaded UI selection/app checks/key summary match imported plan");
  var current=vm.Plan;Check(!vm.ImportXml(original,()=>false)&&ReferenceEquals(vm.Plan,current)&&!vm.IsLoadingPlan,"cancel import retains current object and values");
  try{vm.ImportXml("<invalid/>",()=>true);Check(false,"invalid XML");}catch(ArgumentException){Check(ReferenceEquals(vm.Plan,current)&&!vm.IsLoadingPlan,"failed import retains current object and guard state");}
  var next=Seed();vm.Plan=next;Check(ReferenceEquals(vm.Plan,next)&&notifications==2&&prompts==0&&vm.SelectedUser==next.Users[0],"opening another plan uses the same guarded replacement");
  return count;
 }
}
