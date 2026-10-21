# 上游来源与同步记录

- 来源：https://github.com/cschneegans/unattend-generator
- 原作者网站：https://schneegans.de/windows/unattend-generator/
- 基础固定提交：97c1141d51a9cdae6831107cb3311c2f9eb8121b。
- 选择性同步：b20a1e52bc8cd988224737a008600cc734e267c6（Remove support for JScript，仅 Main.cs 与 modifier/Script.cs）。
- ComputerNamePrompt.ps1 来自官网发布提交 27237f82e9a8498d9c5bab7d398dc34b7682a1e8，保留在 Core/Resources 中。
- 许可证：MIT，原文见 vendor/UnattendGenerator/LICENSE.txt。

2026-10-21：官网发布仍标注 27237f8。本地 V1.13 补齐下载区的手动应答文件名和 ISO 输出，固定 vendor 逐文件保持不变；不整库更新生成核心。对应官网 Main.cs 已读取核对，不能据此声称已核对尚未发布的仓库最新状态。GitHub commits API 本次未取得可用结果。

本地界面与适配模块在原生成核心上实现配置、导入合并及输出封装。ISO 使用独立组件 DiscUtils，具体版本和许可证见 THIRD_PARTY_NOTICES.md；使用说明见 docs/AnswerFileExport.md。
