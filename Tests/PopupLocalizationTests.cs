using System.Text;
using System.Xml;
using UnattendLocal.Core;
using UnattendLocal.Desktop;
using UnattendLocal.Localization;

static class PopupLocalizationTests
{
    public static int Run(GeneratorService generator)
    {
        int count = 0;
        void Check(bool condition, string scenario)
        {
            if (!condition) throw new Exception("Popup localization failed: " + scenario);
            count++;
        }

        void CheckText(string source, string chinese, string english, string scenario, string? forbidden = null)
        {
            string shown = UiText.Translate(source);
            string expected = UiText.IsEnglish ? english : chinese;
            Check(shown.Contains(expected, StringComparison.Ordinal) &&
                (forbidden == null || !shown.Contains(forbidden, StringComparison.Ordinal)), scenario);
        }

        Exception Failure(Action action)
        {
            try { action(); }
            catch (Exception error) { return error; }
            throw new Exception("Expected validation to fail.");
        }

        InstallPlan LocalUserPlan() => QuickPresets.Create(QuickPresets.LocalAccount);

        string previous = UiText.Language;
        try
        {
            foreach (string language in new[] { "zh-CN", "en-US" })
            {
                UiText.SetLanguage(language);
                const string raw = "RAW_RUNTIME_DIAGNOSTIC_Example";
                var cases = new (Exception Error, string Chinese, string English)[]
                {
                    (new FileNotFoundException(raw), "找不到文件或文件夹", "file or folder could not be found"),
                    (new DirectoryNotFoundException(raw), "找不到文件或文件夹", "file or folder could not be found"),
                    (new UnauthorizedAccessException(raw), "没有权限", "was denied"),
                    (new PathTooLongException(raw), "文件路径过长", "path is too long"),
                    (new IOException(raw), "文件读取或写入失败", "could not be read or written"),
                    (new OverflowException(raw), "数值超出允许范围", "outside the allowed range"),
                    (new ArgumentOutOfRangeException(null, raw), "数值超出允许范围", "outside the allowed range"),
                    (new FormatException(raw), "输入内容无效", "input is invalid"),
                    (new Schneegans.Unattend.ConfigurationException(raw), "无法生成 Windows 应答文件", "cannot generate a Windows answer file"),
                    (new InvalidOperationException(raw), "发生意外错误", "An unexpected error"),
                    (new XmlException(raw, null, 2, 7), "第 2 行、第 7 列", "line 2, column 7"),
                    (new System.Text.Json.JsonException(raw, null, 2, 7), "第 3 行的第 8 个字节", "byte 8 on line 3"),
                };
                foreach (var (error, zh, en) in cases)
                    CheckText(DiagnosticText.ForDialogError(error), zh, en, $"{language} {error.GetType().Name}", raw);
                CheckText(DiagnosticText.ForDialogError(new OverflowException(raw), importing: true),
                    "导入文件中的数值", "number in the imported file", $"{language} import overflow", raw);

                var viewModel = new MainViewModel();
                var xml = Failure(() => viewModel.ImportXml("<unattend>\n<settings></unattend>", () => true));
                CheckText(DiagnosticText.ForDialogError(xml, importing: true), "第 2 行", "line 2",
                    $"{language} real malformed answer XML", "does not match");
                var json = Failure(() => viewModel.ImportJson("{\n\"SchemaVersion\": }", () => true));
                CheckText(DiagnosticText.ForDialogError(json, importing: true), "JSON 内容无效", "JSON is invalid",
                    $"{language} real malformed plan JSON", "invalid start of a value");
                var shell = Failure(() => ShellSettings.ValidateDocument("pins", "{\n\"pinnedList\": }"));
                CheckText(DiagnosticText.ForDialogError(shell), "JSON 内容无效", "JSON is invalid",
                    $"{language} nested Start pins JSON error", "invalid start of a value");
                var fragment = Failure(() => AdditionalXmlSettings.Parse(new XmlComponentEntry { Content = "<OEMInformation>" }));
                CheckText(DiagnosticText.ForDialogError(fragment), "额外 XML 无效", "additional XML fragment is invalid",
                    $"{language} nested XML fragment error", "does not match");
                var policy = Failure(() => AppControlSettings.ReadPolicy("<AppLockerPolicy>"));
                CheckText(DiagnosticText.ForDialogError(policy), "XML 格式无效", "XML is invalid",
                    $"{language} AppLocker XML syntax error", "Unexpected end");
                var schema = Failure(() => AppControlSettings.ReadPolicy("<AppLockerPolicy Version=\"invalid\"/>"));
                CheckText(DiagnosticText.ForDialogError(schema), "不符合所需结构", "does not match the required structure",
                    $"{language} AppLocker XML schema error", "The 'Version' attribute");

                void CheckSetting(InstallPlan plan, string field, string zh, string en, int? index = null)
                {
                    var error = Failure(() => generator.Generate(plan));
                    Check(error is ConfigurationValidationException setting && setting.Field == field && setting.UserIndex == index,
                        $"{language} {field} keeps navigation");
                    CheckText(DiagnosticText.ForDialogError(error), zh, en, $"{language} {field} localized error");
                }

                var plan = LocalUserPlan();
                plan.Users[0].Name = "Invalid/Name";
                CheckSetting(plan, "Users.Name", "本地账户名称无效", "local account name is invalid", 0);
                plan = LocalUserPlan();
                plan.Users.Add(new UserEntry { Name = plan.Users[0].Name.ToUpperInvariant(), AllowEmptyPassword = true });
                CheckSetting(plan, "Users.Name", "本地账户名称重复", "local account name is duplicated", 1);
                plan = LocalUserPlan();
                plan.Users[0].Administrator = false;
                CheckSetting(plan, nameof(InstallPlan.Users), "至少配置一个", "at least one local administrator");
                plan = LocalUserPlan();
                plan.ComputerName = plan.Users[0].Name;
                CheckSetting(plan, "Users.Name", "不能与计算机名相同", "must differ from the computer name", 0);
                plan = LocalUserPlan();
                plan.ComputerName = "123456";
                CheckSetting(plan, nameof(InstallPlan.ComputerName), "计算机名无效", "computer name is invalid");

                plan = XmlPlanImport.Import(Encoding.UTF8.GetString(generator.Generate(LocalUserPlan())), generator);
                plan.ImportedXml!.Blockers = [raw, UiText.Source("text.b80caac11032")];
                CheckText(DiagnosticText.ForDialogError(Failure(() => generator.Generate(plan))),
                    "请核对设置", "check the settings", $"{language} saved blocker export", raw);
                string overwrite = UiText.Format("dialog.overwriteFile", "报告-原名.xml");
                CheckText(overwrite, "要替换它吗", "Replace it?", $"{language} overwrite prompt");
                Check(UiText.Translate(overwrite).Contains("报告-原名.xml"), $"{language} overwrite prompt keeps filename intact");
            }
        }
        finally { UiText.SetLanguage(previous); }

        using var legacyResource = typeof(ComputerNamingSettings).Assembly
            .GetManifestResourceStream("UnattendLocal.Core.Resources.LegacyComputerNamePrompt.ps1")!;
        using var legacyReader = new StreamReader(legacyResource);
        string legacyScript = legacyReader.ReadToEnd();
        var legacyPlan = LocalUserPlan();
        legacyPlan.ComputerNameMode = ComputerNamingSettings.Script;
        legacyPlan.ComputerNameScript = legacyScript;
        string legacyXml = Encoding.UTF8.GetString(generator.Generate(legacyPlan));
        string legacyBaseline = Encoding.UTF8.GetString(generator.GenerateStandalone(legacyPlan));

        foreach (string source in new[] { legacyXml, legacyBaseline })
        {
            var imported = XmlPlanImport.Import(source, generator);
            Check(imported.ComputerNameMode == ComputerNamingSettings.Prompt &&
                imported.ComputerNameScript == "", "legacy XML still imports as the built-in prompt");
            Check(!imported.ImportedXml!.Notices.Any(note => note.Contains("GetComputerName.ps1", StringComparison.Ordinal)),
                "legacy built-in prompt is not reported as a retained custom script");
            Check(ComputerNamingSettings.Inspect(XmlMerge.Read(Encoding.UTF8.GetString(generator.Generate(imported))))
                .Getter.Contains("请输入有效的计算机名"), "re-export upgrades the legacy prompt");
            Check(!imported.ImportedXml.SourceXml.Contains("请输入有效的计算机名"), "original XML remains intact");

            // Reproduce a previously saved JSON plan whose source and baseline both predate localization.
            imported.ImportedXml.BaselineXml = legacyBaseline;
            imported = PlanStore.Deserialize(PlanStore.Serialize(imported, true));
            var exported = XmlMerge.Read(Encoding.UTF8.GetString(generator.Generate(imported)));
            Check(ComputerNamingSettings.Inspect(exported).Getter.Contains("请输入有效的计算机名"),
                "re-export upgrades a previously saved JSON plan");
            imported.ComputerNameMode = ComputerNamingSettings.Existing;
            imported.ComputerName = "FIXED-PROMPT";
            exported = XmlMerge.Read(Encoding.UTF8.GetString(generator.Generate(imported)));
            Check(!ComputerNamingSettings.Inspect(exported).Enabled && exported.OuterXml.Contains("FIXED-PROMPT"),
                "a fixed name can still replace the legacy prompt");
        }

        legacyPlan.ComputerNameScript += "\n# User comment to preserve";
        var custom = XmlPlanImport.Import(Encoding.UTF8.GetString(generator.Generate(legacyPlan)), generator);
        Check(custom.ComputerNameMode == ComputerNamingSettings.Script &&
            ComputerNamingSettings.Inspect(XmlMerge.Read(Encoding.UTF8.GetString(generator.Generate(custom))))
                .Getter.Trim() == legacyPlan.ComputerNameScript.Trim(), "customized legacy script stays verbatim");

        var ambiguous = XmlMerge.Read(legacyBaseline);
        var setter = ambiguous.SelectNodes("//*[local-name()='File']")!.Cast<XmlElement>()
            .Single(file => file.GetAttribute("path").EndsWith("SetComputerName.ps1", StringComparison.OrdinalIgnoreCase));
        setter.InnerText += "\n# Custom setter";
        var ambiguousPlan = XmlPlanImport.Import(ambiguous.OuterXml, generator);
        string ambiguousExport = Encoding.UTF8.GetString(generator.Generate(ambiguousPlan));
        Check(ambiguousPlan.ComputerNameMode == ComputerNamingSettings.Existing &&
            ambiguousExport.Contains("Custom setter") && !ambiguousExport.Contains("请输入有效的计算机名"),
            "an ambiguous naming pipeline keeps the legacy getter and custom setter");

        return count;
    }
}
