# XML 导入 / XML import

## V1.12.1 安全审计修复 · 2026-10-21

- 补齐原生同步命令 Credentials/Password 的预览遮蔽，并明确自定义脚本敏感内容需自行核对；实际安装导出仍保留所需凭据。
- 旧版应用移除转换只修改已识别的移除内容，保留无关脚本中的双反斜杠等原始字符串。
- 开关导入核对完整命令、参数值、辅助文件及执行阶段，防止相反注册表值被误映射。
- 按数字 Order 检查脚本提取、调用及默认用户配置单元加载/卸载顺序；异常管线保留原文并提示。
- JSON 保存和 XML 导出先写同目录临时文件，写入及刷盘成功后再替换目标，写入失败保留旧文件。
- 拒绝账户名称中的制表符、换行等控制字符，错误定位到具体账户名称。
- 版本元数据统一为 1.12.1 / 1.12.1.0；截图继续沿用已标注的 V1.7 实机图片。

## V1.12：拖放与等效脚本兼容 / Drag-and-drop and equivalent scripts

### 文件类型与大小

- 将一个 `.xml` 或 `.json` 文件拖入主窗口任意位置（包括输入框和脚本编辑区），或使用原有导入／打开按钮。
- XML 必须是 Windows 应答文件，JSON 必须具有本程序支持的方案结构；开始菜单 JSON、WLANProfile、AppLocker 或布局 XML 应使用各自设置页的导入入口。
- 每次只支持一个文件，不导入文件夹或脚本文件。
- 文件上限：XML 32 MiB、JSON 128 MiB；XML 文本仍受 16 Mi 字符及结构限制。

### 确认替换与取消

- 文件读取、类型与结构检查完成后，才询问是否替换当前配置。
- 选择“否”会明确显示取消结果；读取、解析或结构校验失败会显示失败原因。
- 两者均保留当前方案、填写值和预览。
- JSON 中省略的密码和密钥仍须重新填写。
- 拖入文件不会执行脚本或自动导出。
- Windows 权限级别不同可能阻止资源管理器向程序拖放；可使用文件按钮完成导入。

### 等效脚本识别

- 只对完整确认等效的历史模板开放映射，包括旧版应用移除的单复数文件名及配套脚本、OneDrive 命令、已知日志截断写法和 Windows 本地路径分隔符。
- 不能只凭文件名、关键词或注释认定等效。
- 未修改时保留原文；编辑后仅合并相关改动，发生冲突时停止导出。

### 差异说明

- 导入说明区分已识别设置、命令顺序差异、未确认等效的命令块与执行管线差异。
- 命令差异显示文件名、命令块序号及比较视图行号；不会直接显示可能包含密码、密钥或令牌的命令正文。
- 比较视图经过已知日志与路径兼容处理，行号可能与原 XML 中的脚本文本略有不同。

### 旧版 MoSetup 兼容项

- “版本与密钥”新增独立的“写入旧版 MoSetup 升级兼容设置”，对应 `AllowUpgradesWithUnsupportedTPMOrCPU=1`。
- 只有完整识别为 specialize 阶段的原命令才勾选；值、阶段、次数或执行链有差异时只读保留。
- 此项与 PE 阶段 TPM、安全启动、内存检查独立，不联动。
- 默认关闭，不保证当前或后续 Windows 安装程序采用；不补足硬件能力。
- 取消只移除本次配置中已识别命令，不修改当前电脑或已部署系统。
- 保存过完整原文的旧 JSON 可迁移编辑基准，保留待处理修改；冲突时要求重新导入。

### File types and limits

- Drop one `.xml` or `.json` file anywhere in the main window, including text and script editors, or use the existing buttons.
- XML must be a Windows answer file; JSON must have a supported application-plan structure.
- Import Start-menu JSON, WLANProfile, AppLocker and layout XML through their own settings.
- Folders, scripts and multiple files are not imported.
- File limits are 32 MiB for XML and 128 MiB for JSON; XML also retains its 16 Mi-character and structure limits.

### Replacement and cancellation

- Reading, type and structure checks precede replacement confirmation.
- No explicitly reports cancellation; read, parse and structure errors report failure.
- Both retain the current plan, entered values and preview.
- Omitted JSON passwords and keys still require re-entry.
- Dropping a file never runs scripts or exports automatically.
- Windows may block Explorer drag-and-drop when the application runs at a higher privilege level; use the file buttons in that case.

