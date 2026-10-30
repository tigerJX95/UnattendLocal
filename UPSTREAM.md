# 上游来源与同步记录

- 来源：https://github.com/cschneegans/unattend-generator
- 原作者网站：https://schneegans.de/windows/unattend-generator/
- 基础固定提交：97c1141d51a9cdae6831107cb3311c2f9eb8121b。
- 选择性同步：b20a1e52bc8cd988224737a008600cc734e267c6（Remove support for JScript，仅 Main.cs 与 modifier/Script.cs）。
- ComputerNamePrompt.ps1 来自官网发布提交 27237f82e9a8498d9c5bab7d398dc34b7682a1e8，保留在 Core/Resources 中。
- 许可证：MIT，原文见 vendor/UnattendGenerator/LICENSE.txt。

2026-10-21：官网发布仍标注 27237f8。本地 V1.13 补齐下载区的手动应答文件名和 ISO 输出，固定 vendor 逐文件保持不变；不整库更新生成核心。对应官网 Main.cs 已读取核对，不能据此声称已核对尚未发布的仓库最新状态。GitHub commits API 本次未取得可用结果。

本地界面与适配模块在原生成核心上实现配置、导入合并及输出封装。ISO 使用独立组件 DiscUtils，具体版本和许可证见 THIRD_PARTY_NOTICES.md；使用说明见 docs/AnswerFileExport.md。

2026-10-22：V1.14 对照官网的 Minimal / LocalUser 预设和额外 XML 组件入口。官网固定发布仍为 27237f82e9a8498d9c5bab7d398dc34b7682a1e8；通过 GitHub 读取当前 Main.cs 核对组件配置模型。vendor 不变，继续使用固定组件与阶段目录；本地额外 XML 采用添加独立子元素的方式，不实现原站的整组件覆盖。OEM 示例依据微软 OEMInformation 文档，仅使用 SupportProvider 与 SupportURL。详见 docs/AdvancedXml.md。

2026-10-22：V1.15 添加微软原生 PreferredPlan 与 DisableWER 设置，限定专用界面的编辑阶段为 specialize；不修改 vendor，不添加新的脚本执行阶段。兼容原组件的其他子元素与 V1.14 额外 XML 编辑来源。参数、有效阶段与限制依据微软官方文档，链接见 docs/NativePreferences.md。

2026-10-23：核对官方提交 27237f82e9a8498d9c5bab7d398dc34b7682a1e8，计算机名交互提示已通过 ScriptComputerNameSettings(PromptScript) 在 V1.15 实现。Core/Resources/ComputerNamePrompt.ps1 的 Git blob 为 73b411835ed1a8e34e4469aa3bddf90a10050869，与上游一致。本次不重复合并，vendor 保持不变。

官方提交：https://github.com/cschneegans/unattend-generator/commit/27237f82e9a8498d9c5bab7d398dc34b7682a1e8

## V1.17 示例能力适配

