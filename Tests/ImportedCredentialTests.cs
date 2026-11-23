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
