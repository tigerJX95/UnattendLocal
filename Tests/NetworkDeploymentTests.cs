using System.Text;
using System.Xml;
using UnattendLocal.Core;
using UnattendLocal.Desktop;

static class NetworkDeploymentTests
{
    public static int Run(GeneratorService generator)
    {
        int count = 0;
        void Check(bool ok, string title) { if (!ok) throw new Exception(title); count++; Console.WriteLine("PASS network deployment " + title); }
        void Reject(Action action, string title) {
            try { action(); }
            catch (ArgumentException e) { Check(e.Message.Contains("/0") && e.Message.Contains("实际网络"), title); return; }
            throw new Exception("Expected /0 rejection: " + title);
        }
        InstallPlan Plan() {
            var p = InstallPlan.New(); p.Users[0].Password = "NetworkDeployment1!";
            p.UseStaticIPv4 = true; p.StaticIPv4Mac = "02-AB-00-00-00-10";
            p.StaticIPv4Address = "192.168.100.1"; p.StaticIPv4Prefix = 24; p.StaticIPv4Gateway = "192.168.100.254";
            p.StaticIPv6Address = "2001:db8:a::20"; p.StaticIPv6Prefix = 64; p.StaticIPv6Gateway = "fe80::1";
            return p;
        }
        string Xml(InstallPlan p) => Encoding.UTF8.GetString(generator.Generate(p));
        XmlNamespaceManager Ns(XmlDocument d) { var n = new XmlNamespaceManager(d.NameTable); n.AddNamespace("u", XmlMerge.Namespace); return n; }
        XmlElement One(XmlDocument d, string path) => (XmlElement)d.SelectSingleNode(path, Ns(d))!;
        void Strip(XmlDocument d) { foreach (XmlComment c in d.SelectNodes("//comment()")!.Cast<XmlComment>().Where(c => c.Value?.StartsWith("UnattendLocal:v1 ") == true).ToArray()) c.ParentNode!.RemoveChild(c); }

        foreach (bool mask in new[] { false, true }) foreach (bool gateway in new[] { false, true }) {
            var p = Plan(); p.StaticIPv4Prefix = mask ? 24 : 0; p.UseStaticIPv4SubnetMask = mask; p.StaticIPv4SubnetMask = mask ? " 0.0.0.0 " : "255.255.255.0";
            if (!gateway) p.StaticIPv4Gateway = "";
            var vm = new MainViewModel { Plan = p };
            Check(vm.HasStaticIPDeploymentWarning && vm.StaticIPDeploymentWarning.Contains("0.0.0.0") && Recommendations.MissingInputs(p).Contains("/0"), "zero warning in active input mode " + mask + gateway);
            Reject(() => generator.Generate(p), "public generation blocks zero " + mask + gateway);
            Reject(() => generator.GenerateStandalone(p), "standalone generation blocks zero " + mask + gateway);
            var restored = PlanStore.Deserialize(PlanStore.Serialize(p, true));
            Check(StaticIPv4Settings.EffectivePrefix(restored) == 0 && restored.StaticIPv4Address == "192.168.100.1", "JSON preserves zero for correction " + mask + gateway);
            Reject(() => generator.Generate(restored), "saved zero remains blocked " + mask + gateway);
            if (mask) p.StaticIPv4SubnetMask = "255.255.255.0"; else p.StaticIPv4Prefix = 24;
            Check(!vm.HasStaticIPDeploymentWarning && Xml(p).Contains("192.168.100.1/24"), "correcting active input clears block " + mask + gateway);
        }
        var incomplete = Plan(); incomplete.StaticIPv4Mac = incomplete.StaticIPv4Address = ""; incomplete.StaticIPv4Prefix = 0;
        Check(StaticIPv4Settings.DeploymentWarning(incomplete).Contains("/0"), "zero warning appears before MAC and address are complete");
        var inactive = Plan(); inactive.StaticIPv4Prefix = 0; inactive.UseStaticIPv4 = false;
        Check(StaticIPv4Settings.DeploymentWarning(inactive) == "" && !Xml(inactive).Contains("Microsoft-Windows-TCPIP"), "disabled zero input is retained without applying it");
        inactive.UseStaticIPv4 = true; inactive.UseStaticIPv4SubnetMask = true; inactive.StaticIPv4SubnetMask = "255.255.255.0";
        Check(Xml(inactive).Contains("192.168.100.1/24"), "inactive prefix zero does not block active valid mask");
        inactive.UseStaticIPv4SubnetMask = false; inactive.StaticIPv4Prefix = 24; inactive.StaticIPv4SubnetMask = "0.0.0.0";
        Check(Xml(inactive).Contains("192.168.100.1/24"), "inactive zero mask does not block active valid prefix");

        foreach (string arch in new[] { "amd64", "arm64", "x86" }) foreach (bool snapshot in new[] { false, true }) foreach (bool ipv6 in new[] { false, true }) {
            var p = Plan(); p.Architecture = arch; p.UseStaticIPv6 = ipv6;
            var doc = XmlMerge.Read(Xml(p)); Strip(doc);
            string path = "//u:UnicastIpAddresses/u:IpAddress" + (ipv6 ? "[2]" : "[1]");
            One(doc, path).InnerText = ipv6 ? "2001:db8:a::20/0" : "192.168.100.1/0";
            if (ipv6) p.StaticIPv6Prefix = 0; else p.StaticIPv4Prefix = 0;
            if (snapshot) XmlPlanImport.AddSnapshot(doc, p);
            var imported = XmlPlanImport.Import(doc.OuterXml, generator);
            string label = arch + snapshot + ipv6;
            Check(imported.ImportedXml is { StaticIPv4ReadOnly: false, Blockers.Length: 0 } && (ipv6 ? imported.StaticIPv6Prefix : imported.StaticIPv4Prefix) == 0, "zero XML imports editable with reliable baseline " + label);
            Check(new MainViewModel { Plan = imported }.HasStaticIPDeploymentWarning, "imported zero is visible " + label);
            Reject(() => Xml(imported), "imported zero cannot roundtrip unchanged " + label);
            var loaded = PlanStore.Deserialize(PlanStore.Serialize(imported, true));
            Reject(() => Xml(loaded), "saved imported zero remains blocked " + label);
            if (ipv6) loaded.StaticIPv6Prefix = 64; else loaded.StaticIPv4Prefix = 24;
            var corrected = XmlMerge.Read(Xml(loaded));
            Check(One(corrected, path).InnerText == (ipv6 ? "2001:db8:a::20/64" : "192.168.100.1/24"), "zero import can be corrected and exported " + label);
            if (ipv6) imported.UseStaticIPv6 = false; else imported.UseStaticIPv4 = false;
            Check(!StaticIPv4Settings.DeploymentWarning(imported).Any() && !Xml(imported).Contains(ipv6 ? "2001:db8:a::20/0" : "192.168.100.1/0"), "explicitly removing owned zero stack permits export " + label);
        }
        foreach (bool ipv6 in new[] { false, true }) {
            var p = Plan(); p.UseStaticIPv6 = ipv6;
            var source = XmlMerge.Read(Xml(p)); Strip(source);
            One(source, "//u:UnicastIpAddresses/u:IpAddress" + (ipv6 ? "[2]" : "[1]")).InnerText = ipv6 ? "2001:db8:a::20/0" : "192.168.100.1/0";
            One(source, "//u:Interface/u:Identifier").InnerText = "Ethernet";
            var unknown = XmlPlanImport.Import(source.OuterXml, generator);
            Check(unknown.ImportedXml!.StaticIPv4ReadOnly && new MainViewModel { Plan = unknown }.HasStaticIPDeploymentWarning, "read-only zero source warning " + ipv6);
            Reject(() => Xml(unknown), "read-only source cannot bypass deployment check " + ipv6);
            var valid = XmlPlanImport.Import(Xml(p), generator);
            var changed = XmlMerge.Read(valid.ImportedXml!.SourceXml);
            One(changed, "//u:UnicastIpAddresses/u:IpAddress" + (ipv6 ? "[2]" : "[1]")).InnerText = ipv6 ? "2001:db8:a::20/0" : "192.168.100.1/0";
            valid.ImportedXml.SourceXml = changed.OuterXml;
            Reject(() => Xml(valid), "final merged document blocks zero hidden behind valid form " + ipv6);
            p.StaticIPv6Prefix = 0;
            if (ipv6) Reject(() => Xml(p), "active IPv6 zero is blocked");
            else Check(Xml(p).Contains("192.168.100.1/24"), "inactive IPv6 zero does not affect IPv4");
        }
        var dual = Plan(); dual.UseStaticIPv6 = true;
        var dualXml = XmlMerge.Read(Xml(dual));
        Check(dualXml.SelectNodes("//u:Route/u:Prefix", Ns(dualXml))!.Cast<XmlElement>().Select(e => e.InnerText).SequenceEqual(new[] { "0", "::/0" }), "normal default routes remain permitted");

        var subnetCases = new[] {
            ("192.168.100.1", 24, "255.255.255.0", "192.168.100.0", "192.168.100.255", "192.168.100.1", "192.168.100.254", 254UL),
            ("172.20.5.9", 12, "255.240.0.0", "172.16.0.0", "172.31.255.255", "172.16.0.1", "172.31.255.254", 1048574UL),
            ("10.4.5.6", 8, "255.0.0.0", "10.0.0.0", "10.255.255.255", "10.0.0.1", "10.255.255.254", 16777214UL),
            ("192.168.101.1", 23, "255.255.254.0", "192.168.100.0", "192.168.101.255", "192.168.100.1", "192.168.101.254", 510UL),
            ("10.0.0.1", 1, "128.0.0.0", "0.0.0.0", "127.255.255.255", "0.0.0.1", "127.255.255.254", 2147483646UL),
            ("192.0.2.0", 31, "255.255.255.254", "192.0.2.0", "", "192.0.2.0", "192.0.2.1", 2UL),
            ("192.0.2.1", 31, "255.255.255.254", "192.0.2.0", "", "192.0.2.0", "192.0.2.1", 2UL),
            ("192.0.2.1", 32, "255.255.255.255", "192.0.2.1", "", "192.0.2.1", "192.0.2.1", 1UL)
        };
        foreach (var (address, prefix, mask, network, broadcast, first, last, hosts) in subnetCases) foreach (bool maskMode in new[] { false, true }) {
            var p = Plan(); p.StaticIPv4Address = address; p.StaticIPv4Prefix = prefix; p.UseStaticIPv4SubnetMask = maskMode; p.StaticIPv4SubnetMask = mask;
            var info = StaticIPv4Settings.CalculateSubnet(p);
            Check(info.Network == network && info.Broadcast == (broadcast.Length == 0 ? null : broadcast) && info.FirstHost == first && info.LastHost == last && info.HostCount == hosts && info.Mask == mask, "independent expected subnet boundaries " + address + "/" + prefix + maskMode);
        }
        var viewModel = new MainViewModel { Plan = Plan() }; var notifications = new HashSet<string>(); viewModel.PropertyChanged += (_, e) => notifications.Add(e.PropertyName ?? "");
        string stored = PlanStore.Serialize(viewModel.Plan, true); string xmlBefore = Xml(viewModel.Plan);
        Check(viewModel.StaticIPv4SubnetPreview.Contains("192.168.100.0/24") && viewModel.StaticIPv4SubnetPreview.Contains("广播地址：192.168.100.255"), "live preview exposes actual network and broadcast");
        Check(stored == PlanStore.Serialize(viewModel.Plan, true) && xmlBefore == Xml(viewModel.Plan), "reading preview never mutates plan or XML");
        viewModel.Plan.StaticIPv4Prefix = 0;
        Check(viewModel.HasStaticIPDeploymentWarning && notifications.Contains(nameof(viewModel.StaticIPDeploymentWarning)) && notifications.Contains(nameof(viewModel.HasStaticIPDeploymentWarning)), "zero edit raises warning notifications");
        viewModel.Plan.StaticIPv4Prefix = 23;
        Check(!viewModel.HasStaticIPDeploymentWarning && viewModel.StaticIPv4SubnetPreview.Contains("192.168.101.255") && notifications.Contains(nameof(viewModel.StaticIPv4SubnetPreview)), "correction refreshes subnet without stale zero warning");
        viewModel.Plan.UseStaticIPv4 = false;
        Check(viewModel.StaticIPv4SubnetPreview == "", "disabled input does not show a misleading subnet");
        viewModel.Plan.UseStaticIPv4 = true; viewModel.Plan.StaticIPv4Address = "unfinished";
        Check(viewModel.StaticIPv4SubnetPreview.StartsWith("网段预览："), "partial address produces an explanation instead of crashing");
        // Reproduce the reported correction flow through the actual view model and generator.
        foreach (bool maskMode in new[] { false, true }) {
            var recovery = new MainViewModel { Plan = Plan() };
            recovery.Plan.UseStaticIPv4SubnetMask = maskMode;
            recovery.Plan.StaticIPv4Prefix = maskMode ? 24 : 0;
            recovery.Plan.StaticIPv4SubnetMask = maskMode ? "0.0.0.0" : "255.255.255.0";
            Reject(() => generator.Generate(recovery.Plan), "zero fails before correction " + maskMode);
            Check(recovery.IsStaticIPv4 && recovery.CanEditStaticIPv4, "failed check does not disable network editing " + maskMode);
            recovery.StaticIPv4SubnetInput = maskMode ? "/24" : "255.255.255.0";
            Check(recovery.Plan.StaticIPv4Prefix == 24 && StaticIPv4Settings.EffectivePrefix(recovery.Plan) == 24, "unified edit updates the actual prefix " + maskMode);
            Check(!recovery.HasStaticIPDeploymentWarning && Xml(recovery.Plan).Contains("192.168.100.1/24"), "correcting unified input permits export " + maskMode);
            Check(recovery.StaticIPv4SubnetInput == (maskMode ? "/24" : "255.255.255.0"), "unified editor retains the typed format " + maskMode);
            recovery.StaticIPv4SubnetInput = maskMode ? "255.255.254.0" : "/23";
            Check(!recovery.HasStaticIPDeploymentWarning && Xml(recovery.Plan).Contains("192.168.100.1/23"), "changing format updates generated prefix " + maskMode);
            recovery.Plan.UseStaticIPv4 = false; string previousInput = recovery.StaticIPv4SubnetInput;
            recovery.StaticIPv4SubnetInput = "/25";
            Check(recovery.StaticIPv4SubnetInput == previousInput, "disabled protocol cannot be changed by an edit binding " + maskMode);
        }
        var readonlyRecovery = new MainViewModel { Plan = Plan() };
        readonlyRecovery.Plan.ImportedXml = new ImportedXml { StaticIPv4ReadOnly = true };
        readonlyRecovery.StaticIPv4SubnetInput = "/25";
        Check(!readonlyRecovery.Plan.UseStaticIPv4SubnetMask, "read-only source is not silently changed by an edit binding");
        var errorReview = SummaryPresentation.FromText("生成前待填：请修正静态 /0\n安装时可能需要操作：语言与区域");
        Check(errorReview.Reviews.Single(r => r.Title == "生成前待填").IsError, "blocking review is an error");
        Check(!errorReview.Reviews.Single(r => r.Title == "安装时可能需要操作").IsError, "interactive steps remain ordinary reminders");
        Check(!SummaryPresentation.FromText("生成前待填：未发现空缺必填项").Reviews.Single().IsError, "successful review is not an error");
        var importErrors = new MainViewModel { Plan = Plan() };
        importErrors.Plan.ImportedXml = new ImportedXml { Blockers = ["示例阻止原因"], Notices = ["示例原文保留说明"] };
        Check(importErrors.HasXmlImportErrors && importErrors.XmlImportErrors == "示例阻止原因" && importErrors.XmlImportNotices == "示例原文保留说明", "import blockers and ordinary notices have separate presentation");
        importErrors.Plan.ImportedXml = new ImportedXml { OriginalOmitted = true };
        Check(importErrors.HasXmlImportErrors && importErrors.XmlImportErrors.Contains("重新导入"), "omitted original XML is presented as an error");
        foreach (string message in new[] { "静态 IPv4：待配置；请填写地址", "静态 IPv6：待配置；请填写地址", "网段预览：地址无效", "安装驱动目录：已启用，目录待配置。", "AppLocker：已启用，策略 XML 待补充或校验", "XML 导入：已导入；存在阻止导出的项目", "XML 导入：XML 原文未随方案保存，请重新导入" })
            Check(DiagnosticText.IsErrorSummary(message), "known diagnostic state is an error: " + message);
        foreach (string message in new[] { "静态 IPv4：网卡 02-AB-00-00-00-10；192.168.100.1/24", "静态 IPv4：原 XML 的网络配置只读保留", "自定义脚本：包含待配置说明的用户文本", "XML 导入：已导入；部分内容原文保留" })
            Check(!DiagnosticText.IsErrorSummary(message), "ordinary information is not a diagnostic error: " + message);
        return count;
    }
}
