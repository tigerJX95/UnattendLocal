using System.Text;
using System.Xml;
using UnattendLocal.Core;
using UnattendLocal.Localization;

static class CoreIsolationImportTests
{
    public static int Run(GeneratorService generator)
    {
        int count = 0;
        void Check(bool condition, string message)
        {
            if (!condition) throw new Exception("Core isolation import: " + message);
            count++;
        }

        string Source(bool disabled, string mode)
        {
            var plan = QuickPresets.Create(QuickPresets.LocalAccount);
            plan.DisableCoreIsolation = disabled;
            var doc = XmlMerge.Read(Encoding.UTF8.GetString(generator.GenerateStandalone(plan)));
            doc.DocumentElement!.PrependChild(doc.CreateComment(
                "https://schneegans.de/windows/unattend-generator/?CoreIsolationMode=" + mode));
            return doc.OuterXml;
        }

        foreach (bool disabled in new[] { false, true })
        foreach (string mode in new[] { "Enabled", "Disabled" })
        {
            string source = Source(disabled, mode);
            var imported = XmlPlanImport.Import(source, generator);
            Check(imported.DisableCoreIsolation == disabled &&
                  !imported.ImportedXml!.Notices.Any(note => note.Contains("CoreIsolationMode", StringComparison.Ordinal)),
                  "recognized comment modes defer to the actual payload");
            Check(XmlMerge.Equal(XmlMerge.Read(source).DocumentElement,
                      XmlMerge.Read(Encoding.UTF8.GetString(generator.Generate(imported))).DocumentElement),
                  "untouched import/export preserves the original source");
            var obsolete = UiText.Concat([
                UiText.Source("text.4e00ee262381"), "CoreIsolationMode",
                UiText.Source("text.29527e6a456d")
            ], [false, true, false]);
            imported.ImportedXml!.Notices = [.. imported.ImportedXml.Notices, obsolete];
            var reloaded = PlanStore.Deserialize(PlanStore.Serialize(imported, true));
            Check(reloaded.DisableCoreIsolation == disabled &&
                  !reloaded.ImportedXml!.Notices.Any(note => note.Contains("CoreIsolationMode", StringComparison.Ordinal)),
                  "reloading an old plan removes the obsolete mode warning");
            imported.DisableCoreIsolation = !disabled;
            var changed = XmlPlanImport.Import(Encoding.UTF8.GetString(generator.Generate(imported)), generator);
            Check(changed.DisableCoreIsolation == !disabled,
                  "a stale form comment cannot undo an explicit security edit");
        }

        var unknown = XmlPlanImport.Import(Source(false, "FutureMode"), generator);
        Check(unknown.ImportedXml!.Notices.Any(note => note.Contains("CoreIsolationMode", StringComparison.Ordinal)),
              "unknown future values still require review");

        string altered = Source(true, "Disabled").Replace(
            "/v \"EnabledBootId\" /t REG_DWORD /d 0 /f", "/v \"EnabledBootId\" /t REG_DWORD /d 1 /f",
            StringComparison.Ordinal);
        var ambiguous = XmlPlanImport.Import(altered, generator);
        Check(!ambiguous.DisableCoreIsolation &&
              SecurityOptions.Inspect(XmlMerge.Read(altered))[nameof(InstallPlan.DisableCoreIsolation)].Ambiguous &&
              ambiguous.ImportedXml!.Notices.Length > 0,
              "recognizing the form mode retains review of modified security scripts");
        Check(XmlMerge.Equal(XmlMerge.Read(altered).DocumentElement,
                  XmlMerge.Read(Encoding.UTF8.GetString(generator.Generate(ambiguous))).DocumentElement),
              "unrecognized security scripts retain their original content");
        ambiguous.DisableCoreIsolation = true;
        try
        {
            generator.Generate(ambiguous);
            throw new Exception("An ambiguous security script was overwritten.");
        }
        catch (ArgumentException)
        {
            count++;
        }

        var configuration = QuickPresets.Create(QuickPresets.LocalAccount);
        var docWithConfiguration = XmlMerge.Read(Encoding.UTF8.GetString(generator.GenerateStandalone(configuration)));
        var ns = new XmlNamespaceManager(docWithConfiguration.NameTable);
        ns.AddNamespace("u", XmlMerge.Namespace);
        var settings = docWithConfiguration.SelectSingleNode(
            "/u:unattend/u:settings[@pass='windowsPE']/u:component[@name='Microsoft-Windows-Setup']", ns)!;
        foreach (var old in settings.ChildNodes.OfType<XmlElement>().Where(e => e.LocalName == "UseConfigurationSet").ToArray())
            settings.RemoveChild(old);
        Check(!XmlPlanImport.Import(docWithConfiguration.OuterXml, generator).UseConfigurationSet,
              "an absent UseConfigurationSet imports as unchecked");
        var flag = docWithConfiguration.CreateElement("UseConfigurationSet", XmlMerge.Namespace);
        settings.AppendChild(flag);
        foreach (string value in new[] { "true", "1", "false", "0" })
        {
            flag.InnerText = value;
            Check(XmlPlanImport.Import(docWithConfiguration.OuterXml, generator).UseConfigurationSet == (value is "true" or "1"),
                  "the configuration-set checkbox follows the native XML value");
        }
        return count;
    }
}
