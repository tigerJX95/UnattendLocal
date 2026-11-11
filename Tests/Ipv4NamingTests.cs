using System.Text;
using UnattendLocal.Core;
using UnattendLocal.Desktop;

static class Ipv4NamingTests
{
    public static int Run(GeneratorService generator)
    {
        int checks = 0;
        void Check(bool condition, string scenario)
        {
            if (!condition) throw new Exception("IPv4 naming: " + scenario);
            checks++;
        }
        foreach (string architecture in new[] { "amd64", "arm64", "x86" })
        foreach (bool hidden in new[] { false, true })
        {
            var plan = QuickPresets.Create(QuickPresets.LocalAccount);
            plan.Architecture = architecture;
            plan.HidePowerShellWindows = hidden;
            plan.ComputerNameMode = ComputerNamingSettings.Ipv4;
            Check(PlanStore.Deserialize(PlanStore.Serialize(plan, true)).ComputerNameMode == ComputerNamingSettings.Ipv4,
                "JSON preserves IPv4 mode");
            foreach (byte[] xml in new[] { generator.Generate(plan), generator.GenerateStandalone(plan) })
            {
                var imported = XmlPlanImport.Import(Encoding.UTF8.GetString(xml), generator);
                Check(imported.ComputerNameMode == ComputerNamingSettings.Ipv4 && imported.ComputerNameScript == "",
                    "XML with and without metadata recognizes the built-in getter");
                Check(imported.HidePowerShellWindows == hidden,
                    "embedded hidden setter does not override the launch-window setting");
                var exported = XmlMerge.Read(Encoding.UTF8.GetString(generator.Generate(imported)));
                Check(ComputerNamingSettings.Inspect(exported).Getter.ReplaceLineEndings("\n").Trim() ==
                    ComputerNamingSettings.Ipv4Script.ReplaceLineEndings("\n").Trim(), "round-trip preserves the getter");
                imported.ComputerNameMode = ComputerNamingSettings.Existing;
                imported.ComputerName = "FIXED-IP";
                exported = XmlMerge.Read(Encoding.UTF8.GetString(generator.Generate(imported)));
                Check(!ComputerNamingSettings.Inspect(exported).Enabled && exported.OuterXml.Contains("FIXED-IP"),
                    "fixed name can replace an imported IPv4 pipeline");
            }
        }
        foreach (string mode in new[] { ComputerNamingSettings.Prompt, ComputerNamingSettings.Mac, ComputerNamingSettings.Script })
        foreach (bool hidden in new[] { false, true })
        {
            var plan = QuickPresets.Create(QuickPresets.LocalAccount);
            plan.ComputerNameMode = mode;
            plan.ComputerNameScript = ComputerNamingSettings.Example;
            plan.HidePowerShellWindows = hidden;
            var imported = XmlPlanImport.Import(Encoding.UTF8.GetString(generator.GenerateStandalone(plan)), generator);
            Check(imported.HidePowerShellWindows == hidden, "other dynamic modes preserve the launch-window setting");
            imported.ComputerNameMode = ComputerNamingSettings.Existing;
            imported.ComputerName = "FIXED-OTHER";
            Check(!ComputerNamingSettings.Inspect(XmlMerge.Read(Encoding.UTF8.GetString(generator.Generate(imported)))).Enabled,
                "other dynamic modes can switch back to a fixed name without metadata");
        }
        var custom = QuickPresets.Create(QuickPresets.LocalAccount);
        custom.ComputerNameMode = ComputerNamingSettings.Script;
        custom.ComputerNameScript = ComputerNamingSettings.Ipv4Script + "\n# Keep this custom command";
        var restored = XmlPlanImport.Import(Encoding.UTF8.GetString(generator.Generate(custom)), generator);
        Check(restored.ComputerNameMode == ComputerNamingSettings.Script &&
            restored.ComputerNameScript.Contains("# Keep this custom command"), "edited getters remain custom scripts");

        var model = new MainViewModel();
        model.Plan.ComputerNameMode = ComputerNamingSettings.Ipv4;
        Check(model.IsIpv4ComputerName && !model.IsMacComputerName && !model.IsScriptComputerName,
            "IPv4 mode exposes its own explanation");
        Check(SettingsSearchCatalog.Search("IPv4").Any(result => result.Target == "SettingAnchor006"),
            "IPv4 naming is searchable");
        return checks;
    }
}
