using System.Globalization;
using System.Net;
using System.Net.Sockets;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Xml;

namespace UnattendLocal.Core;

public static class StaticIPv4Settings
{
    public const string Tcp = "Microsoft-Windows-TCPIP", Dns = "Microsoft-Windows-DNS-Client";
    private const string Wcm = "http://schemas.microsoft.com/WMIConfig/2002/State";
    public static readonly string ReadOnlyNotice = global::UnattendLocal.Localization.UiText.Source("text.09d7cc86467d");
    public static readonly string Description = global::UnattendLocal.Localization.UiText.Source("text.d14093805ca3");
    public record Values(string Mac, string Address, int Prefix, string Gateway, string[] DnsServers);
    public record IPv6Values(string Mac, string Address, int Prefix, string Gateway, string[] DnsServers);
    public record Source(bool Present, bool Known, Values? Values) { public IPv6Values? IPv6 { get; init; } }
    public static bool CanEdit(InstallPlan p) => p.ImportedXml?.StaticIPv4ReadOnly != true;

    public static readonly string IPv4ZeroPrefixNotice = global::UnattendLocal.Localization.UiText.Source("text.b2edd83ef741");
    public static readonly string IPv6ZeroPrefixNotice = global::UnattendLocal.Localization.UiText.Source("text.78878665bdd6");
    private static IEnumerable<AddressFamily> ZeroPrefixFamilies(XmlDocument doc)
    {
        // Address prefixes only. Default-route prefixes and script text are unrelated.
        foreach (XmlElement address in doc.SelectNodes("//u:component[@name='" + Tcp + "']/u:Interfaces/u:Interface/u:UnicastIpAddresses/u:IpAddress", Ns(doc))!) {
            string[] parts = address.InnerText.Trim().Split('/');
            if (parts.Length == 2 && int.TryParse(parts[1].Trim(), NumberStyles.None, CultureInfo.InvariantCulture, out int prefix) && prefix == 0 && IPAddress.TryParse(parts[0].Trim(), out var ip))
                yield return ip.AddressFamily;
        }
    }
    public static string DeploymentWarning(InstallPlan p)
    {
        bool ipv4 = false, ipv6 = p.UseStaticIPv6 && p.StaticIPv6Prefix == 0;
        if (p.UseStaticIPv4) {
            try { ipv4 = EffectivePrefix(p) == 0; } catch (ArgumentException) { /* Invalid mask has its own validation. */ }
        }
        if (!CanEdit(p) && p.ImportedXml is { OriginalOmitted: false, SourceXml.Length: > 0 } imported) {
            try {
                var families = ZeroPrefixFamilies(XmlMerge.Read(imported.SourceXml)).ToArray();
                ipv4 |= families.Contains(AddressFamily.InterNetwork);
                ipv6 |= families.Contains(AddressFamily.InterNetworkV6);
            } catch (Exception e) when (e is XmlException or ArgumentException) { /* Invalid source is rejected by import validation. */ }
        }
        string warning = global::UnattendLocal.Localization.UiText.Join("\n", new[] { ipv4 ? IPv4ZeroPrefixNotice : "", ipv6 ? IPv6ZeroPrefixNotice : "" }.Where(s => s.Length > 0), false);
        return warning.Length > 0 && !CanEdit(p) ? global::UnattendLocal.Localization.UiText.Concat(new object?[] { warning, global::UnattendLocal.Localization.UiText.Source("text.5e22ec6f8d1e") },new bool[]{false,false}) : warning;
    }
    private static bool IsZeroIPv4(InstallPlan p) { try { return EffectivePrefix(p) == 0; } catch (ArgumentException) { return false; } }
    public static void ValidateDeployment(InstallPlan p)
    {
        string warning = DeploymentWarning(p);
        if (warning.Length > 0) throw new ConfigurationValidationException(!CanEdit(p) ? nameof(InstallPlan.ImportedXml) : p.UseStaticIPv4 && IsZeroIPv4(p) ? nameof(InstallPlan.StaticIPv4SubnetMask) : nameof(InstallPlan.StaticIPv6Prefix), warning);
    }
    public static void ValidateDeployment(XmlDocument doc)
    {
        var families = ZeroPrefixFamilies(doc).Distinct().ToArray();
        if (families.Length > 0) throw new ConfigurationValidationException(nameof(InstallPlan.ImportedXml), global::UnattendLocal.Localization.UiText.Concat(new object?[] { global::UnattendLocal.Localization.UiText.Join("\n", families.Select(f => f == AddressFamily.InterNetwork ? IPv4ZeroPrefixNotice : IPv6ZeroPrefixNotice), false), global::UnattendLocal.Localization.UiText.Source("text.ab196eb0301f") },new bool[]{false,false}));
    }

