using System.Text;
using System.Text.Json.Nodes;
using System.Xml;
using UnattendLocal.Core;
using UnattendLocal.Desktop;

static class NativeDeploymentTests
{
    public static int Run(GeneratorService g)
    {
        int count = 0;
        void Check(bool ok, string message) { if (!ok) throw new Exception(message); count++; Console.WriteLine("PASS V1.7 " + message); }
        void Reject(Action action, string message) { try { action(); } catch (ArgumentException) { Check(true, message); return; } throw new Exception("Expected rejection: " + message); }
        InstallPlan Plan() { var p = InstallPlan.New(); p.Users[0].Password = "SyntheticDeployment17!"; p.ComputerName = "DEMO-PC17"; return p; }
        string Xml(InstallPlan p) => Encoding.UTF8.GetString(g.Generate(p));
        string Strip(string text) { var d = XmlMerge.Read(text); foreach (var c in d.SelectNodes("//comment()")!.Cast<XmlComment>().ToArray()) c.ParentNode!.RemoveChild(c); return d.OuterXml; }
        XmlElement[] Nodes(XmlDocument d, string name) => d.SelectNodes("//*[local-name()='" + name + "']")!.Cast<XmlElement>().ToArray();
        bool Same(string a, string b) => XmlMerge.Equal(XmlMerge.Read(a).DocumentElement, XmlMerge.Read(b).DocumentElement);
        XmlElement Add(XmlNode parent, string name, string? value = null) { var n = parent.OwnerDocument!.CreateElement(name, XmlMerge.Namespace); if (value != null) n.InnerText = value; parent.AppendChild(n); return n; }
        var defaults = Plan(); Check(!defaults.UseWorkgroup && !defaults.UsePeLogPath && !Xml(defaults).Contains("<JoinWorkgroup>") && !Xml(defaults).Contains("<LogPath>"), "safe defaults omit both new native settings");
        var legacy = PlanStore.Deserialize("{\"SchemaVersion\":1,\"Users\":[]}"); Check(!legacy.UseWorkgroup && !legacy.UsePeLogPath, "old non-imported plans keep both settings disabled");
        foreach (string name in new[] { "A", "a1", "LAB-17", "WORKGROUP", "A23456789012345" })
        { WorkgroupSettings.ValidateName(name); Check(true, "workgroup supported name " + name); }
        foreach (string? name in new string?[] { null, "", " ", "123", "中文", "A234567890123456", "-LAB", "LAB-", "MY GROUP", "LAB_17", "LAB.DOMAIN", "A/B", "A\\B", "A\nB", "A\0B", "ＡＢＣ" })
            Reject(() => WorkgroupSettings.ValidateName(name), "unsupported workgroup rejected");
        foreach (string path in new[] { @"C:\", @"D:\SetupLogs", @"D:\Setup Logs\", @"E:\日志\安装 & 日志", "D:\\" + new string('a', 128) + "\\" + new string('b', 127) })
        { PeLogSettings.ValidatePath(path); Check(true, "valid PE directory accepted"); }
        foreach (string? path in new string?[] { null, "", "D:", "D:relative", @"\Logs", @"\\server\share", @"\\?\D:\Logs", @"X:\Logs", @"%TEMP%\Logs", @"D:/Logs", @"D:\..\Logs", @"D:\.\Logs", @"D:\\Logs", @"D:\Logs.\Part", "D:\\Logs ", @"D:\CON\Logs", @"D:\nul.txt\logs", @"D:\COM1\Logs", @"D:\LPT9\Logs", @"D:\a?b", @"D:\a*b", @"D:\a:b", @"D:\a|b", "D:\\a\nb", "D:\\a\u0000b", "D:\\a\uD800b", @"D:\setup.log", @"D:\setup.TXT", @"D:\trace.etl", "D:\\" + new string('a', 256), "D:\\" + new string('a', 128) + "\\" + new string('b', 128) })
            Reject(() => PeLogSettings.ValidatePath(path), "invalid PE directory rejected");
        var conflict = Plan(); conflict.UseWorkgroup = true; conflict.WorkgroupName = conflict.ComputerName.ToLowerInvariant(); Reject(() => g.Generate(conflict), "known computer/workgroup collision rejected");
        conflict.UseWorkgroup = false; conflict.WorkgroupName = null!; conflict.PeLogPath = null!; Check(g.Generate(conflict).Length > 0, "disabled retained inputs do not block generation");
        foreach (var arch in new[] { "amd64", "arm64", "x86" })
        foreach (bool group in new[] { false, true })
        foreach (bool logs in new[] { false, true })
        {
            var p = Plan(); p.Architecture = arch; p.UseWorkgroup = group; p.WorkgroupName = "LAB-17"; p.UsePeLogPath = logs; p.PeLogPath = @"D:\Setup Logs\日志";
            string output = Xml(p); var doc = XmlMerge.Read(output); var wg = Nodes(doc, "JoinWorkgroup"); var lp = Nodes(doc, "LogPath");
            Check(wg.Length == (group ? 1 : 0) && lp.Length == (logs ? 1 : 0), "independent native field combinations " + arch + group + logs);
            if (group) Check(wg[0].InnerText == p.WorkgroupName && wg[0].ParentNode!.ParentNode!.Attributes!["processorArchitecture"]!.Value == arch && wg[0].ParentNode!.ParentNode!.ParentNode!.Attributes!["pass"]!.Value == "specialize", "workgroup native value/architecture/pass");
            if (logs) Check(lp[0].InnerText == p.PeLogPath && lp[0].ParentNode!.Attributes!["processorArchitecture"]!.Value == arch && lp[0].ParentNode!.ParentNode!.Attributes!["pass"]!.Value == "windowsPE", "PE log native value/architecture/pass");
            foreach (bool snapshot in new[] { false, true })
            {
                string source = snapshot ? output : Strip(output); var restored = XmlPlanImport.Import(source, g);
                Check(restored.UseWorkgroup == group && restored.UsePeLogPath == logs && (!group || restored.WorkgroupName == p.WorkgroupName) && (!logs || restored.PeLogPath == p.PeLogPath), "actual XML restores both fields snapshot=" + snapshot);
                Check(Same(source, Xml(restored)), "unchanged native XML roundtrip snapshot=" + snapshot);
            }
        }
        var both = Plan(); both.UseWorkgroup = true; both.WorkgroupName = "LAB-17"; both.UsePeLogPath = true; both.PeLogPath = @"D:\Logs";
        string original = Xml(both);
        foreach (bool secrets in new[] { false, true })
        {
            var copy = PlanStore.Deserialize(PlanStore.Serialize(both, secrets)); Check(copy.UseWorkgroup && copy.WorkgroupName == "LAB-17" && copy.UsePeLogPath && copy.PeLogPath == @"D:\Logs", "plan keeps names and paths secrets=" + secrets);
        }
        var editing = XmlPlanImport.Import(original, g); editing.WorkgroupName = "NEW-TEAM"; editing.PeLogPath = @"E:\SetupLogs";
        var changed = XmlMerge.Read(Xml(editing)); Check(Nodes(changed, "JoinWorkgroup").Single().InnerText == "NEW-TEAM" && Nodes(changed, "LogPath").Single().InnerText == @"E:\SetupLogs", "recognized imports support editing both fields");
        editing.UseWorkgroup = false; changed = XmlMerge.Read(Xml(editing)); Check(Nodes(changed, "JoinWorkgroup").Length == 0 && Nodes(changed, "LogPath").Length == 1, "cancel workgroup preserves PE log setting");
        editing.UsePeLogPath = false; changed = XmlMerge.Read(Xml(editing)); Check(Nodes(changed, "JoinWorkgroup").Length == 0 && Nodes(changed, "LogPath").Length == 0, "cancel both recognized fields");
        var reopen = XmlPlanImport.Import(Xml(editing), g); Check(!reopen.UseWorkgroup && !reopen.ImportedXml!.WorkgroupReadOnly && !Xml(editing).Contains("Microsoft-Windows-UnattendedJoin"), "cancelled workgroup leaves no empty component on reimport"); reopen.UseWorkgroup=true;reopen.WorkgroupName="REOPEN-17";Check(Xml(reopen).Contains("REOPEN-17"),"cancelled then reimported workgroup can be enabled again");
        var optional = XmlMerge.Read(Strip(original)); Nodes(optional, "JoinWorkgroup").Single().ParentNode!.ParentNode!.Attributes!.RemoveNamedItem("versionScope"); var optionalPlan = XmlPlanImport.Import(optional.OuterXml, g); optionalPlan.UseWorkgroup = false; Check(!Xml(optionalPlan).Contains("<JoinWorkgroup>"), "optional component metadata does not prevent cancellation");
        var sibling = XmlMerge.Read(Strip(original)); var setup = Nodes(sibling, "LogPath").Single().ParentNode!; Add(setup, "UnknownSibling17", "retain"); var siblingPlan = XmlPlanImport.Import(sibling.OuterXml, g); siblingPlan.UsePeLogPath = false; var siblingOut = Xml(siblingPlan); Check(siblingOut.Contains("UnknownSibling17") && !siblingOut.Contains("<LogPath>"), "cancel log directory preserves unrelated Setup sibling");
        foreach (string kind in new[] { "duplicate", "domain", "credentials", "extra", "attribute", "nested", "wrongPass", "foreign", "badValue", "badComponent", "componentSibling", "duplicateComponent" })
        {
            var d = XmlMerge.Read(Strip(original)); var n = Nodes(d, "JoinWorkgroup").Single(); var id = (XmlElement)n.ParentNode!; var c = (XmlElement)id.ParentNode!;
            switch (kind)
            {
                case "duplicate": id.AppendChild(n.CloneNode(true)); break;
                case "domain": Add(id, "JoinDomain", "example.invalid"); break;
                case "credentials": Add(Add(id, "Credentials"), "Password", "SyntheticOnly17"); break;
                case "extra": Add(id, "TimeoutPeriodInMinutes", "15"); break;
                case "attribute": n.SetAttribute("extra", "true"); break;
                case "nested": n.InnerXml = "<Unknown xmlns='" + XmlMerge.Namespace + "'>text</Unknown>"; break;
                case "wrongPass": c.ParentNode!.RemoveChild(c); var stage = d.DocumentElement!.ChildNodes.OfType<XmlElement>().Single(n=>n.GetAttribute("pass")=="auditUser"); stage.AppendChild(c); break;
                case "foreign": id.RemoveChild(n); var other = d.CreateElement("JoinWorkgroup", "urn:unknown"); other.InnerText = "TEAM"; id.AppendChild(other); break;
                case "badValue": n.InnerText = "中文工作组"; break;
                case "badComponent": c.SetAttribute("language", "wrong"); break;
                case "componentSibling": Add(c, "UnknownSibling", "retain"); break;
                case "duplicateComponent": c.ParentNode!.AppendChild(c.CloneNode(true)); break;
            }
            var p = XmlPlanImport.Import(d.OuterXml, g); Check(p.ImportedXml!.WorkgroupReadOnly && !p.UseWorkgroup && p.ImportedXml.Notices.Contains(WorkgroupSettings.ReadOnlyNotice), "workgroup ambiguity explicit read-only: " + kind);
            Check(Same(d.OuterXml, Xml(p)), "unknown workgroup content survives: " + kind); p.ComputerName = "OTHER17"; if(kind=="duplicateComponent")Reject(()=>g.Generate(p), "duplicate components prevent ambiguous unrelated merge"); else Check(Xml(p).Contains("OTHER17"), "unrelated edit preserves unknown join config: " + kind); p.UseWorkgroup = true; p.WorkgroupName = "TEAM"; Reject(() => g.Generate(p), "unknown join cannot be overwritten: " + kind);
        }
        foreach (string kind in new[] { "duplicate", "attribute", "nested", "wrongParent", "foreign", "badValue", "componentMeta", "customPe", "duplicateSetup" })
        {
            var d = XmlMerge.Read(Strip(original)); var n = Nodes(d, "LogPath").Single(); var c = (XmlElement)n.ParentNode!;
            switch (kind)
            {
                case "duplicate": c.AppendChild(n.CloneNode(true)); break;
                case "attribute": n.SetAttribute("extra", "yes"); break;
                case "nested": n.InnerXml = "<Unknown xmlns='" + XmlMerge.Namespace + "'/>"; break;
                case "wrongParent": c.RemoveChild(n); Add(c, "Unknown").AppendChild(n); break;
                case "foreign": c.RemoveChild(n); var other = d.CreateElement("LogPath", "urn:unknown"); other.InnerText = @"D:\Logs"; c.AppendChild(other); break;
                case "badValue": n.InnerText = @"\\server\share"; break;
                case "componentMeta": c.SetAttribute("language", "wrong"); break;
                case "customPe": var run = Add(c, "RunSynchronous"); var cmd = Add(run, "RunSynchronousCommand"); Add(cmd, "Order", "999"); Add(cmd, "Path", @"cmd.exe /c X:\custom-install.cmd"); break;
                case "duplicateSetup": c.ParentNode!.AppendChild(c.CloneNode(true)); break;
            }
            var p = XmlPlanImport.Import(d.OuterXml, g); Check(p.ImportedXml!.PeLogReadOnly && !p.UsePeLogPath && p.ImportedXml.Notices.Contains(PeLogSettings.ReadOnlyNotice), "PE log ambiguity explicit read-only: " + kind);
            if(p.ImportedXml!.RequiresDiskReview){Check(!p.ConfirmImportedXmlRisk,"custom PE still requires explicit risk review");p.ConfirmImportedXmlRisk=true;} Check(Same(d.OuterXml, Xml(p)), "unknown PE log content survives: " + kind); p.UsePeLogPath = true; p.PeLogPath = @"D:\NewLogs"; Reject(() => g.Generate(p), "unknown PE log cannot be overwritten: " + kind);
        }
        var migrated = XmlPlanImport.Import(original, g); migrated.ComputerName = "PENDING17"; migrated.Architecture = "arm64";
        var oldBase = Plan(); string oldBaseXml = Encoding.UTF8.GetString(g.GenerateStandalone(oldBase));
        var json = JsonNode.Parse(PlanStore.Serialize(migrated, true))!; foreach (var f in new[] { "UseWorkgroup", "WorkgroupName", "UsePeLogPath", "PeLogPath" }) json.AsObject().Remove(f);
        json["ImportedXml"]!["WorkgroupImportVersion"] = 0; json["ImportedXml"]!["PeLogImportVersion"] = 0; json["ImportedXml"]!["BaselineXml"] = oldBaseXml;
        var migratedPlan = PlanStore.Deserialize(json.ToJsonString()); Check(migratedPlan.UseWorkgroup && migratedPlan.WorkgroupName == "LAB-17" && migratedPlan.UsePeLogPath && migratedPlan.PeLogPath == @"D:\Logs" && migratedPlan.ComputerName == "PENDING17" && migratedPlan.Architecture == "arm64", "missing old fields migrate without dropping pending name/architecture");
        Check(Xml(migratedPlan).Contains("PENDING17"), "migrated editing baseline can export pending changes");
        var ambiguousJson = JsonNode.Parse(PlanStore.Serialize(XmlPlanImport.Import(original, g), true))!; ambiguousJson["ImportedXml"]!["WorkgroupImportVersion"] = 0; ambiguousJson["UseWorkgroup"] = false;
        var ambiguous = PlanStore.Deserialize(ambiguousJson.ToJsonString()); Check(!ambiguous.UseWorkgroup && ambiguous.ImportedXml!.Blockers.Any(n => n.Contains("工作组编辑基准")), "ambiguous old baseline requests reimport without overwriting explicit choice"); Reject(() => g.Generate(ambiguous), "unmigrated baseline blocks unsafe export");
        var risk = Plan(); risk.ConfirmWipeDisk = true; risk.UsePeLogPath = true; Check(!risk.ConfirmWipeDisk, "enabling PE logs clears disk consent"); risk.ConfirmWipeDisk = true; risk.PeLogPath = @"D:\Logs"; Check(!risk.ConfirmWipeDisk, "changing PE path clears disk consent"); risk.ConfirmWipeDisk = true; risk.PeLogPath = @"D:\Logs"; Check(risk.ConfirmWipeDisk, "unchanged path does not invalidate consent");
        var rec = Plan(); rec.UseWorkgroup = rec.UsePeLogPath = true; rec.WorkgroupName = "TEAM"; rec.PeLogPath = @"D:\Logs"; Recommendations.Apply(rec, "wired"); Check(rec.UseWorkgroup && rec.WorkgroupName == "TEAM" && rec.UsePeLogPath && rec.PeLogPath == @"D:\Logs", "recommendations preserve both installation choices");
        var vm = new MainViewModel { Plan = both }; vm.Plan.UseWorkgroup = false; Check(!vm.IsWorkgroup && vm.WorkgroupSummary.Contains("不写入"), "workgroup editor and summary follow enabled state"); vm.Plan.UseWorkgroup = true; vm.Plan.UsePeLogPath = false; Check(vm.IsWorkgroup && !vm.IsPeLog && vm.PeLogSummary.Contains("默认"), "two editor states remain independent");
        vm.Plan.UsePeLogPath = true; Check(vm.HelpTips["workgroup"].Body.Contains("LAB-17") && vm.HelpTips["pelog"].Body.Contains(@"D:\Logs"), "dynamic help reflects current name and path");
        var sections = SummaryPresentation.FromText(Recommendations.Summary(both)); Check(sections.Sections.Single(s => s.Kind == "network").Rows.Any(n => n.SourceLabel == "工作组") && sections.Sections.Single(s => s.Kind == "protection").Rows.Any(n => n.SourceLabel == "PE 日志目录"), "both summaries appear in appropriate groups");
        Check(SettingsSearchCatalog.Search("JoinWorkgroup", 5).Any(n => n.Target == "WorkgroupCheck") && SettingsSearchCatalog.Search("LogPath", 3).Any(n => n.Target == "PeLogEditor"), "both features have scoped search targets");
        var noFields = Plan(); noFields.WorkgroupName = "RetainedDisabled17"; noFields.PeLogPath = @"D:\RetainedDisabled17"; var snapshotComment=XmlMerge.Read(Xml(noFields)).SelectNodes("//comment()")!.Cast<XmlComment>().Single(c=>c.Value!.StartsWith("UnattendLocal:v1 ")).Value!;var snapshotJson=JsonNode.Parse(Encoding.UTF8.GetString(Convert.FromBase64String(snapshotComment.Split(' ',3)[2])))!;Check(snapshotJson["WorkgroupName"]!.GetValue<string>()=="" && snapshotJson["PeLogPath"]!.GetValue<string>()=="" && PlanStore.Serialize(noFields, false).Contains("RetainedDisabled17"), "disabled values stay in saved plans but not XML restoration notes");
        var domainBase = Plan(); domainBase.AccountMode = "安装时创建本地账户"; var domainDoc = XmlMerge.Read(Strip(Xml(domainBase))); var s = domainDoc.DocumentElement!.ChildNodes.OfType<XmlElement>().Single(n => n.GetAttribute("pass") == "specialize"); var comp = Add(s, "component");
        foreach (var a in new Dictionary<string, string> { ["name"] = "Microsoft-Windows-UnattendedJoin", ["processorArchitecture"] = "amd64", ["publicKeyToken"] = "31bf3856ad364e35", ["language"] = "neutral", ["versionScope"] = "nonSxS" }) comp.SetAttribute(a.Key, a.Value);
        var identification = Add(comp, "Identification"); Add(identification, "JoinDomain", "example.invalid"); Add(Add(identification, "Credentials"), "Password", "SyntheticDomainCredential17"); Add(identification, "MachinePassword", "SyntheticMachineCredential17"); Add(Add(identification, "Provisioning"), "AccountData", "SyntheticProvisioning17");
        var domainPlan = XmlPlanImport.Import(domainDoc.OuterXml, g); Check(domainPlan.ImportedXml!.WorkgroupReadOnly && Same(domainDoc.OuterXml, Xml(domainPlan)), "domain join is preserved without presenting an editable workgroup");
        string preview = GeneratorService.Preview(g.Generate(domainPlan)); Check(!preview.Contains("SyntheticDomainCredential17") && !preview.Contains("SyntheticMachineCredential17") && !preview.Contains("SyntheticProvisioning17") && preview.Contains("example.invalid"), "domain password, machine password and provisioning payload are redacted only in preview");
        Reject(() => XmlPlanImport.Import(preview.Split('\n', 2)[1], g), "redacted domain preview cannot be imported as installable XML");
        Check(!PlanStore.Serialize(domainPlan, false).Contains("SyntheticDomainCredential17") && Xml(domainPlan).Contains("SyntheticDomainCredential17"), "omit-secrets plan excludes original credentials while full export preserves them");
        return count;
    }
}
