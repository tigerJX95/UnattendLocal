# 界面操作说明

## 📂 V1.12 文件拖放与导入详情 / File dropping and import details

将一个 Windows 应答 XML 或本程序保存的 JSON 方案拖入主窗口，输入框和脚本编辑区同样支持。读取检查后确认替换；选择“否”显示取消，当前配置保留。多文件、目录、无效文件及其他 JSON／XML 不替换当前方案。原按钮继续可用，JSON 省略的凭据仍需填写。

完整识别的等效旧脚本映射到已有设置。导入说明按具体命令块列出未确认差异，不因日志或已知移除写法不同就报告功能缺失。版本与密钥页新增独立 MoSetup 旧版兼容项，支持搜索与摘要；无法可靠识别的来源保持只读。

Drop one Windows answer XML or an application JSON plan anywhere in the main window, including editors. File checks precede replacement confirmation; No reports cancellation and keeps the current configuration. Multiple files, folders, invalid content and unrelated JSON/XML do not replace the plan. Existing buttons remain available; omitted credentials still require re-entry.

Fully recognized equivalent historical scripts map to existing settings. Details locate unconfirmed differences by command block instead of treating logging or known removal variants as missing features. Edition and key includes a searchable independent legacy MoSetup option with a summary; unrecognized source variants stay read-only.

## 🔎 配置错误定位 / Configuration error navigation

V1.11 在检查与预览或导出失败时，为带明确字段标识的错误显示设置位置和“前往修改”。覆盖静态 IPv4/IPv6 的 MAC、地址、掩码/前缀、网关和 DNS，以及本地账户名称/密码、内置 Administrator 密码、密钥、驱动目录、密码天数和对应数字格式错误等。点击后定位、高亮并聚焦相关输入；账户错误会选中对应账户。默认“确定”、Enter、Esc 或关闭只关闭提示，不自动跳转。

定位只改变界面显示和焦点，不自动启用设置或覆盖内容。原 XML 的阻止项提供“查看导入说明”；文件读取、导入候选文件及无法定位的错误仍显示原说明，不将候选文件错误误定位到当前方案。完整校验与原有拦截保持。

V1.11 shows a setting location and Go to edit for errors with an explicit field identity during Check and Preview or export. Supported cases include static IPv4/IPv6 fields, local account names/passwords, the built-in Administrator password, keys, driver directories, password days and named numeric-format errors. Navigation locates, highlights and focuses the input, selecting the affected account when applicable. OK, Enter, Esc or closing the dialog dismisses it without navigation.

Navigation only changes presentation and focus. It does not enable settings or overwrite values. Source XML blockers offer View import details. File read failures, candidate-file import errors and errors without a known destination keep their original explanation and do not incorrectly point to the current plan. Existing validation remains enforced.

## 💬 分行帮助与配置状态

设置帮助、按钮、下拉选项、应用移除和搜索结果的悬停说明按独立规则分行。标题、当前状态与填写说明分开；应用移除的“匹配项：”继续独占一行，标识逐项列出。命令、路径和括号/引号内的技术内容保持完整。

“待配置”表示当前已启用的配置缺项或不合法，后面显示具体原因，不代表功能未实现。该状态及具体原因使用适配浅色/深色的红色；未启用协议、只读保留状态和普通说明保持原色。配置修正后提示即时更新，灰色示例仍不作为实际输入。

Settings help, buttons, dropdown options, app removal and search-result tooltips start independent rules on separate lines. Titles, current states and instructions are separated. Application matching identifiers retain a dedicated heading and one identifier per line. Commands, paths and technical content inside parentheses or quotation marks remain intact.

“待配置” (configuration pending) means that an enabled configuration is incomplete or invalid; the following reason identifies what to correct. The pending state and its reason use theme-aware red, while disabled protocols, read-only preservation and ordinary instructions retain their normal color. Corrections update the state immediately; gray examples are not actual input.

## 📖 安装目标提示

导出前检查区与配置摘要将磁盘操作和 Windows 安装目标分别说明：自动分区不等于必然已选好安装目标；指定已有分区不等于自动分区；InstallToAvailablePartition 表示安装程序选择可用分区。没有指定目标时，提示“可能需要选择”，不宣称一定手动选盘。

生成后的预览顶部及导出危险操作提示读取实际 XML 的目标配置。导入的保留目标不能被界面的手动模式默认值误判为尚未指定。复杂、不完整或重复目标要求核对原文；提示不代替镜像、目标盘和真实安装验证。

## 🎨 可视化调色板

“配色与背景”保留常用强调色列表与 HEX 输入。点击强调色或纯色背景色块，打开独立选色窗口：

- 拖动二维色板调整饱和度和亮度，直接点击或拖动色相彩条选中该处颜色，不再显示独立的下方色相滑条；上方色板同步，可继续调整深浅。
- RGB 分量和 #RRGGBB 双向同步，预览即时更新；无效 HEX 不能作为自定义色确定。
- 色板可用方向键调整，Shift 加大步长，色相条支持方向键、Home/End，Shift 加大步长；RGB 滑块与 HEX 输入也可通过键盘操作。
- 确定才写回方案；取消、Esc 或关闭窗口不改变打开前的值。
- “恢复 Windows 默认”先进入待确认状态，再点击确定生效。强调色对应恢复整组 Windows 默认配色，纯色背景对应恢复 Windows 默认背景；保留原填色与其他页面内容。

本功能选择目标 Windows 的初始颜色，不改变本工具主题。窗口沿用工具的浅色/深色配色。JSON、XML 的已有颜色字段及生成语义保持，未增加透明通道或颜色名称支持。

## 💡 文件夹位置示意

在“桌面与文件夹 → 开始菜单电源按钮旁的文件夹”标题右侧，点击图片图标查看离线位置示意。高亮区域位于 Windows 11 开始菜单底部、电源按钮左侧，与固定应用区和任务栏不同。

图形为本地静态示意，明确标为非实机截图，仅用文档与下载作示例，不代表当前已勾选这两项。仅查看说明，不修改勾选、联网或截取屏幕。实际布局可能随 Windows 版本变化。

## 📦 脚本与应用列表

脚本导入和另存按钮采用不同方向的文档箭头。提示分别为“导入脚本文件，新增条目并默认停用”“导出选中脚本内容到文件”。导入后新增停用条目，另存输出选中条目的脚本内容。

应用移除使用双列复选框，详细影响说明在悬停提示中；“匹配项：”独立起行，下方标识逐项换行；说明仍参与搜索，筛选不会清除隐藏项目的选择。保持原有默认不勾选、批量选择确认和未知脚本只读保护，不改变实际移除脚本。

应用描述使用独立的纯文本提示模板，避免被全局“标题＋正文”模板当作对象解析而显示空框。选色底部三个按钮统一使用标准图标布局、专用图形和单行提示；恢复默认不再被过大的内边距裁切。

## 📚 References

- https://learn.microsoft.com/en-us/windows-hardware/customize/desktop/unattend/microsoft-windows-setup-imageinstall-osimage-installto
- https://learn.microsoft.com/en-us/windows-hardware/customize/desktop/unattend/microsoft-windows-setup-imageinstall-osimage-installtoavailablepartition
- https://support.microsoft.com/zh-cn/windows/experience/personalization/customize-the-windows-start-menu
