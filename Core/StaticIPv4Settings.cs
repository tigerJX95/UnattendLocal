using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Xml;

namespace UnattendLocal.Core;

public static class StaticIPv4Settings
{
    public const string Tcp = "Microsoft-Windows-TCPIP", Dns = "Microsoft-Windows-DNS-Client";
    private const string Wcm = "http://schemas.microsoft.com/WMIConfig/2002/State";
    public const string ReadOnlyNotice = "原 XML 含多网卡、非 MAC 标识或额外/修改过的 TCPIP、DNS 配置，网络原文保留；本版静态 IPv4 区域只读，不会用界面默认值覆盖。";
    public const string Description = "默认不启用，不写入新的 IP 或 DNS 设置。启用后按目标网卡的 MAC 地址，在 specialize 阶段设置一个静态 IPv4、1–30 位前缀、可选同子网网关和最多三个 IPv4 DNS。只适用于目标 Windows，不配置 Windows PE，不改变 IPv6，不创建 Wi-Fi 连接，不修改当前电脑。MAC 必须来自目标电脑/虚拟机，不能使用当前电脑的 MAC 代替；目标网卡驱动须可用。网关留空不生成默认路由，DNS 留空不写入 DNS 覆盖，不保证名称解析或互联网访问。重复地址、MAC 不匹配、组织策略或其他脚本都可能影响实际联网；本工具不探测可用地址。关闭只影响新生成配置，不恢复已安装系统。方案会保存 MAC、IP 和 DNS。";
    public record Values(string Mac, string Address, int Prefix, string Gateway, string[] DnsServers);
    public record Source(bool Present, bool Known, Values? Values);
    public static bool CanEdit(InstallPlan p) => p.ImportedXml?.StaticIPv4ReadOnly != true;

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
    public static Values Read(InstallPlan p)
    {
        string mac = Mac(p.StaticIPv4Mac);
        uint ip = Address(p.StaticIPv4Address, "静态 IPv4", true);
        if (p.StaticIPv4Prefix is < 1 or > 30) throw new ArgumentException("本版静态 IPv4 前缀长度支持 1–30；/31 点对点及 /32 主机路由暂不在本功能范围内。");
        uint mask = uint.MaxValue << (32 - p.StaticIPv4Prefix), network = ip & mask, broadcast = network | ~mask;
        if (ip == network || ip == broadcast) throw new ArgumentException("静态 IPv4 不能是所填前缀下的网络地址或广播地址。");
        string gateway = (p.StaticIPv4Gateway ?? "").Trim();
        if (gateway.Length > 0) {
            uint next = Address(gateway, "默认网关", true);
            if ((next & mask) != network || next == network || next == broadcast || next == ip)
                throw new ArgumentException("默认网关须为同一子网中的其他有效主机地址；无需默认路由时可留空。");
            gateway = Format(next);
        }
        string rawDns = p.StaticIPv4Dns ?? "";
        if (rawDns.Length > 256) throw new ArgumentException("DNS 地址列表过长。");
        string[] dns = rawDns.Split([',', ';', ' ', '\r', '\n', '\t', '，', '；'], StringSplitOptions.RemoveEmptyEntries)
            .Select(s => Format(Address(s, "DNS", false))).ToArray();
        if (dns.Length > 3 || dns.Distinct().Count() != dns.Length) throw new ArgumentException("最多填写三个不重复的 IPv4 DNS 地址，用空格、逗号或分号分隔。");
        return new(mac, Format(ip), p.StaticIPv4Prefix, gateway, dns);
    }
    public static void Validate(InstallPlan p) { if (p.UseStaticIPv4) _ = Read(p); }
    public static string Summary(InstallPlan p)
    {
        if (!CanEdit(p)) return "静态 IPv4：原 XML 的网络配置只读保留，界面默认值不代表原配置。";
        if (!p.UseStaticIPv4) return "静态 IPv4：不写入新的 IP / DNS 覆盖；导入的其他网络原文按合并规则保留。";
        try { var v = Read(p); return $"静态 IPv4：网卡 {v.Mac}；{v.Address}/{v.Prefix}；网关：{(v.Gateway.Length == 0 ? "未指定" : v.Gateway)}；DNS：{(v.DnsServers.Length == 0 ? "未指定" : string.Join("、", v.DnsServers))}"; }
        catch (ArgumentException e) { return "静态 IPv4：待完善；" + e.Message; }
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
        if (!p.UseStaticIPv4) return;
        var v = Read(p);
        if (Nodes(doc).Length > 0) throw new ArgumentException("生成基准中已有 TCPIP / DNS 组件，不能重复添加静态 IPv4。");
        var settings = doc.SelectSingleNode("/u:unattend/u:settings[@pass='specialize']", ns) as XmlElement;
        if (settings == null) { settings = Add(doc.DocumentElement!, "settings"); settings.SetAttribute("pass", "specialize"); }
        var iface = Add(Add(Component(settings, Tcp, p.Architecture), "Interfaces"), "Interface", list: true);
        // Microsoft requires IPv4 settings, identifier, unicast addresses, then routes in this order.
        var ipv4 = Add(iface, "Ipv4Settings"); Add(ipv4, "DhcpEnabled", "false"); Add(ipv4, "RouterDiscoveryEnabled", "false");
        Add(iface, "Identifier", v.Mac);
        Add(Add(iface, "UnicastIpAddresses"), "IpAddress", v.Address + "/" + v.Prefix.ToString(CultureInfo.InvariantCulture), true, "1");
        if (v.Gateway.Length > 0) {
            var route = Add(Add(iface, "Routes"), "Route", list: true);
            Add(route, "Identifier", "1"); Add(route, "Metric", "10"); Add(route, "NextHopAddress", v.Gateway); Add(route, "Prefix", "0");
        }
        if (v.DnsServers.Length > 0) {
            var dns = Add(Add(Component(settings, Dns, p.Architecture), "Interfaces"), "Interface", list: true);
            Add(dns, "Identifier", v.Mac); var order = Add(dns, "DNSServerSearchOrder");
            for (int i = 0; i < v.DnsServers.Length; i++) Add(order, "IpAddress", v.DnsServers[i], true, (i + 1).ToString(CultureInfo.InvariantCulture));
        }
    }
    private static XmlElement[] Nodes(XmlDocument doc) => doc.SelectNodes("//u:component[@name='" + Tcp + "' or @name='" + Dns + "']", Ns(doc))!.Cast<XmlElement>().ToArray();
    public static Source Inspect(XmlDocument doc)
    {
        var nodes = Nodes(doc); if (nodes.Length == 0) return new(false, true, null);
        var unknown = new Source(true, false, null);
        try {
            if (nodes.Any(n => n.ParentNode is not XmlElement s || s.LocalName != "settings" || s.GetAttribute("pass") != "specialize")) return unknown;
            var tcp = nodes.Single(n => n.GetAttribute("name") == Tcp); var ns = Ns(doc);
            string Text(XmlNode node, string path) => node.SelectSingleNode(path, ns)?.InnerText ?? "";
            var cidr = Text(tcp, "u:Interfaces/u:Interface/u:UnicastIpAddresses/u:IpAddress").Split('/');
            if (cidr.Length != 2 || !int.TryParse(cidr[1], NumberStyles.None, CultureInfo.InvariantCulture, out int prefix)) return unknown;
            var dns = nodes.SingleOrDefault(n => n.GetAttribute("name") == Dns);
            var p = new InstallPlan { UseStaticIPv4 = true, Architecture = tcp.GetAttribute("processorArchitecture"), StaticIPv4Mac = Text(tcp, "u:Interfaces/u:Interface/u:Identifier"), StaticIPv4Address = cidr[0], StaticIPv4Prefix = prefix, StaticIPv4Gateway = Text(tcp, "u:Interfaces/u:Interface/u:Routes/u:Route/u:NextHopAddress"), StaticIPv4Dns = dns == null ? "" : string.Join(",", dns.SelectNodes("u:Interfaces/u:Interface/u:DNSServerSearchOrder/u:IpAddress", ns)!.Cast<XmlElement>().Select(e => e.InnerText)) };
            var values = Read(p);
            var expected = XmlMerge.Read("<unattend xmlns='" + XmlMerge.Namespace + "'/>"); Apply(expected, Ns(expected), p);
            var generated = Nodes(expected);
            if (generated.Length != nodes.Length || nodes.Any(n => !XmlMerge.Equal(n, generated.Single(g => g.GetAttribute("name") == n.GetAttribute("name"))))) return unknown;
            return new(true, true, values);
        }
        catch (Exception e) when (e is ArgumentException or InvalidOperationException or FormatException or OverflowException) { return unknown; }
    }
    private static void Assign(InstallPlan p, Source source)
    {
        p.UseStaticIPv4 = source.Values != null;
        if (source.Values is not {} v) return;
        p.StaticIPv4Mac = v.Mac; p.StaticIPv4Address = v.Address; p.StaticIPv4Prefix = v.Prefix; p.StaticIPv4Gateway = v.Gateway; p.StaticIPv4Dns = string.Join(", ", v.DnsServers);
    }
    public static Source ReadIntoPlan(XmlDocument doc, InstallPlan p, List<string> notices)
    {
        var source = Inspect(doc); Assign(p, source);
        if (!source.Known) notices.Add(ReadOnlyNotice);
        return source;
    }
    public static void UpdateImportState(ImportedXml data, Source source) { data.StaticIPv4ReadOnly = !source.Known; data.StaticIPv4ImportVersion = 1; }
    public static void ValidateImported(InstallPlan p, XmlDocument source)
    {
        if (!Inspect(source).Known && p.UseStaticIPv4) throw new ArgumentException(ReadOnlyNotice);
    }
    public static void NormalizeLoaded(InstallPlan p, JsonElement root)
    {
        if (p.ImportedXml is not { OriginalOmitted: false } data || data.SourceXml.Length == 0) return;
        var state = Inspect(XmlMerge.Read(data.SourceXml));
        if (data.StaticIPv4ImportVersion < 1 && state.Present && state.Known) {
            if (!root.TryGetProperty(nameof(InstallPlan.UseStaticIPv4), out _) && data.BaselineXml.Length > 0 && Nodes(XmlMerge.Read(data.BaselineXml)).Length == 0) {
                Assign(p, state); var baseline = XmlMerge.Read(data.BaselineXml); Apply(baseline, Ns(baseline), p); data.BaselineXml = baseline.OuterXml;
            }
            else data.Blockers = data.Blockers.Append("旧方案的网络编辑基准不完整，请重新导入原 XML；当前待处理修改保留。").Distinct().ToArray();
        }
        data.Notices = data.Notices.Where(n => n != "原文保留，未实现的组件：" + Tcp && n != "原文保留，未实现的组件：" + Dns && n != ReadOnlyNotice).ToArray();
        if (!state.Known) data.Notices = data.Notices.Append(ReadOnlyNotice).ToArray();
        UpdateImportState(data, state);
    }
}
