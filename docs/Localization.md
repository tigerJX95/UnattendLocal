# 国际化与语言包 / Internationalization

[下载最新版本 / Download](https://github.com/tigerJX95/UnattendLocal/releases/latest) · [中文首页](../README.md) · [English README](../README_EN.md)

## 使用界面语言

在主窗口右上角选择“简体中文”或“English”，立即更新页面显示并记住选择。未保存的配置和已选选项保留。

界面语言只影响工具的显示，不改变目标 Windows 的显示语言、区域或键盘配置。旧 JSON 方案中的中文选项值继续使用原有格式；不会因英文界面被改成英文。用户名、密码、SSID、路径、标识符、脚本、策略及原始 XML 不作为译文处理。

程序目录包含：

```text
Languages/
  languages.json
  zh-CN.json
  en-US.json
```

`zh-CN.json` 和 `en-US.json` 是 UTF-8 的“资源键: 译文”对象。可独立替换或修订语言文件，重新启动程序，或切换到该语言重新加载；无须修改程序代码。只修改文字值，保留资源键、`{0}` 等占位符、链接和文件筛选模式。带 `StructuredLines` 标记的摘要模板还需保留行数、各行占位符和标签分隔符；它们用于分组显示。

语言包支持部分覆盖。缺少条目时使用内置的对应语言；新增语言缺少条目时回退英语。缺少整个文件时仍可使用内置简体中文和英语。重复键、未知键、空值、损坏的占位符和被改变的受保护内容会使整个文件被拒绝，不会部分加载；界面显示提示并保留有效资源。语言文件只作为文本数据读取，不执行脚本或 XAML。读取上限为每包 4 MiB、语言列表 64 KiB。

已有的第三方许可和脚本原文保持原样。语言包不翻译安装时脚本控制台输出，也不改变系统文件对话框由 Windows 自身提供的按钮语言。

## 维护翻译

源码中的语言文件位于 `Localization/Resources/`，构建时复制到程序的 `Languages` 目录，并嵌入一份基础资源作为回退。维护译文后重新构建即可更新发布包。

| 文件 | 作用 |
| --- | --- |
| `zh-CN.json` | 可维护的中文显示文案，也是翻译任务的源文 |
| `en-US.json` | 英语显示文案 |
| `languages.json` | 语言代码和原生名称列表 |
| `catalog.json` | 稳定键、兼容源值、使用位置、参数及受保护内容 |
| `translation-state.json` | 各语言已审阅源文的 SHA-256，检测源文变化后待更新的译文 |

日常修改只编辑对应语言 JSON。中文显示文案与 `catalog.json` 的兼容源值分别保存：修订措辞时修改 `zh-CN.json`，保留兼容源值，避免改变已保存方案的取值。源文变化后，检查会要求重新审阅对应英语条目。

Python 3.10 及以上可运行以下维护工具；桌面程序、普通构建和验证不依赖 Python：

```powershell
python tools/localization.py check
python tools/localization.py export --language en-US --file translations-en-US.json
# 翻译或审阅 entries 中的 target 字段，保留其余字段。
python tools/localization.py import --language en-US --file translations-en-US.json
python tools/localization.py check
```

默认只导出缺失或源文变化的条目。全面校对时为 `export` 加 `--all`。任务文件包含键、源文、源文哈希、使用位置、占位符、保护内容及目标译文。导入前校验整批内容；任何条目过期或不合法都不写入。保存采用临时文件替换；若两份文件之间发生中断，状态检查会要求再次审阅。

自动翻译可以由 CI 或外部服务读取相同任务文件，填写 `target`，经人工核对后导入；流程不绑定翻译厂商。默认不联网发送文案，也不读取方案或用户凭据。重要风险说明应人工核对后再发布。

新增界面文案时先注册稳定、语义明确的键：

```powershell
python tools/localization.py add --key export.exampleHelp --source "核对应答文件后导出。" --context "Desktop/MainWindow.xaml:Text"
```

XAML 静态显示使用 `{loc:Text export.exampleHelp}`；绑定的说明使用 `{loc:Display Value={Binding Summary}}`。需要保持原选项值的 `Content` 使用 `{loc:Source key}`，通过显示模板翻译。C# 使用 `UiText.Source(key)` 返回兼容值，在显示处 `UiText.Translate(...)`；参数说明通过 `UiText.Format(key, UiText.Arg(value))` 构造，并在 catalog 的 `RawArgs` 中将用户输入参数标记为 `true`，避免翻译数据。不要把显示译文写回配置或给可编辑文本框加翻译转换器。

为新增资源运行任务导出、补译与检查，再运行 `verify.ps1`。新增命令、链接或技术示例时同步维护 `ProtectedTokens`、`FilterPatterns` 或 `Opaque` 标记。不要修改现有资源键或兼容源值来修订翻译。原始兼容选项常量保留在代码中供旧方案比较使用；显示译文只在资源中维护。

## 增加其他语言

在 `languages.json` 添加语言代码及原生名称，例如 `{"Code":"fr-FR","Name":"Français"}`，并放入同名 `fr-FR.json`。运行时可直接读取新增语言，无须增加语言枚举或分支代码。源码维护时通过相同的导出 / 导入工具建立翻译状态；新增语言可以先部分翻译，缺失内容使用英语。

## English

Choose Simplified Chinese or English at the top right of the main window. The interface updates immediately and remembers the selection. This does not change the installation plan, the target Windows language, scripts, user input, IDs or generated XML.

UTF-8 language packs are independent JSON dictionaries in the `Languages` directory beside the executable. Replace or edit a pack, then restart or select that language again. Embedded Chinese and English resources remain available when files are absent. Partial packs overlay the bundled language; additional languages fall back to English for missing keys. Invalid packs are rejected as a whole. Third-party licenses and script contents remain in their original language; Windows supplies the native file-dialog button language.

Source packs live in `Localization/Resources`. Stable keys and canonical compatibility values are recorded in `catalog.json`; editable Chinese wording lives in `zh-CN.json`. Revise displayed wording without changing canonical values used by old plans. `translation-state.json` records the source hash reviewed for each target entry.

Use the Python commands above to validate resources and export/import incremental jobs. Add `--all` to export a complete review batch. Jobs include source hashes, contexts, placeholders and protected tokens. A stale or invalid entry rejects the entire import before any writes. An interrupted two-file update is detected as stale state by validation. The optional tool requires Python 3.10+; the application and normal build do not.

External translation services can fill the same job format without changing the desktop architecture. No translation service is contacted automatically, and user plans or credentials are not translation inputs. Review safety-critical messages before release.

Add another language by registering its culture code and native name in `languages.json` and supplying `<code>.json`. No program enum or language-specific branch is required. Mark user-supplied dynamic arguments as raw in the catalog; localize only presentation bindings, never editable data or persisted option values. Run `verify.ps1` before publishing.
