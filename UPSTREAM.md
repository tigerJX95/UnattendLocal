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
