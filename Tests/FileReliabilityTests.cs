using System.Text;
using UnattendLocal.Core;
using UnattendLocal.Desktop;

static class FileReliabilityTests
{
    public static int Run()
    {
        int count = 0;
        void Check(bool value, string label) { if (!value) throw new Exception(label); count++; }
        void Reject(Action action, string label)
        {
            try { action(); } catch (ArgumentException) { count++; return; }
            throw new Exception(label);
        }
        var folder = Path.Combine(Path.GetTempPath(), "unattend-reliability-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(folder);
        try
        {
            var path = Path.Combine(folder, "input.json");
            const string text = "{\"SchemaVersion\":1,\"Workgroup\":\"中文😀\"}\r\n\n";
            foreach (var encoding in new Encoding[] { new UTF8Encoding(false, true), new UTF8Encoding(true, true), new UnicodeEncoding(false, true, true), new UnicodeEncoding(true, true, true), new UTF32Encoding(false, true, true), new UTF32Encoding(true, true, true) })
            {
                var bytes = encoding.GetPreamble().Concat(encoding.GetBytes(text)).ToArray();
                File.WriteAllBytes(path, bytes);
                Check(TextFileImport.Read(path, bytes.Length, "too large") == text, "exact limit and verbatim encoding round trip");
                Check(PlanFileImport.Read(path) == text, "plan import shares strict reader");
                Reject(() => TextFileImport.Read(path, bytes.Length - 1, "too large"), "one byte beyond limit");
            }
            foreach (var bytes in new byte[][] {
                [0xc3, 0x28], [0xef, 0xbb, 0xbf, 0xc3],
                [0xff, 0xfe, 0x00, 0xd8], [0xfe, 0xff, 0xd8, 0x00],
                [0xff, 0xfe, 0, 0, 0, 0, 0x11, 0], [0, 0, 0xfe, 0xff, 0, 0x11, 0, 0],
                [0xff, 0xfe, 0x61] })
            {
                File.WriteAllBytes(path, bytes);
                Reject(() => PlanFileImport.Read(path), "malformed encoding must not become replacement characters");
            }
            // Valid replacement characters in the source are retained, not rejected.
            File.WriteAllText(path, "合法字符：�", new UTF8Encoding(false));
            Check(TextFileImport.Read(path, 100, "limit") == "合法字符：�", "valid U+FFFD retained");
            File.WriteAllBytes(path, []);
            Check(TextFileImport.Read(path, 1, "limit") == "", "empty file delegated to document validator");
            Reject(() => TextFileImport.Read(path, 0, "limit"), "invalid size rejected");
            using (var file = File.Create(path)) file.SetLength(PlanFileImport.MaxXmlBytes + 1);
            var xmlPath = Path.ChangeExtension(path, ".xml");
            File.Move(path, xmlPath);
            Reject(() => PlanFileImport.Read(xmlPath), "oversized XML rejected before decoding");

            var scriptPath = Path.Combine(folder, "sample.ps1");
            var script = new ScriptEntry { Type = "Ps1", Content = "Write-Output '中文'\r\n" };
            ScriptFiles.Write(script, scriptPath);
            Check(File.ReadAllBytes(scriptPath).SequenceEqual(ScriptFiles.Encode(script, scriptPath)), "script encoding preserved by shared writer");
            script.Content = "";
            var previous = File.ReadAllBytes(scriptPath);
            Reject(() => ScriptFiles.Write(script, scriptPath), "invalid script rejected");
            Check(previous.SequenceEqual(File.ReadAllBytes(scriptPath)), "failed validation preserves existing script");

            var prefs = Path.Combine(folder, "ui.json");
            Check(UiPreferences.Save("Dark", prefs) && UiPreferences.Load(prefs) == "Dark", "theme save round trip");
            Check(!UiPreferences.Save("invalid", prefs) && UiPreferences.Load(prefs) == "Dark", "invalid theme preserves previous file");
            File.WriteAllBytes(prefs, Encoding.UTF8.GetBytes("{\"theme\":\"Dark\",\"other\":\"").Concat(new byte[] { 0xc3 }).Concat(Encoding.UTF8.GetBytes("\"}")).ToArray());
            Check(UiPreferences.Load(prefs) == "Light", "corrupt preferences use default");
            var occupied = Path.Combine(folder, "occupied");
            Directory.CreateDirectory(occupied);
            File.WriteAllText(Path.Combine(occupied, "keep.txt"), "keep");
            Check(!UiPreferences.Save("Dark", occupied), "failed rename reported");
            Check(File.ReadAllText(Path.Combine(occupied, "keep.txt")) == "keep", "failed replacement preserves destination");
            Check(!Directory.EnumerateFiles(folder, "*.tmp").Any(), "failed rename cleans staged file");
        }
        finally { Directory.Delete(folder, true); }
        Console.WriteLine($"File reliability: {count} checks passed.");
        return count;
    }
}