参考原作者[示例脚本页](https://schneegans.de/windows/unattend-generator/samples/)中的离线 .NET 3.5、VC++ 运行库与 AutoRun 用途，在 `Core/Resources/Templates/` 编写本地模板；不是整体替换生成核心。新增版本限制、安装错误处理、本地源限制与下载签名检查，说明见 [ScriptTemplates.md](docs/ScriptTemplates.md)。固定 vendor 文件保持不变。

## V1.18 示例能力适配

继续参考原作者示例页的 ICMP 与关闭远程协助用途，在本地新增两个脚本模板。ICMP 范围收窄为专用 / 域网络、本地子网和回显请求类型；采用独立规则名并检查归属，不整体放行所有 ICMP 报文。脚本名称属于本工具的编辑与还原元数据，不进入原生成器的命令内容。固定 vendor 文件保持不变。

## V1.19 示例能力适配

参考原作者示例页中的 WinGet 应用安装和添加桌面快捷方式用途；本地模板默认安装 Google Chrome，包 ID 可编辑。保留 FirstLogon / System 分类，加入有界就绪等待、精确包与源选择、退出码检查，以及公共桌面解析、同名文件保护和临时文件清理。快捷方式默认链接属于本地示例，不整体更新 vendor。微软参考资料见 [ScriptTemplates.md](docs/ScriptTemplates.md)。

## V1.22 示例能力适配

参考原作者示例页的“设置 C 盘卷标”和“启用卓越性能”用途，继续保留脚本模板分类和 System 执行阶段。卷标模板自动识别目标系统盘并检查 NTFS、名称长度及字符；卓越性能模板使用专用 GUID、检查所有返回码、复用已有计划并保护 GUID 冲突。默认停用，不修改固定 vendor 生成核心。

## V1.23 通用安装模板与高级诊断

将此前按 Chrome、7-Zip 命名的安装入口重组为 WinGet、EXE 两类通用模板，保留 FirstLogon / System 阶段。WinGet 要求填写精确包 ID；EXE 要求本地路径、预置 SHA256 和厂商静默参数，不自动寻找特定软件。具体软件仅在文档示例说明；已有方案和 XML 中的脚本原文不自动迁移。防火墙日志属于基于微软 NetSecurity 官方文档的可选高级诊断扩展。vendor 保持不变。官方依据见 docs/ScriptTemplates.md。

## V1.24 原作者示例能力适配与本地安全修复

2026-10-26 核对原站及示例页；原站页面显示提交 82d4ffd。适配原作者“从 MAC 地址生成计算机名”与“安装期间暂停 Windows Update 一小时”的用途，继续使用原有命名管线和 System 执行阶段。MAC 候选按物理以太网接口类型识别并限制唯一，暂停更新保留更晚的现有到期值；不是整库更新。固定 vendor 文件保持不变。

语言包占位符校验的非回溯/超时修复属于本地代码，不声称来自原作者。具体说明见计算机命名、脚本模板与安全检查文档。

## V1.25 PE 基础安装接入

2026-10-27：接入固定核心的 GeneratePESettings，再通过 ScriptPESetttings 重发加入前置检查的完整脚本，保持原作者写入 PE 脚本及执行链一致。首阶段限定 x64、UEFI/GPT、指定单个目标磁盘、镜像索引和恢复分区。前置检查采用微软 DISM /English /Get-ImageInfo、Windows PE 启动信息以及目标磁盘查询；原始 vendor 代码不变。旧版 Setup 路径继续默认启用，相关限制、使用与验证见 docs/GeneratedPe.md。

## V1.26 MSI 安装与 Windows 沙盒模板

2026-10-27：依据微软 Windows Installer 文档新增普通 System 阶段 MSI 脚本模板，与既有 WinGet/EXE 安装方式并列；另依据微软文档新增 FirstLogon 阶段 Windows 沙盒组件模板，使用本地组件源且禁止自动重启。不是更新 vendor 生成核心；安装包路径、哈希校验、返回码与限制见 docs/ScriptTemplates.md。

## V1.27 磁盘部署安全与启动兼容性

2026-10-27：以 V1.26 两项模板完整项目为基线，重新读取并核对：

- 动态选盘提交 `8a362f03a8b97bc7e583643818be33444636bb43`（2026-09-04）。
- 移除旧磁盘断言脚本提交 `28c2d0f951f69caa3b0099b5eaab4dc02fee514b`（2026-09-15）。
- 限制 bcdedit 提交 `22b79226057e6fc522236ee99f133eafee1cf748`（2026-09-18）。
- 当前主分支读取对应最近提交 `82d4ffdd11ea3266f8f3b29d4fcd80f66bfdbaf6`，`modifier/Disk.cs` blob 为 `75330016ba22ca786d5b9f333fbd7000f5f3b4fa`。

固定核心已经包含四类目标选择模型、旧断言移除及 GPT-only bcdedit。当前 Disk.cs 与本地固定文件的实质差异是后续 UseConfigurationSet 无条件处理，不属于本轮磁盘选择/启动修复范围。因此不直接摘取旧提交，不回填已删除的断言 API，也不整库替换 vendor。

本地增加 `PeDiskSafety` 与 `PeTargetDisk.vbs`，经既有 ScriptTargetDiskSettings/ScriptPESetttings 写入完整 PE 流程：严格输出、唯一匹配、按字节容量判断、安装源排除、目标快照，以及暂停之后/破坏性 DiskPart 调用之前的再次检查。保留既有脚本阶段和常规 Setup 默认路径。

A03 在这里是本地磁盘误操作防护目标，不将其表述为官方确认的漏洞编号或已消除所有数据丢失风险。自定义脚本仍为完全可信代码；设备变化、安装源映射与真实安装的边界见 GeneratedPe.md。六项真实 VM 安装仍待验证，未将自动生成检查或模拟磁盘测试计作真实安装通过。


## V1.30 PE 分页文件接入

2026-10-30：通过官方 GitHub 核对提交 `82d4ffdd11ea3266f8f3b29d4fcd80f66bfdbaf6` 的 `modifier/Disk.cs`，其分页文件模型及处理与本地固定核心一致。本地此前将 PE `PagingFileSettings` 固定为 Automatic；本轮把现有自定义 C 盘分页文件容量映射到 `CustomPagingFileSettings`，沿用映像释放后的离线注册表处理。常规安装仍使用既有 System 配置，不叠加两套实现，不新增脚本执行阶段，也不修改 vendor。`NoPagingFileSettings` 仍未接入界面。

官方依据：[分页文件模型](https://github.com/cschneegans/unattend-generator/blob/82d4ffdd11ea3266f8f3b29d4fcd80f66bfdbaf6/modifier/Disk.cs#L116-L125)。

## V1.31 PE CompactOS 接入

核对 `cschneegans/unattend-generator` 固定文件 `modifier/Disk.cs`（blob `75330016ba22ca786d5b9f333fbd7000f5f3b4fa`）：`GeneratePESettings.CompactOs` 决定 DISM Apply-Image 是否带 `/Compact`。复用现有 CompactOsMode，普通 Setup 原生 Compact 设置不变，不修改 vendor 核心。默认和禁用均不传 /Compact，不对镜像内已有压缩文件执行解压。

来源：https://github.com/cschneegans/unattend-generator/blob/82d4ffdd11ea3266f8f3b29d4fcd80f66bfdbaf6/modifier/Disk.cs#L634-L637
