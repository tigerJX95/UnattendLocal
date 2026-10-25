# 国际化 PR 检查 / i18n pull-request checks

V1.20 为国际化资源引入独立工作流 `.github/workflows/i18n-check.yml`。现在对每个 PR 都触发，并且始终报告一个稳定的 `i18n required gate` 检查结果；只有当语言资源、桌面 UI、本地化回归测试、检查脚本、工作流本身或本说明文档变化时，才运行完整的翻译资源校验和 Windows 回归测试。与国际化无关的 PR 会跳过重型检查，并让门禁正常通过。亦可手动触发。

- **Validate translations (Linux, Python 标准库)**：验证 JSON / UTF-8、重复键、语言标识、默认中英文完整键集、未知键、空值、格式占位符、受保护的标记、文件筛选器模式、结构化行、源文本哈希及英文审核状态哈希；输出各语言已提交资源键的覆盖率。
- **Build and localization regression (Windows, .NET 10)**：编译测试运行器并执行现有 `--localization-only` 回归测试，包括资源语义校验、运行时语言切换、用户设置持久化、语言包校验以及切换语言前后的 XML / JSON 输出一致性。

提交第三方语言时，在 `Localization/Resources/languages.json` 添加合法语言代码与显示名，并提供同名 `<code>.json` 文件。第三方语言包允许未覆盖的键，但已提交的项必须合法；缺失内容按应用已有回退机制处理。`zh-CN` 与 `en-US` 为正式内置语言，必须覆盖所有键。

本地可运行：`python Tools/check_i18n.py`。Windows 且有 .NET 10 SDK 时，可再运行 `dotnet build Tests/Tests.csproj -c Release -warnaserror` 与 `dotnet Tests/bin/Release/net10.0/Tests.dll --localization-only`。

翻译语义、文案自然度及 WPF 控件溢出仍须人工审核。PR 检查不自动发布 EXE，且只赋予 `contents: read` 权限。已有通用 `quality.yml` 会继续独立运行。

## 分支保护 / Required check

首次在 PR 中确认 `i18n required gate` 正常出现后，在分支规则中仅将 **`i18n required gate`** 设置为必需检查，不要将 `Validate translations` 或 `Build and localization regression` 设置为必需检查；它们对无关 PR 会跳过。检测变更失败时，门禁会失败而不是放行。`pull_request` 不使用路径过滤，以免出现始终 Pending 的必需检查。

对 `main` / `master` 的直接 push 和手动运行仍会执行完整检查，但这不会替代 PR 分支保护。