    public record IPv4Subnet(string Address, int Prefix, string Mask, string Network, string? Broadcast, string FirstHost, string LastHost, ulong HostCount);
    public static IPv4Subnet CalculateSubnet(InstallPlan p)
    {
        uint ip = Address(p.StaticIPv4Address, global::UnattendLocal.Localization.UiText.Source("text.69050783cafb"), true);
        int prefix = EffectivePrefix(p);
        if (prefix == 0) throw new ArgumentException(IPv4ZeroPrefixNotice);
        if (prefix is < 1 or > 32) throw new ArgumentException(global::UnattendLocal.Localization.UiText.Source("text.0d3a69d2e957"));
        uint network = ip & Mask(prefix), last = network | ~Mask(prefix);
        uint firstHost = prefix >= 31 ? network : network + 1, lastHost = prefix >= 31 ? last : last - 1;
        return new(Format(ip), prefix, SubnetMask(prefix), Format(network), prefix >= 31 ? null : Format(last), Format(firstHost), Format(lastHost), (ulong)lastHost - firstHost + 1);
    }
    public static string SubnetPreview(InstallPlan p)
    {
        if (!CanEdit(p) || !p.UseStaticIPv4 || DeploymentWarning(p).Length > 0) return "";
        try {
            var subnet = CalculateSubnet(p);
            string range = subnet.Prefix == 32 ? global::UnattendLocal.Localization.UiText.Concat(new object?[] { global::UnattendLocal.Localization.UiText.Source("text.329dc845fd9f"), subnet.FirstHost },new bool[]{false,false}) : global::UnattendLocal.Localization.UiText.Concat(new object?[] { global::UnattendLocal.Localization.UiText.Concat(new object?[] { (subnet.Prefix == 31 ? global::UnattendLocal.Localization.UiText.Source("text.1197517a0426") : global::UnattendLocal.Localization.UiText.Source("text.990a0a2e7cfc")), subnet.FirstHost },new bool[] { false, false }), " — ", subnet.LastHost },new bool[]{false,false,false});
            return global::UnattendLocal.Localization.UiText.Format("message.2cd26786aaf0", new string[] { global::UnattendLocal.Localization.UiText.Arg((subnet.Network), 0, null), global::UnattendLocal.Localization.UiText.Arg((subnet.Prefix), 0, null), global::UnattendLocal.Localization.UiText.Arg((subnet.Mask), 0, null), global::UnattendLocal.Localization.UiText.Arg(((subnet.Broadcast ?? global::UnattendLocal.Localization.UiText.Source("text.6f46ae627330"))), 0, null), global::UnattendLocal.Localization.UiText.Arg((range), 0, null), global::UnattendLocal.Localization.UiText.Arg((subnet.HostCount.ToString(CultureInfo.InvariantCulture)), 0, null) });
        } catch (ArgumentException e) { return global::UnattendLocal.Localization.UiText.Concat(new object?[] { global::UnattendLocal.Localization.UiText.Source("text.ba9df495f9f6"), e.Message },new bool[]{false,false}); }
    }

    public static string Mac(string? value)
    {
        string raw = (value ?? "").Trim();
        if (!Regex.IsMatch(raw, @"\A(?:[0-9A-Fa-f]{12}|[0-9A-Fa-f]{2}(?:-[0-9A-Fa-f]{2}){5}|[0-9A-Fa-f]{2}(?::[0-9A-Fa-f]{2}){5})\z"))
            throw new ArgumentException(global::UnattendLocal.Localization.UiText.Source("text.1d216a991195"));
        raw = raw.Replace("-", "").Replace(":", "").ToUpperInvariant();
        if (raw == "000000000000" || (Convert.ToByte(raw[..2], 16) & 1) != 0)
            throw new ArgumentException(global::UnattendLocal.Localization.UiText.Source("text.2dfc6a6720ea"));
        return global::UnattendLocal.Localization.UiText.Join("-", Enumerable.Range(0, 6).Select(i => raw.Substring(i * 2, 2)), false);
    }

