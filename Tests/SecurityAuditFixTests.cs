using System.Text;
using System.Xml;
using UnattendLocal.Core;

// Reproductions from the V1.12 audit. No imported command is executed.
static class SecurityAuditFixTests
{
    public static int Run(GeneratorService generator)
    {
        int count = 0;
        void Check(bool value, string message) { if (!value) throw new Exception(message); count++; Console.WriteLine("PASS V1.12.1 " + message); }
        InstallPlan Seed() { var p = InstallPlan.New(); p.Users[0].Password = "Synthetic1121!"; return p; }
        string Output(InstallPlan p) => Encoding.UTF8.GetString(generator.Generate(p));
        XmlDocument Plain(string xml) { var d = XmlMerge.Read(xml); foreach (XmlComment c in d.SelectNodes("//comment()")!.Cast<XmlComment>().ToArray()) c.ParentNode!.RemoveChild(c); return d; }
        XmlElement Stage(XmlDocument doc) => doc.SelectNodes("//*[local-name()='File']")!.Cast<XmlElement>().Single(f => f.GetAttribute("path").EndsWith("\\Specialize.ps1"));
        XmlNamespaceManager Ns(XmlDocument doc) { var ns = new XmlNamespaceManager(doc.NameTable); ns.AddNamespace("u", XmlMerge.Namespace); return ns; }

        var doc = Plain(Output(Seed())); var ns = Ns(doc);
        var sync = (XmlElement)doc.SelectSingleNode("//u:settings[@pass='specialize']/u:component[@name='Microsoft-Windows-Deployment']/u:RunSynchronous", ns)!;
        var cmd = doc.CreateElement("RunSynchronousCommand", XmlMerge.Namespace); sync.AppendChild(cmd);
        void Add(XmlElement parent, string name, string value) { var n = doc.CreateElement(name, XmlMerge.Namespace); n.InnerText = value; parent.AppendChild(n); }
        Add(cmd, "Order", "20"); Add(cmd, "Path", "cmd.exe /c ver");
        var creds = doc.CreateElement("Credentials", XmlMerge.Namespace); cmd.AppendChild(creds);
        Add(creds,"Domain","AUDIT"); Add(creds,"Username","SyntheticUser"); Add(creds,"Password","FAKE_NATIVE_SECRET_1121");
        byte[] exported = generator.Generate(XmlPlanImport.Import(doc.OuterXml, generator));
        Check(Encoding.UTF8.GetString(exported).Contains("FAKE_NATIVE_SECRET_1121"), "native credential preserved in installation export");
        Check(!GeneratorService.Preview(exported).Contains("FAKE_NATIVE_SECRET_1121"), "native deployment credential redacted from preview");
        Check(GeneratorService.Preview(exported).Contains("自定义脚本"), "preview states custom script redaction boundary");

        var plan = Seed(); plan.RemovedApps = new(["RemoveOneDrive", "RemoveCalculator"]);
        foreach (string newline in new[] { "\n", "\r\n" })
        {
            doc = Plain(LegacyRemovalFixture.FromCurrent(Output(plan)));
            const string literal = "Write-Output 'C:\\\\Audit\\\\KeepDouble';";
            var stage = Stage(doc);
            stage.InnerText = stage.InnerText.Replace("$scripts = @(\n", "$scripts = @(\n\t{\n\t\t" + literal + "\n\t};\n").Replace("\r\n", "\n").Replace("\n", newline);
            var imported = XmlPlanImport.Import(doc.OuterXml, generator);
            Check(!imported.ImportedXml!.AppRemovalReadOnly, "legacy app list remains editable " + newline.Length);
            Check(Stage(XmlMerge.Read(Output(imported))).InnerText.Contains(literal), "unchanged legacy payload preserved " + newline.Length);
            imported.RemovedApps.Remove("RemoveCalculator");
            Check(Stage(XmlMerge.Read(Output(imported))).InnerText.Contains(literal), "editing legacy apps preserves unrelated doubled slashes " + newline.Length);
        }

        plan = Seed(); plan.DisableFastStartup = true;
        doc = Plain(Output(plan)); Stage(doc).InnerText = Stage(doc).InnerText.Replace("HiberbootEnabled /t REG_DWORD /d 0", "HiberbootEnabled /t REG_DWORD /d 1");
        var opposite = XmlPlanImport.Import(doc.OuterXml, generator);
        Check(!opposite.DisableFastStartup, "opposite registry value does not enable DisableFastStartup");
        Check(Stage(XmlMerge.Read(Output(opposite))).InnerText.Contains("HiberbootEnabled /t REG_DWORD /d 1"), "unknown opposite value remains in unchanged export");
        XmlPlanImport.AddSnapshot(doc, plan);
        Check(!XmlPlanImport.Import(doc.OuterXml, generator).DisableFastStartup, "stale reversible settings note cannot override actual opposite registry value");
        foreach (string property in new[] { nameof(InstallPlan.DisableFastStartup), nameof(InstallPlan.EnableLongPaths), nameof(InstallPlan.DisableWindowsUpdate), nameof(InstallPlan.DisableAppSuggestions), nameof(InstallPlan.DisablePointerPrecision), nameof(InstallPlan.HideEdgeFre), nameof(InstallPlan.DisableEdgeStartupBoost), nameof(InstallPlan.ClassicContextMenu), nameof(InstallPlan.ShowFileExtensions), nameof(InstallPlan.HideInfoTip), nameof(InstallPlan.LaunchToThisPC), nameof(InstallPlan.ShowEndTask), nameof(InstallPlan.DisableWidgets), nameof(InstallPlan.LeftTaskbar), nameof(InstallPlan.HideTaskViewButton), nameof(InstallPlan.ShowAllTrayIcons), nameof(InstallPlan.DisableBingResults), nameof(InstallPlan.DeleteEdgeDesktopIcon), nameof(InstallPlan.TurnOffSystemSounds), nameof(InstallPlan.PreventDeviceEncryption) })
        {
            plan = Seed(); typeof(InstallPlan).GetProperty(property)!.SetValue(plan, true);
            var result = XmlPlanImport.Import(Plain(Output(plan)).OuterXml, generator);
            Check((bool)typeof(InstallPlan).GetProperty(property)!.GetValue(result)!, "complete native flag recognized " + property);
        }

        plan = Seed(); plan.AllowUnsupportedUpgrade = true; plan.RemovedApps.Add("RemoveCalculator");
        doc = Plain(Output(plan)); ns = Ns(doc);
        var commands = doc.SelectNodes("//u:settings[@pass='specialize']//u:RunSynchronousCommand", ns)!.Cast<XmlElement>().ToArray();
        var extraction = commands.Single(c => c.SelectSingleNode("u:Path", ns)!.InnerText.Contains("Extensions.ExtractScript"));
        var invocation = commands.Single(c => c.SelectSingleNode("u:Path", ns)!.InnerText.EndsWith("Specialize.ps1\""));
        var a = extraction.SelectSingleNode("u:Order", ns)!; var b = invocation.SelectSingleNode("u:Order", ns)!;
        (a.InnerText, b.InnerText) = (b.InnerText, a.InnerText);
        var reordered = XmlPlanImport.Import(doc.OuterXml, generator);
        Check(!reordered.AllowUnsupportedUpgrade, "invocation before extraction does not map MoSetup");
        Check(reordered.ImportedXml!.AppRemovalReadOnly && !reordered.RemovedApps.Contains("RemoveCalculator"), "invalid execution pipeline prevents app mapping");
        a.InnerText = b.InnerText;
        Check(!XmlPlanImport.Import(doc.OuterXml, generator).AllowUnsupportedUpgrade, "duplicate native order rejected for mapping");

        // Numeric range errors invalidate the native specialize pipeline, while
        // XML sibling order alone must not invalidate a valid numeric sequence.
        plan = Seed(); plan.DisableFastStartup = true; plan.ClassicContextMenu = true;
        doc = Plain(Output(plan)); ns = Ns(doc);
        var parent = doc.SelectSingleNode("//u:settings[@pass='specialize']//u:RunSynchronous", ns)!;
        foreach (var child in parent.ChildNodes.OfType<XmlElement>().ToArray().Reverse()) parent.AppendChild(child);
        var siblingOrder = XmlPlanImport.Import(doc.OuterXml, generator);
        Check(siblingOrder.DisableFastStartup && siblingOrder.ClassicContextMenu, "numeric order takes precedence over XML sibling order");
        var load = doc.SelectNodes("//u:settings[@pass='specialize']//u:RunSynchronousCommand", ns)!.Cast<XmlElement>().Single(n => NativeScriptPipeline.Text(n).StartsWith("reg.exe load "));
        var unload = doc.SelectNodes("//u:settings[@pass='specialize']//u:RunSynchronousCommand", ns)!.Cast<XmlElement>().Single(n => NativeScriptPipeline.Text(n).StartsWith("reg.exe unload "));
        var loadOrder = load.SelectSingleNode("u:Order", ns)!; var unloadOrder = unload.SelectSingleNode("u:Order", ns)!;
        (loadOrder.InnerText, unloadOrder.InnerText) = (unloadOrder.InnerText, loadOrder.InnerText);
        var wrongHiveOrder = XmlPlanImport.Import(doc.OuterXml, generator);
        Check(!wrongHiveOrder.ClassicContextMenu && wrongHiveOrder.DisableFastStartup, "reversed hive chain prevents user mapping but preserves independent specialize feature");
        loadOrder.InnerText = "999";
        var outOfRange = XmlPlanImport.Import(doc.OuterXml, generator);
        Check(!outOfRange.DisableFastStartup && !outOfRange.ClassicContextMenu, "out-of-range numeric order invalidates native pipeline");
        Check(XmlMerge.Equal(doc.DocumentElement, XmlMerge.Read(Output(outOfRange)).DocumentElement), "invalid order source remains unchanged when unedited");

        foreach (string name in new[] { "Audit\tUser", "Audit\nUser", "Audit\rUser", "Audit\u0001User" })
        {
            plan = Seed(); plan.Users[0].Name = name;
            try { _ = Output(plan); throw new Exception("Control character account exported"); }
            catch (ConfigurationValidationException e) { Check(e.Field == "Users.Name" && e.UserIndex == 0, "invalid account control character locates exact name field"); }
        }
        var dir = Path.Combine(Path.GetTempPath(), "unattend1121-" + Guid.NewGuid().ToString("N")); Directory.CreateDirectory(dir);
        try
        {
            string target = Path.Combine(dir, "config.json"); File.WriteAllText(target,"ORIGINAL");
            try { AtomicFile.Write(target, stream => { stream.Write(new byte[1024]); throw new IOException("Synthetic write failure"); }); throw new Exception("Expected write failure"); }
            catch (IOException) { Check(File.ReadAllText(target) == "ORIGINAL", "failed partial write preserves existing configuration"); }
            Check(Directory.GetFiles(dir).Length == 1, "failed write cleans temporary file");
            AtomicFile.WriteAllText(target, "新配置", new UTF8Encoding(false));
            Check(File.ReadAllText(target) == "新配置" && !File.ReadAllBytes(target).AsSpan().StartsWith(new byte[] { 0xef,0xbb,0xbf }), "atomic text save preserves encoding");
            AtomicFile.WriteAllBytes(target, [1,2,3]); Check(File.ReadAllBytes(target).SequenceEqual(new byte[] {1,2,3}), "atomic byte export replaces destination");
            string absent = Path.Combine(dir,"absent.xml");
            try { AtomicFile.Write(absent, stream => throw new IOException("Synthetic failure")); } catch (IOException) { }
            Check(!File.Exists(absent), "failed first save does not create partial destination");
            string blocked = Path.Combine(dir,"directory"); Directory.CreateDirectory(blocked);
            try { AtomicFile.WriteAllBytes(blocked,[4]); } catch (Exception e) when (e is IOException or UnauthorizedAccessException) { }
            Check(Directory.Exists(blocked) && Directory.GetFiles(dir).Length == 1, "replace failure retains destination and cleans temporary file");
        }
        finally { Directory.Delete(dir, true); }
        return count;
    }
}
