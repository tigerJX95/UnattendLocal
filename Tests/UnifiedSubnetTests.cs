using System.Text;
using System.Xml;
using UnattendLocal.Core;
using UnattendLocal.Desktop;

static class UnifiedSubnetTests
{
    public static int Run(GeneratorService generator)
    {
        int count=0;
        void Check(bool ok,string name){if(!ok)throw new Exception(name);count++;Console.WriteLine("PASS unified subnet "+name);}
        InstallPlan Plan(){var p=InstallPlan.New();p.Users[0].Password="UnifiedTest1!";p.UseStaticIPv4=true;p.StaticIPv4Mac="02-AB-00-00-00-10";p.StaticIPv4Address="192.168.100.20";p.StaticIPv4Gateway="";p.StaticIPv4Dns="";return p;}
        string Xml(InstallPlan p)=>Encoding.UTF8.GetString(generator.Generate(p));
        void Reject(InstallPlan p,string label){try{_=generator.Generate(p);}catch(ArgumentException){Check(true,label);return;}throw new Exception("Expected rejection: "+label);}
        foreach(var (prefix,mask) in new[]{(0,"0.0.0.0"),(1,"128.0.0.0"),(8,"255.0.0.0"),(12,"255.240.0.0"),(16,"255.255.0.0"),(24,"255.255.255.0"),(28,"255.255.255.240"),(31,"255.255.255.254"),(32,"255.255.255.255")}){
            Check(StaticIPv4Settings.PrefixFromInput("/"+prefix)==prefix,"slash prefix "+prefix);
            Check(StaticIPv4Settings.PrefixFromInput(mask)==prefix,"equivalent dotted mask "+prefix);
        }
        foreach(string invalid in new[]{"","/","24","28","/33","/-1","/+24","/024","/2.4","/ 24","/24/","/24\nX","255.0.255.0","255.255.255","256.255.255.0","255.255.255.00","192.168.1.20/24"}){
            var vm=new MainViewModel{Plan=Plan()};vm.StaticIPv4SubnetInput=invalid;
            Check(vm.StaticIPv4SubnetInput==invalid,"unfinished or invalid input retained "+invalid);
            Reject(vm.Plan,"invalid text never falls back to last prefix "+invalid);
            var restored=PlanStore.Deserialize(PlanStore.Serialize(vm.Plan,true));
            Check(new MainViewModel{Plan=restored}.StaticIPv4SubnetInput==invalid,"JSON retains incomplete input "+invalid);
            Reject(restored,"reopened incomplete input stays blocked "+invalid);
            vm.StaticIPv4SubnetInput="/24";Check(Xml(vm.Plan).Contains("192.168.100.20/24"),"invalid input remains directly correctable "+invalid);
        }
        foreach(string arch in new[]{"amd64","arm64","x86"})foreach(string input in new[]{"/24","255.255.255.0","/28","255.255.255.240","/31","255.255.255.254","/32","255.255.255.255"}){
            var vm=new MainViewModel{Plan=Plan()};vm.Plan.Architecture=arch;vm.StaticIPv4SubnetInput=input;
            int expected=input.Contains("28")||input.EndsWith("240")?28:input.EndsWith("31")||input.EndsWith("254")?31:input.EndsWith("32")||input.EndsWith("255.255.255.255")?32:24;
            string xml=Xml(vm.Plan);
            Check(xml.Contains("192.168.100.20/"+expected)&&vm.Plan.StaticIPv4Prefix==expected,"active prefix updates export and compatibility value "+arch+input);
            Check(vm.StaticIPv4SubnetInput==input,"typed format not rewritten during validation "+arch+input);
            var restored=PlanStore.Deserialize(PlanStore.Serialize(vm.Plan,true));
            Check(new MainViewModel{Plan=restored}.StaticIPv4SubnetInput==input&&StaticIPv4Settings.Read(restored).Prefix==expected,"JSON roundtrip "+arch+input);
            var imported=XmlPlanImport.Import(xml,generator);
            Check(!imported.ImportedXml!.StaticIPv4ReadOnly&&StaticIPv4Settings.Read(imported).Prefix==expected,"XML import remains editable "+arch+input);
            Check(new MainViewModel{Plan=imported}.StaticIPv4SubnetInput==input,"snapshot preserves chosen format "+arch+input);
            Check(Xml(imported).Contains("192.168.100.20/"+expected),"imported XML re-export "+arch+input);
        }
        var legacy=Plan();legacy.UseStaticIPv4SubnetMask=false;legacy.StaticIPv4Prefix=28;legacy.StaticIPv4SubnetMask="invalid inactive mask";
        string before=PlanStore.Serialize(legacy,true);var oldVm=new MainViewModel{Plan=legacy};
        Check(oldVm.StaticIPv4SubnetInput=="/28"&&PlanStore.Serialize(legacy,true)==before,"opening old prefix mode ignores inactive text without changing plan");
        legacy.UseStaticIPv4SubnetMask=true;legacy.StaticIPv4SubnetMask="255.255.255.0";legacy.StaticIPv4Prefix=0;
        Check(oldVm.StaticIPv4SubnetInput=="255.255.255.0"&&Xml(legacy).Contains("192.168.100.20/24"),"old active mask wins over unused old prefix");
        foreach(string zero in new[]{"/0","0.0.0.0"}){
            oldVm.StaticIPv4SubnetInput=zero;Check(oldVm.HasStaticIPDeploymentWarning&&oldVm.StaticIPv4SubnetInput==zero,"zero warning retains format "+zero);Reject(oldVm.Plan,"zero export blocked "+zero);
            oldVm.StaticIPv4SubnetInput="255.255.255.240";Check(!oldVm.HasStaticIPDeploymentWarning&&oldVm.StaticIPv4SubnetPreview.Contains("192.168.100.16/28"),"correction immediately updates warning and preview "+zero);
        }
        var detached=Plan();detached.UseStaticIPv4=false;var protectedVm=new MainViewModel{Plan=detached};before=PlanStore.Serialize(detached,true);protectedVm.StaticIPv4SubnetInput="/25";
        Check(PlanStore.Serialize(detached,true)==before,"disabled input cannot change values");
        detached.UseStaticIPv4=true;detached.ImportedXml=new ImportedXml{StaticIPv4ReadOnly=true};before=PlanStore.Serialize(detached,true);protectedVm.StaticIPv4SubnetInput="/25";
        Check(PlanStore.Serialize(detached,true)==before,"read-only input cannot change values");
        foreach(string query in new[]{"子网掩码","前缀长度","StaticIPv4Prefix","subnet mask","CIDR","/24"})
            Check(SettingsSearchCatalog.Search(query).Count(r=>r.Target=="StaticIPv4SubnetEditor")==1,"single destination for both input aliases "+query);
        Check(!SettingsSearchCatalog.All.Any(r=>r.Target is "StaticIPv4PrefixEditor" or "StaticIPv4MaskModeEditor" or "StaticIPv4SubnetMaskEditor"),"removed controls have no stale search destinations");
        return count;
    }
}
