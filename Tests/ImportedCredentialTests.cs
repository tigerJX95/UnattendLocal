using System.Text;
using System.Xml;
using UnattendLocal.Core;
using UnattendLocal.Localization;

static class ImportedCredentialTests
{
    public static int Run(GeneratorService generator)
    {
        int count = 0;
        void Check(bool ok, string message)
        {
            if (!ok) throw new Exception("Imported credentials: " + message);
            count++;
        }
        void RejectExport(InstallPlan plan, string message)
        {
            try { generator.Generate(plan); }
            catch (ArgumentException) { Check(true, message); return; }
            throw new Exception("Imported credentials: " + message);
        }

        XmlNode Node(XmlDocument doc, string path)
        {
            var ns = new XmlNamespaceManager(doc.NameTable);
            ns.AddNamespace("u", XmlMerge.Namespace);
            return doc.SelectSingleNode(path, ns)!;
        }

        var plan = InstallPlan.New();
        plan.Users[0].Password = "FixtureAccountPassword!";
        plan.ObscurePasswords = true;
        var original = Encoding.UTF8.GetString(generator.Generate(plan));
        foreach (bool omit in new[] { false, true })
        {
            var edited = XmlMerge.Read(original);
            var password = Node(edited, "//u:LocalAccount/u:Password");
            if (omit) password.ParentNode!.RemoveChild(password);
            else Node(edited, "//u:LocalAccount/u:Password/u:Value").InnerText = "";
            var imported = XmlPlanImport.Import(edited.OuterXml, generator);
            Check(imported.Users[0].Password == "" && imported.Users[0].NeedsPassword && !imported.Users[0].AllowEmptyPassword,
                "blank or omitted account password requires an explicit choice");
            Check(imported.ImportedXml!.Blockers.Length == 0, "an empty password remains editable");
            RejectExport(imported, "unconfirmed empty password blocks export");
            imported.Users[0].Password = "[REDACTED]";
            imported.Users[0].NeedsPassword = false;
            RejectExport(imported, "an edited placeholder still blocks export");
            imported.Users[0].Password = "ReplacementPassword!";
            byte[] exported = generator.Generate(imported);
            var repaired = XmlPlanImport.Import(Encoding.UTF8.GetString(exported), generator);
            Check(repaired.Users[0].Password == "ReplacementPassword!" && !repaired.Users[0].NeedsPassword,
                "a replacement password is used in exported XML");
            string logon = Node(XmlMerge.Read(Encoding.UTF8.GetString(exported)), "//u:AutoLogon/u:Password/u:Value").InnerText;
            Check(Encoding.Unicode.GetString(Convert.FromBase64String(logon)) == "ReplacementPassword!Password",
                "a changed local password updates the linked automatic logon");

            var allowed = XmlPlanImport.Import(edited.OuterXml, generator);
            allowed.Users[0].AllowEmptyPassword = true;
            var accepted = XmlPlanImport.Import(Encoding.UTF8.GetString(generator.Generate(allowed)), generator);
            Check(accepted.Users[0].Password == "", "explicitly allowed empty password exports as empty");
        }

        foreach (string bad in new[] { "bad encoding!", "[Password redacted]", "[REDACTED]" })
        {
            var edited = XmlMerge.Read(original);
            Node(edited, "//u:LocalAccount/u:Password/u:Value").InnerText = bad;
            var imported = XmlPlanImport.Import(edited.OuterXml, generator);
            Check(imported.Users[0].Password == bad && imported.ImportedXml!.SourceXml.Contains(bad) &&
                imported.ImportedXml.Blockers.Contains(UiText.Source("import.credentials.local")), "invalid password text remains unchanged");
            RejectExport(imported, "invalid account password blocks export");
        }

        var diagnosticPlan = InstallPlan.New();
        diagnosticPlan.DiskMode = InstallationSettings.Existing;
        diagnosticPlan.TargetDiskId = 1;
        diagnosticPlan.TargetPartitionId = 3;
        diagnosticPlan.ImageSelection = "按镜像索引";
        diagnosticPlan.ImageIndex = 6;
        diagnosticPlan.Users[0].Name = "PrivateOwner";
        diagnosticPlan.Users[0].DisplayName = "Private display name";
        diagnosticPlan.Users[0].Password = "FixturePrivatePassword!";
        diagnosticPlan.AutoLogon = true;
        byte[] source = generator.Generate(diagnosticPlan);
        var originalDoc = XmlMerge.Read(Encoding.UTF8.GetString(source));
        var extension = originalDoc.CreateElement("PrivatePayload", "https://private.example/");
        extension.SetAttribute("private", "SSO_SECRET");
        extension.InnerText = "PRIVATE_XML_CONTENT";
        originalDoc.DocumentElement!.AppendChild(extension);
        originalDoc.DocumentElement.AppendChild(originalDoc.CreateComment("PRIVATE_METADATA"));
        byte[] sample = Encoding.UTF8.GetBytes(originalDoc.OuterXml);
        string diagnosticXml = Encoding.UTF8.GetString(RedactedAnswerFile.Create(sample, generator));
        var diagnosticEnvelope = XmlMerge.Parse(diagnosticXml);
        Check(!XmlMerge.IsAnswerFile(diagnosticEnvelope.DocumentElement) &&
            XmlMerge.IsAnswerFile(diagnosticEnvelope.DocumentElement!.ChildNodes.OfType<XmlElement>().Single()),
            "diagnostic XML has a separate root and contains one native answer");
        bool nativeReadRejected = false;
        try { XmlMerge.Read(diagnosticXml); }
        catch (ArgumentException) { nativeReadRejected = true; }
        Check(nativeReadRejected, "the deployment reader rejects the diagnostic file");
        var diagnosticDoc = RedactedAnswerFile.ReadForImport(diagnosticXml);
        var diagnostic = XmlPlanImport.Import(diagnosticXml, generator);
        Check(diagnostic.ImportedXml!.Notices.Contains(UiText.Source("review.diagnostic.imported")) &&
            diagnostic.ImportedXml.Blockers.Contains(UiText.Source("review.diagnostic.placeholders")),
            "the import identifies the diagnostic and blocks unresolved placeholders");
        var user = diagnostic.Users.Single();
        Check(diagnostic.TargetDiskId == 1 && diagnostic.TargetPartitionId == 3 && diagnostic.ImageIndex == 6,
            "diagnostic XML round-trips target disk, partition, and image index");
        Check(user.Name == "TestUser1" && user.Administrator && diagnostic.AutoLogon &&
            Node(diagnosticDoc, "//u:AutoLogon/u:Username").InnerText == user.Name,
            "sample account preserves group and automatic logon reference");
        Check(user.Password == "" && user.NeedsPassword && !user.AllowEmptyPassword,
            "diagnostic account password is imported as pending");
        Check(!diagnosticXml.Contains("Private", StringComparison.OrdinalIgnoreCase) &&
            !diagnosticXml.Contains("SSO_SECRET") && !diagnosticXml.Contains("UnattendLocal:v1 ") &&
            !diagnosticXml.Contains(Convert.ToBase64String(Encoding.Unicode.GetBytes("FixturePrivatePassword!Password"))),
            "sample excludes plaintext and encoded credentials, unknown values, comments, and snapshot");
        RejectExport(diagnostic, "diagnostic configuration needs review before deployment");
        foreach (XmlNode commands in diagnosticDoc.SelectNodes("//*[local-name()='RunSynchronous' or local-name()='RunAsynchronous' or local-name()='FirstLogonCommands' or local-name()='LogonCommands']")!.Cast<XmlNode>().ToArray())
            commands.ParentNode!.RemoveChild(commands);
        var envelope = diagnosticEnvelope.DocumentElement!;
        envelope.ReplaceChild(diagnosticEnvelope.ImportNode(diagnosticDoc.DocumentElement!, true), envelope.ChildNodes.OfType<XmlElement>().Single());
        var repairedDiagnostic = XmlPlanImport.Import(diagnosticEnvelope.OuterXml, generator);
        repairedDiagnostic.Users[0].Password = "FixtureReplacementPassword!";
        repairedDiagnostic.Users[0].NeedsPassword = false;
        var repairedXml = Encoding.UTF8.GetString(generator.Generate(repairedDiagnostic));
        var reviewed = XmlPlanImport.Import(repairedXml, generator);
        Check(reviewed.Users[0].Name == "TestUser1" && reviewed.Users[0].Password == "FixtureReplacementPassword!" &&
            reviewed.TargetDiskId == 1 && reviewed.TargetPartitionId == 3 && reviewed.AutoLogon &&
            !reviewed.ImportedXml!.Notices.Contains(UiText.Source("review.diagnostic.imported")),
            "resolved diagnostic inputs can be exported with the replacement password");
        envelope.AppendChild(envelope.ChildNodes.OfType<XmlElement>().Single().CloneNode(true));
        bool duplicateAnswerRejected = false;
        try { XmlPlanImport.Import(diagnosticEnvelope.OuterXml, generator); }
        catch (ArgumentException) { duplicateAnswerRejected = true; }
        Check(duplicateAnswerRejected, "a diagnostic with multiple answer files is rejected");

        plan.EditionMode = "按 SKU 使用通用安装密钥";
        plan.EditionId = "pro";
        plan.WifiMode = WifiSettings.Wpa2;
        plan.Ssid = "Fixture";
        plan.WifiPassword = "FixtureWifiPassword!";
        var badFields = XmlMerge.Read(Encoding.UTF8.GetString(generator.Generate(plan)));
        Node(badFields, "//u:ProductKey/u:Key").InnerText = "INVALID-KEY";
        var file = Node(badFields, "//*[local-name()='File' and contains(@path,'Wifi.xml')]");
        file.InnerText = file.InnerText.Replace("FixtureWifiPassword!", "[REDACTED]", StringComparison.Ordinal);
        var badPlan = XmlPlanImport.Import(badFields.OuterXml, generator);
        Check(badPlan.ImportedXml!.SourceXml.Contains("INVALID-KEY") && badPlan.ImportedXml.SourceXml.Contains("[REDACTED]") &&
            badPlan.ImportedXml.Blockers.Contains(UiText.Source("import.credentials.installKey")) &&
            badPlan.ImportedXml.Blockers.Contains(UiText.Source("import.credentials.wifi")), "bad key and WLAN material are preserved with field-specific errors");
        RejectExport(badPlan, "invalid key and WLAN fields block export");
        return count;
    }
}
