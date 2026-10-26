# 计算机命名与多组语言键盘

## 计算机命名

固定名称沿用旧版行为，留空由 Windows 自动生成。安装时输入沿用原生成器的 ComputerNamePrompt.ps1，通过独立对话框重复收集有效名称，取消也会继续提示；该方式需要人工参与。脚本方式填写 PowerShell，返回一个名称。动态命名方式暂用 TEMPNAME，再按原生 specialize 脚本流程应用返回值；工具不在当前电脑执行。

名称需符合 Windows 规则：1–15 字符，不能为纯数字或含非法字符，也不应与本地账户同名。自定义脚本应自行处理失败、联网和设备信息。淡色示例不参与生成，需明确填写。脚本按原文保存在方案及 XML 中，不自动识别其中的密码或令牌。

导入时仅对完整识别的原生命名流程开放编辑。修改过的辅助脚本、条件执行或缺失提取调用等情况保留原文并阻止不可靠覆盖。

## 按 MAC 地址生成计算机名 / MAC-based computer naming

在“基础设置”的计算机名方式中选择“按 MAC 地址生成计算机名”。安装时使用唯一物理以太网适配器的 MAC，去掉分隔符并加 `PC-` 前缀，例如 `00-11-22-AA-BB-CC` 生成 `PC-001122AABBCC`，共 15 个字符。它沿用原作者的计算机名脚本应用流程，不在配置工具当前电脑执行。

按接口类型识别有线网卡，不依赖 `Ethernet` 或“以太网”等显示名称。网卡驱动必须已经可用；Wi-Fi 不作为候选。无候选、多块候选、全零或广播 MAC 都会报错，不任意取第一块，也不静默改用随机名。虚拟机需要驱动将网卡识别为物理接口，并为每台分配唯一 MAC。更换网卡会改变结果，仍须确认名称在网络中唯一。

JSON 保存和本程序原始 XML 恢复均保留该方式。导入时，只有完全匹配内置 MAC 脚本的完整命名流程才恢复为此选项；修改过的脚本继续作为自定义脚本处理，避免丢失用户修改。

Choose **Derive computer name from MAC address** under Basic settings. The setup-time getter uses exactly one physical Ethernet adapter and returns `PC-` plus its 12 uppercase MAC digits (15 characters), for example `PC-001122AABBCC`. It follows the existing upstream naming pipeline. Drivers must be available; Wi-Fi is excluded. Missing, ambiguous, all-zero or broadcast addresses fail explicitly. VM adapter presentation and unique MAC assignment must be verified. A changed adapter changes the name. JSON and original XML roundtrips preserve the mode; edited getter scripts remain custom scripts.

Source: [upstream MAC naming example](https://schneegans.de/windows/unattend-generator/samples/), [Get-NetAdapter](https://learn.microsoft.com/en-us/powershell/module/netadapter/get-netadapter).

## 第二、第三组区域与键盘

在指定语言与区域模式下，两组可以独立启用。生成顺序为第一组、已启用的第二组、已启用的第三组；系统与用户区域格式仍取自第一组，Windows 显示语言另行选择。多组输入配置不安装语言包，也不改变本程序中文界面。

交互语言模式不生成固定配置，但保留方案中填写的附加组。恢复推荐会关闭附加组，不清空填写内容；命名方式作为身份设置保留。JSON 和带/不带本工具快照的 XML 均支持已识别配置的恢复；超过三组或无法识别的输入列表保留原文，不能不可靠覆盖。
