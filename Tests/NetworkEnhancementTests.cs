using System.Text;
using System.Text.Json.Nodes;
using System.Xml;
using UnattendLocal.Core;
using UnattendLocal.Desktop;

static class NetworkEnhancementTests
{
    private const string Wcm = "http://schemas.microsoft.com/WMIConfig/2002/State";
    public static int Run(GeneratorService generator)
    {
        int count = 0;
        void Check(bool ok, string title) { if (!ok) throw new Exception(title); count++; Console.WriteLine("PASS V1.9 " + title); }
        void Reject(Action action, string title) { try { action(); } catch (ArgumentException) { Check(true, title); return; } throw new Exception("Expected rejection: " + title); }
        InstallPlan Plan() { var p = InstallPlan.New(); p.Users[0].Password = "NetworkTest1!"; p.UseStaticIPv4 = true; p.StaticIPv4Mac = "02-AB-00-00-00-10"; p.StaticIPv4Address = "192.168.10.20"; p.StaticIPv4Gateway = "192.168.10.1"; p.StaticIPv4Dns = "192.168.10.2,192.168.10.3"; return p; }
        string Xml(InstallPlan p) => Encoding.UTF8.GetString(generator.Generate(p));
        XmlNamespaceManager Ns(XmlDocument d) { var n = new XmlNamespaceManager(d.NameTable); n.AddNamespace("u", XmlMerge.Namespace); return n; }
        XmlElement Tcp(XmlDocument d) => (XmlElement)d.SelectSingleNode("//u:component[@name='Microsoft-Windows-TCPIP']", Ns(d))!;
        XmlElement Dns(XmlDocument d) => (XmlElement)d.SelectSingleNode("//u:component[@name='Microsoft-Windows-DNS-Client']", Ns(d))!;
        XmlElement One(XmlDocument d, string path) => (XmlElement)d.SelectSingleNode(path, Ns(d))!;
        string Strip(string xml) { var d = XmlMerge.Read(xml); foreach (var c in d.SelectNodes("//comment()")!.Cast<XmlComment>().Where(c => c.Value?.StartsWith("UnattendLocal:v1 ") == true).ToArray()) c.ParentNode!.RemoveChild(c); return d.OuterXml; }
        bool SameNetwork(XmlDocument a, XmlDocument b) => XmlMerge.Equal(Tcp(a), Tcp(b)) && XmlMerge.Equal(Dns(a), Dns(b));
        string Variant(string input)
        {
            var d = XmlMerge.Read(Strip(input)); var n = Ns(d);
            Tcp(d).RemoveAttribute("versionScope"); Dns(d).RemoveAttribute("versionScope");
            foreach (XmlElement e in d.SelectNodes("//u:Interface/u:Identifier", n)!) e.InnerText = e.InnerText.ToLowerInvariant();
            foreach (XmlElement e in d.SelectNodes("//u:DhcpEnabled | //u:RouterDiscoveryEnabled", n)!) e.InnerText = "0";
            One(d, "//u:UnicastIpAddresses/u:IpAddress").SetAttribute("keyValue", Wcm, "addressA");
            One(d, "//u:Route/u:Identifier").InnerText = "42";
            var order = One(d, "//u:DNSServerSearchOrder");
            var children = order.ChildNodes.OfType<XmlElement>().ToArray(); children[0].SetAttribute("keyValue", Wcm, "2"); children[1].SetAttribute("keyValue", Wcm, "10");
            order.PrependChild(children[1]);
            // Preserve user comments and unrelated native configuration on an unchanged roundtrip.
            Tcp(d).PrependChild(d.CreateComment("network owner note"));
            return d.OuterXml;
        }
        Check(!InstallPlan.New().UseStaticIPv4SubnetMask && !InstallPlan.NewCustom().UseStaticIPv4SubnetMask, "old and custom plans default to prefix mode");
        Check(StaticIPv4Settings.SubnetMask(24) == "255.255.255.0" && StaticIPv4Settings.PrefixFromMask("255.255.254.0") == 23, "known independent mask examples");
        for (int prefix = 0; prefix <= 32; prefix++) {
            string mask = string.Join(".", Enumerable.Range(0, 4).Select(i => Enumerable.Range(0, 8).Where(bit => i * 8 + bit < prefix).Sum(bit => 1 << (7 - bit))));
            var p = Plan(); p.StaticIPv4Address = "10.0.0.1"; p.StaticIPv4Gateway = p.StaticIPv4Dns = ""; p.UseStaticIPv4SubnetMask = true; p.StaticIPv4SubnetMask = mask; p.StaticIPv4Prefix = -99;
            Check(StaticIPv4Settings.Read(p).Prefix == prefix && StaticIPv4Settings.SubnetMask(prefix) == mask, "mask mode selects independently derived prefix /" + prefix);
            var d = XmlMerge.Read(Xml(p));
            Check(One(d, "//u:UnicastIpAddresses/u:IpAddress").InnerText == "10.0.0.1/" + prefix, "mask emits native CIDR /" + prefix);
            var saved = PlanStore.Deserialize(PlanStore.Serialize(p, true));
            Check(saved.UseStaticIPv4SubnetMask && saved.StaticIPv4SubnetMask == mask && saved.StaticIPv4Prefix == -99, "JSON preserves active mask and inactive prefix /" + prefix);
        }
        foreach (string mask in new[] { "", "255.0.255.0", "255.255.255.1", "255.255.254.255", "255.255.255.253", "0.0.0.1", "255.255.256.0", "255.255.255", "255.255.0255.0", "/24", "24", "0xffffff00", "255.255.255.0/24", "::", "-1.0.0.0", "255..255.0" }) {
            var p = Plan(); p.UseStaticIPv4SubnetMask = true; p.StaticIPv4SubnetMask = mask;
            Reject(() => Xml(p), "invalid mask cannot export: " + mask);
        }
        foreach (string host in new[] { "192.168.10.16", "192.168.10.23" }) { var p = Plan(); p.UseStaticIPv4SubnetMask = true; p.StaticIPv4SubnetMask = "255.255.255.248"; p.StaticIPv4Address = host; Reject(() => Xml(p), "mask-specific network/broadcast rejected " + host); }
        foreach (string gateway in new[] { "192.168.10.1", "192.168.10.16", "192.168.10.23", "192.168.10.20" }) { var p = Plan(); p.UseStaticIPv4SubnetMask = true; p.StaticIPv4SubnetMask = "255.255.255.248"; p.StaticIPv4Gateway = gateway; Reject(() => Xml(p), "mask-specific invalid gateway rejected " + gateway); }
        var modes = Plan(); modes.UseStaticIPv4SubnetMask = true; modes.StaticIPv4SubnetMask = " 255.255.255.248 "; modes.StaticIPv4Gateway = "192.168.10.17";
        Check(StaticIPv4Settings.Read(modes).Prefix == 29 && StaticIPv4Settings.Summary(modes).Contains("255.255.255.248"), "valid /29 gateway and review summary");
        modes.UseStaticIPv4SubnetMask = false; modes.StaticIPv4SubnetMask = "bad";
        Check(StaticIPv4Settings.Read(modes).Prefix == 24, "inactive invalid mask does not affect prefix mode");
        modes.UseStaticIPv4 = false; modes.UseStaticIPv4SubnetMask = true;
        Check(!Xml(modes).Contains("Microsoft-Windows-TCPIP"), "disabled static network ignores incomplete mask");
        var snap = Plan(); snap.UseStaticIPv4SubnetMask = true; snap.StaticIPv4SubnetMask = "255.255.255.0"; snap.StaticIPv4Prefix = 15;
        var snapImport = XmlPlanImport.Import(Xml(snap), generator);
        Check(snapImport.UseStaticIPv4SubnetMask && snapImport.StaticIPv4SubnetMask == "255.255.255.0" && StaticIPv4Settings.Read(snapImport).Prefix == 24, "snapshot import uses actual XML prefix and preserves mask input mode");
        var bareImport = XmlPlanImport.Import(Strip(Xml(snap)), generator);
        Check(!bareImport.UseStaticIPv4SubnetMask && bareImport.StaticIPv4Prefix == 24, "native XML without snapshot defaults to effective prefix");
        var vm = new MainViewModel { Plan = Plan() }; var events = new List<string>(); vm.PropertyChanged += (_, e) => events.Add(e.PropertyName ?? "");
        Check(vm.IsStaticIPv4Prefix && !vm.IsStaticIPv4SubnetMask, "prefix editor initially active");
        vm.Plan.UseStaticIPv4SubnetMask = true; vm.Plan.StaticIPv4SubnetMask = "255.255.255.0";
        Check(!vm.IsStaticIPv4Prefix && vm.IsStaticIPv4SubnetMask && events.Contains(nameof(vm.IsStaticIPv4SubnetMask)) && vm.HelpTips["staticmask"].ToString()!.Contains("255.255.255.0"), "mode change refreshes bindings and dynamic help");
        vm.Plan.UseStaticIPv4 = false; Check(!vm.IsStaticIPv4Prefix && !vm.IsStaticIPv4SubnetMask, "both editors disabled with network option");
        var recommendation = PlanStore.Deserialize(PlanStore.Serialize(snap, true)); Recommendations.Apply(recommendation, "wired");
        Check(recommendation.UseStaticIPv4SubnetMask && recommendation.StaticIPv4SubnetMask == snap.StaticIPv4SubnetMask, "recommendations preserve explicit network mask");
        foreach (string term in new[] { "子网掩码", "subnet mask", "StaticIPv4SubnetMask", "255.255.255.0" }) Check(SettingsSearchCatalog.Search(term).Any(r => r.Target == "StaticIPv4SubnetMaskEditor"), "mask setting searchable " + term);

        foreach (string arch in new[] { "amd64", "arm64", "x86" }) {
            var p = Plan(); p.Architecture = arch; string variant = Variant(Xml(p)); var original = XmlMerge.Read(variant);
            var import = XmlPlanImport.Import(variant, generator);
            Check(import.UseStaticIPv4 && import.ImportedXml is { StaticIPv4ReadOnly: false } && import.StaticIPv4Mac == "02-AB-00-00-00-10" && import.StaticIPv4Dns == "192.168.10.2, 192.168.10.3", "semantic native import and numeric DNS key order " + arch);
            Check(SameNetwork(original, XmlMerge.Read(Xml(import))), "untouched network retains original XML and comments " + arch);
            import.ComputerName = "V18NET"; var unrelated = XmlMerge.Read(Xml(import));
            Check(SameNetwork(original, unrelated) && Tcp(unrelated).InnerXml.Contains("network owner note"), "unrelated edits preserve original network spelling and comments " + arch);
            import.StaticIPv4Mac = "02-ab-00-00-00-11"; import.StaticIPv4Address = "192.168.10.21"; import.StaticIPv4Dns = "192.168.10.3,192.168.10.2,192.168.10.4";
            var edited = XmlMerge.Read(Xml(import)); var values = StaticIPv4Settings.Inspect(edited).Values;
            Check(values is { Address: "192.168.10.21", Mac: "02-AB-00-00-00-11" } && values.DnsServers.SequenceEqual(new[] { "192.168.10.3", "192.168.10.2", "192.168.10.4" }), "explicit network edits safely merge canonical keys " + arch);
            Check(One(edited, "//u:ComputerName").InnerText == "V18NET", "network merge retains unrelated pending edits " + arch);
            import.UseStaticIPv4 = false; string disabled = Xml(import);
            Check(!disabled.Contains("Microsoft-Windows-TCPIP") && !disabled.Contains("Microsoft-Windows-DNS-Client"), "equivalent native network can be removed " + arch);
            var repeat = XmlPlanImport.Import(disabled, generator); repeat.UseStaticIPv4 = true; repeat.StaticIPv4Mac = p.StaticIPv4Mac; repeat.StaticIPv4Address = p.StaticIPv4Address; repeat.StaticIPv4Gateway = p.StaticIPv4Gateway; repeat.StaticIPv4Dns = p.StaticIPv4Dns;
            Check(StaticIPv4Settings.Inspect(XmlMerge.Read(Xml(repeat))).Known, "network can be enabled again after export/reimport " + arch);
        }
        string seed = Variant(Xml(Plan()));
        foreach (string mutation in new[] { "duplicate-dns-key", "alpha-dns-key", "zero-dns-key", "leading-zero-key", "extra-node", "extra-attr", "extra-text", "dhcp-true", "different-mac", "different-arch", "wrong-order", "route-metric", "extra-address", "missing-dhcp", "component-attr", "nested-component", "nested-mac", "nested-bool", "nested-route-id" }) {
            var d = XmlMerge.Read(seed); var ns = Ns(d); var tcp = Tcp(d); var dns = Dns(d);
            var iface = (XmlElement)tcp.SelectSingleNode("u:Interfaces/u:Interface", ns)!;
            var entries = dns.SelectNodes(".//u:IpAddress", ns)!.Cast<XmlElement>().ToArray();
            switch (mutation) {
                case "duplicate-dns-key": entries[0].SetAttribute("keyValue", Wcm, entries[1].GetAttribute("keyValue", Wcm)); break;
                case "alpha-dns-key": entries[0].SetAttribute("keyValue", Wcm, "first"); break;
                case "zero-dns-key": entries[0].SetAttribute("keyValue", Wcm, "0"); break;
                case "leading-zero-key": entries[0].SetAttribute("keyValue", Wcm, "02"); break;
                case "extra-node": dns.AppendChild(d.CreateElement("DNSDomain", XmlMerge.Namespace))!.InnerText = "example.test"; break;
                case "extra-attr": entries[0].SetAttribute("custom", "keep"); break;
                case "extra-text": iface.AppendChild(d.CreateTextNode("keep me")); break;
                case "dhcp-true": One(d, "//u:DhcpEnabled").InnerText = "true"; break;
                case "different-mac": One(d, "//u:component[@name='Microsoft-Windows-DNS-Client']//u:Identifier").InnerText = "02-AB-00-00-00-11"; break;
                case "different-arch": dns.SetAttribute("processorArchitecture", "x86"); break;
                case "wrong-order": iface.PrependChild(iface.SelectSingleNode("u:Identifier", ns)!); break;
                case "route-metric": One(d, "//u:Route/u:Metric").InnerText = "20"; break;
                case "extra-address": var ip = One(d, "//u:UnicastIpAddresses/u:IpAddress"); ip.ParentNode!.AppendChild(ip.CloneNode(true)); break;
                case "missing-dhcp": var dhcp = One(d, "//u:DhcpEnabled"); dhcp.ParentNode!.RemoveChild(dhcp); break;
                case "nested-mac": var mac = One(d, "//u:Interface/u:Identifier"); string oldMac = mac.InnerText; mac.InnerText = ""; mac.AppendChild(d.CreateElement("Extra", XmlMerge.Namespace))!.InnerText = oldMac; break;
                case "nested-bool": var boolean = One(d, "//u:DhcpEnabled"); boolean.InnerText = ""; boolean.AppendChild(d.CreateElement("Extra", XmlMerge.Namespace))!.InnerText = "0"; break;
                case "nested-route-id": var routeId = One(d, "//u:Route/u:Identifier"); routeId.InnerText = ""; routeId.AppendChild(d.CreateElement("Extra", XmlMerge.Namespace))!.InnerText = "42"; break;
                case "component-attr": tcp.SetAttribute("custom", "keep"); break;
                case "nested-component": var parent = tcp.ParentNode!; parent.RemoveChild(tcp); dns.AppendChild(tcp); break;
            }
            if (mutation == "different-arch") { Reject(() => XmlPlanImport.Import(d.OuterXml, generator), "multiple architectures rejected atomically"); continue; }
            var import = XmlPlanImport.Import(d.OuterXml, generator);
            Check(import.ImportedXml is { StaticIPv4ReadOnly: true } && !import.UseStaticIPv4, "unknown semantic variant remains read-only " + mutation);
            import.ComputerName = "PRESERVENET";
            if (mutation == "component-attr") Reject(() => Xml(import), "invalid component metadata is rejected by final XML schema validation");
            else Check(SameNetwork(d, XmlMerge.Read(Xml(import))), "unknown variant preserved during unrelated changes " + mutation);
            import.UseStaticIPv4 = true; Reject(() => Xml(import), "unknown variant cannot be overwritten " + mutation);
        }
        var oldPlan = XmlPlanImport.Import(seed, generator); oldPlan.ComputerName = "PENDING18";
        var old = JsonNode.Parse(PlanStore.Serialize(oldPlan, true))!.AsObject(); old["UseStaticIPv4"] = false; old["ImportedXml"]!["StaticIPv4ReadOnly"] = true; old["ImportedXml"]!["StaticIPv4ImportVersion"] = 1;
        var before = XmlMerge.Read(oldPlan.ImportedXml!.BaselineXml); foreach (var c in new[] { Tcp(before), Dns(before) }) c.ParentNode!.RemoveChild(c); old["ImportedXml"]!["BaselineXml"] = before.OuterXml;
        var migrated = PlanStore.Deserialize(old.ToJsonString());
        Check(migrated.UseStaticIPv4 && migrated.ImportedXml is { StaticIPv4ReadOnly: false, StaticIPv4ImportVersion: 3 } && migrated.ComputerName == "PENDING18", "V1.7 read-only native network migrates with unrelated pending edits");
        migrated.StaticIPv4Address = "192.168.10.22"; Check(StaticIPv4Settings.Inspect(XmlMerge.Read(Xml(migrated))).Values?.Address == "192.168.10.22", "migrated network establishes editable baseline");
        old["UseStaticIPv4"] = true; var ambiguous = PlanStore.Deserialize(old.ToJsonString());
        Check(ambiguous.ImportedXml!.Blockers.Any(n => n.Contains("网络")), "ambiguous legacy pending network edits block unsafe migration");
        var conflict = XmlPlanImport.Import(seed, generator); var mutated = XmlMerge.Read(conflict.ImportedXml!.SourceXml); One(mutated, "//u:UnicastIpAddresses/u:IpAddress").InnerText = "192.168.10.25/24"; conflict.ImportedXml.SourceXml = mutated.OuterXml; conflict.StaticIPv4Address = "192.168.10.22";
        Reject(() => Xml(conflict), "source/baseline disagreement never bypasses atomic merge conflict");
        return count;
    }
}
