using System.Text;
using DiscUtils.Iso9660;
using UnattendLocal.Core;
using UnattendLocal.Desktop;

static class AnswerFileExportTests
{
    public static int Run(GeneratorService generator, string? samples = null)
    {
        int count = 0;
        void Check(bool ok, string message) { if (!ok) throw new Exception(message); count++; }
        string directory = Path.Combine(Path.GetTempPath(), "unattend-export-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        try
        {
            Check(!InstallPlan.New().ExportNonAutomaticFilename, "default automatic filename");
            Check(!PlanStore.Deserialize("{\"SchemaVersion\":1,\"Users\":[]}").ExportNonAutomaticFilename, "legacy plan default");
            foreach (string architecture in new[] { "amd64", "arm64", "x86" })
            {
                var plan = InstallPlan.New(); plan.Architecture = architecture; plan.Users[0].Password = "Synthetic113!";
                byte[] automatic = generator.Generate(plan);
                plan.ExportNonAutomaticFilename = true;
                Check(generator.Generate(plan).SequenceEqual(automatic), "filename preference does not change XML " + architecture);
                foreach (bool secrets in new[] { false, true })
                {
                    var restored = PlanStore.Deserialize(PlanStore.Serialize(plan, secrets));
                    Check(restored.ExportNonAutomaticFilename, "JSON retains filename preference " + architecture + secrets);
                    Check(secrets || !restored.Users[0].Password.Contains("Synthetic113!"), "existing credential omission " + architecture);
                }
                var imported = XmlPlanImport.Import(Encoding.UTF8.GetString(automatic), generator);
                imported.ExportNonAutomaticFilename = true;
                Check(generator.Generate(imported).Length > 0, "export preference does not conflict with imported XML " + architecture);
                foreach (bool manual in new[] { false, true })
                {
                    string filename = AnswerFileExport.FileName(manual);
                    string xmlPath = Path.Combine(directory, filename);
                    string isoPath = Path.Combine(directory, AnswerFileExport.IsoName(manual));
                    AnswerFileExport.WriteXml(xmlPath, automatic);
                    Check(File.ReadAllBytes(xmlPath).SequenceEqual(automatic), "XML bytes preserved");
                    AnswerFileExport.WriteIso(isoPath, automatic, manual);
                    using var stream = File.OpenRead(isoPath);
                    using var disc = new CDReader(stream, true);
                    Check(disc.GetFiles(@"\").Length == 1 && disc.GetDirectories(@"\").Length == 0, "ISO contains one root file");
                    using var file = disc.OpenFile(filename, FileMode.Open);
                    using var extracted = new MemoryStream(); file.CopyTo(extracted);
                    Check(extracted.ToArray().SequenceEqual(automatic), "ISO preserves full unredacted XML bytes");
                    Check(disc.VolumeLabel == "UNATTENDLOCAL", "ISO volume label");
                    if (samples != null)
                    {
                        Directory.CreateDirectory(samples);
                        File.Copy(xmlPath, Path.Combine(samples, architecture + "-" + filename), true);
                        File.Copy(isoPath, Path.Combine(samples, architecture + "-" + AnswerFileExport.IsoName(manual)), true);
                    }
                }
            }
            string target = Path.Combine(directory, "existing.iso");
            File.WriteAllText(target, "OLD");
            try { AnswerFileExport.WriteIso(target, null!, false); throw new Exception("null accepted"); }
            catch (ArgumentNullException) { Check(File.ReadAllText(target) == "OLD", "invalid input preserves existing output"); }
            string blocked = Path.Combine(directory, "directory.iso"); Directory.CreateDirectory(blocked);
            try { AnswerFileExport.WriteIso(blocked, Encoding.UTF8.GetBytes("<xml/>"), false); throw new Exception("directory accepted"); }
            catch (Exception error) when (error is IOException or UnauthorizedAccessException)
            { Check(Directory.Exists(blocked) && !Directory.GetFiles(directory, ".unattend-save-*").Any(), "failed ISO replacement cleans temporary file"); }
            var vm = new MainViewModel(); vm.Plan.ExportNonAutomaticFilename = true;
            Check(vm.ExportXmlButtonText == "导出 notautounattend.xml", "button reflects filename choice");
            Check(vm.ExportUsage.Contains("setup.exe /Unattend:D:\\notautounattend.xml"), "explicit setup command shown");
            vm.Plan = InstallPlan.New();
            Check(vm.ExportXmlButtonText == "导出 autounattend.xml", "loading plan refreshes button");
            Check(SettingsSearchCatalog.All.Any(s => s.Target == "ExportIsoButton"), "ISO search entry");
            return count;
        }
        finally { Directory.Delete(directory, true); }
    }
}