    private static uint Address(string? value, string label, bool host)
    {
        string raw = (value ?? "").Trim();
        if (!Regex.IsMatch(raw, @"\A(?:0|[1-9][0-9]{0,2})(?:\.(?:0|[1-9][0-9]{0,2})){3}\z"))
            throw new ArgumentException(global::UnattendLocal.Localization.UiText.Concat(new object?[] { label, global::UnattendLocal.Localization.UiText.Source("text.494b8928a6d1") },new bool[]{false,false}));
        var parts = raw.Split('.').Select(int.Parse).ToArray();
        if (parts.Any(n => n > 255) || parts[0] == 0 || parts[0] >= 224 || host && parts[0] == 127)
            throw new ArgumentException(global::UnattendLocal.Localization.UiText.Concat(new object?[] { label, global::UnattendLocal.Localization.UiText.Source("text.a3cd5c9554a2") },new bool[]{false,false}));
        return parts.Aggregate(0u, (n, octet) => (n << 8) | (uint)octet);
    }
    private static string Format(uint ip) => global::UnattendLocal.Localization.UiText.Join(".", new[] { ip >> 24, ip >> 16 & 255, ip >> 8 & 255, ip & 255 }, false);
    public static string SubnetMask(int prefix)
    {
        if (prefix is < 0 or > 32) throw new ArgumentException(global::UnattendLocal.Localization.UiText.Source("text.336dba0bd6e1"));
        return Format(Mask(prefix));
    }
    public static int PrefixFromMask(string? value)
    {
        string raw = (value ?? "").Trim();
        if (!Regex.IsMatch(raw, @"\A(?:0|[1-9][0-9]{0,2})(?:\.(?:0|[1-9][0-9]{0,2})){3}\z"))
            throw new ArgumentException(global::UnattendLocal.Localization.UiText.Source("text.b00482c2351a"));
        var octets = raw.Split('.').Select(int.Parse).ToArray();
        if (octets.Any(n => n > 255)) throw new ArgumentException(global::UnattendLocal.Localization.UiText.Source("text.9fcc9a3ee55e"));
        uint mask = octets.Aggregate(0u, (n, octet) => (n << 8) | (uint)octet);
        int prefix = System.Numerics.BitOperations.PopCount(mask);
        if (prefix is < 0 or > 32 || mask != Mask(prefix))
            throw new ArgumentException(global::UnattendLocal.Localization.UiText.Source("text.97ff620bb229"));
        return prefix;
    }
    public static int PrefixFromInput(string? value)
    {
        string raw = (value ?? "").Trim();
        if (raw.StartsWith('/')) {
            if (!Regex.IsMatch(raw, @"\A/(?:0|[1-9][0-9]?)\z") || !int.TryParse(raw[1..], out int prefix) || prefix > 32)
                throw new ArgumentException(global::UnattendLocal.Localization.UiText.Source("text.37a96ddc9185"));
            return prefix;
        }
        if (!raw.Contains('.')) throw new ArgumentException(global::UnattendLocal.Localization.UiText.Source("text.075e2b0113a4"));
        return PrefixFromMask(raw);
    }
    public static int EffectivePrefix(InstallPlan p) => ConfigurationValidationException.Read(nameof(InstallPlan.StaticIPv4SubnetMask),
        () => p.UseStaticIPv4SubnetMask ? PrefixFromInput(p.StaticIPv4SubnetMask) : p.StaticIPv4Prefix);
    public static Values Read(InstallPlan p)
    {
        string mac = ConfigurationValidationException.Read(nameof(InstallPlan.StaticIPv4Mac), () => Mac(p.StaticIPv4Mac));
        uint ip = ConfigurationValidationException.Read(nameof(InstallPlan.StaticIPv4Address), () => Address(p.StaticIPv4Address, global::UnattendLocal.Localization.UiText.Source("text.69050783cafb"), true));
        int prefix = EffectivePrefix(p);
        if (prefix is < 0 or > 32) throw new ConfigurationValidationException(nameof(InstallPlan.StaticIPv4SubnetMask), global::UnattendLocal.Localization.UiText.Source("text.8633941738f5"));
        uint mask = Mask(prefix), network = ip & mask, broadcast = network | ~mask;
        if (prefix < 31 && (ip == network || ip == broadcast)) throw new ConfigurationValidationException(nameof(InstallPlan.StaticIPv4Address), global::UnattendLocal.Localization.UiText.Source("text.f16e938433dd"));
        string gateway = (p.StaticIPv4Gateway ?? "").Trim();
        if (gateway.Length > 0) {
            uint next = ConfigurationValidationException.Read(nameof(InstallPlan.StaticIPv4Gateway), () => Address(gateway, global::UnattendLocal.Localization.UiText.Source("text.51aae58f282e"), true));
            if (prefix == 32 || (next & mask) != network || prefix < 31 && (next == network || next == broadcast) || next == ip)
                throw new ConfigurationValidationException(nameof(InstallPlan.StaticIPv4Gateway), global::UnattendLocal.Localization.UiText.Source("text.cdcaa0184123"));
            gateway = Format(next);
        }
        string rawDns = p.StaticIPv4Dns ?? "";
        if (rawDns.Length > 256) throw new ConfigurationValidationException(nameof(InstallPlan.StaticIPv4Dns), global::UnattendLocal.Localization.UiText.Source("text.cd19e7ef5ae4"));
        string[] dns = rawDns.Split([',', ';', ' ', '\r', '\n', '\t', '，', '；'], StringSplitOptions.RemoveEmptyEntries)
            .Select(s => ConfigurationValidationException.Read(nameof(InstallPlan.StaticIPv4Dns), () => Format(Address(s, "DNS", false)))).ToArray();
        if (dns.Length > 3 || dns.Distinct().Count() != dns.Length) throw new ConfigurationValidationException(nameof(InstallPlan.StaticIPv4Dns), global::UnattendLocal.Localization.UiText.Source("text.50d606b39198"));
        return new(mac, Format(ip), prefix, gateway, dns);
    }
    private static uint Mask(int prefix) => prefix == 0 ? 0u : uint.MaxValue << (32-prefix);
    public static string IPv6Address(string? value, string label, bool host = true)
    {
        string raw=(value??"").Trim();
        if(raw.IndexOfAny(['%', '/', '[', ']', '.'])>=0 || !IPAddress.TryParse(raw,out var ip) || ip.AddressFamily!=AddressFamily.InterNetworkV6 || ip.IsIPv4MappedToIPv6 || ip.Equals(IPAddress.IPv6Any) || ip.IsIPv6Multicast || host && ip.Equals(IPAddress.IPv6Loopback))
            throw new ArgumentException(global::UnattendLocal.Localization.UiText.Concat(new object?[] { label, global::UnattendLocal.Localization.UiText.Source("text.30503cd8e139") },new bool[]{false,false}));
        return ip.ToString();
    }
    public static IPv6Values ReadIPv6(InstallPlan p)
    {
        string mac=ConfigurationValidationException.Read(nameof(InstallPlan.StaticIPv4Mac),()=>Mac(p.StaticIPv4Mac)),address=ConfigurationValidationException.Read(nameof(InstallPlan.StaticIPv6Address),()=>IPv6Address(p.StaticIPv6Address,global::UnattendLocal.Localization.UiText.Source("text.fe924a910aed")));
        int prefix=p.StaticIPv6Prefix;
        if(prefix is <0 or >64)throw new ConfigurationValidationException(nameof(InstallPlan.StaticIPv6Prefix), global::UnattendLocal.Localization.UiText.Source("text.7cf2cafe2866"));
        string gateway=(p.StaticIPv6Gateway??"").Trim();
        if(gateway.Length>0) {
            gateway=ConfigurationValidationException.Read(nameof(InstallPlan.StaticIPv6Gateway),()=>IPv6Address(gateway,global::UnattendLocal.Localization.UiText.Source("text.98a14cd61cbb")));
            var next=IPAddress.Parse(gateway);var ip=IPAddress.Parse(address);
            byte[] a=ip.GetAddressBytes(),b=next.GetAddressBytes();
            bool same=true;for(int bit=0;bit<prefix;bit++)if((a[bit/8]&(1<<(7-bit%8)))!=(b[bit/8]&(1<<(7-bit%8)))){same=false;break;}
            if(gateway==address||!next.IsIPv6LinkLocal&&!same)throw new ConfigurationValidationException(nameof(InstallPlan.StaticIPv6Gateway), global::UnattendLocal.Localization.UiText.Source("text.89e7451ba5a0"));
        }
        string raw=p.StaticIPv6Dns??"";if(raw.Length>256)throw new ConfigurationValidationException(nameof(InstallPlan.StaticIPv6Dns), global::UnattendLocal.Localization.UiText.Source("text.fa047d1f7d8f"));
        var dns=raw.Split([',',';',' ','\r','\n','\t','，','；'],StringSplitOptions.RemoveEmptyEntries).Select(a=>ConfigurationValidationException.Read(nameof(InstallPlan.StaticIPv6Dns),()=>IPv6Address(a,"IPv6 DNS",false))).ToArray();
        if(dns.Length>3||dns.Distinct().Count()!=dns.Length)throw new ConfigurationValidationException(nameof(InstallPlan.StaticIPv6Dns), global::UnattendLocal.Localization.UiText.Source("text.f105f11f6c06"));
        return new(mac,address,prefix,gateway,dns);
    }
    public static void Validate(InstallPlan p) { if (p.UseStaticIPv4) _ = Read(p); if(p.UseStaticIPv6)_=ReadIPv6(p); }
    public static string SummaryIPv6(InstallPlan p)
    {
        if(!CanEdit(p))return global::UnattendLocal.Localization.UiText.Source("text.ae075781a7ef");
        if(!p.UseStaticIPv6)return global::UnattendLocal.Localization.UiText.Source("text.e69a04fe33af");
        try {var v=ReadIPv6(p);return global::UnattendLocal.Localization.UiText.Format("message.3de7d0a7bfc4", new string[] { global::UnattendLocal.Localization.UiText.Arg((v.Mac), 0, null), global::UnattendLocal.Localization.UiText.Arg((v.Address), 0, null), global::UnattendLocal.Localization.UiText.Arg((v.Prefix), 0, null), global::UnattendLocal.Localization.UiText.Arg(((v.Gateway.Length==0?global::UnattendLocal.Localization.UiText.Source("text.7409a608060f"):v.Gateway)), 0, null), global::UnattendLocal.Localization.UiText.Arg(((v.DnsServers.Length==0?global::UnattendLocal.Localization.UiText.Source("text.7409a608060f"):global::UnattendLocal.Localization.UiText.Join("、", v.DnsServers, false))), 0, null) });}
        catch(ArgumentException e){return global::UnattendLocal.Localization.UiText.Concat(new object?[] { global::UnattendLocal.Localization.UiText.Source("text.6ef5844b40db"), e.Message },new bool[]{false,false});}
    }
    public static string Summary(InstallPlan p)
    {
        if (!CanEdit(p)) return global::UnattendLocal.Localization.UiText.Source("text.04277ac2b1ee");
        if (!p.UseStaticIPv4) return global::UnattendLocal.Localization.UiText.Source("text.e04d2c4d693c");
        try { var v = Read(p); return global::UnattendLocal.Localization.UiText.Format("message.baa9f5608377", new string[] { global::UnattendLocal.Localization.UiText.Arg((v.Mac), 0, null), global::UnattendLocal.Localization.UiText.Arg((v.Address), 0, null), global::UnattendLocal.Localization.UiText.Arg((v.Prefix), 0, null), global::UnattendLocal.Localization.UiText.Arg((SubnetMask(v.Prefix)), 0, null), global::UnattendLocal.Localization.UiText.Arg(((v.Gateway.Length == 0 ? global::UnattendLocal.Localization.UiText.Source("text.7409a608060f") : v.Gateway)), 0, null), global::UnattendLocal.Localization.UiText.Arg(((v.DnsServers.Length == 0 ? global::UnattendLocal.Localization.UiText.Source("text.7409a608060f") : global::UnattendLocal.Localization.UiText.Join("、", v.DnsServers, false))), 0, null) }); }
        catch (ArgumentException e) { return global::UnattendLocal.Localization.UiText.Concat(new object?[] { global::UnattendLocal.Localization.UiText.Source("text.7db8f86a9f4b"), e.Message },new bool[]{false,false}); }
    }
    private static XmlNamespaceManager Ns(XmlDocument doc) { var ns = new XmlNamespaceManager(doc.NameTable); ns.AddNamespace("u", XmlMerge.Namespace); return ns; }
    private static XmlElement Add(XmlNode parent, string name, string? text = null, bool list = false, string? key = null)
    {
        var doc = parent.OwnerDocument!; var e = doc.CreateElement(name, XmlMerge.Namespace);
        if (text != null) e.InnerText = text;
        if (list) Set("action", "add");
        if (key != null) Set("keyValue", key);
        parent.AppendChild(e); return e;
        void Set(string name, string value) { var attr = doc.CreateAttribute("wcm", name, Wcm); attr.Value = value; e.Attributes.Append(attr); }
    }
    private static XmlElement Component(XmlElement settings, string name, string arch)
    {
        var c = Add(settings, "component");
        foreach (var pair in new Dictionary<string,string> { ["name"] = name, ["processorArchitecture"] = arch, ["publicKeyToken"] = "31bf3856ad364e35", ["language"] = "neutral", ["versionScope"] = "nonSxS" }) c.SetAttribute(pair.Key, pair.Value);
        return c;
    }
    public static void Apply(XmlDocument doc, XmlNamespaceManager ns, InstallPlan p)
    {
        if(!p.UseStaticIPv4&&!p.UseStaticIPv6)return;
        var v4=p.UseStaticIPv4?Read(p):null;var v6=p.UseStaticIPv6?ReadIPv6(p):null;
        if(Nodes(doc).Length>0)throw new ArgumentException(global::UnattendLocal.Localization.UiText.Source("text.f058c0a4ee7c"));
        var settings=doc.SelectSingleNode("/u:unattend/u:settings[@pass='specialize']",ns) as XmlElement;
        if(settings==null){settings=Add(doc.DocumentElement!,"settings");settings.SetAttribute("pass","specialize");}
        var iface=Add(Add(Component(settings,Tcp,p.Architecture),"Interfaces"),"Interface",list:true);
        // Native child order: IPv4, IPv6, Identifier, addresses, routes.
        foreach(string protocol in new[]{v4!=null?"Ipv4Settings":"",v6!=null?"Ipv6Settings":""}.Where(s=>s.Length>0)) {
            var option=Add(iface,protocol);Add(option,"DhcpEnabled","false");Add(option,"RouterDiscoveryEnabled","false");
        }
        string mac=v4?.Mac??v6!.Mac;Add(iface,"Identifier",mac);
        var addresses=Add(iface,"UnicastIpAddresses");int key=0;
        if(v4!=null)Add(addresses,"IpAddress",v4.Address+"/"+v4.Prefix.ToString(CultureInfo.InvariantCulture),true,(++key).ToString(CultureInfo.InvariantCulture));
        if(v6!=null)Add(addresses,"IpAddress",v6.Address+"/"+v6.Prefix.ToString(CultureInfo.InvariantCulture),true,(++key).ToString(CultureInfo.InvariantCulture));
        var gateways=new[]{v4?.Gateway??"",v6?.Gateway??""}.Where(s=>s.Length>0).ToArray();
        if(gateways.Length>0) {
            var routes=Add(iface,"Routes");int id=0;
            foreach(string gateway in gateways){var route=Add(routes,"Route",list:true);Add(route,"Identifier",(++id).ToString(CultureInfo.InvariantCulture));Add(route,"Metric","10");Add(route,"NextHopAddress",gateway);Add(route,"Prefix",gateway.Contains(':')?"::/0":"0");}
        }
        var servers=(v4?.DnsServers??[]).Concat(v6?.DnsServers??[]).ToArray();
        if(servers.Length>0) {
            var dns=Add(Add(Component(settings,Dns,p.Architecture),"Interfaces"),"Interface",list:true);Add(dns,"Identifier",mac);var order=Add(dns,"DNSServerSearchOrder");
            for(int i=0;i<servers.Length;i++)Add(order,"IpAddress",servers[i],true,(i+1).ToString(CultureInfo.InvariantCulture));
        }
    }
    private static XmlElement[] Nodes(XmlDocument doc) => NativeSetupXml.Components(doc, Tcp).Concat(NativeSetupXml.Components(doc, Dns)).ToArray();
    // A comparison copy only: never normalize unrelated XML or write back an unedited component.
    private static XmlElement Comparable(XmlElement source)
    {
        var node = (XmlElement)source.CloneNode(true); var ns = Ns(node.OwnerDocument!);
        if (!node.HasAttribute("versionScope")) node.SetAttribute("versionScope", "nonSxS");
        foreach (XmlElement e in node.SelectNodes(".//u:Identifier | .//u:DhcpEnabled | .//u:RouterDiscoveryEnabled | .//u:IpAddress | .//u:NextHopAddress | .//u:Metric | .//u:Prefix", ns)!)
            if (e.ChildNodes.OfType<XmlElement>().Any()) throw new ArgumentException(global::UnattendLocal.Localization.UiText.Source("text.30ea4fcd28f1"));
        foreach (XmlElement e in node.SelectNodes(".//u:Identifier", ns)!)
            if (e.ParentNode is XmlElement parent && parent.LocalName == "Interface") {
                // The documented native MAC spelling uses hyphens; aliases remain read-only.
                if (!Regex.IsMatch(e.InnerText.Trim(), @"\A[0-9A-Fa-f]{2}(?:-[0-9A-Fa-f]{2}){5}\z")) throw new ArgumentException(global::UnattendLocal.Localization.UiText.Source("text.b4e1485eb7c2"));
                e.InnerText = Mac(e.InnerText);
            }
        foreach (XmlElement e in node.SelectNodes(".//u:DhcpEnabled | .//u:RouterDiscoveryEnabled", ns)!)
            if (e.InnerText.Trim() == "0") e.InnerText = "false";
        foreach(XmlElement list in node.SelectNodes(".//u:UnicastIpAddresses",ns)!) {
            var entries=list.ChildNodes.OfType<XmlElement>().ToArray();
            if(entries.Any(e=>!Regex.IsMatch(e.GetAttribute("keyValue",Wcm),@"\A[A-Za-z0-9]{1,32}\z"))||entries.Select(e=>e.GetAttribute("keyValue",Wcm)).Distinct().Count()!=entries.Length)throw new ArgumentException(global::UnattendLocal.Localization.UiText.Source("text.1cf25df4475e"));
            for(int i=0;i<entries.Length;i++)entries[i].SetAttribute("keyValue",Wcm,(i+1).ToString(CultureInfo.InvariantCulture));
        }
        var ids=node.SelectNodes(".//u:Route/u:Identifier",ns)!.Cast<XmlElement>().ToArray();
        if(ids.Any(e=>!Regex.IsMatch(e.InnerText.Trim(),@"\A[1-9][0-9]{0,8}\z"))||ids.Select(e=>e.InnerText.Trim()).Distinct().Count()!=ids.Length)throw new ArgumentException(global::UnattendLocal.Localization.UiText.Source("text.7c8fa6cd3727"));
        for(int i=0;i<ids.Length;i++)ids[i].InnerText=(i+1).ToString(CultureInfo.InvariantCulture);
        foreach(XmlElement e in node.SelectNodes(".//u:IpAddress | .//u:NextHopAddress",ns)!) {
            var parts=e.InnerText.Trim().Split('/');
            if(parts[0].Contains(':')) {parts[0]=IPv6Address(parts[0],global::UnattendLocal.Localization.UiText.Source("text.e7cd27d90395"),false);e.InnerText=global::UnattendLocal.Localization.UiText.Join("/", parts, false);}
        }
        foreach (XmlElement list in node.SelectNodes(".//u:DNSServerSearchOrder", ns)!) {
            var addresses = list.ChildNodes.OfType<XmlElement>().ToArray();
            if (addresses.Any(e => e.LocalName != "IpAddress" || e.NamespaceURI != XmlMerge.Namespace || !Regex.IsMatch(e.GetAttribute("keyValue", Wcm), @"\A[1-9][0-9]{0,8}\z"))) throw new ArgumentException(global::UnattendLocal.Localization.UiText.Source("text.39fed8a4c7f1"));
            var ordered = addresses.OrderBy(e => int.Parse(e.GetAttribute("keyValue", Wcm), CultureInfo.InvariantCulture)).ToArray();
            if (ordered.Select(e => e.GetAttribute("keyValue", Wcm)).Distinct().Count() != ordered.Length) throw new ArgumentException(global::UnattendLocal.Localization.UiText.Source("text.8219666f0b9d"));
            for (int i = 0; i < ordered.Length; i++) { ordered[i].SetAttribute("keyValue", Wcm, (i + 1).ToString(CultureInfo.InvariantCulture)); list.AppendChild(ordered[i]); }
        }
        return node;
    }
    private static bool SameValues(Values? a, Values? b) => a != null && b != null && a.Mac == b.Mac && a.Address == b.Address && a.Prefix == b.Prefix && a.Gateway == b.Gateway && a.DnsServers.SequenceEqual(b.DnsServers);
    private static bool SameIPv6(IPv6Values? a,IPv6Values? b)=>a==null&&b==null||a!=null&&b!=null&&a.Mac==b.Mac&&a.Address==b.Address&&a.Prefix==b.Prefix&&a.Gateway==b.Gateway&&a.DnsServers.SequenceEqual(b.DnsServers);
    internal static XmlDocument PrepareMergeSource(XmlDocument source, string baselineXml, XmlDocument updated)
    {
        var baseline = XmlMerge.Read(baselineXml); var before = Nodes(baseline); var after = Nodes(updated);
        bool unchanged = before.Length == after.Length && before.All(n => after.Count(a => a.GetAttribute("name") == n.GetAttribute("name") && XmlMerge.Equal(a, n)) == 1);
        if (unchanged || before.Length == 0) return source;
        var state = Inspect(source); var previous = Inspect(baseline);
        if (!state.Known || !previous.Known) return source;
        if (!(state.Values==null&&previous.Values==null||SameValues(state.Values, previous.Values)) || !SameIPv6(state.IPv6,previous.IPv6))
            throw new ArgumentException(global::UnattendLocal.Localization.UiText.Source("text.36c298911721"));
        var current = Nodes(source);
        if (current.Length != before.Length || current.Any(n => !before.Any(e => e.GetAttribute("name") == n.GetAttribute("name") && e.GetAttribute("processorArchitecture") == n.GetAttribute("processorArchitecture")))) return source;
        // All editable network values agree with the baseline. Only an explicit network edit
        // permits replacing these owned components for the existing atomic three-way merge.
        foreach (var n in current) n.ParentNode!.ReplaceChild(source.ImportNode(before.Single(e => e.GetAttribute("name") == n.GetAttribute("name")), true), n);
        return source;
    }
    public static Source Inspect(XmlDocument doc)
    {
        var nodes = Nodes(doc); if (nodes.Length == 0) return new(false, true, null);
        var unknown = new Source(true, false, null);
        try {
            if (nodes.Any(n => !NativeSetupXml.ValidComponent(doc, n, n.GetAttribute("name"), "specialize") || n.SelectNodes(".//*")!.Cast<XmlElement>().Prepend(n).Any(e => !NativeSetupXml.NoSignificantText(e) && e.ChildNodes.OfType<XmlElement>().Any()))) return unknown;
            if (nodes.Select(n => n.GetAttribute("processorArchitecture")).Distinct().Count() != 1) return unknown;
            var comparable = nodes.Select(Comparable).ToArray();
            var tcp = comparable.Single(n => n.GetAttribute("name") == Tcp); var ns = Ns(doc);
            string Text(XmlNode node, string path) => node.SelectSingleNode(path, ns)?.InnerText ?? "";
            var dns = comparable.SingleOrDefault(n => n.GetAttribute("name") == Dns);
            var addresses=tcp.SelectNodes("u:Interfaces/u:Interface/u:UnicastIpAddresses/u:IpAddress",ns)!.Cast<XmlElement>().Select(e=>e.InnerText.Split('/')).ToArray();
            if(addresses.Length is <1 or >2 || addresses.Any(a=>a.Length!=2))return unknown;
            var p=new InstallPlan{Architecture=tcp.GetAttribute("processorArchitecture"),StaticIPv4Mac=Text(tcp,"u:Interfaces/u:Interface/u:Identifier")};
            foreach(var cidr in addresses) {
                if(!int.TryParse(cidr[1],NumberStyles.None,CultureInfo.InvariantCulture,out int prefix))return unknown;
                if(cidr[0].Contains(':')){if(p.UseStaticIPv6)return unknown;p.UseStaticIPv6=true;p.StaticIPv6Address=cidr[0];p.StaticIPv6Prefix=prefix;}
                else{if(p.UseStaticIPv4)return unknown;p.UseStaticIPv4=true;p.StaticIPv4Address=cidr[0];p.StaticIPv4Prefix=prefix;}
            }
            foreach(XmlElement route in tcp.SelectNodes("u:Interfaces/u:Interface/u:Routes/u:Route",ns)!) {
                string gateway=Text(route,"u:NextHopAddress");
                if(gateway.Contains(':')){if(p.StaticIPv6Gateway.Length>0)return unknown;p.StaticIPv6Gateway=gateway;}
                else{if(p.StaticIPv4Gateway.Length>0)return unknown;p.StaticIPv4Gateway=gateway;}
            }
            if(!p.UseStaticIPv4&&p.StaticIPv4Gateway.Length>0||!p.UseStaticIPv6&&p.StaticIPv6Gateway.Length>0)return unknown;
            var servers=dns?.SelectNodes("u:Interfaces/u:Interface/u:DNSServerSearchOrder/u:IpAddress",ns)!.Cast<XmlElement>().Select(e=>e.InnerText).ToArray()??[];
            p.StaticIPv4Dns=global::UnattendLocal.Localization.UiText.Join(",", servers.Where(a=>!a.Contains(':')), false);p.StaticIPv6Dns=global::UnattendLocal.Localization.UiText.Join(",", servers.Where(a=>a.Contains(':')), false);
            if(!p.UseStaticIPv4&&p.StaticIPv4Dns.Length>0||!p.UseStaticIPv6&&p.StaticIPv6Dns.Length>0)return unknown;
            var values=p.UseStaticIPv4?Read(p):null;var ipv6=p.UseStaticIPv6?ReadIPv6(p):null;
            var expected = XmlMerge.Read("<unattend xmlns='" + XmlMerge.Namespace + "'/>"); Apply(expected, Ns(expected), p);
            var generated = Nodes(expected);
            if (generated.Length != comparable.Length || comparable.Any(n => !XmlMerge.Equal(n, generated.Single(g => g.GetAttribute("name") == n.GetAttribute("name"))))) return unknown;
            return new(true, true, values){IPv6=ipv6};
        }
        catch (Exception e) when (e is ArgumentException or InvalidOperationException or FormatException or OverflowException) { return unknown; }
    }
    private static void Assign(InstallPlan p, Source source)
    {
        p.UseStaticIPv4 = source.Values != null;
        p.UseStaticIPv6 = source.IPv6 != null;
        if(source.IPv6 is {} v6){p.StaticIPv4Mac=v6.Mac;p.StaticIPv6Address=v6.Address;p.StaticIPv6Prefix=v6.Prefix;p.StaticIPv6Gateway=v6.Gateway;p.StaticIPv6Dns=global::UnattendLocal.Localization.UiText.Join(", ", v6.DnsServers, false);}
        if (source.Values is not {} v) return;
        p.StaticIPv4Mac = v.Mac; p.StaticIPv4Address = v.Address; p.StaticIPv4Prefix = v.Prefix; if (p.UseStaticIPv4SubnetMask) p.StaticIPv4SubnetMask = (p.StaticIPv4SubnetMask??"").TrimStart().StartsWith('/') ? "/" + v.Prefix.ToString(CultureInfo.InvariantCulture) : SubnetMask(v.Prefix); p.StaticIPv4Gateway = v.Gateway; p.StaticIPv4Dns = global::UnattendLocal.Localization.UiText.Join(", ", v.DnsServers, false);
    }
    public static Source ReadIntoPlan(XmlDocument doc, InstallPlan p, List<string> notices)
    {
        var source = Inspect(doc); Assign(p, source);
        if (!source.Known) notices.Add(ReadOnlyNotice);
        return source;
    }
    public static void UpdateImportState(ImportedXml data, Source source) { data.StaticIPv4ReadOnly = !source.Known; data.StaticIPv4ImportVersion = 3; }
    public static void ValidateImported(InstallPlan p, XmlDocument source)
    {
        if (!Inspect(source).Known && (p.UseStaticIPv4||p.UseStaticIPv6)) throw new ArgumentException(ReadOnlyNotice);
    }
    public static void NormalizeLoaded(InstallPlan p, JsonElement root)
    {
        if (p.ImportedXml is not { OriginalOmitted: false } data || data.SourceXml.Length == 0) return;
        var state = Inspect(XmlMerge.Read(data.SourceXml));
        if (data.StaticIPv4ImportVersion < 3 && state.Present && state.Known && data.BaselineXml.Length > 0 && Nodes(XmlMerge.Read(data.BaselineXml)).Length == 0) {
            if (!root.TryGetProperty(nameof(InstallPlan.UseStaticIPv4), out _) || data.StaticIPv4ReadOnly && !p.UseStaticIPv4 && !p.UseStaticIPv6) {
                Assign(p, state); var baseline = XmlMerge.Read(data.BaselineXml); Apply(baseline, Ns(baseline), p); data.BaselineXml = baseline.OuterXml;
            }
            else data.Blockers = data.Blockers.Append(global::UnattendLocal.Localization.UiText.Source("text.ef02f4b1eabb")).Distinct().ToArray();
        }
        data.Notices = data.Notices.Where(n => n != global::UnattendLocal.Localization.UiText.Concat(new object?[] { global::UnattendLocal.Localization.UiText.Source("text.8af4bbf685c1"), Tcp },new bool[]{false,false}) && n != global::UnattendLocal.Localization.UiText.Concat(new object?[] { global::UnattendLocal.Localization.UiText.Source("text.8af4bbf685c1"), Dns },new bool[]{false,true}) && n != ReadOnlyNotice && n != ReadOnlyNotice.Replace(global::UnattendLocal.Localization.UiText.Source("text.03a248c344ca"), global::UnattendLocal.Localization.UiText.Source("text.efc9f66d0cac"))).ToArray();
        if (!state.Known) data.Notices = data.Notices.Append(ReadOnlyNotice).ToArray();
        UpdateImportState(data, state);
    }
}
