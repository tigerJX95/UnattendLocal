using System.Text;
using System.Xml;
using UnattendLocal.Core;
using UnattendLocal.Desktop;

static class ScriptEnhancementTests
{
    public static int Run(GeneratorService generator)
    {
        int count = 0;
        void Check(bool ok, string message) { if (!ok) throw new Exception(message); count++; }
        void Reject(Action action) { try { action(); } catch (ArgumentException) { count++; return; } throw new Exception("Expected invalid script name rejection"); }
        XmlDocument Xml(InstallPlan plan) { var d = new XmlDocument(); d.LoadXml(Encoding.UTF8.GetString(generator.Generate(plan))); return d; }
        string[] Payloads(XmlDocument doc) => doc.SelectNodes("//*[local-name()='File']")!.Cast<XmlElement>().Select(e => e.GetAttribute("path") + "\n" + e.InnerText).ToArray();

        foreach (var template in ScriptTemplates.All)
        {
            var plan = QuickPresets.Create(QuickPresets.LocalAccount);
            var row = ScriptTemplates.Add(plan, template.Id);
            Check(row.Name == template.Name && row.Label.StartsWith(template.Name), "template name identifies list entry");
            row.Enabled = true;
            var before = Xml(plan);
            row.Name = "中文 & <标题> ' ; $(whoami)";
            var after = Xml(plan);
            Check(Payloads(before).SequenceEqual(Payloads(after)), "name never enters executable files or changes their paths");
            var imported = XmlPlanImport.Import(after.OuterXml, generator);
            Check(imported.Scripts.Single().Name == row.Name, "valid restoration metadata retains script name");
            imported.Scripts[0].Name = "重新命名";
            Check(Payloads(Xml(imported)).SequenceEqual(Payloads(after)), "renaming imported script does not alter executable payloads");
            Check(PlanStore.Deserialize(PlanStore.Serialize(plan, false)).Scripts.Single().Name == row.Name, "name persists in plan without credential opt-in");
            var copy = CustomScripts.Duplicate(plan, row);
            Check(copy.Name == row.Name && !copy.Enabled && copy.Content == row.Content, "duplicate retains name and safe disabled state");
        }
        var legacy = PlanStore.Deserialize("{\"SchemaVersion\":1,\"Scripts\":[{\"Enabled\":false,\"Phase\":\"System\",\"Type\":\"Ps1\",\"Content\":\"Write-Host 'legacy'\"}]}");
        Check(legacy.Scripts.Single().Name == "" && legacy.Scripts.Single().Label == "系统安装时 · .ps1", "old plans without names retain existing labels");
        var script = legacy.Scripts.Single();
        bool labelChanged = false; script.PropertyChanged += (_, e) => labelChanged |= e.PropertyName == nameof(ScriptEntry.Label);
        script.Name = "测试";
        Check(labelChanged && script.Label.StartsWith("测试 · "), "name edits refresh list labels");
        script.Type = "Js";
        Check(script.Label.Contains("JScript") && script.Label.Contains("停止支持"), "names do not hide removed format warning");
        script.Name = new string('中', 80);
        Check(script.Name.Length == 80, "80-character name accepted");
        foreach (var bad in new[] { new string('中', 81), "a\nb", "a\rb", "a\0b", "a\tb", "\ud800", "\uffff" }) Reject(() => script.Name = bad);
        Check(script.Name.Length == 80, "rejected name cannot replace prior value");
        script.Name = null!; Check(script.Name == "", "null name normalizes to optional empty name");
        var vm = new MainViewModel(); vm.SelectedScriptTemplate = ScriptTemplates.All.Single(t => t.Id == "local-ping"); vm.AddSelectedScriptTemplate();
        vm.Preview = "stale"; vm.SelectedScript!.Name = "局域网连通性";
        Check(vm.Preview != "stale" && vm.SelectedScript.Label.StartsWith("局域网连通性"), "UI tracks name changes and invalidates restoration preview");
        Check(SettingsSearchCatalog.All.Any(s => s.Target == "ScriptNameEditor"), "script name editor discoverable");
        Check(SettingsSearchCatalog.All.Any(s => s.Target == "ScriptTemplateSelector" && s.Aliases.Contains("Ping") && s.Aliases.Contains("远程协助")), "new templates searchable");
        Console.WriteLine($"Script enhancements: {count} checks passed.");
        return count;
    }
}
