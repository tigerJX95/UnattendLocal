using System.Text;
using System.Text.Json;
using System.Xml;
using UnattendLocal.Core;

static class V128SecurityTests
{
    public static int Run(GeneratorService generator)
    {
        int checks = 0;
        void Check(bool value, string message) { if (!value) throw new Exception("V1.28: " + message); checks++; }
        void Reject(string json)
        {
            try { PlanStore.Deserialize(json); }
            catch (ArgumentException) { checks++; return; }
            throw new Exception("V1.28 accepted ambiguous or null configuration");
        }
        foreach (string json in new[] {
            "{\"SchemaVersion\":1,\"Scripts\":[{\"Enabled\":false,\"Enabled\":true}]}",
            "{\"SchemaVersion\":1,\"Users\":[{\"Password\":\"first\",\"password\":\"second\"}]}",
            "{\"SchemaVersion\":1,\"ImportedXml\":{\"SourceXml\":\"\",\"SourceXml\":\"different\"}}",
            "{\"SchemaVersion\":1,\"PeTargetScript\":null}",
            "{\"SchemaVersion\":1,\"Scripts\":[{\"Content\":null}]}",
            "{\"SchemaVersion\":1,\"Users\":[{\"Password\":null}]}" }) Reject(json);
        var textPlan = PlanStore.Deserialize("{\"SchemaVersion\":1,\"PeTargetScript\":\"{\\\"a\\\":1,\\\"a\\\":2}\"}");
        Check(textPlan.PeTargetScript.Contains("\"a\":2"), "JSON-looking script text stays opaque");
        foreach (string newline in new[] { "\r", "\n", "\r\n" })
        {
            var p = new InstallPlan { PeTargetMode = "script", PeTargetScript = string.Join(newline, Enumerable.Repeat("'", 1025)) };
            try { PeDiskSafety.Validate(p); throw new Exception("V1.28 accepted overlong script"); }
            catch (ConfigurationValidationException) { checks++; }
            p.PeTargetScript = string.Join(newline, Enumerable.Repeat("'", 1024));
            PeDiskSafety.Validate(p); checks++;
        }
        const string secret = "INACTIVE-PE-SCRIPT-PRIVATE-DRAFT";
        foreach (string mode in new[] { "fixed", "match", "interactive", "script" })
        foreach (bool enabled in new[] { false, true })
        {
            var p = new InstallPlan { UseGeneratedPe = enabled, PeTargetMode = mode, PeTargetScript = "' " + secret + "\r\nWScript.Echo 0" };
            var doc = XmlMerge.Read("<unattend xmlns='urn:schemas-microsoft-com:unattend' />");
            XmlPlanImport.AddSnapshot(doc, p);
            string comment = doc.SelectNodes("//comment()")!.Cast<XmlComment>().Single().Value!;
            string payload = Encoding.UTF8.GetString(Convert.FromBase64String(comment.Split(' ', 3)[2]));
            using var data = JsonDocument.Parse(payload);
            string stored = data.RootElement.GetProperty(nameof(p.PeTargetScript)).GetString()!;
            Check(stored.Contains(secret) == (enabled && mode == "script"), "only active PE script enters XML recovery metadata");
            Check(p.PeTargetScript.Contains(secret), "export never changes the source draft");
            Check(PlanStore.Deserialize(PlanStore.Serialize(p, true)).PeTargetScript.Contains(secret), "JSON retains editable drafts");
        }
        var selector = PeDiskSafety.Selector(new InstallPlan { TargetDiskId = 0 });
        Check(selector.Contains("seenPartitions <> partitions"), "partial partition mapping fails closed");
        Check(selector.Contains("Cannot identify a mapped source volume."), "invalid mapped volume identity fails closed");
        return checks;
    }
}
