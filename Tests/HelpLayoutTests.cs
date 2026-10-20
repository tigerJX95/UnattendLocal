using UnattendLocal.Core;
using UnattendLocal.Desktop;

static class HelpLayoutTests
{
    public static int Run(GeneratorService generator)
    {
        int count = 0;
        void Check(bool value, string label) { if (!value) throw new Exception(label); count++; Console.WriteLine("PASS help layout " + label); }
        var p = InstallPlan.New(); p.Users[0].Password = "HelpLayout1!";
        p.UseStaticIPv4 = true;
        var vm = new MainViewModel { Plan = p };
        var mac = vm.HelpTips["staticmac"];
        Check(mac.Sections.Count == 3 && mac.Sections[0].IsError && !mac.Sections[1].IsError && !mac.Sections[2].IsError,
            "missing IPv4 MAC is red while disabled IPv6 and general instructions are ordinary text");
        Check(mac.Sections[0].Text.StartsWith("静态 IPv4：待配置；\n请填写目标网卡") && !mac.Body.Contains("待完善"), "pending label and specific correction have separate lines");
        var dns = vm.HelpTips["staticdns"].Body;
        Check(dns.Contains("\n请使用目标网络") && dns.Contains("\n工具不测试解析服务"), "DNS requirement and service limitation start on new lines");
        var ip = vm.HelpTips["staticipv4"].Body;
        Check(ip.Contains("\nIPv4 支持") && ip.Contains("\nIPv6 遵循") && ip.Contains("\n目标网卡驱动"), "protocol ranges and driver limitation have separate starts");
        string json = PlanStore.Serialize(p, true);
        _ = vm.HelpTips.Values.SelectMany(topic => topic.Sections).ToArray();
        Check(PlanStore.Serialize(p, true) == json, "help reads do not fill example values or alter saved configuration");
        p.StaticIPv4Mac = "02-AB-00-00-00-10"; p.StaticIPv4Address = "192.168.100.20"; p.StaticIPv4Prefix = 24;
        Check(vm.HelpTips["staticmac"].Sections.All(section => !section.IsError), "correcting IPv4 clears the stale red state");
        p.UseStaticIPv6 = true; p.StaticIPv6Address = "";
        mac = vm.HelpTips["staticmac"];
        Check(!mac.Sections[0].IsError && mac.Sections[1].IsError && !mac.Sections[2].IsError, "incomplete IPv6 does not color valid IPv4 or instructions");
        p.StaticIPv6Address = "2001:db8:a::20"; p.StaticIPv6Prefix = 64;
        byte[] before = generator.Generate(p);
        Check(vm.HelpTips["staticmac"].Sections.All(section => !section.IsError), "correcting IPv6 clears its red state");
        _ = HelpCatalog.Current(2, "staticmac", p, p.Users[0]);
        Check(before.SequenceEqual(generator.Generate(p)), "help presentation does not change generated XML");
        p.UseSetupDrivers = true; p.SetupDriverPaths = "";
        Check(vm.HelpTips["setupdrivers"].Sections[0].IsError && vm.HelpTips["setupdrivers"].Body.Contains("目录待配置"), "driver pending configuration remains a red diagnostic");
        p.ImportedXml = new ImportedXml { StaticIPv4ReadOnly = true };
        Check(vm.HelpTips["staticmac"].Sections.All(section => !section.IsError), "read-only preservation information is not a pending error");
        const string technical = "示例（https://example.test/a;mode=1；保留）对应 /24。命令 “Write-Host '说明；保留。'” 不改。";
        Check(HelpText.Paragraphs(technical) == "示例（https://example.test/a;mode=1；保留）对应 /24。\n命令 “Write-Host '说明；保留。'” 不改。", "quoted commands and parenthesized technical content remain intact");
        Check(HelpText.Paragraphs("第一项。\n\n第二项；\n第三项。") == "第一项。\n\n第二项；\n第三项。", "existing paragraph breaks are retained without blank-line inflation");
        var topics = Enumerable.Range(0, HelpCatalog.PageCount).SelectMany(HelpCatalog.ForPage).ToArray();
        Check(topics.Select(topic => topic.Id).Distinct().Count() == topics.Length && topics.All(topic => !string.IsNullOrWhiteSpace(topic.Body)), "all help topics remain uniquely available");
        Check(topics.All(topic => HelpText.Paragraphs(topic.Body) == topic.Body), "all catalog prose is formatted consistently and repeated access is stable");
        var removal = vm.AppRemovalChoices.First(item => item.Description.Contains("匹配项："));
        Check(removal.TooltipDescription.Split('\n').Contains("匹配项：") && removal.TooltipDescription.Contains('\n'), "application matching identifiers keep their own heading and lines");
        Check(SettingsSearchCatalog.All.Single(result => result.AppId == removal.Id).TooltipDescription == removal.TooltipDescription, "search result and application checkbox show the same complete multiline instructions");
        return count;
    }
}
