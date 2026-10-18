# 自定义脚本文件

导入支持 `.ps1`、`.cmd`、`.reg`、`.vbs`，按扩展名选择格式，每次在列表末尾新增一条默认停用的 System 脚本并选中。请自行调整执行阶段和核对内容，再明确启用。不根据脚本内容推断权限或阶段，不执行文件；原有条目和顺序保持。文件路径不会加入方案。

支持严格 UTF-8（有无 BOM 均可）以及带 BOM 的 UTF-16 LE / BE。无 BOM 的文件只按 UTF-8 读取；不猜测 ANSI、GBK 或 UTF-16，不使用替换字符掩盖解码错误。UTF-32 暂不支持。单条限 2 Mi 字符（UTF-16 代码单元），读取上限 8 MiB，空白与 XML 非法字符拒绝。合法代理对字符可正常保存和生成 XML。总列表最多 60 条；同阶段最多启用 15 条的原有限制保持。

另存只包含当前脚本正文，不保存执行阶段、启用状态或其他方案内容；重新导入仍按默认停用的 System 条目处理。保存前完成内容校验，在目标目录写入临时文件、关闭后替换用户选择的目标，避免校验失败或临时写入失败破坏已有文件。已有文件由系统保存对话框确认覆盖。

| 格式 | 另存编码 | 补充说明 |
| --- | --- | --- |
| PS1 | UTF-8，带 BOM | 兼容 Windows PowerShell 5.1 的非 ASCII 脚本读取 |
| CMD | UTF-8，无 BOM | 目标 CMD 代码页必须匹配；不自动插入 chcp，不保证直接运行中文命令 |
| REG | UTF-16 LE，带 BOM | 缺少标准 Version 5.00 头时仅在输出中补齐，编辑器原文不变 |
| VBS | UTF-16 LE，带 BOM | 安装阶段支持情况由目标 Windows 决定 |

除 REG 文件头外，保留文本和原换行；不保证另存字节与原文件完全相同，编码会按上表转换。文件保存不改变应答 XML 提取文件时的原作者编码策略。JScript（.js）继续停止支持，不通过文件操作恢复。

脚本内的密码、令牌与路径按原文保存，方案的凭据省略不解析脚本。文件操作检查文本结构与编码，不检查脚本语义或运行效果。

参考：[Microsoft PowerShell 字符编码说明](https://learn.microsoft.com/en-us/powershell/module/microsoft.powershell.core/about/about_character_encoding?view=powershell-5.1)。

## English

Import appends one disabled System-phase script, selected for review, based on the PS1/CMD/REG/VBS extension. It never infers a stage or executes the file. Existing entries remain intact, and source paths are not stored.

Supported encodings are strict UTF-8, with or without BOM, and BOM-marked UTF-16 LE/BE. Convert ANSI/GBK or UTF-32 first. Limits: 2 Mi UTF-16 code units per script, 8 MiB input, 60 total entries and 15 enabled scripts per stage. Invalid text is rejected before changing the list.

Save-as writes text only, without enabled state or stage. PS1 uses UTF-8 BOM; REG/VBS use UTF-16 LE BOM; CMD uses UTF-8 without BOM and needs a matching target code page. A missing standard REG header is added to the output only. Text and line endings otherwise remain unchanged; saved bytes may differ due to encoding conversion. Writes complete a temporary file before replacing the selected destination. Script syntax and target behavior are not verified.