### Equivalent scripts

- Only fully recognized historical templates are mapped: singular/plural app-removal helper names and their bodies, the matching OneDrive command, known logging variants and local Windows path separators.
- Filenames, keywords and comments alone do not establish equivalence.
- Untouched content stays intact; edits merge related changes and conflicts stop export.

### Difference notices

- Details distinguish mapped settings, command-order differences, command blocks not confirmed equivalent and execution-pipeline differences.
- Block notices show a filename, block number and comparison-view line number without exposing command bodies that may contain credentials or tokens.
- Compatibility normalization of logging and paths can make those line numbers differ slightly from the original script.

### Legacy MoSetup compatibility

- Edition and key includes an independent legacy MoSetup option for `AllowUpgradesWithUnsupportedTPMOrCPU=1`.
- Only the complete recognized specialize command is mapped.
- Different values, stages, duplicates or execution pipelines remain preserved and read-only.
- This option is independent of the three PE checks and defaults off.
- Current or future Windows installers are not guaranteed to honor it.
- It does not add hardware capabilities.
- Clearing it removes the recognized command from this configuration only.
- Older JSON plans with complete source XML can migrate their editing baseline while retaining pending edits; conflicts require reimport.

选择文件后，工具先读取与解析，在替换当前配置前确认：

- 是：导入并替换当前配置，当前未保存修改被放弃。
- 否：取消整次导入，保留当前配置并显示取消提示。
- 读取或解析失败：显示导入失败，当前配置保留。

未知或无法可靠映射的 XML 内容保留原文。已实现的功能也可能遇到不同版本或修改后的脚本，不能仅凭生成参数注释认定已完整还原。应用移除脚本未完整识别时清单只读，原脚本继续随 XML 导出。Remove 参数不是目标镜像的已安装应用清单。

The tool reads and parses the file before asking whether to replace the current configuration. Yes commits the import; No cancels the entire import and retains the current configuration. Read/parse failures show an error and retain the current configuration.

Unknown XML remains intact. An implemented feature can still have historical or modified scripts that cannot be mapped reliably. Form comments do not establish successful restoration. Unrecognized app-removal scripts remain in exported XML and the removal list becomes read-only. Remove parameters are not an inventory of apps installed in the target image.

## V0.14：已识别的旧版移除流程 / Recognized historical removal flows

支持完整识别的 RemovePackages.ps1、RemoveCapabilities.ps1、RemoveFeatures.ps1 及配套 OneDrive 命令。只有选择器、脚本主体、提取与调用阶段等均能可靠识别时才开放清单编辑；不支持的变体继续只读保留。不改清单时保留旧脚本；修改清单后，将已识别的移除流程转换为当前固定生成器格式，其他 XML 内容保留。旧方案的恢复也要求原文和编辑基准完整；迁移冲突时提示重新导入，不覆盖待处理修改。

Fully recognized RemovePackages.ps1, RemoveCapabilities.ps1, RemoveFeatures.ps1 and the matching OneDrive command can be edited. Selectors, helper bodies, extraction and execution stages must be recognized reliably. Other variants remain read-only. Unedited lists retain the historical scripts; edited lists convert the recognized removal flow to the current fixed generator format while preserving other XML. Older plans require the original source and a complete editing baseline; migration conflicts request reimport and retain pending changes.

## 导入与载入保护 / Import and loading protection

导入不会触发“从基础值重新开始”的起点选择。已还原的通用密钥/SKU、语言、账户、CompactOS、任务栏、应用移除和自定义脚本载入同一方案。兼容完整识别的日志截断外壳及 Windows 本地路径重复分隔符；只在识别视图中比较，不改写原文件或自定义 REG/脚本内容。未编辑时原内容保留，编辑后仅合并有关改动。

Import does not trigger the custom-plan reset prompt. Restored generic key/SKU, language, accounts, CompactOS, taskbar, app-removal and custom scripts load into the same plan. A fully recognized logging wrapper and repeated separators in local Windows paths are supported through a comparison view. The original file and custom REG/script payloads are retained. Unedited content stays intact; edits merge only the relevant changes.
