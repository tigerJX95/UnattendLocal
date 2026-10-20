using System.Text;
using System.Text.Json.Nodes;
using System.Xml;
using UnattendLocal.Core;
using UnattendLocal.Desktop;

static class DualStackNetworkTests
{
    const string Wcm="http://schemas.microsoft.com/WMIConfig/2002/State";
    public static int Run(GeneratorService generator)
    {
        int count=0;void Check(bool ok,string text){if(!ok)throw new Exception(text);count++;Console.WriteLine("PASS V1.9 "+text);}
        void Reject(Action action,string label){try{action();}catch(ArgumentException){Check(true,label);return;}throw new Exception("Expected rejection: "+label);}
        InstallPlan Plan(){var p=InstallPlan.New();p.Users[0].Password="DualStackTest1!";p.StaticIPv4Mac="02-AB-00-00-00-10";p.StaticIPv4Address="172.16.20.30";p.StaticIPv4Prefix=16;p.StaticIPv4Gateway="172.16.0.1";p.StaticIPv4Dns="192.0.2.53,192.0.2.54";p.UseStaticIPv6=true;p.StaticIPv6Address="2001:db8:a::20";p.StaticIPv6Gateway="fe80::1";p.StaticIPv6Dns="2001:db8:a::53,2001:db8:a::54";return p;}
        string Xml(InstallPlan p)=>Encoding.UTF8.GetString(generator.Generate(p));
        XmlNamespaceManager Ns(XmlDocument d){var n=new XmlNamespaceManager(d.NameTable);n.AddNamespace("u",XmlMerge.Namespace);return n;}
        XmlElement One(XmlDocument d,string path)=>(XmlElement)d.SelectSingleNode(path,Ns(d))!;
        XmlElement Tcp(XmlDocument d)=>One(d,"//u:component[@name='Microsoft-Windows-TCPIP']");
        XmlElement Dns(XmlDocument d)=>One(d,"//u:component[@name='Microsoft-Windows-DNS-Client']");
        XmlElement[] Network(XmlDocument d)=>d.SelectNodes("//u:component[@name='Microsoft-Windows-TCPIP' or @name='Microsoft-Windows-DNS-Client']",Ns(d))!.Cast<XmlElement>().ToArray();
        string Strip(string xml){var d=XmlMerge.Read(xml);foreach(XmlComment c in d.SelectNodes("//comment()")!.Cast<XmlComment>().Where(c=>c.Value?.StartsWith("UnattendLocal:v1 ")==true).ToArray())c.ParentNode!.RemoveChild(c);return d.OuterXml;}
        bool Same(XmlDocument a,XmlDocument b)=>Network(a).Length==Network(b).Length&&Network(a).All(n=>Network(b).Any(m=>XmlMerge.Equal(n,m)));
        Check(!InstallPlan.New().UseStaticIPv6&&!InstallPlan.NewCustom().UseStaticIPv6,"IPv6 is opt-in in both starting modes");
        foreach(string arch in new[]{"amd64","arm64","x86"})foreach(int mode in new[]{4,6,46})foreach(bool gateway in new[]{false,true})foreach(bool dns in new[]{false,true}) {
            var p=Plan();p.Architecture=arch;p.UseStaticIPv4=mode!=6;p.UseStaticIPv6=mode!=4;
            if(!gateway)p.StaticIPv4Gateway=p.StaticIPv6Gateway="";
            if(!dns)p.StaticIPv4Dns=p.StaticIPv6Dns="";
            string xml=Xml(p);var d=XmlMerge.Read(xml);var ns=Ns(d);var iface=One(d,"//u:component[@name='Microsoft-Windows-TCPIP']/u:Interfaces/u:Interface");
            string label=arch+"/"+mode+"/"+gateway+"/"+dns;
            var names=iface.ChildNodes.OfType<XmlElement>().Select(e=>e.LocalName).ToArray();
            var expected=new List<string>();if(mode!=6)expected.Add("Ipv4Settings");if(mode!=4)expected.Add("Ipv6Settings");expected.AddRange(["Identifier","UnicastIpAddresses"]);if(gateway)expected.Add("Routes");
            Check(names.SequenceEqual(expected),"native interface order "+label);
            Check(iface.SelectNodes("u:Ipv4Settings/u:DhcpEnabled | u:Ipv6Settings/u:DhcpEnabled | u:Ipv4Settings/u:RouterDiscoveryEnabled | u:Ipv6Settings/u:RouterDiscoveryEnabled",ns)!.Cast<XmlElement>().All(e=>e.InnerText=="false"),"only explicit static protocols configured "+label);
            Check(d.SelectNodes("//u:UnicastIpAddresses/u:IpAddress",ns)!.Count==(mode==46?2:1)&&Network(d).Length==(dns?2:1),"one adapter and correct address/DNS count "+label);
            Check(d.SelectNodes("//u:settings[@pass='windowsPE']/u:component[@name='Microsoft-Windows-TCPIP']",ns)!.Count==0,"target-only configuration "+label);
            if(gateway)Check(iface.SelectNodes("u:Routes/u:Route",ns)!.Cast<XmlElement>().All(e=>e.ChildNodes.OfType<XmlElement>().Select(c=>c.LocalName).SequenceEqual(new[]{"Identifier","Metric","NextHopAddress","Prefix"})&&e.SelectSingleNode("u:Prefix",ns)!.InnerText==(e.SelectSingleNode("u:NextHopAddress",ns)!.InnerText.Contains(':')?"::/0":"0")),"documented route layout "+label);
            if(dns){var entries=Dns(d).SelectNodes(".//u:DNSServerSearchOrder/u:IpAddress",ns)!.Cast<XmlElement>().ToArray();var expectedDns=(mode!=6?new[]{"192.0.2.53","192.0.2.54"}:Array.Empty<string>()).Concat(mode!=4?new[]{"2001:db8:a::53","2001:db8:a::54"}:Array.Empty<string>()).ToArray();Check(entries.Select(e=>e.InnerText).SequenceEqual(expectedDns)&&entries.Select(e=>e.GetAttribute("keyValue",Wcm)).SequenceEqual(Enumerable.Range(1,entries.Length).Select(i=>i.ToString())),"unique ordered mixed-family DNS "+label);}
            foreach(bool snapshot in new[]{false,true}) {
                string source=snapshot?xml:Strip(xml);var imported=XmlPlanImport.Import(source,generator);
                Check(imported.UseStaticIPv4==p.UseStaticIPv4&&imported.UseStaticIPv6==p.UseStaticIPv6&&imported.ImportedXml is {StaticIPv4ReadOnly:false},"native/snapshot enabled protocols "+label+snapshot);
                Check(Same(XmlMerge.Read(source),XmlMerge.Read(Xml(imported))),"native/snapshot unedited preservation "+label+snapshot);
                imported.ComputerName="DUALSTACK";Check(Same(d,XmlMerge.Read(Xml(imported))),"unrelated edit preserves network "+label+snapshot);
            }
        }
        foreach(int prefix in Enumerable.Range(1,64)) {var p=Plan();p.StaticIPv6Prefix=prefix;Check(StaticIPv4Settings.ReadIPv6(p).Prefix==prefix,"native IPv6 prefix accepted /"+prefix);if(prefix is 1 or 64)Check(StaticIPv4Settings.Inspect(XmlMerge.Read(Xml(p))).Known,"IPv6 boundary prefix exports/imports /"+prefix);}
        foreach(int prefix in new[]{-1,65,127,128,129}){var p=Plan();p.StaticIPv6Prefix=prefix;Reject(()=>Xml(p),"unsupported native IPv6 prefix rejected /"+prefix);}
        foreach(string ip in new[]{"","::","::1","ff02::1","::ffff:192.0.2.1","::192.0.2.1","fe80::1%4","[2001:db8::1]","2001:db8::1/64","2001:db8:::1","192.168.1.2"}){var p=Plan();p.StaticIPv6Address=ip;Reject(()=>Xml(p),"invalid IPv6 host rejected "+ip);}
        foreach(string ip in new[]{"fd12:3456:789a::20","fe80::20","2001:DB8:A:0:0:0:0:20"}){var p=Plan();p.StaticIPv6Address=ip;Check(StaticIPv4Settings.ReadIPv6(p).Address.Length>0,"valid IPv6 spelling/scope "+ip);}
        foreach(string gateway in new[]{"2001:db8:b::1","2001:db8:a::20","::","::1","ff02::1","fe80::1%1","192.0.2.1"}){var p=Plan();p.StaticIPv6Gateway=gateway;Reject(()=>Xml(p),"invalid IPv6 gateway rejected "+gateway);}
        foreach(string gateway in new[]{"","fe80::1","2001:db8:a::1"}){var p=Plan();p.StaticIPv6Gateway=gateway;Check(StaticIPv4Settings.ReadIPv6(p).Gateway==gateway,"optional/link-local/same-prefix IPv6 gateway "+gateway);}
        foreach(string dns in new[]{"2001:db8::53,2001:0db8:0:0:0:0:0:53","2001:db8::1,2001:db8::2,2001:db8::3,2001:db8::4","192.0.2.53","ff02::1","::","fe80::1%2"}){var p=Plan();p.StaticIPv6Dns=dns;Reject(()=>Xml(p),"invalid/duplicate IPv6 DNS rejected "+dns);}
        var local=Plan();local.StaticIPv6Dns="::1";Check(StaticIPv4Settings.ReadIPv6(local).DnsServers.Single()=="::1","local DNS can intentionally use loopback");
        foreach(var (address,prefix,gateway) in new[]{("10.1.2.3",8,"10.0.0.1"),("172.20.1.2",12,"172.16.0.1"),("192.168.5.20",16,"192.168.0.1"),("203.0.113.20",24,"203.0.113.1"),("192.0.2.0",31,"192.0.2.1"),("192.0.2.1",31,"192.0.2.0"),("192.0.2.0",32,"")}) {
            var p=Plan();p.UseStaticIPv4=true;p.UseStaticIPv6=false;p.StaticIPv4Address=address;p.StaticIPv4Prefix=prefix;p.StaticIPv4Gateway=gateway;p.StaticIPv4Dns="";
            Check(StaticIPv4Settings.Read(p).Address==address,"private/public/special IPv4 prefix "+address+"/"+prefix);
            Check(StaticIPv4Settings.Inspect(XmlMerge.Read(Xml(p))).Known,"special IPv4 native roundtrip "+prefix);
        }
        Check(StaticIPv4Settings.SubnetMask(0)=="0.0.0.0"&&StaticIPv4Settings.SubnetMask(31)=="255.255.255.254"&&StaticIPv4Settings.SubnetMask(32)=="255.255.255.255","independent boundary subnet masks");
        var single=Plan();single.UseStaticIPv4=true;single.StaticIPv4Prefix=32;Reject(()=>Xml(single),"/32 requires empty gateway");
        var retained=Plan();retained.UseStaticIPv4=true;retained.UseStaticIPv6=false;retained.StaticIPv6Address="unfinished";string keep=retained.StaticIPv6Address;Check(!Xml(retained).Contains("Ipv6Settings")&&retained.StaticIPv6Address==keep,"disabled incomplete protocol does not affect export or clear input");
        var restored=PlanStore.Deserialize(PlanStore.Serialize(retained,true));Check(restored.StaticIPv6Address==keep&&!restored.UseStaticIPv6&&restored.UseStaticIPv4,"JSON retains inactive protocol inputs independently");
        var dual=Plan();dual.UseStaticIPv4=true;var edit=XmlPlanImport.Import(Strip(Xml(dual)),generator);edit.UseStaticIPv6=false;var only4=XmlMerge.Read(Xml(edit));Check(StaticIPv4Settings.Inspect(only4) is {Values:not null,IPv6:null},"disabling IPv6 keeps IPv4 address, DNS and route");
        edit.UseStaticIPv6=true;edit.UseStaticIPv4=false;var only6=XmlMerge.Read(Xml(edit));Check(StaticIPv4Settings.Inspect(only6) is {Values:null,IPv6:not null},"disabling IPv4 keeps IPv6 address, DNS and route");
        edit.UseStaticIPv6=false;Check(Network(XmlMerge.Read(Xml(edit))).Length==0,"disabling both removes owned network only");
        var vm=new MainViewModel{Plan=Plan()};var notices=new HashSet<string>();vm.PropertyChanged+=(_,e)=>notices.Add(e.PropertyName??"");vm.Plan.StaticIPv6Address="2001:db8:a::21";
        Check(vm.IsStaticIPv6&&vm.IsStaticIP&&vm.StaticIPv6Summary.Contains("::21")&&vm.HelpTips["staticipv6"].Body.Contains("::21")&&notices.Contains("StaticIPv6Summary"),"IPv6 edits update summary, help and bindings");
        foreach(string target in new[]{"StaticIPv6Enabled","StaticIPv6AddressEditor","StaticIPv6PrefixEditor","StaticIPv6GatewayEditor","StaticIPv6DnsEditor"})Check(SettingsSearchCatalog.Search("IPv6").Count(r=>r.Target==target)==1,"IPv6 search anchor unique "+target);
        var summary=SummaryPresentation.FromText(vm.ConfigurationSummary);Check(summary.Sections.Single(s=>s.Kind=="network").Rows.Any(row=>row.SourceLabel=="静态 IPv6"),"IPv6 appears in grouped configuration summary");
        foreach(var item in vm.AppRemovalChoices){var lines=item.TooltipDescription.Split('\n');Check(item.Description.Contains("匹配项：") ? lines.Contains("匹配项：")&&!lines.Any(line=>line.Contains("。 匹配项")) : item.TooltipDescription.Replace("\n", "").Replace(" ", "")==item.Description.Replace(" ", ""),"removal tooltip separates existing identifiers without inventing them "+item.Id);}

        var variant=XmlMerge.Read(Strip(Xml(dual)));var ns2=Ns(variant);
        foreach(var c in Network(variant))c.RemoveAttribute("versionScope");
        foreach(XmlElement e in variant.SelectNodes("//u:Interface/u:Identifier",ns2)!)e.InnerText=e.InnerText.ToLowerInvariant();
        foreach(XmlElement e in variant.SelectNodes("//u:DhcpEnabled | //u:RouterDiscoveryEnabled",ns2)!)e.InnerText="0";
        foreach(XmlElement e in variant.SelectNodes("//u:IpAddress",ns2)!)if(e.InnerText.Contains(':'))e.InnerText=e.InnerText.Replace("db8","0DB8");
        var dnsList=One(variant,"//u:DNSServerSearchOrder");var servers=dnsList.ChildNodes.OfType<XmlElement>().ToArray();for(int i=0;i<servers.Length;i++)servers[i].SetAttribute("keyValue",Wcm,((i+1)*10).ToString());dnsList.PrependChild(servers[^1]);
        Tcp(variant).PrependChild(variant.CreateComment("keep this network comment"));
        string equivalent=variant.OuterXml;var importedVariant=XmlPlanImport.Import(equivalent,generator);
        Check(importedVariant.UseStaticIPv4&&importedVariant.UseStaticIPv6&&!importedVariant.ImportedXml!.StaticIPv4ReadOnly,"equivalent dual-stack spelling and numeric DNS keys recognized");
        importedVariant.ComputerName="KEEPBOTH";Check(Same(variant,XmlMerge.Read(Xml(importedVariant))),"unrelated edit preserves dual-stack comments and key spelling");
        importedVariant.StaticIPv6Address="2001:db8:a::22";var modified=XmlMerge.Read(Xml(importedVariant));Check(StaticIPv4Settings.Inspect(modified).IPv6?.Address=="2001:db8:a::22"&&One(modified,"//u:ComputerName").InnerText=="KEEPBOTH","semantic dual-stack edit retains pending unrelated edits");
        foreach(string mutation in new[]{"extra-address","unknown-policy","nested-v6-address","nested-gateway","nested-dns","dns-cross-family-order","duplicate-ip-key","duplicate-route-id","dhcp-v6","discovery-v6","route-metric","wrong-order","extra-attribute","alias"}) {
            var d=XmlMerge.Read(equivalent);var n=Ns(d);var iface=One(d,"//u:component[@name='Microsoft-Windows-TCPIP']/u:Interfaces/u:Interface");var ip=One(d,"//u:UnicastIpAddresses/u:IpAddress[2]");
            switch(mutation){
                case "extra-address":ip.ParentNode!.AppendChild(ip.CloneNode(true));break;
                case "unknown-policy":iface.AppendChild(d.CreateElement("ExtraPolicy",XmlMerge.Namespace));break;
                case "nested-v6-address":var text=ip.InnerText;ip.InnerText="";ip.AppendChild(d.CreateElement("Extra",XmlMerge.Namespace))!.InnerText=text;break;
                case "nested-gateway":var gateway=One(d,"//u:Route[2]/u:NextHopAddress");var value=gateway.InnerText;gateway.InnerText="";gateway.AppendChild(d.CreateElement("Extra",XmlMerge.Namespace))!.InnerText=value;break;
                case "nested-dns":var server=One(d,"//u:DNSServerSearchOrder/u:IpAddress");var serverValue=server.InnerText;server.InnerText="";server.AppendChild(d.CreateElement("Extra",XmlMerge.Namespace))!.InnerText=serverValue;break;
                case "dns-cross-family-order":One(d,"//u:DNSServerSearchOrder/u:IpAddress").SetAttribute("keyValue",Wcm,"1");break;
                case "duplicate-ip-key":ip.SetAttribute("keyValue",Wcm,"1");break;
                case "duplicate-route-id":One(d,"//u:Route[2]/u:Identifier").InnerText="1";break;
                case "dhcp-v6":One(d,"//u:Ipv6Settings/u:DhcpEnabled").InnerText="true";break;
                case "discovery-v6":One(d,"//u:Ipv6Settings/u:RouterDiscoveryEnabled").InnerText="true";break;
                case "route-metric":One(d,"//u:Route[2]/u:Metric").InnerText="25";break;
                case "wrong-order":iface.PrependChild(One(d,"//u:UnicastIpAddresses"));break;
                case "extra-attribute":ip.SetAttribute("custom","keep");break;
                case "alias":One(d,"//u:Interface/u:Identifier").InnerText="Ethernet";break;
            }
            var unknown=XmlPlanImport.Import(d.OuterXml,generator);Check(unknown.ImportedXml!.StaticIPv4ReadOnly&&!unknown.UseStaticIPv4&&!unknown.UseStaticIPv6,"unknown dual-stack remains read-only "+mutation);unknown.ComputerName="PRESERVEV6";
            Check(Same(d,XmlMerge.Read(Xml(unknown))),"unknown dual-stack preserved "+mutation);unknown.UseStaticIPv6=true;Reject(()=>Xml(unknown),"unknown network cannot be overwritten by IPv6 "+mutation);
        }
        var oldPlan=XmlPlanImport.Import(equivalent,generator);oldPlan.ComputerName="PENDINGV6";var old=JsonNode.Parse(PlanStore.Serialize(oldPlan,true))!.AsObject();old["UseStaticIPv4"]=false;old["UseStaticIPv6"]=false;old["ImportedXml"]!["StaticIPv4ImportVersion"]=1;old["ImportedXml"]!["StaticIPv4ReadOnly"]=true;
        var before=XmlMerge.Read(oldPlan.ImportedXml!.BaselineXml);foreach(var c in Network(before))c.ParentNode!.RemoveChild(c);old["ImportedXml"]!["BaselineXml"]=before.OuterXml;
        var migration=PlanStore.Deserialize(old.ToJsonString());Check(migration.UseStaticIPv4&&migration.UseStaticIPv6&&!migration.ImportedXml!.StaticIPv4ReadOnly&&migration.ComputerName=="PENDINGV6","legacy read-only dual-stack migration preserves pending non-network fields");migration.StaticIPv6Address="2001:db8:a::23";Check(StaticIPv4Settings.Inspect(XmlMerge.Read(Xml(migration))).IPv6?.Address=="2001:db8:a::23","migrated dual-stack baseline supports edits");
        var numericRoute=XmlMerge.Read(equivalent);One(numericRoute,"//u:Route[2]/u:Prefix").InnerText="0";
        Check(!StaticIPv4Settings.Inspect(numericRoute).Known,"ambiguous numeric IPv6 route prefix stays read-only without deployment equivalence evidence");
        var changedSource=XmlPlanImport.Import(equivalent,generator);var changedXml=XmlMerge.Read(changedSource.ImportedXml!.SourceXml);One(changedXml,"//u:UnicastIpAddresses/u:IpAddress[2]").InnerText="2001:db8:a::99/64";changedSource.ImportedXml.SourceXml=changedXml.OuterXml;changedSource.StaticIPv4Address="172.16.20.31";
        Reject(()=>Xml(changedSource),"cross-stack source/baseline mismatch blocks coherent network edit");
        return count;
    }
}
