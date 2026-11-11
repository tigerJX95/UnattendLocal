# 参与翻译 / Contributing translations

UnattendLocal 的界面译文集中保存在本目录的 `Resources/`。翻译贡献通过仓库的 Pull Request 合并。新增语言、增量翻译和日常校对使用同一套工具。请根据准备贡献的译文选择语言代码和显示名称。

| 文件 | 用途 |
| --- | --- |
| `Resources/zh-CN.json` | 简体中文源文 |
| `Resources/<语言代码>.json` | 对应语言的界面译文 |
| `Resources/languages.json` | 语言代码和原生名称 |
| `Resources/translation-state.json` | 工具维护的源文审阅记录 |
| `Resources/catalog.json` | 稳定键、使用位置、占位符和技术内容规则 |

## 新增语言包

1. 在 GitHub Fork 本仓库，将自己的 Fork 克隆到本地，从当前 `main` 新建翻译分支。
2. 安装 Python 3.10 或更新版本，在仓库根目录运行下面的初始化命令。输入你所选语言的代码和显示名称。工具会创建空语言包和审阅记录，并注册该语言。

   ```powershell
   $LanguageCode = Read-Host "语言代码"
   $LanguageName = Read-Host "语言显示名称"
   $JobFile = "translations-$LanguageCode.json"
   python Tools/localization.py init --language $LanguageCode --name $LanguageName
   python Tools/localization.py export --language $LanguageCode --file $JobFile
   ```

3. 打开上一步导出的任务文件，在 `entries` 中填写 `target`。每条任务提供 `source`、使用位置、占位符和受保护内容。技术示例会预填原文。保留键名和任务元数据。
4. 完成一批后导入。`--translated-only` 跳过空白目标文字，已完成的条目写入语言包并记录源文版本。可以分批提交。

   ```powershell
   python Tools/localization.py import --language $LanguageCode --file $JobFile --translated-only
   python Tools/localization.py check
   python Tools/check_i18n.py
   ```

5. 提交 `Localization/Resources/` 下对应语言的 JSON 文件、`languages.json` 和 `translation-state.json`，推送到自己的 Fork，向本仓库 `main` 创建 PR。任务 JSON 是本地工作文件，PR 中只提交语言包及相关维护文件。

已有语言可先用 `python Tools/localization.py status --language $LanguageCode` 查看已复核、待复核和缺译数量，再导出任务。默认导出缺译和待复核条目，全面校对时加 `--all`。

可翻译条目的源文变化后，旧译文会放在 `previous_target` 供参考，`target` 留空。复核后把确认的译文填入 `target`，也可以填回原译文。旧版任务如仍在 `target` 中预填旧译文，请重新导出后再确认。分批导入跳过空白项，严格导入会拒绝空白项。

## 语言代码示例

以繁体中文为例，语言代码可填写 `zh-Hant`，显示名称可填写「繁體中文」。[术语表示例](Glossary.zh-Hant.md)展示了如何整理常用词和校对建议。

## 翻译时保留的内容

- 只编辑任务的 `target`。保留 `{0}`、`{1}` 等参数，注意各参数代表的数值、路径或名称。
- 保留 URL、命令、注册表路径、XML 标记、文件筛选模式和标记为 `opaque` 的技术示例。
- `StructuredLines` 摘要保留原有行数、每行参数和标签分隔符。
- 风险提示清楚区分清空、格式化、删除、禁用和只读保留。用词可参照术语表，由译者结合界面审核。
- 源文变化导致任务过期时，重新导出并迁移已完成译文。工具会在写入前拒绝过期条目。

新增语言支持部分翻译，缺失条目回退英语。完整度由资源检查输出。PR 描述中说明覆盖范围、用词地区、校对方式和界面实测情况。

## 在程序中预览

运行或构建当前版本，将修改后的 `Resources/languages.json` 和对应语言 JSON 放到 EXE 旁的 `Languages/`，在右上角语言菜单选择该语言。重新选择语言会重载文件。检查设置名称、悬停提示、错误弹窗、参数位置及较长说明是否完整显示。

具备 .NET 10 SDK 和 Windows 构建环境时，可运行 `verify.ps1` 做完整验证。GitHub PR 会校验语言资源、源文审阅状态、翻译工具及运行时本地化。维护者在合并前确认语言显示和实际译文。

桌面语言包覆盖配置工具的界面。安装阶段的 PowerShell / VBScript 提示，以及第三方许可原文，各自有独立的维护位置。例如，安装时输入计算机名的提示位于 [`Core/Resources/ComputerNamePrompt.ps1`](../Core/Resources/ComputerNamePrompt.ps1)，当前按目标 Windows 界面语言选择简体中文或英语。请在 PR 中注明发现的覆盖缺口。

## English

Fork the repository and create a translation branch from current `main`. Language packs are UTF-8 JSON files in `Resources/`. Use Python 3.10+ from the repository root.

For a new language, run `Tools/localization.py init --language <code> --name "<native name>"`, then export a job using `export --language <code> --file <job.json>`. For an existing language, start with export.

Edit each task's `target`. Preserve keys, placeholders, source metadata, protected tokens, file filter patterns and structured summary lines. Opaque technical examples are prefilled. Import with `--translated-only` to submit completed entries in batches. Strict import rejects blank targets. Run `python Tools/localization.py status --language <code>` to count current, pending-review, and missing entries. When the source of a translatable entry changes, the previous translation appears in `previous_target` and `target` stays blank until you enter the reviewed translation. Export a fresh job before confirming unchanged wording from an older prefilled job.

Run both `python Tools/localization.py check` and `python Tools/check_i18n.py`. Commit the language pack, manifest for a new language, and generated review state. In the PR, describe the locale, translated scope, review method and UI testing. Runtime language files belong in `Languages/` beside the executable for preview. Missing entries in additional languages fall back to English.

Setup-time script messages are maintained separately. For example, [`Core/Resources/ComputerNamePrompt.ps1`](../Core/Resources/ComputerNamePrompt.ps1) currently selects Simplified Chinese or English from the target Windows UI culture. Adding an app language does not automatically translate this prompt.
