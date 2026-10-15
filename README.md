# UnattendLocal — Windows 无人值守配置工具

**简体中文** | [English](README_EN.md)

UnattendLocal 是基于 [Christoph Schneegans 的 UnattendGenerator](https://github.com/cschneegans/unattend-generator) 开发的中文 Windows 桌面配置工具，用于在本地编辑、检查和生成 Windows 10 / 11 的 `autounattend.xml`。

当前版本：**V0.7**。本版新增讲述人辅助安装、允许卸载 Edge 和关闭 C 盘系统还原三项设置，延续原有生成核心和脚本执行阶段。工具用于准备安装配置，不会在当前电脑执行生成的安装脚本。

## 💡 开发初衷

> 方便自己在本地离线配置，也方便下一次直接复用。

最初做 UnattendLocal，是想给自己准备一个可以在本地离线使用的 Windows 安装配置工具。把常用设置保存成方案，重新安装系统或换一台电脑时，直接载入，再根据实际环境调整，让重复的准备工作更省心。

项目以 Christoph Schneegans 的成熟生成核心为基础，将常用选项整理成中文桌面界面，并补上方案保存、配置摘要、检查预览和已有 XML 的导入编辑。希望从选择设置到生成文件，整个过程都更直观，也更容易核对。

现在把它公开，也希望方便有同样需求的人使用，并通过大家的实际测试逐步完善。开发会继续保持小步推进，优先做好常用功能、配置可靠性和安装验证。

这里的离线使用指在本地编辑、保存与生成配置；微软账户登录、联网下载图片或其他联网脚本，仍取决于目标安装环境的网络条件。

## 📸 运行截图

| 配置方案 · 浅色主题 | 版本与密钥 · 深色主题 |
| :---: | :---: |
| ![配置方案：浅色主题](docs/images/overview.png) | ![版本与密钥：深色主题](docs/images/edition-and-key-dark.png) |

截图来自 V0.1 pre41 的实际运行界面，用于展示浅色与深色主题；当前 V0.7 已调整导航顺序并新增部分设置，实际界面以当前版本为准。

## ✨ 主要功能

- 配置安装架构、显示语言、区域、键盘、时区、计算机名及安装版本 / 产品密钥。
- 配置镜像选择、已有分区或自动分区、CompactOS；涉及清空或格式化时显示风险说明并要求核对。
- 配置本地账户、密码、首次登录、密码到期与锁定策略，以及 Wi-Fi、离线安装和 OOBE。
- 配置资源管理器、开始菜单、任务栏、系统桌面图标、视觉效果、配色、纯色或图片背景及锁屏图片。
- 按项选择 60 个应用与系统能力移除项目，提供中文影响说明、关键词搜索和选择摘要，默认不勾选。
- 配置讲述人辅助安装、Edge 卸载入口和 C 盘系统还原行为，三项默认不勾选。
- 配置 VMware、VirtualBox、KVM / QEMU（VirtIO）和 Parallels 的工具安装，附带所需介质及作用范围说明。
- 配置常用系统行为，包括快速启动、长路径、系统声音、设备自动加密、硬件关联应用、更新自动重启、NTFS 最后访问时间、进程创建审计、UAC / WPBT 及远程桌面 / PowerShell 执行策略等选项。
- 按原设计管理 System、DefaultUser、FirstLogon、UserOnce 自定义脚本，提供示例与执行范围说明。
- 保存和恢复 JSON 方案，提供配置摘要、推荐差异、检查、脱敏预览和 XML 导出。
- 导入已有 XML 后编辑已支持设置，保留未知内容；遇到无法可靠合并的修改时停止导出并说明原因。

## ✨ V0.7 更新

- 新增“讲述人辅助安装”：在 PE、系统安装阶段和每位用户首次登录时启动屏幕朗读，需介质及音频支持。
- 新增“允许卸载 Edge”：尝试提供卸载入口，不实际卸载 Edge；效果受 Windows 版本、权限和更新影响。
- 新增“关闭 C 盘系统还原”：全机首次登录时执行，可能删除已有还原点；默认不勾选。
- 三项均默认关闭，支持方案保存、摘要、推荐差异/恢复、动态帮助及 XML 导入后独立修改或取消。
- 导入时核对完整脚本及执行管线，保留未知内容；完整识别的讲述人 PE 启动命令不再单独触发磁盘风险确认，实际磁盘及未知 PE 操作仍须核对。

## 📦 下载与使用

从仓库的 **Releases** 页面下载并直接运行对应的 `UnattendLocal_V0.7_x64_SelfContained.exe` 或 `UnattendLocal_V0.7_x64_FrameworkDependent.exe`：

| 程序文件 | 运行要求 |
| --- | --- |
| `UnattendLocal_V0.7_x64_SelfContained.exe` | 集成 .NET 运行时，无需单独安装；通常选择此版。 |
| `UnattendLocal_V0.7_x64_FrameworkDependent.exe` | 文件较小，需安装 .NET 10 Desktop Runtime x64。 |

两版功能相同。首次安装测试建议使用虚拟机。

1. 选择推荐场景或自定义配置，填写账户、镜像等必要信息。
2. 按需要选择系统、外观及应用移除设置。
3. 点击“检查与预览”，核对摘要及待处理项，再导出实际 XML。
4. 将 `autounattend.xml` 放在目标安装介质根目录；遮蔽后的预览不能代替实际 XML。

可通过“打开方案”恢复 JSON，也可通过“导入应答 XML”继续编辑已有应答配置。方案省略凭据后，重新打开可能需要补填密码 / 密钥；导入原 XML 未保存时需要重新导入。

## 📖 使用说明

当前使用普通 Release 发布，部分非核心上游设置将逐步补齐。当前使用固定生成核心及选择性同步，未宣称与上游最新功能完全对齐。

启用讲述人后不会自动关闭朗读；定制 PE 可能缺少语音或音频支持。允许卸载 Edge 只尝试调整系统策略文件，不实际卸载、不移除 WebView2，可能因系统版本、权限或更新而不生效。关闭 C 盘系统还原可能删除已有还原点，不能只当作暂停创建还原点。关闭 UAC 会降低权限提升保护；阻止 WPBT 不修改固件、不卸载已有软件，可能影响厂商通过该机制提供的程序。虚拟机工具需挂载官方工具介质并与目标 Windows / 架构匹配；未找到时不安装。VirtIO Guest Tools 不替代 Windows PE 阶段驱动，导入 XML 中已有的 PE 驱动脚本按原文保留。命令行审计会将参数明文写入安全日志，可能含凭据；仅启用审计时不会强制关闭已有命令行记录策略。更新重启策略受 Windows 版本、组织策略、截止期限或用户安排的重启影响，不保证阻止全部重启；启用后需自行安排重启完成更新。NTFS 最后访问时间设置可能影响依赖此时间的归档、备份或清理规则。桌面图片脚本必须返回图片原始字节，安装时需能访问图片；锁屏效果受版本和策略限制。应用移除可能影响相关功能，取消勾选不恢复已安装系统的应用。远程桌面需要目标版本支持，RemoteSigned 的有效策略可能被更高优先级策略覆盖。

导出的 XML、包含凭据的 JSON 和自定义脚本可能含敏感内容。分享前自行移除密码、密钥和令牌；预览不会自动识别任意脚本中的敏感信息。磁盘清空 / 格式化可能删除数据，请核对目标磁盘并完成备份。

## 🛠️ 从源码生成

使用 Visual Studio 2026 与 .NET 10 SDK，安装“.NET 桌面开发”，打开仓库根目录的 `UnattendLocal.sln`。四个项目为 Desktop、Core、vendor/UnattendGenerator、Tests，默认将 Desktop 设为启动项目。

在源码根目录执行：

```powershell
dotnet restore UnattendLocal.sln
dotnet build UnattendLocal.sln -c Release -m:1 -nr:false
dotnet run --project Tests/Tests.csproj -c Release --no-build
dotnet publish Desktop/Desktop.csproj -c Release -m:1 -nr:false -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -o publish/win-x64
```



## 🙏 引用与鸣谢

- **Christoph Schneegans**：[上游生成器源码](https://github.com/cschneegans/unattend-generator) · [原作者网站](https://schneegans.de/windows/unattend-generator/) · [使用说明](https://schneegans.de/windows/unattend-generator/usage/) · [脚本示例](https://schneegans.de/windows/unattend-generator/samples/)。本项目以其成熟生成核心为基础进行中文界面与本地适配。
- **James Newton-King 与 Newtonsoft.Json 贡献者**：[Newtonsoft.Json](https://github.com/JamesNK/Newtonsoft.Json)，用于上游生成核心的数据处理。
- **Microsoft / .NET 与 WPF 贡献者**：[.NET](https://github.com/dotnet/runtime) · [WPF](https://github.com/dotnet/wpf)，提供桌面与运行环境。
- **ChatGPT / Codex（OpenAI）**：[ChatGPT](https://chatgpt.com/) · [Codex](https://openai.com/codex/)。在需求梳理、代码实现、问题排查、中文说明与文档整理过程中提供辅助支持。

保留上游版权及 MIT 许可证，见 [vendor/UnattendGenerator/LICENSE.txt](vendor/UnattendGenerator/LICENSE.txt)。第三方记录与固定基线说明随完整源码包提供。本地工具不是原作者的官方桌面发行版。

## 💬 反馈

欢迎提交 Issues，说明工具版本、Windows 环境、复现步骤、预期与实际结果。安装问题请补充目标镜像版本、版本类型、架构与出现阶段。日志或配置只提供脱敏后的最小复现内容。
