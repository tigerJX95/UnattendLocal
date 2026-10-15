using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Xml;
using Schneegans.Unattend;
namespace UnattendLocal.Core;

public static class WifiSettings
{
    public const string Wpa2 = "预设 WPA2 Wi-Fi", Wpa3 = "预设 WPA3 Wi-Fi", Open = "预设开放 Wi-Fi", Profile = "使用 Wi-Fi 配置 XML";
    public const int MaxProfileLength = 512 * 1024;
    private const string Root = @"C:\Windows\Setup\Scripts\";
    private const string WlanNamespace = "http://www.microsoft.com/networking/WLAN/profile/v1";
    public static bool IsPreset(InstallPlan p) => p.WifiMode is Wpa2 or Wpa3 or Open;
    public static bool NeedsPassword(InstallPlan p) => p.WifiMode is Wpa2 or Wpa3;
    public static bool HasProfile(InstallPlan p) => IsPreset(p) || p.WifiMode == Profile;
    public static void ValidateName(string name, bool ssid)
    {
        int length = Encoding.UTF8.GetByteCount(name);
        if (string.IsNullOrWhiteSpace(name) || length > (ssid ? 32 : 256) || name.Any(char.IsControl) || name.IndexOfAny(['"', '$', '`']) >= 0)
            throw new ArgumentException(ssid ? "Wi-Fi 名称应为 1–32 字节，且不能包含控制字符、双引号、美元符号或反引号。" : "无线配置名称为空、过长或含不支持的脚本特殊字符，请使用不含控制字符、双引号、美元符号或反引号的名称。");
    }
    public static IWifiSettings Build(InstallPlan p) => p.WifiMode switch
    {
        "安装时手动选择" => new InteractiveWifiSettings(),
        "隐藏无线网络选择页" => new SkipWifiSettings(),
        Profile => p.NeedsWifiProfile ? throw new ArgumentException("无线配置 XML 未随方案保存，请重新导入或粘贴完整配置。") : ReadProfile(p.WifiProfileXml),
        Wpa2 or Wpa3 or Open => Parameters(p),
        _ => throw new ArgumentException("请选择有效的网络配置方式。")
    };
    private static ParameterizedWifiSettings Parameters(InstallPlan p)
    {
        ValidateName(p.Ssid, true);
        if (NeedsPassword(p))
        {
            if (p.NeedsWifiPassword) throw new ArgumentException("请重新输入 Wi-Fi 密码。");
            if (p.WifiPassword.Length is < 8 or > 63 || p.WifiPassword.Any(c => c < 32 || c > 126))
                throw new ArgumentException("WPA2 / WPA3 密码应为 8–63 位可打印 ASCII 字符。");
        }
        return new(p.Ssid, NeedsPassword(p) ? p.WifiPassword : "", p.ConnectWifi,
            p.WifiMode == Wpa3 ? WifiAuthentications.WPA3SAE : p.WifiMode == Open ? WifiAuthentications.Open : WifiAuthentications.WPA2PSK, p.HiddenWifi);
    }
    public static XmlWifiSettings ReadProfile(string xml)
    {
        if (string.IsNullOrWhiteSpace(xml) || xml.Length > MaxProfileLength) throw new ArgumentException("无线配置 XML 为空或超过 512 Ki 字符。");
        try
        {
            var doc = new XmlDocument { XmlResolver = null };
            using (var reader = XmlReader.Create(new StringReader(xml), new XmlReaderSettings { DtdProcessing = DtdProcessing.Prohibit, XmlResolver = null, MaxCharactersInDocument = MaxProfileLength })) doc.Load(reader);
            if (doc.DocumentElement is not { LocalName: "WLANProfile", NamespaceURI: WlanNamespace }) throw new ArgumentException("请选择 WLANProfile 无线配置 XML，不能使用 Windows 应答 XML。");
            if (doc.SelectNodes("//processing-instruction()")!.Count != 0) throw new ArgumentException("无线配置 XML 不支持处理指令。");
            var pending = new Stack<(XmlElement Element, int Depth)>(); pending.Push((doc.DocumentElement, 1)); int count = 0;
            while (pending.Count > 0)
            {
                var node = pending.Pop(); if (node.Depth > 64 || ++count > 10000) throw new ArgumentException("无线配置 XML 层级或元素过多。");
                foreach (var child in node.Element.ChildNodes.OfType<XmlElement>()) pending.Push((child, node.Depth + 1));
            }
            var profile = new XmlWifiSettings(doc.OuterXml);
            ValidateName(profile.Name, false); ValidateName(profile.Ssid, true);
            var ns = WlanNs(doc);
            foreach (XmlNode node in doc.SelectNodes("//w:sharedKey/w:protected", ns)!)
                if (node.InnerText is not ("false" or "0")) throw new ArgumentException("无线配置中的密钥已加密，不能可靠迁移到目标电脑。请使用 netsh wlan export profile key=clear 导出的配置，并妥善保管明文密钥。");
            // The fixed native connector uses profile.Name for both netsh name and ssid.
            if (profile.ConnectAutomatically && profile.Name != profile.Ssid)
                throw new ArgumentException("自动连接的无线配置名称需与 SSID 一致。请调整配置名称，或将 XML 的 connectionMode 设为 manual 后在目标系统手动连接。");
            return profile;
        }
        catch (Exception e) when (e is XmlException or System.Xml.Schema.XmlSchemaException or ConfigurationException or FormatException or InvalidOperationException)
        { throw new ArgumentException("无线配置 XML 格式、架构或 SSID 编码无效，请核对从 Windows 导出的完整 WLANProfile。", e); }
    }
    private static XmlNamespaceManager WlanNs(XmlDocument doc) { var ns = new XmlNamespaceManager(doc.NameTable); ns.AddNamespace("w", WlanNamespace); return ns; }
    private static string Normal(string value) => string.Join('\n', value.Replace("\r\n", "\n").Split('\n').Select(l => l.Trim()).Where(l => l.Length > 0));
    private static bool Related(string line) => line.Contains("WlanSvc", StringComparison.OrdinalIgnoreCase) || Regex.IsMatch(line, @"netsh\.exe\s+wlan\s+(?:add\s+profile|connect)\b", RegexOptions.IgnoreCase, TimeSpan.FromSeconds(1));
    private static readonly Lazy<string> WaitBlock = new(() =>
    {
        var doc = new UnattendGenerator().GenerateXml(Configuration.Default with { WifiSettings = new ParameterizedWifiSettings("WifiTemplate", "TestPassword123", false, WifiAuthentications.WPA2PSK, false) });
        return SystemMaintenanceSettings.KnownSpecializeBlocks(doc).Single(b => b.Contains("WlanSvc", StringComparison.Ordinal));
    });
    public record State(bool Enabled, bool Ambiguous, string Xml);
    public static State Inspect(XmlDocument doc)
    {
        var files = doc.SelectNodes("//*[local-name()='File']")!.Cast<XmlElement>().ToArray();
        var wifiFiles = files.Where(f => f.GetAttribute("path").Equals(Root + "Wifi.xml", StringComparison.OrdinalIgnoreCase)).ToArray();
        var ns = new XmlNamespaceManager(doc.NameTable); ns.AddNamespace("u", XmlMerge.Namespace);
        var lines = files.SelectMany(f => Normal(f.InnerText).Split('\n')).Concat(doc.SelectNodes("//u:Path|//u:CommandLine", ns)!.Cast<XmlElement>().Select(n => n.InnerText)).Where(Related).ToArray();
        bool mentioned = wifiFiles.Length > 0 || lines.Length > 0 || files.Any(f => f.GetAttribute("path").EndsWith("\\Wifi.xml", StringComparison.OrdinalIgnoreCase));
        if (wifiFiles.Length != 1) return new(false, mentioned, "");
        try
        {
            var profile = ReadProfile(wifiFiles[0].InnerText);
            var blocks = SystemMaintenanceSettings.KnownSpecializeBlocks(doc);
            string add = "netsh.exe wlan add profile filename=\"" + Root + "Wifi.xml\" user=all;";
            string connect = "netsh.exe wlan connect name=\"" + profile.Name + "\" ssid=\"" + profile.Name + "\";";
            var expected = Normal(WaitBlock.Value).Split('\n').Where(Related).Concat(new[] { add }).Concat(profile.ConnectAutomatically ? new[] { connect } : []).ToArray();
            bool enabled = blocks.Count(b => b == WaitBlock.Value) == 1 && blocks.Count(b => b == add) == 1 && (!profile.ConnectAutomatically || blocks.Count(b => b == connect) == 1) && lines.Length == expected.Length && expected.All(l => lines.Count(v => v == l) == 1);
            return new(enabled, !enabled, wifiFiles[0].InnerText);
        }
        catch (ArgumentException) { return new(false, true, ""); }
    }
    private static string BasicMode(XmlDocument doc)
    {
        var ns = new XmlNamespaceManager(doc.NameTable); ns.AddNamespace("u", XmlMerge.Namespace);
        return doc.SelectSingleNode("//u:OOBE/u:HideWirelessSetupInOOBE", ns)?.InnerText is "true" or "1" ? "隐藏无线网络选择页" : "安装时手动选择";
    }
    public static void ReadIntoPlan(XmlDocument doc, InstallPlan p, List<string> notes)
    {
        var state = Inspect(doc);
        bool preferXml = p.WifiMode == Profile;
        p.WifiMode = BasicMode(doc); p.WifiProfileXml = ""; p.NeedsWifiProfile = false; p.WifiPassword = ""; p.NeedsWifiPassword = false; p.Ssid = "";
        if (!state.Enabled)
        {
            if (state.Ambiguous) notes.Add("无线配置或安装执行管线未完整识别，原文保留；网络页不覆盖这些内容，请用原工具调整后重新导入。");
            return;
        }
        var profile = ReadProfile(state.Xml); var ns = WlanNs(profile.ProfileXml);
        string? Text(string path) => profile.ProfileXml.SelectSingleNode(path, ns)?.InnerText;
        string mode = Text("//w:authentication") switch { "WPA2PSK" => Wpa2, "WPA3SAE" => Wpa3, "open" => Open, _ => Profile };
        var candidate = new InstallPlan { WifiMode = mode, Ssid = profile.Ssid, WifiPassword = Text("//w:keyMaterial") ?? "", HiddenWifi = Text("//w:nonBroadcast") is "true" or "1", ConnectWifi = profile.ConnectAutomatically };
        bool canonical = false;
        if (mode != Profile)
        {
            try { canonical = XmlMerge.Canonical(((IProfileWifiSettings)Build(candidate)).ProfileXml.DocumentElement!) == XmlMerge.Canonical(profile.ProfileXml.DocumentElement!); }
            catch (ArgumentException) { }
        }
        if (!preferXml && canonical)
        {
            p.WifiMode = candidate.WifiMode; p.Ssid = candidate.Ssid; p.WifiPassword = candidate.WifiPassword; p.HiddenWifi = candidate.HiddenWifi; p.ConnectWifi = candidate.ConnectWifi;
        }
        else { p.WifiMode = Profile; p.WifiProfileXml = state.Xml; }
    }
    public static void ValidateImported(InstallPlan p, XmlDocument doc)
    {
        if (HasProfile(p) && Inspect(doc).Ambiguous) throw new ArgumentException("原 XML 的无线配置或执行管线未完整识别，不能用网络页覆盖；请用原工具调整后重新导入。");
    }
    public static string Summary(InstallPlan p) => p.WifiMode switch
    {
        Profile => "无线配置：使用 WLANProfile XML（名称、认证及连接方式由 XML 决定；可能含凭据）" + (p.NeedsWifiProfile || string.IsNullOrWhiteSpace(p.WifiProfileXml) ? "；需重新导入配置" : ""),
        Wpa2 or Wpa3 or Open => "无线配置：" + (p.WifiMode == Open ? "开放 Wi-Fi（无密码、不加密）" : p.WifiMode) + "；" + (p.HiddenWifi ? "隐藏网络" : "广播网络") + "；" + (p.ConnectWifi ? "自动连接" : "只添加配置，不发起连接") + (p.WifiMode == Open ? "（无无线链路加密，可能仍需门户认证）" : ""),
        _ => "无线配置：" + p.WifiMode
    };
    public static void NormalizeLoaded(InstallPlan p, JsonElement json)
    {
        if (json.TryGetProperty(nameof(InstallPlan.WifiProfileXml), out _) || p.ImportedXml is not { OriginalOmitted: false } data || data.SourceXml.Length == 0 || data.BaselineXml.Length == 0 || HasProfile(p)) return;
        var source = XmlMerge.Read(data.SourceXml); if (!Inspect(source).Enabled || p.WifiMode != BasicMode(XmlMerge.Read(data.BaselineXml))) return;
        var before = JsonSerializer.Deserialize<InstallPlan>(JsonSerializer.Serialize(p))!; before.ImportedXml = null; before.ConfirmWipeDisk = before.ConfirmFormatPartition = before.ConfirmDiskScript = true;
        var notes = new List<string>(); ReadIntoPlan(source, p, notes);
        var after = JsonSerializer.Deserialize<InstallPlan>(JsonSerializer.Serialize(before))!; ReadIntoPlan(source, after, new());
        try
        {
            var service = new GeneratorService();
            data.BaselineXml = XmlMerge.Apply(data.BaselineXml, Encoding.UTF8.GetString(service.GenerateStandalone(before)), XmlMerge.Read(Encoding.UTF8.GetString(service.GenerateStandalone(after)))).OuterXml;
            data.Notices = data.Notices.Where(n => !n.StartsWith("Wi-Fi 配置并非当前支持的", StringComparison.Ordinal)).Concat(notes).Distinct().ToArray();
        }
        catch (Exception e) when (e is ArgumentException or ConfigurationException or XmlException)
        { data.Blockers = data.Blockers.Append("旧方案的无线配置编辑基准无法可靠迁移，请重新导入原 XML。").Distinct().ToArray(); }
    }
}
