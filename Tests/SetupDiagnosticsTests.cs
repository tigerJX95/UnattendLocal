using System.Text;
using System.Xml;
using UnattendLocal.Core;
using UnattendLocal.Desktop;
using UnattendLocal.Localization;

static class SetupDiagnosticsTests
{
    public static int Run(GeneratorService generator)
    {
        int checks = 0;
        void Check(bool condition, string message)
        {
            if (!condition) throw new Exception("Setup diagnostics: " + message);
            checks++;
        }

        void Reject(Action operation, string message)
        {
            try { operation(); }
            catch (ArgumentException) { checks++; return; }
            throw new Exception("Setup diagnostics accepted " + message);
        }

        const string paths = "E:\\Drivers\\Audio & USB\r\n\\\\server\\share\\Other drivers";
        var draft = AdditionalXmlSettings.CreateOfflineDriverDraft(paths);
        Check(!draft.Enabled && draft.Pass == "offlineServicing"
              && draft.Component == "Microsoft-Windows-PnpCustomizationsNonWinPE",
              "the offline driver form creates a disabled native fragment");
        foreach (string invalid in new[] { "", "relative", "E:\\Drivers\\file.inf", "E:\\Drivers\ne:\\drivers\\" })
            Reject(() => AdditionalXmlSettings.CreateOfflineDriverDraft(invalid), "invalid driver directory");

        var plan = QuickPresets.Create(QuickPresets.Minimal);
        plan.XmlComponents.Add(draft);
        Check(!Encoding.UTF8.GetString(generator.Generate(plan)).Contains("<DriverPaths>"), "disabled draft has no installation effect");
        draft.Enabled = true;
        Reject(() => generator.Generate(plan), "unconfirmed offline drivers");
        plan.ConfirmAdditionalXml = true;
        byte[] xml = generator.Generate(plan);
        var answer = XmlMerge.Read(Encoding.UTF8.GetString(xml));
        var ns = new XmlNamespaceManager(answer.NameTable);
        ns.AddNamespace("u", XmlMerge.Namespace);
        ns.AddNamespace("wcm", "http://schemas.microsoft.com/WMIConfig/2002/State");
        var rows = answer.SelectNodes("/u:unattend/u:settings[@pass='offlineServicing']/u:component[@name='Microsoft-Windows-PnpCustomizationsNonWinPE']/u:DriverPaths/u:PathAndCredentials", ns)!
            .OfType<XmlElement>().ToArray();
        Check(rows.Select(row => row.InnerText).SequenceEqual(SetupDriverSettings.ReadPaths(paths)) &&
              rows.Select(row => row.GetAttribute("keyValue", ns.LookupNamespace("wcm")!)).SequenceEqual(new[] { "1", "2" }),
            "each path and list key survives XML escaping and generation");
        Check(UiText.EnglishText(InstallationReview.ForExport(xml).Phases).Contains("Target Windows driver paths"),
            "current export review identifies the offline drivers");
        var saved = PlanStore.Deserialize(PlanStore.Serialize(plan, true));
        Check(saved.XmlComponents.Single().Content == draft.Content, "JSON keeps the draft");
        var imported = XmlPlanImport.Import(Encoding.UTF8.GetString(xml), generator);
        Check(imported.ImportedXml!.Blockers.Length == 0 && imported.XmlComponents.Single().Pass == "offlineServicing",
            "exported answer XML restores the draft");
        var conflict = AdditionalXmlSettings.CreateOfflineDriverDraft("G:\\Drivers");
        conflict.Enabled = true;
        plan.XmlComponents.Add(conflict);
        plan.ConfirmAdditionalXml = true;
        Reject(() => generator.Generate(plan), "duplicate offline DriverPaths");
        Check(SettingsSearchCatalog.Search("PnpCustomizationsNonWinPE").Any(row => row.Target == "OfflineDriverForm") &&
              SettingsSearchCatalog.Search("setupact").Any(row => row.Target == "SetupLogsButton") &&
              SettingsSearchCatalog.Search("DISM").Any(row => row.Target == "SetupLogsButton"),
              "both entries are searchable");

        string directory = Directory.CreateTempSubdirectory("ul149-setup-logs-").FullName;
        try
        {
            string file = Path.Combine(directory, "SetupAPI.Offline.LOG");
            const string sample = "Info Setup started\r\nError 驱动程序未安装\r\nERROR service\r\n";
            foreach (var encoding in new Encoding[] { new UTF8Encoding(false), new UTF8Encoding(true), Encoding.Unicode,
                         Encoding.BigEndianUnicode, new UTF32Encoding(false, true), new UTF32Encoding(true, true) })
            {
                File.WriteAllText(file, sample, encoding);
                var result = SetupLogPreview.Read(file);
                Check(result.Text == sample && !result.Truncated && result.Filter(" ", out int? unfiltered) == sample && unfiltered is null &&
                      result.Filter(" error ", out int? count).Split(Environment.NewLine).SequenceEqual(new[] { "Error 驱动程序未安装", "ERROR service" }) &&
                      count == 2 && result.Filter("absent", out int? absent) == "" && absent == 0,
                      "Unicode BOM support and literal case-insensitive line filtering with counts");
                File.WriteAllText(file, new string('A', 300000) + "\r\n最近错误\n", encoding);
                result = SetupLogPreview.Read(file);
                Check(result.Truncated && result.Text == "最近错误\n", "bounded tail starts at a complete line");

                File.WriteAllText(file, new string('A', 300000) + "\r最近错误\rINFO service\r", encoding);
                result = SetupLogPreview.Read(file);
                Check(result.Truncated && result.Text == "最近错误\rINFO service\r", "bounded tail supports carriage-return line separators");

                const string firstLine = "最近错误\r\n";
                int unitBytes = encoding.GetByteCount("x");
                int padding = (256 * 1024 - unitBytes - encoding.GetByteCount(firstLine)) / unitBytes;
                string suffix = firstLine + new string('x', padding);
                File.WriteAllText(file, "discard\r\n" + suffix, encoding);
                result = SetupLogPreview.Read(file);
                Check(result.Truncated && result.Text == suffix, "a CRLF split at the tail boundary creates no leading blank line");
            }
            foreach (string name in new[] { "dism.LOG", "Cbs.log", "OfflineUpdates.log" })
            {
                string servicingLog = Path.Combine(directory, name);
                File.WriteAllText(servicingLog, "INFO first\n");
                Check(SetupLogPreview.Read(servicingLog).Filter("error", out int? before) == "" && before == 0 &&
                      SetupLogPreview.FilePatterns.Split(';').Contains(name, StringComparer.OrdinalIgnoreCase),
                      "the servicing log is readable and offered by the file picker");
                File.AppendAllText(servicingLog, "ERROR appended\n");
                Check(SetupLogPreview.Read(servicingLog).Filter("error", out int? after) == "ERROR appended" && after == 1,
                      "rereading a servicing log exposes appended lines under the same keyword");
            }
            File.WriteAllText(file, "discard\n" + new string('a', 256 * 1024 - 1) + "\n");
            Check(SetupLogPreview.Read(file).Text.Length == 256 * 1024, "a byte boundary on a complete line retains that line");
            File.WriteAllText(file, new string('a', 300000), Encoding.Unicode);
            Check(SetupLogPreview.Read(file) is { Truncated: true, Text.Length: 0 }, "a single oversized line remains bounded");
            File.WriteAllBytes(file, []);
            Check(SetupLogPreview.Read(file) is { Truncated: false, Text.Length: 0 }, "empty error logs are supported");
            File.WriteAllBytes(file, [0x61, 0, 0x62]);
            Reject(() => SetupLogPreview.Read(file), "binary input");
            Reject(() => SetupLogPreview.Read(Path.Combine(directory, "custom.log")), "unsupported log names");
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
        return checks;
    }
}
