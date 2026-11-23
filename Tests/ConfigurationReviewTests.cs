using System.Text;
using System.Xml;
using UnattendLocal.Core;
using UnattendLocal.Desktop;
using UnattendLocal.Localization;

static class ConfigurationReviewTests
{
    public static int Run(GeneratorService generator)
    {
        int count = 0;
        void Check(bool ok, string message)
        {
            if (!ok) throw new Exception("Configuration review: " + message);
            count++;
        }

        var vm = new MainViewModel();
        var plan = vm.Plan;
        plan.ConfirmWipeDisk = true;
        string baseline = PlanStore.Serialize(plan, includeSecrets: true);
        Check(vm.CompareSavedPlan(baseline).Count == 0 && plan.ConfirmWipeDisk,
            "unchanged plan matches serialized copy despite transient consent");
        plan.ComputerName = "PrivateMachine";
        plan.WifiPassword = "PrivateCredential";
        plan.Users[0].Password = "PrivateUserCredential";
        plan.Scripts.Add(new ScriptEntry { Content = "PrivateScript", Enabled = false });
        plan.XmlComponents.Add(new XmlComponentEntry { Content = "<PrivateExtra />" });
        plan.ConfirmWipeDisk = true;
        vm.Preview = "previous preview";
        var fields = vm.CompareSavedPlan(baseline);
        Check(fields.ToHashSet(StringComparer.Ordinal).SetEquals(new[]
        {
            nameof(plan.ComputerName), nameof(plan.WifiPassword), nameof(plan.Users),
            nameof(plan.Scripts), nameof(plan.XmlComponents)
        }), "comparison returns changed field names without their values");
        Check(ReferenceEquals(plan, vm.Plan) && plan.ConfirmWipeDisk && vm.Preview == "previous preview" &&
            plan.WifiPassword == "PrivateCredential" && plan.Scripts[0].Content == "PrivateScript",
            "comparison does not replace the plan, clear consent, or invalidate the preview");
        var reference = PlanStore.Deserialize(PlanStore.Serialize(plan, includeSecrets: true));
        plan.Scripts.Add(new ScriptEntry { Content = "SecondScript", Enabled = false });
        reference.Scripts.Insert(0, new ScriptEntry { Content = "SecondScript", Enabled = false });
        Check(PlanComparison.FindChangedFields(plan, reference).SequenceEqual(new[] { nameof(plan.Scripts) }),
            "script order is significant");
        try
        {
            _ = vm.CompareSavedPlan("{");
            throw new Exception("Expected a malformed JSON failure");
        }
        catch (System.Text.Json.JsonException)
        {
            Check(ReferenceEquals(plan, vm.Plan) && plan.Scripts.Count == 2, "invalid comparison leaves the current plan intact");
        }

        const string xml = """
            <unattend xmlns="urn:schemas-microsoft-com:unattend" xmlns:s="https://schneegans.de/windows/unattend-generator/">
              <settings pass="offlineServicing">
                <component name="Microsoft-Windows-PnpCustomizationsNonWinPE">
                  <DriverPaths><PathAndCredentials><Path>PRIVATE_DRIVER_PATH</Path></PathAndCredentials></DriverPaths>
                </component>
              </settings>
              <settings pass="windowsPE">
                <component name="Microsoft-Windows-Setup">
                  <UseConfigurationSet>true</UseConfigurationSet><DynamicUpdate><Enable>true</Enable></DynamicUpdate>
                  <ImageInstall><OSImage><InstallFrom><Path>PRIVATE_IMAGE_PATH</Path><MetaData /></InstallFrom></OSImage></ImageInstall>
                  <LogPath>PRIVATE_LOG_PATH</LogPath><DiskConfiguration />
                </component>
              </settings>
              <settings pass="specialize">
                <component name="Microsoft-Windows-Deployment">
                  <RunSynchronous><RunSynchronousCommand><Path>PRIVATE_COMMAND</Path></RunSynchronousCommand></RunSynchronous>
                </component>
              </settings>
              <settings pass="auditSystem" />
              <s:Extensions><s:Build><s:PEScriptCopy>PRIVATE_PE_SCRIPT</s:PEScriptCopy></s:Build></s:Extensions>
            </unattend>
            """;

        string withComment = xml.Replace("<settings pass=\"auditSystem\" />",
            "<!--UnattendLocal:editable snapshot--><settings pass=\"auditSystem\" />");
        Check(PlanComparison.FindChangedAnswerSections(xml, withComment).Count == 0,
            "XML comparison ignores comments containing editing metadata");
        const string ordered = """
            <unattend xmlns="urn:schemas-microsoft-com:unattend" xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance" xmlns:t="urn:test">
              <settings pass="specialize"><component name="Example" processorArchitecture="amd64"><Test xsi:type="t:test">text</Test></component></settings>
            </unattend>
            """;
        string reformatted = ordered.Replace("xmlns:t", "xmlns:types")
            .Replace("t:test", "types:test").Replace("name=\"Example\" processorArchitecture=\"amd64\"", "processorArchitecture=\"amd64\" name=\"Example\"");
        Check(PlanComparison.FindChangedAnswerSections(ordered, reformatted).Count == 0,
            "XML comparison ignores equivalent prefix and attribute order changes");
        Check(PlanComparison.FindChangedAnswerSections(ordered, reformatted.Replace("urn:test", "urn:other"))
            .SequenceEqual(new[] { "specialize" }), "different namespaces for QName values remain meaningful");
        Check(PlanComparison.FindChangedAnswerSections(xml,
            xml.Replace("PRIVATE_DRIVER_PATH", "DIFFERENT_DRIVER_PATH")
                .Replace("PRIVATE_COMMAND", "DIFFERENT_COMMAND"))
            .SequenceEqual(new[] { "offlineServicing", "specialize" }),
            "XML comparison reports changed passes without exposing their values");
        Check(PlanComparison.FindChangedAnswerSections(xml, xml.Replace("PRIVATE_PE_SCRIPT", "PE_SCRIPT_CHANGED"))
            .SequenceEqual(new[] { UiText.Source("plan.compare.otherXml") }),
            "XML comparison detects changes in embedded deployment extensions");
        Check(PlanComparison.FindChangedAnswerSections(xml, xml.Replace("<settings pass=\"auditSystem\" />", ""))
            .SequenceEqual(new[] { "auditSystem" }),
            "XML comparison reports missing configuration passes");
        try
        {
            _ = PlanComparison.FindChangedAnswerSections(xml, "<WLANProfile />");
            throw new Exception("Expected a non-answer XML failure");
        }
        catch (ArgumentException)
        {
            Check(true, "XML comparison requires a Windows answer file");
        }

        var review = InstallationReview.ForExport(Encoding.UTF8.GetBytes(xml));
        string english = UiText.EnglishText(review.Phases);
        Check(english.IndexOf("windowsPE", StringComparison.Ordinal) < english.IndexOf("offlineServicing", StringComparison.Ordinal) &&
            english.Contains("specialize") && !english.Contains("auditSystem"), "phases follow catalog order and omit empty entries");
        Check(english.Contains("PE script deployment") && english.Contains("Disk partitioning") &&
            english.Contains("Image path") && english.Contains("Dynamic Update") && english.Contains("Configuration set") &&
            english.Contains("Target Windows driver paths") && english.Contains("Synchronous commands"),
            "phase review describes actual deployment operations and dependencies");
        Check(!english.Contains("PRIVATE_") && !review.Phases.Contains("PRIVATE_"), "phase summary hides paths and command text in both languages");

        string privateAnswer = xml.Replace("<settings pass=\"auditSystem\" />", """
            <!--PRIVATE_EDIT_METADATA-->
            <settings pass="oobeSystem">
              <component name="Microsoft-Windows-Shell-Setup">
                <ComputerName>PRIVATE_MACHINE</ComputerName>
                <TimeZone>PRIVATE_TIME_ZONE</TimeZone>
                <ProductKey>PRIVATE_KEY</ProductKey>
                <UserAccounts><LocalAccounts>
                  <LocalAccount><Name>PRIVATE_ADMIN</Name><Group>Administrators</Group><Password><Value>PRIVATE_PASSWORD</Value></Password></LocalAccount>
                  <LocalAccount><Name>PRIVATE_USER</Name><Group>Users</Group></LocalAccount>
                  <LocalAccount><Name>PRIVATE_OTHER</Name><Group>PRIVATE_CUSTOM_GROUP</Group></LocalAccount>
                </LocalAccounts></UserAccounts>
                <AutoLogon><Enabled>true</Enabled><Username>PRIVATE_USER</Username><LogonCount>2</LogonCount></AutoLogon>
                <FirstLogonCommands><SynchronousCommand><CommandLine>PRIVATE_LOGON_COMMAND</CommandLine></SynchronousCommand></FirstLogonCommands>
                <LogonCommands><AsynchronousCommand><CommandLine>PRIVATE_ASYNC_LOGON</CommandLine></AsynchronousCommand></LogonCommands>
              </component>
            </settings>
            <settings pass="specialize"><component name="PRIVATE_NETWORK">
              <IpAddress>PRIVATE_ADDRESS</IpAddress>
              <RunAsynchronous><RunAsynchronousCommand><Path>PRIVATE_ASYNC_COMMAND</Path></RunAsynchronousCommand></RunAsynchronous>
            </component></settings>
            """).Replace("<s:Extensions>", "<s:Extensions><s:File path='PRIVATE_PATH.ps1'>PRIVATE_SCRIPT</s:File>")
            .Replace("<DiskConfiguration />", "<DiskConfiguration><Disk><WillWipeDisk>1</WillWipeDisk><CreatePartitions><CreatePartition><Order>1</Order></CreatePartition></CreatePartitions></Disk></DiskConfiguration>")
            .Replace("<component ", "<component processorArchitecture=\"amd64\" ");
        byte[] privateBytes = Encoding.UTF8.GetBytes(privateAnswer);
        string[] details = ["requests a disk wipe", "Create-partition entries: 1", "Synchronous commands: 1",
            "Asynchronous commands: 1", "First-logon commands: 1", "Asynchronous logon commands: 1",
            "Local accounts: 3 (Administrators: 1, Users: 1, other or unspecified: 1)",
            "Automatic logon count: 2", "Computer name specified", "Time zone specified"];
        string screen = InstallationReview.ForExport(privateBytes).Phases;
        Check(details.All(UiText.EnglishText(screen).Contains), "the dialog reviews structured details");
        string diagnostic = Encoding.UTF8.GetString(RedactedAnswerFile.Create(privateBytes, generator));
        Check(!diagnostic.Contains("PRIVATE_") && !screen.Contains("PRIVATE_") &&
            !UiText.EnglishText(screen).Contains("PRIVATE_") &&
            Encoding.UTF8.GetString(privateBytes) == privateAnswer, "the diagnostic XML and dialog omit private values");
        Check(diagnostic.Contains("<WillWipeDisk>true</WillWipeDisk>") && diagnostic.Contains("<AsynchronousCommand>") &&
            diagnostic.Contains("<CommandLine>[REDACTED]</CommandLine>"), "the diagnostic XML retains deployment structure");
        const string metadata = "<MetaData xmlns:wcm='http://schemas.microsoft.com/WMIConfig/2002/State' wcm:action='add'>";
        string ReviewImage(string entries) => UiText.EnglishText(InstallationReview.ForExport(
            Encoding.UTF8.GetBytes(xml.Replace("<MetaData />", entries))).Phases);
        const string indexBody = "<Key>/IMAGE/INDEX</Key><Value> 6 </Value></MetaData>";
        Check(ReviewImage(metadata + indexBody).Contains("Select Windows image by index (6)"),
            "review shows the image index from the final XML");
        foreach (string key in new[] { "NAME", "DESCRIPTION" })
        {
            string selection = ReviewImage(metadata + $"<Key>/IMAGE/{key}</Key><Value>PRIVATE_IMAGE_LABEL</Value></MetaData>");
            Check(selection.Contains("Select Windows image by " + key.ToLowerInvariant()) && !selection.Contains("PRIVATE_"),
                "review shows the selection method without copying a custom image label or path");
        }
        Check(!ReviewImage("").Contains("Select Windows image"), "missing metadata adds no image selection claim");
        foreach (string entries in new[] { "<MetaData />", metadata + indexBody + metadata + indexBody,
            metadata.Replace("'add'", "'remove'") + indexBody,
            metadata + indexBody.Replace("> 6 <", ">2147483648<"),
            metadata + indexBody.Replace("> 6 <", ">0<"),
            metadata + indexBody.Replace("/IMAGE/INDEX", "/image/index"),
            metadata + indexBody.Replace("</MetaData>", "<Value>PRIVATE_DUPLICATE</Value></MetaData>") })
        {
            string selection = ReviewImage(entries);
            Check(selection.Contains("Review the Windows image selection") && !selection.Contains("by index") && !selection.Contains("PRIVATE_"),
                "unrecognized or multiple rules prompt review without making a selection claim");
        }

        const string firstRun = """
            <settings pass="oobeSystem">
              <component name="Microsoft-Windows-Shell-Setup">
                <OOBE>
                  <HideWirelessSetupInOOBE> 1 </HideWirelessSetupInOOBE>
                  <HideOnlineAccountScreens>true</HideOnlineAccountScreens>
                </OOBE>
              </component>
            </settings>
            """;
        string ReviewWithPhase(string settings) => UiText.EnglishText(InstallationReview.ForExport(
            Encoding.UTF8.GetBytes(xml.Replace("<settings pass=\"auditSystem\" />", settings))).Phases);
        string withHiddenPages = ReviewWithPhase(firstRun);
        Check(withHiddenPages.Contains("Hide OOBE network page requested") &&
              withHiddenPages.Contains("Hide OOBE online sign-in page requested"), "review identifies OOBE page requests");
        Check(!ReviewWithPhase(firstRun.Replace("> 1 <", "> 0 <").Replace(">true<", ">false<")).Contains("requested"),
            "false OOBE values do not imply that a page will be hidden");
        Check(!ReviewWithPhase(firstRun.Replace("oobeSystem", "auditUser")).Contains("requested"),
            "OOBE page requests are only reported in the documented phase");

        var ordinary = QuickPresets.Create(QuickPresets.LocalAccount);
        ordinary.Users[0].Password = "FixturePassword!";
        ordinary.Users[0].AllowEmptyPassword = false;
        ordinary.ImageSelection = UiText.Source("text.9cbbdbe44e5d");
        ordinary.ImageIndex = 4;
        var preview = new MainViewModel { Plan = ordinary };
        byte[] answer = generator.Generate(ordinary);
        var exportedReview = InstallationReview.ForExport(answer);
        string previousLanguage = UiText.Language;
        try
        {
            UiText.SetLanguage("zh-CN");
            var prepared = preview.ShowXmlPreview(answer);
            Check(prepared.Phases == exportedReview.Phases && prepared.Phases.Contains("oobeSystem"),
                "preview and export describe the same installation phases");
            string chinese = UiText.Translate(exportedReview.Phases);
            Check(chinese.Contains("按索引 4 选择 Windows 镜像"),
                "Chinese export review includes the selected image index");
            Check(preview.Preview.Contains(chinese),
                "Chinese preview displays the exported phase review");

            UiText.SetLanguage("en-US");
            preview.RefreshPreviewLanguage();
            string translated = UiText.Translate(exportedReview.Phases);
            Check(translated.Contains("Select Windows image by index (4)"),
                "English export review translates the only note in the Windows PE phase");
            Check(preview.Preview.Contains(translated) && !preview.Preview.Contains("按索引 4 选择 Windows 镜像"),
                "English preview displays the English export review after a language switch");
        }
        finally { UiText.SetLanguage(previousLanguage); }

        var original = XmlMerge.Read(Encoding.UTF8.GetString(answer));
        foreach (XmlNode comment in original.SelectNodes("//comment()")!.Cast<XmlNode>().ToArray())
            comment.ParentNode!.RemoveChild(comment);
        var ns = new XmlNamespaceManager(original.NameTable);
        ns.AddNamespace("u", XmlMerge.Namespace);
        var keyNode = original.SelectSingleNode("//u:InstallFrom/u:MetaData/u:Key", ns)!;
        var valueNode = original.SelectSingleNode("//u:InstallFrom/u:MetaData/u:Value", ns)!;
        Check(XmlPlanImport.Import(original.OuterXml, generator).ImageIndex == 4, "a recognized index remains editable");
        keyNode.InnerText = "/IMAGE/NAME";
        valueNode.InnerText = "Example image";
        Check(XmlPlanImport.Import(original.OuterXml, generator).ImageName == valueNode.InnerText, "a recognized name remains editable");
        keyNode.InnerText = "/IMAGE/DESCRIPTION";
        valueNode.InnerText = "Example image description";
        var imported = XmlPlanImport.Import(original.OuterXml, generator);
        Check(imported.ImageIndex == null &&
            imported.ImportedXml is { Blockers.Length: 0 } data &&
            data.Notices.Contains(UiText.Source("import.imageSelection.preserved")),
            "description is preserved separately from the supported editor fields");
        var restored = PlanStore.Deserialize(PlanStore.Serialize(imported, includeSecrets: true));
        var exported = XmlMerge.Read(Encoding.UTF8.GetString(generator.Generate(restored)));
        var exportedNs = new XmlNamespaceManager(exported.NameTable);
        exportedNs.AddNamespace("u", XmlMerge.Namespace);
        Check(exported.SelectSingleNode("//u:InstallFrom/u:MetaData/u:Key", exportedNs)?.InnerText == "/IMAGE/DESCRIPTION" &&
            exported.SelectSingleNode("//u:InstallFrom/u:MetaData/u:Value", exportedNs)?.InnerText == valueNode.InnerText,
            "JSON round trip and re-export preserve the original selector");
        restored.ImageSelection = UiText.Source("text.9cbbdbe44e5d");
        restored.ImageIndex = 2;
        try
        {
            _ = generator.Generate(restored);
            throw new Exception("Export unexpectedly accepted a second selection rule");
        }
        catch (ConfigurationValidationException error) when (error.Field == nameof(InstallPlan.ImageSelection))
        {
            Check(error.Message == UiText.Source("import.imageSelection.conflict"), "conflicting selector is rejected at the image setting");
        }
        return count;
    }
}
