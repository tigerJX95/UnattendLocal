using System.Text;
using System.Xml;
using UnattendLocal.Core;
using UnattendLocal.Desktop;

static class ScriptTemplateTests
{
    public static int Run(GeneratorService generator)
    {
        int count = 0;
        void Check(bool ok, string message) { if (!ok) throw new Exception(message); count++; }
        void Reject(Action action) { try { action(); } catch (ArgumentException) { count++; return; } throw new Exception("Expected template rejection"); }
        foreach (var template in ScriptTemplates.All)
        foreach (var architecture in new[] { "amd64", "arm64", "x86" })
        {
            var plan = QuickPresets.Create(QuickPresets.LocalAccount); plan.Architecture = architecture;
            var row = ScriptTemplates.Add(plan, template.Id);
            Check(!row.Enabled && row.Type == "Ps1" && row.Phase == template.Phase, "template added as disabled script at correct stage");
            Check(!Encoding.UTF8.GetString(generator.Generate(plan)).Contains(template.Content.Split('\n')[0]), "disabled template not embedded as executable file");
            var restored = PlanStore.Deserialize(PlanStore.Serialize(plan, true));
            Check(restored.Scripts.Single().Content == row.Content && !restored.Scripts.Single().Enabled, "disabled template JSON roundtrip");
            row.Enabled = true;
            var xml = Encoding.UTF8.GetString(generator.Generate(plan));
            var doc = new XmlDocument(); doc.LoadXml(xml);
            Check(doc.SelectNodes("//*[local-name()='File']")!.Cast<XmlElement>().Any(n => n.InnerText.Replace("\r\n", "\n").Trim() == row.Content.Replace("\r\n", "\n").Trim()), "enabled payload embedded without prose changes");
            var imported = XmlPlanImport.Import(xml, generator);
            Check(imported.Scripts.Any(s => s.Enabled && s.Phase == row.Phase && s.Content.Replace("\r\n", "\n").Trim() == row.Content.Replace("\r\n", "\n").Trim()), "XML reimport preserves template and stage");
            Check(generator.Generate(imported).Length > 0, "reimported XML remains exportable");
            Reject(() => ScriptTemplates.Add(plan, template.Id));
            Check(plan.Scripts.Count == 1, "duplicate rejection keeps collection unchanged");
            row.Content += "\n# custom edit";
            var copy = PlanStore.Deserialize(PlanStore.Serialize(plan, true));
            Check(copy.Scripts.Single().Content == row.Content, "edited template remains ordinary script");
            Check(template.Requirements.Contains("\n\n"), "Chinese requirements contain explicit paragraph breaks");
        }
        var vm = new MainViewModel(); vm.Preview = "stale";
        vm.SelectedScriptTemplate = ScriptTemplates.All[2]; vm.AddSelectedScriptTemplate();
        Check(vm.SelectedScript == vm.Plan.Scripts.Last() && !vm.SelectedScript.Enabled && vm.Preview != "stale", "UI selects added entry and invalidates preview");
        Reject(() => ScriptTemplates.Add(vm.Plan, "unknown"));
        var full = new InstallPlan(); for (int i = 0; i < 60; i++) full.Scripts.Add(new ScriptEntry { Enabled = false });
        Reject(() => ScriptTemplates.Add(full, "netfx3"));
        Check(full.Scripts.Count == 60, "capacity rejection is atomic");
        Check(SettingsSearchCatalog.All.Any(s => s.Target == "ScriptTemplateSelector" && s.Aliases.Contains("NetFx3")), "templates discoverable through settings search");
        Console.WriteLine($"Script templates: {count} checks passed.");
        return count;
    }
}
