using System.Text;
using System.Xml;
using UnattendLocal.Core;
using UnattendLocal.Localization;

static class DeploymentReviewTests
{
    public static int Run(GeneratorService generator)
    {
        int count = 0;
        void Check(bool ok, string name)
        {
            if (!ok) throw new Exception("Deployment review: " + name);
            count++;
        }
        void Reject(Action action, string name)
        {
            try { action(); }
            catch (Exception error) when (error is ArgumentException or XmlException) { count++; return; }
            throw new Exception("Deployment review accepted " + name);
        }

        Check(ConfigurationSetReview.Destination("$1/Tools/install.cmd") == @"%SystemDrive%\Tools\install.cmd", "system drive mapping");
        Check(ConfigurationSetReview.Destination(@"$$\Setup\Scripts\SetupComplete.cmd") == @"%WINDIR%\Setup\Scripts\SetupComplete.cmd", "Windows mapping");
        Check(ConfigurationSetReview.Destination(@"d\Data\notes.txt") == @"D:\Data\notes.txt", "explicit drive mapping");
        foreach (string unknown in new[] { "file.txt", @"Other\a.txt", @"$1\..\a.txt", @"$1\a:b", @"$1\\a", @"$1\.\a" })
            Check(ConfigurationSetReview.Destination(unknown) is null, "unmapped or unsafe relative path");

        string temporary = Path.Combine(Path.GetTempPath(), "ul151-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(temporary);
        try
        {
            string root = Path.Combine(temporary, "$OEM$");
            Directory.CreateDirectory(Path.Combine(root, "$1", "Tools"));
            Directory.CreateDirectory(Path.Combine(root, "$$", "Setup"));
            File.WriteAllText(Path.Combine(root, "$1", "Tools", "a.cmd"), "PRIVATE_CONTENT_NOT_READ");
            File.WriteAllText(Path.Combine(root, "$$", "Setup", "b.cmd"), "PRIVATE_CONTENT_NOT_READ");
            File.WriteAllText(Path.Combine(root, "readme.txt"), "PRIVATE_CONTENT_NOT_READ");
            var review = ConfigurationSetReview.Read(root);
            Check(review.FileCount == 3 && review.Files.Count == 3 && review.Skipped == 0 && !review.Limited, "complete folder scan");
            Check(review.Files.Count(f => f.Destination is null) == 1, "unknown root file stays unmapped");
            string report = UiText.EnglishText(review.Describe());
            Check(report.Contains("%WINDIR%") && report.Contains("Unmapped") && !report.Contains("PRIVATE_CONTENT"), "report displays mappings without opening payloads");
            Reject(() => ConfigurationSetReview.Read(temporary), "non-OEM root");

            string deep = Path.Combine(root, "$1");
            for (int i = 0; i < 34; i++) { deep = Path.Combine(deep, "d"); Directory.CreateDirectory(deep); }
            File.WriteAllText(Path.Combine(deep, "hidden.txt"), "");
            var depthLimited = ConfigurationSetReview.Read(root);
            Check(depthLimited.Limited && depthLimited.Skipped == 1 && depthLimited.FileCount == 3,
                "depth limit marks skipped content rather than claiming completeness");
            Directory.Delete(Path.Combine(root, "$1", "d"), recursive: true);

            // Unix does not require extra privilege to create the link used by this regression.
            if (!OperatingSystem.IsWindows())
            {
                Directory.CreateSymbolicLink(Path.Combine(root, "loop"), root);
                var withLink = ConfigurationSetReview.Read(root);
                Check(withLink.FileCount == 3 && withLink.Skipped == 1, "link cycle is skipped");
            }
            using (var cancellation = new CancellationTokenSource())
            {
                cancellation.Cancel();
                try { ConfigurationSetReview.Read(root, cancellation.Token); throw new Exception("Cancellation ignored"); }
                catch (OperationCanceledException) { count++; }
            }
            for (int i = 0; i < ConfigurationSetReview.MaxEntries; i++)
                File.WriteAllText(Path.Combine(root, "entry" + i), "");
            var limited = ConfigurationSetReview.Read(root);
            Check(limited.Limited && limited.Files.Count == ConfigurationSetReview.MaxRows &&
                limited.FileCount <= ConfigurationSetReview.MaxEntries, "directory scan and display stay bounded");

            const string eventNs = "http://schemas.microsoft.com/win/2004/08/events/event";
            string Event(int id, string provider = "Microsoft-Windows-AppLocker", string file = @"C:\Private\demo.exe") =>
                $"<Event xmlns='{eventNs}'><System><Provider Name='{provider}'/><EventID>{id}</EventID><TimeCreated SystemTime='2026-11-18T01:00:00Z'/></System><UserData><RuleAndFileData xmlns='urn:test'><FilePath>{file}</FilePath></RuleAndFileData></UserData></Event>";
            int[] allowed = [8002, 8005, 8020, 8023], audit = [8003, 8006, 8021, 8024],
                blocked = [8004, 8007, 8022, 8025], errors = [8000, 8008, 8027];
            string xml = "<Events>" + string.Concat(allowed.Concat(audit).Concat(blocked).Concat(errors).Select(id => Event(id))) +
                Event(8029) + Event(8004, "Other-Provider") + "</Events>";
            var events = AppLockerEventReview.Parse(xml);
            Check(events.Allowed == 4 && events.Audit == 4 && events.Blocked == 4 && events.Errors == 3 &&
                events.Other == 1 && events.Ignored == 1 && events.Rows.Count == 11, "official IDs counted separately with provider isolation");
            Check(events.Rows.All(row => row.File == @"C:\Private\demo.exe"), "UserData file paths read without execution");
            string english = UiText.EnglishText(events.Describe());
            Check(english.Contains("Audit finding (allowed)") && english.Contains("Blocked") &&
                !english.Contains("审计"), "event outcomes translated while file paths are retained");
            var eventData = AppLockerEventReview.Parse(Event(8004).Replace("<UserData><RuleAndFileData xmlns='urn:test'><FilePath>", "<EventData><Data Name='FilePath'>")
                .Replace("</FilePath></RuleAndFileData></UserData>", "</Data></EventData>"));
            Check(eventData.Blocked == 1 && eventData.Rows[0].File.EndsWith("demo.exe"), "EventData payload also supported");
            Check(AppLockerEventReview.Parse(Event(8003)).Audit == 1, "single event supported");
            Check(AppLockerEventReview.Parse("<Events xmlns='" + eventNs + "'>" + Event(8007) + "</Events>").Blocked == 1, "namespaced event collection supported");
            Check(AppLockerEventReview.Parse(Event(8004, file: "a&#10;b&#x202e;c")).Rows[0].File == "a b c", "untrusted display fields cannot inject lines or bidi overrides");
            Check(AppLockerEventReview.Parse(Event(8004, file: new string('x', 1000))).Rows[0].File.Length == 513, "event fields bounded");
            var missingMetadata = AppLockerEventReview.Parse("<Events><Event xmlns='" + eventNs + "'/></Events>");
            Check(missingMetadata.Ignored == 1 && missingMetadata.Rows.Count == 0, "missing provider is not treated as an AppLocker finding");
            Check(UiText.EnglishText(AppLockerEventReview.Parse("<Events/>").Describe()).Contains("does not establish"),
                "empty result does not assert policy success");
            Reject(() => AppLockerEventReview.Parse("<!DOCTYPE Events [<!ENTITY x SYSTEM 'file:///missing'>]><Events>&x;</Events>"), "DTD and external entity");
            Reject(() => AppLockerEventReview.Parse("<Events><Event/></Events>"), "wrong event namespace");
            Reject(() => AppLockerEventReview.Parse("<unattend/>"), "unrelated XML");
            Reject(() => AppLockerEventReview.Parse(new string('x', AppLockerEventReview.MaxBytes + 1)), "oversized XML");
            Reject(() => AppLockerEventReview.Parse("<Events>" + string.Concat(Enumerable.Repeat("<n>", 34)) + string.Concat(Enumerable.Repeat("</n>", 34)) + "</Events>"), "deep XML");
            Reject(() => AppLockerEventReview.Parse("<Events>" + string.Concat(Enumerable.Repeat("<Event xmlns='" + eventNs + "'/>", AppLockerEventReview.MaxEvents + 1)) + "</Events>"), "excess events");
            var many = AppLockerEventReview.Parse("<Events>" + string.Concat(Enumerable.Repeat(Event(8003), 600)) + "</Events>");
            Check(many.Audit == 600 && many.Rows.Count == AppLockerEventReview.MaxRows &&
                UiText.EnglishText(many.Describe()).Contains("first 500"), "full event counts with limited display");
            string eventPath = Path.Combine(temporary, "events.xml");
            File.WriteAllText(eventPath, xml, Encoding.Unicode);
            Check(AppLockerEventReview.Read(eventPath).Audit == 4, "strict Unicode file input");
            using (var cancellation = new CancellationTokenSource())
            {
                cancellation.Cancel();
                try { AppLockerEventReview.Parse(xml, cancellation.Token); throw new Exception("Cancellation ignored"); }
                catch (OperationCanceledException) { count++; }
            }

            foreach (string value in new[] { "true", "1", "false", "0", "\n true \t", "\r\n 0 \n" })
            {
                string answer = $"<unattend xmlns='{XmlMerge.Namespace}'><settings pass='windowsPE'><component name='Microsoft-Windows-Setup'><UseConfigurationSet>{value}</UseConfigurationSet></component></settings></unattend>";
                var state = ConfigurationSetSettings.Inspect(XmlMerge.Read(answer));
                bool expected = value.Trim() is "true" or "1";
                Check(state.Enabled == expected && !state.Ambiguous, "XML boolean whitespace recognized");
                var imported = XmlPlanImport.Import(answer, generator);
                Check(imported.UseConfigurationSet == expected, "XML import retains configuration-set state");
            }
            string minimal = $"<unattend xmlns='{XmlMerge.Namespace}'><settings pass='windowsPE'><component name='Microsoft-Windows-Setup'><UseConfigurationSet>True</UseConfigurationSet></component></settings></unattend>";
            Check(ConfigurationSetSettings.Inspect(XmlMerge.Read(minimal)).Ambiguous, "invalid boolean remains protected");
        }
        finally { Directory.Delete(temporary, recursive: true); }
        return count;
    }
}
