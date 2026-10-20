using System.Text;
using System.Text.Json.Nodes;
using System.Xml;
using UnattendLocal.Core;

static class StaticNetworkTests
{
    public static int Run(GeneratorService generator)
    {
        int count = 0;
        void Check(bool ok, string title) { if (!ok) throw new Exception(title); count++; Console.WriteLine("PASS V1.0 " + title); }
        InstallPlan Plan() { var p = InstallPlan.New(); p.Users[0].Password = "NetworkTest1!"; p.UseStaticIPv4 = true; p.StaticIPv4Mac = "02:00:00:00:00:10"; p.StaticIPv4Address = "192.168.10.20"; p.StaticIPv4Prefix = 24; p.StaticIPv4Gateway = "192.168.10.1"; p.StaticIPv4Dns = "192.168.10.1, 192.168.10.2"; return p; }
        string Xml(InstallPlan p) => Encoding.UTF8.GetString(generator.Generate(p));
        XmlNamespaceManager Ns(XmlDocument d) { var n = new XmlNamespaceManager(d.NameTable); n.AddNamespace("u", XmlMerge.Namespace); return n; }
        XmlElement Tcp(XmlDocument d) => (XmlElement)d.SelectSingleNode("//u:component[@name='Microsoft-Windows-TCPIP']", Ns(d))!;
        XmlElement Dns(XmlDocument d) => (XmlElement)d.SelectSingleNode("//u:component[@name='Microsoft-Windows-DNS-Client']", Ns(d))!;
        string Strip(string xml) { var d = XmlMerge.Read(xml); foreach (XmlComment c in d.SelectNodes("//comment()")!.Cast<XmlComment>().ToArray()) if (c.Value?.StartsWith("UnattendLocal:v1 ") == true) c.ParentNode!.RemoveChild(c); return d.OuterXml; }
        void Reject(InstallPlan p, string title) { try { _ = Xml(p); } catch (ArgumentException) { Check(true, title); return; } throw new Exception("Expected rejection: " + title); }

        Check(!InstallPlan.New().UseStaticIPv4 && !InstallPlan.NewCustom().UseStaticIPv4, "static network defaults off");
        var inactive = Plan(); inactive.UseStaticIPv4 = false; inactive.StaticIPv4Mac = "bad"; inactive.StaticIPv4Prefix = -1;
        Check(!Xml(inactive).Contains("Microsoft-Windows-TCPIP"), "inactive inputs do not write network overrides");
        foreach (string arch in new[] { "amd64", "arm64", "x86" }) foreach (bool gateway in new[] { false, true }) foreach (bool dns in new[] { false, true }) {
            var p = Plan(); p.Architecture = arch; if (!gateway) p.StaticIPv4Gateway = ""; if (!dns) p.StaticIPv4Dns = "";
            string xml = Xml(p); var d = XmlMerge.Read(xml); var n = Ns(d); var iface = Tcp(d).SelectSingleNode("u:Interfaces/u:Interface", n)!;
            Check(Tcp(d).ParentNode is XmlElement pass && pass.GetAttribute("pass") == "specialize" && Tcp(d).GetAttribute("processorArchitecture") == arch, "correct network stage/architecture " + arch + gateway + dns);
            Check(iface.ChildNodes.OfType<XmlElement>().Select(e => e.LocalName).SequenceEqual(gateway ? new[] { "Ipv4Settings", "Identifier", "UnicastIpAddresses", "Routes" } : new[] { "Ipv4Settings", "Identifier", "UnicastIpAddresses" }), "documented TCPIP child order " + arch + gateway + dns);
            Check(iface.SelectSingleNode("u:Identifier", n)!.InnerText == "02-00-00-00-00-10" && iface.SelectSingleNode("u:UnicastIpAddresses/u:IpAddress", n)!.InnerText == "192.168.10.20/24" && iface.SelectSingleNode("u:Ipv4Settings/u:DhcpEnabled", n)!.InnerText == "false", "MAC and static CIDR payload " + arch + gateway + dns);
            Check(d.SelectNodes("//u:Ipv6Settings", n)!.Count == 0 && (d.SelectNodes("//u:Route", n)!.Count == 1) == gateway && (d.SelectNodes("//u:component[@name='Microsoft-Windows-DNS-Client']", n)!.Count == 1) == dns, "optional route/DNS and untouched IPv6 " + arch + gateway + dns);
            if (gateway) Check(iface.SelectSingleNode("u:Routes/u:Route", n)!.ChildNodes.OfType<XmlElement>().Select(e => e.LocalName).SequenceEqual(new[] { "Identifier", "Metric", "NextHopAddress", "Prefix" }) && iface.SelectSingleNode("u:Routes/u:Route/u:Prefix", n)!.InnerText == "0", "default route and documented order " + arch + dns);
            foreach (bool snapshot in new[] { false, true }) {
                string original = snapshot ? xml : Strip(xml); var imported = XmlPlanImport.Import(original, generator);
                Check(imported.UseStaticIPv4 && imported.ImportedXml is { StaticIPv4ReadOnly: false } && imported.StaticIPv4Mac == "02-00-00-00-00-10" && XmlMerge.Equal(XmlMerge.Read(original).DocumentElement, XmlMerge.Read(Xml(imported)).DocumentElement), "actual network import/unchanged roundtrip " + arch + gateway + dns + snapshot);
            }
        }
        foreach (string ip in new[] { "192.168.10.0", "192.168.10.255", "0.0.0.0", "127.0.0.1", "224.1.2.3", "255.255.255.255", "192.168.010.20", "192.168.1", "0xc0a80a14", "192.168.10.256", "192.168.10.20/24", "::1", "" }) { var p = Plan(); p.StaticIPv4Address = ip; Reject(p, "invalid host rejected: " + ip); }
        foreach (string mac in new[] { "", "00-00-00-00-00-00", "FF-FF-FF-FF-FF-FF", "01-00-00-00-00-10", "02:00-00:00-00:10", "02-00-00-00-00", "x2-00-00-00-00-10" }) { var p = Plan(); p.StaticIPv4Mac = mac; Reject(p, "invalid MAC rejected: " + mac); }
        foreach (int prefix in new[] { -1, 33, 34, 65, 100 }) { var p = Plan(); p.StaticIPv4Prefix = prefix; Reject(p, "out of scope prefix rejected: " + prefix); }
        foreach (string gateway in new[] { "192.168.20.1", "192.168.10.20", "192.168.10.0", "192.168.10.255", "bad" }) { var p = Plan(); p.StaticIPv4Gateway = gateway; Reject(p, "invalid gateway rejected: " + gateway); }
        foreach (string dns in new[] { "192.168.1.1,192.168.1.1", "1.1.1.1,2.2.2.2,3.3.3.3,4.4.4.4", "2001:db8::1", "0.0.0.0" }) { var p = Plan(); p.StaticIPv4Dns = dns; Reject(p, "invalid DNS rejected: " + dns); }
        var flat = Plan(); flat.StaticIPv4Mac = "020000000010"; flat.StaticIPv4Dns = "192.168.10.1； 192.168.10.2"; Check(StaticIPv4Settings.Read(flat).Mac == "02-00-00-00-00-10" && StaticIPv4Settings.Read(flat).DnsServers.Length == 2, "MAC and DNS separators normalized");

        string seed = Strip(Xml(Plan())); var edit = XmlPlanImport.Import(seed, generator); edit.StaticIPv4Dns = "192.168.10.2,192.168.10.3,192.168.10.4"; edit.StaticIPv4Mac = "02-00-00-00-00-11"; edit.StaticIPv4Address = "192.168.10.21"; edit.StaticIPv4Gateway = "";
        var changed = XmlMerge.Read(Xml(edit)); Check(StaticIPv4Settings.Inspect(changed) is { Known: true, Values: { Address: "192.168.10.21", Gateway: "" } } && changed.SelectNodes("//u:DNSServerSearchOrder/u:IpAddress", Ns(changed))!.Count == 3, "network fields and multiple keyed DNS values merge");
        Check(Dns(changed).SelectSingleNode("u:Interfaces/u:Interface/u:Identifier", Ns(changed))!.InnerText == "02-00-00-00-00-11", "TCPIP and DNS identifiers stay aligned");
        edit.UseStaticIPv4 = false; Check(!Xml(edit).Contains("Microsoft-Windows-TCPIP") && !Xml(edit).Contains("Microsoft-Windows-DNS-Client"), "disabling removes only recognized native network components");

        foreach (string mutation in new[] { "extra-ipv6", "second-interface", "extra-dns-policy", "different-dns-mac", "duplicate-dns-key", "alias", "missing-dhcp", "dhcp-true", "extra-route", "pe-stage" }) {
            var d = XmlMerge.Read(seed); var n = Ns(d); var tcp = Tcp(d); var iface = tcp.SelectSingleNode("u:Interfaces/u:Interface", n)!;
            switch (mutation) {
                case "extra-ipv6": iface.AppendChild(d.CreateElement("Ipv6Settings", XmlMerge.Namespace)); break;
                case "second-interface": iface.ParentNode!.AppendChild(iface.CloneNode(true)); break;
                case "extra-dns-policy": Dns(d).AppendChild(d.CreateElement("DNSDomain", XmlMerge.Namespace))!.InnerText = "example.test"; break;
                case "different-dns-mac": Dns(d).SelectSingleNode("u:Interfaces/u:Interface/u:Identifier", n)!.InnerText = "02-00-00-00-00-11"; break;
                case "duplicate-dns-key": Dns(d).SelectNodes(".//u:IpAddress", n)![1]!.Attributes!["keyValue", "http://schemas.microsoft.com/WMIConfig/2002/State"]!.Value = "1"; break;
                case "alias": iface.SelectSingleNode("u:Identifier", n)!.InnerText = "Ethernet"; break;
                case "missing-dhcp": var dhcp = iface.SelectSingleNode("u:Ipv4Settings/u:DhcpEnabled", n)!; dhcp.ParentNode!.RemoveChild(dhcp); break;
                case "dhcp-true": iface.SelectSingleNode("u:Ipv4Settings/u:DhcpEnabled", n)!.InnerText = "true"; break;
                case "extra-route": var route = iface.SelectSingleNode("u:Routes/u:Route", n)!; route.ParentNode!.AppendChild(route.CloneNode(true)); break;
                case "pe-stage": var pe = (XmlElement)d.SelectSingleNode("/u:unattend/u:settings[@pass='windowsPE']", n)!; tcp.ParentNode!.RemoveChild(tcp); pe.AppendChild(tcp); break;
            }
            var p = XmlPlanImport.Import(d.OuterXml, generator); Check(p.ImportedXml is { StaticIPv4ReadOnly: true } && !p.UseStaticIPv4 && p.ImportedXml.Notices.Contains(StaticIPv4Settings.ReadOnlyNotice), "unknown network is read-only: " + mutation);
            p.ComputerName = "NETPRESERVE"; var output = XmlMerge.Read(Xml(p)); Check(XmlMerge.Equal(Tcp(d), Tcp(output)) && XmlMerge.Equal(Dns(d), Dns(output)), "unknown network preserved through unrelated edit: " + mutation);
            p.UseStaticIPv4 = true; Reject(p, "unknown network cannot be overridden: " + mutation);
        }
        var saved = PlanStore.Deserialize(PlanStore.Serialize(Plan(), false)); Check(saved.UseStaticIPv4 && saved.StaticIPv4Address == "192.168.10.20" && saved.StaticIPv4Mac.Length > 0 && saved.Users[0].NeedsPassword, "network persists independently of omitted credentials");
        var pending = XmlPlanImport.Import(seed, generator); pending.ComputerName = "PENDINGNET"; var old = JsonNode.Parse(PlanStore.Serialize(pending, true))!.AsObject();
        foreach (string field in new[] { "UseStaticIPv4", "StaticIPv4Mac", "StaticIPv4Address", "StaticIPv4Prefix", "StaticIPv4Gateway", "StaticIPv4Dns" }) old.Remove(field);
        old["ImportedXml"]!["StaticIPv4ImportVersion"] = 0; var baseline = XmlMerge.Read(pending.ImportedXml!.BaselineXml);
        foreach (var comp in new[] { Tcp(baseline), Dns(baseline) }) comp.ParentNode!.RemoveChild(comp);
        old["ImportedXml"]!["BaselineXml"] = baseline.OuterXml;
        var migrated = PlanStore.Deserialize(old.ToJsonString()); Check(migrated.UseStaticIPv4 && migrated.ComputerName == "PENDINGNET" && StaticIPv4Settings.Inspect(XmlMerge.Read(Xml(migrated))).Known, "old XML plan migrates network baseline and preserves pending edits");
        var vm = new UnattendLocal.Desktop.MainViewModel { Plan = Plan() }; Check(vm.IsStaticIPv4 && vm.StaticIPv4Summary.Contains("192.168.10.20"), "view model updates static network editor and summary"); vm.Plan.UseStaticIPv4 = false; Check(!vm.IsStaticIPv4 && vm.CanEditStaticIPv4, "disabled static network keeps editable checkbox");
        var recommended = Plan(); Recommendations.Apply(recommended, "wired"); Check(recommended.UseStaticIPv4 && recommended.StaticIPv4Address == "192.168.10.20", "recommendation leaves explicit target network identity untouched");
        foreach (string term in new[] { "静态 IPv4", "MAC", "默认网关", "DNS", "CIDR" }) Check(UnattendLocal.Desktop.SettingsSearchCatalog.Search(term).Any(r => r.Target.StartsWith("StaticIPv4")), "network setting searchable: " + term);
        Check(Recommendations.Summary(Plan()).Contains("静态 IPv4") && Recommendations.MissingInputs(new InstallPlan { UseStaticIPv4 = true }).Contains("静态 IPv4"), "summary and missing-input review include static network");
        return count;
    }
}
