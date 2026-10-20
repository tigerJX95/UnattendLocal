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
    public const string ReadOnlyNotice = "原 XML 含多网卡、非 MAC 标识或额外/修改过的 TCPIP、DNS 配置，网络原文保留；本版 IP 配置区域只读，不会用界面默认值覆盖。";
    public const string Description = "IPv4 与 IPv6 可分别启用，也可同时启用，在 specialize 阶段配置同一个目标 MAC。切换协议页只切换显示，不清空另一页。每种协议最多一个静态地址、可选默认网关和三个 DNS；双栈 DNS 按 IPv4 列表再 IPv6 列表生成。IPv4 支持合法私网与公网地址，在同一输入框填写 /1–/32 或等价点分十进制连续掩码（如 /24 或 255.255.255.0）；静态地址 /0 覆盖全部地址范围，本工具会在配置页提示并阻止导出，保留输入且不自动转换；/31 用于点对点，/32 是单地址且本界面网关须留空。IPv6 遵循微软无人值守地址参数范围 /0–/64，本工具部署检查允许 /1–/64，网关可为同前缀单播或链路本地地址。不填写 %区域、括号或 CIDR。目标网卡驱动须可用；不探测地址占用，不配置 Windows PE，也不创建 Wi-Fi 连接。";
    public record Values(string Mac, string Address, int Prefix, string Gateway, string[] DnsServers);
    public record IPv6Values(string Mac, string Address, int Prefix, string Gateway, string[] DnsServers);
    public record Source(bool Present, bool Known, Values? Values) { public IPv6Values? IPv6 { get; init; } }
    public static bool CanEdit(InstallPlan p) => p.ImportedXml?.StaticIPv4ReadOnly != true;

    public const string IPv4ZeroPrefixNotice = "静态 IPv4 的 /0（子网掩码 0.0.0.0）覆盖全部 IPv4 地址范围，可能导致直连判断及网关通信异常。本工具阻止此配置导出；请按实际网络修改前缀或子网掩码，不会自动改为 /24。";
    public const string IPv6ZeroPrefixNotice = "静态 IPv6 的 /0 覆盖全部 IPv6 地址范围，可能导致直连判断及网关通信异常。本工具阻止此配置导出；请按实际网络修改前缀，不会自动改为 /64。";
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
        string warning = string.Join("\n", new[] { ipv4 ? IPv4ZeroPrefixNotice : "", ipv6 ? IPv6ZeroPrefixNotice : "" }.Where(s => s.Length > 0));
        return warning.Length > 0 && !CanEdit(p) ? warning + "\n原 XML 的网络配置只读，请先在原工具修正后重新导入。" : warning;
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
        if (families.Length > 0) throw new ConfigurationValidationException(nameof(InstallPlan.ImportedXml), string.Join("\n", families.Select(f => f == AddressFamily.InterNetwork ? IPv4ZeroPrefixNotice : IPv6ZeroPrefixNotice)) + "\n若来自只读 XML，请先在原工具修正后重新导入。");
    }

    public record IPv4Subnet(string Address, int Prefix, string Mask, string Network, string? Broadcast, string FirstHost, string LastHost, ulong HostCount);
    public static IPv4Subnet CalculateSubnet(InstallPlan p)
    {
        uint ip = Address(p.StaticIPv4Address, "静态 IPv4", true);
        int prefix = EffectivePrefix(p);
        if (prefix == 0) throw new ArgumentException(IPv4ZeroPrefixNotice);
        if (prefix is < 1 or > 32) throw new ArgumentException("静态 IPv4 网段预览需要 1–32 的前缀长度。");
        uint network = ip & Mask(prefix), last = network | ~Mask(prefix);
        uint firstHost = prefix >= 31 ? network : network + 1, lastHost = prefix >= 31 ? last : last - 1;
        return new(Format(ip), prefix, SubnetMask(prefix), Format(network), prefix >= 31 ? null : Format(last), Format(firstHost), Format(lastHost), (ulong)lastHost - firstHost + 1);
    }
    public static string SubnetPreview(InstallPlan p)
    {
        if (!CanEdit(p) || !p.UseStaticIPv4 || DeploymentWarning(p).Length > 0) return "";
        try {
            var subnet = CalculateSubnet(p);
            string range = subnet.Prefix == 32 ? "单地址：" + subnet.FirstHost : (subnet.Prefix == 31 ? "点对点两端：" : "主机位范围：") + subnet.FirstHost + " — " + subnet.LastHost;
            return $"网段：{subnet.Network}/{subnet.Prefix} · 掩码：{subnet.Mask}\n广播地址：{subnet.Broadcast ?? "不适用（/31 点对点或 /32 单地址）"}\n{range}（{subnet.HostCount.ToString(CultureInfo.InvariantCulture)} 个地址）\n范围按掩码计算，未排除全部特殊用途地址；不代表地址可分配、未被占用或目标网络可达。";
        } catch (ArgumentException e) { return "网段预览：" + e.Message; }
    }

    public static string Mac(string? value)
    {
        string raw = (value ?? "").Trim();
        if (!Regex.IsMatch(raw, @"\A(?:[0-9A-Fa-f]{12}|[0-9A-Fa-f]{2}(?:-[0-9A-Fa-f]{2}){5}|[0-9A-Fa-f]{2}(?::[0-9A-Fa-f]{2}){5})\z"))
            throw new ArgumentException("请填写目标网卡的完整 MAC 地址，例如 02-00-00-00-00-10；支持横线、冒号或连续 12 位十六进制。");
        raw = raw.Replace("-", "").Replace(":", "").ToUpperInvariant();
        if (raw == "000000000000" || (Convert.ToByte(raw[..2], 16) & 1) != 0)
            throw new ArgumentException("目标 MAC 必须为有效的单播地址，不能为全零、广播或组播地址。");
        return string.Join("-", Enumerable.Range(0, 6).Select(i => raw.Substring(i * 2, 2)));
    }

    private static uint Address(string? value, string label, bool host)
    {
        string raw = (value ?? "").Trim();
        if (!Regex.IsMatch(raw, @"\A(?:0|[1-9][0-9]{0,2})(?:\.(?:0|[1-9][0-9]{0,2})){3}\z"))
            throw new ArgumentException(label + "应为完整点分十进制 IPv4，例如 192.168.10.20；不要填写 CIDR 或省略段。");
        var parts = raw.Split('.').Select(int.Parse).ToArray();
        if (parts.Any(n => n > 255) || parts[0] == 0 || parts[0] >= 224 || host && parts[0] == 127)
            throw new ArgumentException(label + "不是本功能支持的单播 IPv4 地址。");
        return parts.Aggregate(0u, (n, octet) => (n << 8) | (uint)octet);
    }
    private static string Format(uint ip) => string.Join(".", new[] { ip >> 24, ip >> 16 & 255, ip >> 8 & 255, ip & 255 });
    public static string SubnetMask(int prefix)
    {
        if (prefix is < 0 or > 32) throw new ArgumentException("静态 IPv4 前缀长度支持 0–32。");
        return Format(Mask(prefix));
    }
    public static int PrefixFromMask(string? value)
    {
        string raw = (value ?? "").Trim();
        if (!Regex.IsMatch(raw, @"\A(?:0|[1-9][0-9]{0,2})(?:\.(?:0|[1-9][0-9]{0,2})){3}\z"))
            throw new ArgumentException("子网掩码应为完整点分十进制，例如 255.255.255.0。");
        var octets = raw.Split('.').Select(int.Parse).ToArray();
        if (octets.Any(n => n > 255)) throw new ArgumentException("子网掩码的每一段须为 0–255。");
        uint mask = octets.Aggregate(0u, (n, octet) => (n << 8) | (uint)octet);
        int prefix = System.Numerics.BitOperations.PopCount(mask);
        if (prefix is < 0 or > 32 || mask != Mask(prefix))
            throw new ArgumentException("子网掩码必须为连续的 1 后接连续的 0，且对应 /0–/32。");
        return prefix;
    }
    public static int PrefixFromInput(string? value)
    {
        string raw = (value ?? "").Trim();
        if (raw.StartsWith('/')) {
            if (!Regex.IsMatch(raw, @"\A/(?:0|[1-9][0-9]?)\z") || !int.TryParse(raw[1..], out int prefix) || prefix > 32)
                throw new ArgumentException("前缀长度请填写 /1–/32，例如 /24；/0 可保留输入，但部署检查会阻止导出。");
            return prefix;
        }
        if (!raw.Contains('.')) throw new ArgumentException("子网掩码/前缀长度请填写完整点分十进制（如 255.255.255.0）或 /24；不要只填 24。");
        return PrefixFromMask(raw);
    }
    public static int EffectivePrefix(InstallPlan p) => ConfigurationValidationException.Read(nameof(InstallPlan.StaticIPv4SubnetMask),
        () => p.UseStaticIPv4SubnetMask ? PrefixFromInput(p.StaticIPv4SubnetMask) : p.StaticIPv4Prefix);
    public static Values Read(InstallPlan p)
    {
        string mac = ConfigurationValidationException.Read(nameof(InstallPlan.StaticIPv4Mac), () => Mac(p.StaticIPv4Mac));
        uint ip = ConfigurationValidationException.Read(nameof(InstallPlan.StaticIPv4Address), () => Address(p.StaticIPv4Address, "静态 IPv4", true));
        int prefix = EffectivePrefix(p);
        if (prefix is < 0 or > 32) throw new ConfigurationValidationException(nameof(InstallPlan.StaticIPv4SubnetMask), "静态 IPv4 前缀长度支持 0–32；/31 用于点对点，/32 为单地址。");
        uint mask = Mask(prefix), network = ip & mask, broadcast = network | ~mask;
        if (prefix < 31 && (ip == network || ip == broadcast)) throw new ConfigurationValidationException(nameof(InstallPlan.StaticIPv4Address), "静态 IPv4 不能是所填前缀下的网络地址或广播地址。");
        string gateway = (p.StaticIPv4Gateway ?? "").Trim();
        if (gateway.Length > 0) {
            uint next = ConfigurationValidationException.Read(nameof(InstallPlan.StaticIPv4Gateway), () => Address(gateway, "默认网关", true));
            if (prefix == 32 || (next & mask) != network || prefix < 31 && (next == network || next == broadcast) || next == ip)
                throw new ConfigurationValidationException(nameof(InstallPlan.StaticIPv4Gateway), "默认网关须为同一子网中的其他有效主机地址；/32 请留空网关，需要时在其他设置中配置路由。");
            gateway = Format(next);
        }
        string rawDns = p.StaticIPv4Dns ?? "";
        if (rawDns.Length > 256) throw new ConfigurationValidationException(nameof(InstallPlan.StaticIPv4Dns), "DNS 地址列表过长。");
        string[] dns = rawDns.Split([',', ';', ' ', '\r', '\n', '\t', '，', '；'], StringSplitOptions.RemoveEmptyEntries)
            .Select(s => ConfigurationValidationException.Read(nameof(InstallPlan.StaticIPv4Dns), () => Format(Address(s, "DNS", false)))).ToArray();
        if (dns.Length > 3 || dns.Distinct().Count() != dns.Length) throw new ConfigurationValidationException(nameof(InstallPlan.StaticIPv4Dns), "最多填写三个不重复的 IPv4 DNS 地址，用空格、逗号或分号分隔。");
        return new(mac, Format(ip), prefix, gateway, dns);
    }
    private static uint Mask(int prefix) => prefix == 0 ? 0u : uint.MaxValue << (32-prefix);
    public static string IPv6Address(string? value, string label, bool host = true)
    {
        string raw=(value??"").Trim();
        if(raw.IndexOfAny(['%', '/', '[', ']', '.'])>=0 || !IPAddress.TryParse(raw,out var ip) || ip.AddressFamily!=AddressFamily.InterNetworkV6 || ip.IsIPv4MappedToIPv6 || ip.Equals(IPAddress.IPv6Any) || ip.IsIPv6Multicast || host && ip.Equals(IPAddress.IPv6Loopback))
            throw new ArgumentException(label+"须为完整 IPv6 单播地址，例如 2001:db8:10::20；不要填写前缀、方括号或 %区域。");
        return ip.ToString();
    }
    public static IPv6Values ReadIPv6(InstallPlan p)
    {
        string mac=ConfigurationValidationException.Read(nameof(InstallPlan.StaticIPv4Mac),()=>Mac(p.StaticIPv4Mac)),address=ConfigurationValidationException.Read(nameof(InstallPlan.StaticIPv6Address),()=>IPv6Address(p.StaticIPv6Address,"静态 IPv6"));
        int prefix=p.StaticIPv6Prefix;
        if(prefix is <0 or >64)throw new ConfigurationValidationException(nameof(InstallPlan.StaticIPv6Prefix), "微软无人值守 IPv6 静态地址前缀支持 0–64；此输入不接受 /65–/128。");
        string gateway=(p.StaticIPv6Gateway??"").Trim();
        if(gateway.Length>0) {
            gateway=ConfigurationValidationException.Read(nameof(InstallPlan.StaticIPv6Gateway),()=>IPv6Address(gateway,"IPv6 默认网关"));
            var next=IPAddress.Parse(gateway);var ip=IPAddress.Parse(address);
            byte[] a=ip.GetAddressBytes(),b=next.GetAddressBytes();
            bool same=true;for(int bit=0;bit<prefix;bit++)if((a[bit/8]&(1<<(7-bit%8)))!=(b[bit/8]&(1<<(7-bit%8)))){same=false;break;}
            if(gateway==address||!next.IsIPv6LinkLocal&&!same)throw new ConfigurationValidationException(nameof(InstallPlan.StaticIPv6Gateway), "IPv6 网关须为同前缀的其他单播地址，或目标网卡可达的链路本地地址（如 fe80::1）；无需默认路由时可留空。");
        }
        string raw=p.StaticIPv6Dns??"";if(raw.Length>256)throw new ConfigurationValidationException(nameof(InstallPlan.StaticIPv6Dns), "IPv6 DNS 地址列表过长。");
        var dns=raw.Split([',',';',' ','\r','\n','\t','，','；'],StringSplitOptions.RemoveEmptyEntries).Select(a=>ConfigurationValidationException.Read(nameof(InstallPlan.StaticIPv6Dns),()=>IPv6Address(a,"IPv6 DNS",false))).ToArray();
        if(dns.Length>3||dns.Distinct().Count()!=dns.Length)throw new ConfigurationValidationException(nameof(InstallPlan.StaticIPv6Dns), "最多填写三个不重复的 IPv6 DNS 地址，用空格、逗号或分号分隔。");
        return new(mac,address,prefix,gateway,dns);
    }
    public static void Validate(InstallPlan p) { if (p.UseStaticIPv4) _ = Read(p); if(p.UseStaticIPv6)_=ReadIPv6(p); }
    public static string SummaryIPv6(InstallPlan p)
    {
        if(!CanEdit(p))return "静态 IPv6：原 XML 的网络配置只读保留，界面默认值不代表原配置。";
        if(!p.UseStaticIPv6)return "静态 IPv6：不写入新的 IPv6 覆盖。";
        try {var v=ReadIPv6(p);return $"静态 IPv6：网卡 {v.Mac}；{v.Address}/{v.Prefix}；网关：{(v.Gateway.Length==0?"未指定":v.Gateway)}；DNS：{(v.DnsServers.Length==0?"未指定":string.Join("、",v.DnsServers))}";}
        catch(ArgumentException e){return "静态 IPv6：待配置；"+e.Message;}
    }
    public static string Summary(InstallPlan p)
    {
        if (!CanEdit(p)) return "静态 IPv4：原 XML 的网络配置只读保留，界面默认值不代表原配置。";
        if (!p.UseStaticIPv4) return "静态 IPv4：不写入新的 IP / DNS 覆盖；导入的其他网络原文按合并规则保留。";
        try { var v = Read(p); return $"静态 IPv4：网卡 {v.Mac}；{v.Address}/{v.Prefix}（掩码 {SubnetMask(v.Prefix)}）；网关：{(v.Gateway.Length == 0 ? "未指定" : v.Gateway)}；DNS：{(v.DnsServers.Length == 0 ? "未指定" : string.Join("、", v.DnsServers))}"; }
        catch (ArgumentException e) { return "静态 IPv4：待配置；" + e.Message; }
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
        if(Nodes(doc).Length>0)throw new ArgumentException("生成基准中已有 TCPIP / DNS 组件，不能重复添加 IP 配置。");
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
            if (e.ChildNodes.OfType<XmlElement>().Any()) throw new ArgumentException("网络标量设置含额外结构，不能可靠识别。");
        foreach (XmlElement e in node.SelectNodes(".//u:Identifier", ns)!)
            if (e.ParentNode is XmlElement parent && parent.LocalName == "Interface") {
                // The documented native MAC spelling uses hyphens; aliases remain read-only.
                if (!Regex.IsMatch(e.InnerText.Trim(), @"\A[0-9A-Fa-f]{2}(?:-[0-9A-Fa-f]{2}){5}\z")) throw new ArgumentException("非 MAC 网卡标识。");
                e.InnerText = Mac(e.InnerText);
            }
        foreach (XmlElement e in node.SelectNodes(".//u:DhcpEnabled | .//u:RouterDiscoveryEnabled", ns)!)
            if (e.InnerText.Trim() == "0") e.InnerText = "false";
        foreach(XmlElement list in node.SelectNodes(".//u:UnicastIpAddresses",ns)!) {
            var entries=list.ChildNodes.OfType<XmlElement>().ToArray();
            if(entries.Any(e=>!Regex.IsMatch(e.GetAttribute("keyValue",Wcm),@"\A[A-Za-z0-9]{1,32}\z"))||entries.Select(e=>e.GetAttribute("keyValue",Wcm)).Distinct().Count()!=entries.Length)throw new ArgumentException("IP 列表键无效或重复。");
            for(int i=0;i<entries.Length;i++)entries[i].SetAttribute("keyValue",Wcm,(i+1).ToString(CultureInfo.InvariantCulture));
        }
        var ids=node.SelectNodes(".//u:Route/u:Identifier",ns)!.Cast<XmlElement>().ToArray();
        if(ids.Any(e=>!Regex.IsMatch(e.InnerText.Trim(),@"\A[1-9][0-9]{0,8}\z"))||ids.Select(e=>e.InnerText.Trim()).Distinct().Count()!=ids.Length)throw new ArgumentException("路由标识无效或重复。");
        for(int i=0;i<ids.Length;i++)ids[i].InnerText=(i+1).ToString(CultureInfo.InvariantCulture);
        foreach(XmlElement e in node.SelectNodes(".//u:IpAddress | .//u:NextHopAddress",ns)!) {
            var parts=e.InnerText.Trim().Split('/');
            if(parts[0].Contains(':')) {parts[0]=IPv6Address(parts[0],"导入 IPv6",false);e.InnerText=string.Join("/",parts);}
        }
        foreach (XmlElement list in node.SelectNodes(".//u:DNSServerSearchOrder", ns)!) {
            var addresses = list.ChildNodes.OfType<XmlElement>().ToArray();
            if (addresses.Any(e => e.LocalName != "IpAddress" || e.NamespaceURI != XmlMerge.Namespace || !Regex.IsMatch(e.GetAttribute("keyValue", Wcm), @"\A[1-9][0-9]{0,8}\z"))) throw new ArgumentException("DNS 顺序键无效。");
            var ordered = addresses.OrderBy(e => int.Parse(e.GetAttribute("keyValue", Wcm), CultureInfo.InvariantCulture)).ToArray();
            if (ordered.Select(e => e.GetAttribute("keyValue", Wcm)).Distinct().Count() != ordered.Length) throw new ArgumentException("DNS 顺序键重复。");
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
            throw new ArgumentException("XML 原网络与编辑基准不一致，网络修改存在冲突，未导出；请重新导入当前原 XML。");
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
            p.StaticIPv4Dns=string.Join(",",servers.Where(a=>!a.Contains(':')));p.StaticIPv6Dns=string.Join(",",servers.Where(a=>a.Contains(':')));
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
        if(source.IPv6 is {} v6){p.StaticIPv4Mac=v6.Mac;p.StaticIPv6Address=v6.Address;p.StaticIPv6Prefix=v6.Prefix;p.StaticIPv6Gateway=v6.Gateway;p.StaticIPv6Dns=string.Join(", ",v6.DnsServers);}
        if (source.Values is not {} v) return;
        p.StaticIPv4Mac = v.Mac; p.StaticIPv4Address = v.Address; p.StaticIPv4Prefix = v.Prefix; if (p.UseStaticIPv4SubnetMask) p.StaticIPv4SubnetMask = (p.StaticIPv4SubnetMask??"").TrimStart().StartsWith('/') ? "/" + v.Prefix.ToString(CultureInfo.InvariantCulture) : SubnetMask(v.Prefix); p.StaticIPv4Gateway = v.Gateway; p.StaticIPv4Dns = string.Join(", ", v.DnsServers);
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
            else data.Blockers = data.Blockers.Append("旧方案的网络编辑基准不完整，请重新导入原 XML；当前待处理修改保留。").Distinct().ToArray();
        }
        data.Notices = data.Notices.Where(n => n != "原文保留，未实现的组件：" + Tcp && n != "原文保留，未实现的组件：" + Dns && n != ReadOnlyNotice && n != ReadOnlyNotice.Replace("IP 配置区域", "静态 IPv4 区域")).ToArray();
        if (!state.Known) data.Notices = data.Notices.Append(ReadOnlyNotice).ToArray();
        UpdateImportState(data, state);
    }
}
