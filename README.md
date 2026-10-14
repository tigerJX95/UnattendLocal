# UnattendLocal — Windows 无人值守配置工具

**简体中文** | [English](README_EN.md)

UnattendLocal 是基于 [Christoph Schneegans 的 UnattendGenerator](https://github.com/cschneegans/unattend-generator) 开发的中文 Windows 桌面配置工具，用于在本地编辑、检查和生成 Windows 10 / 11 的 `autounattend.xml`。

当前版本：**V0.1 pre45（预发布）**。本版汇总 pre41–pre44 的更新，延续原有生成核心和脚本执行阶段。工具用于准备安装配置，不会在当前电脑执行生成的安装脚本。

## 初衷

> 方便自己在本地离线配置，也方便下一次直接复用。

最初做 UnattendLocal，是想给自己准备一个可以在本地离线使用的 Windows 安装配置工具。把常用设置保存成方案，重新安装系统或换一台电脑时，直接载入，再根据实际环境调整，让重复的准备工作更省心。

项目以 Christoph Schneegans 的成熟生成核心为基础，将常用选项整理成中文桌面界面，并补上方案保存、配置摘要、检查预览和已有 XML 的导入编辑。希望从选择设置到生成文件，整个过程都更直观，也更容易核对。

现在把它公开，也希望方便有同样需求的人使用，并通过大家的实际测试逐步完善。开发会继续保持小步推进，优先做好常用功能、配置可靠性和安装验证。

这里的离线使用指在本地编辑、保存与生成配置；微软账户登录、联网下载图片或其他联网脚本，仍取决于目标安装环境的网络条件。

## 运行效果

| 配置方案 · 浅色主题 | 版本与密钥 · 深色主题 |
| :---: | :---: |
| ![配置方案：浅色主题](docs/images/overview.png) | ![版本与密钥：深色主题](docs/images/edition-and-key-dark.png) |

截图来自 V0.1 pre41 的实际运行界面，用于展示浅色与深色主题；当前 pre45 已调整导航顺序并新增部分设置，实际界面以当前版本为准。

## 主要功能

- 配置安装架构、显示语言、区域、键盘、时区、计算机名及安装版本 / 产品密钥。
- 配置镜像选择、已有分区或自动分区、CompactOS；涉及清空或格式化时显示风险说明并要求核对。
- 配置本地账户、密码、首次登录、密码到期与锁定策略，以及 Wi-Fi、离线安装和 OOBE。
- 配置资源管理器、开始菜单、任务栏、系统桌面图标、视觉效果、配色、纯色或图片背景及锁屏图片。
- 按项选择 60 个应用与系统能力移除项目，提供中文影响说明、关键词搜索和选择摘要，默认不勾选。
- 配置常用系统行为，包括快速启动、长路径、系统声音、设备自动加密、硬件关联应用及远程桌面 / PowerShell 执行策略等选项。
- 按原设计管理 System、DefaultUser、FirstLogon、UserOnce 自定义脚本，提供示例与执行范围说明。
- 保存和恢复 JSON 方案，提供配置摘要、推荐差异、检查、脱敏预览和 XML 导出。
- 导入已有 XML 后编辑已支持设置，保留未知内容；遇到无法可靠合并的修改时停止导出并说明原因。

## 本次集中更新

- 新增重启后自动登录、硬件关联应用及空 Windows.old 目录处理开关。
- 新增桌面图片和锁屏图片脚本加载模式，保留默认 / 纯色背景。
- 新增应用移除列表、搜索、逐项影响说明及保存 / 导入联动。
- 新增远程桌面和 Windows PowerShell RemoteSigned 设置，默认关闭。
- “应用移除”前移至 OOBE 之后；“自定义脚本”后移至检查与导出之前。
- 界面示例称呼统一为“示例”，保留上游兼容及配置恢复信息。

完整集中记录见 [CHANGELOG.md](CHANGELOG.md)。

## 使用

从仓库的 **Releases** 页面下载 Windows x64 程序包，完整解压后运行 `UnattendLocal.exe`。自包含程序无需单独安装 .NET 运行时。首次测试建议使用虚拟机。

1. 选择推荐场景或自定义配置，填写账户、镜像等必要信息。
2. 按需要选择系统、外观及应用移除设置。
3. 点击“检查与预览”，核对摘要及待处理项，再导出实际 XML。
4. 将 `autounattend.xml` 放在目标安装介质根目录；遮蔽后的预览不能代替实际 XML。

可通过“打开方案”恢复 JSON，也可通过“导入应答 XML”继续编辑已有应答配置。方案省略凭据后，重新打开可能需要补填密码 / 密钥；导入原 XML 未保存时需要重新导入。

## 使用范围

这是预发布版本，允许部分非核心上游设置留待后续实现。当前使用固定生成核心及选择性同步，未宣称与上游最新功能完全对齐。

XML 结构检查不能代替完整 Windows 安装验证。桌面图片脚本必须返回图片原始字节，安装时需能访问图片；锁屏效果受版本和策略限制。应用移除可能影响相关功能，取消勾选不恢复已安装系统的应用。远程桌面需要目标版本支持，RemoteSigned 的有效策略可能被更高优先级策略覆盖。

导出的 XML、包含凭据的 JSON 和自定义脚本可能含敏感内容。分享前自行移除密码、密钥和令牌；预览不会自动识别任意脚本中的敏感信息。磁盘清空 / 格式化可能删除数据，请核对目标磁盘并完成备份。

## 从源码生成

使用 Visual Studio 2026 与 .NET 10 SDK，安装“.NET 桌面开发”，打开仓库根目录的 `UnattendLocal.sln`。四个项目为 Desktop、Core、vendor/UnattendGenerator、Tests，默认将 Desktop 设为启动项目。

在源码根目录执行：

```powershell
dotnet restore UnattendLocal.sln
dotnet build UnattendLocal.sln -c Release -m:1 -nr:false
dotnet run --project Tests/Tests.csproj -c Release --no-build
dotnet publish Desktop/Desktop.csproj -c Release -m:1 -nr:false -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -o publish/win-x64
```

也可运行 `test.cmd` / `build.cmd`。发布输出为 `publish/win-x64/UnattendLocal.exe`，请将程序作为 Release 附件上传，编译缓存及二进制文件不提交到源码仓库。

本版 2841 项数据 / 生成 / 导入联动检查通过，四项目 Release 编译零警告、零错误。pre41–pre44 已收到用户桌面测试通过反馈；真实 Windows 10 / 11 安装、不同版本及部分系统效果仍需继续测试。当前交付 EXE 使用离线 Windows 引用与运行时及 GenerateBundle 打包，不等同于本环境完成了在线 `dotnet publish`。

## 引用与鸣谢

- **Christoph Schneegans**：[上游生成器源码](https://github.com/cschneegans/unattend-generator) · [原作者网站](https://schneegans.de/windows/unattend-generator/) · [使用说明](https://schneegans.de/windows/unattend-generator/usage/) · [脚本示例](https://schneegans.de/windows/unattend-generator/samples/)。本项目以其成熟生成核心为基础进行中文界面与本地适配。
- **James Newton-King 与 Newtonsoft.Json 贡献者**：[Newtonsoft.Json](https://github.com/JamesNK/Newtonsoft.Json)，用于上游生成核心的数据处理。
- **Microsoft / .NET 与 WPF 贡献者**：[.NET](https://github.com/dotnet/runtime) · [WPF](https://github.com/dotnet/wpf)，提供桌面与运行环境。

保留上游版权及 MIT 许可证，见 [vendor/UnattendGenerator/LICENSE.txt](vendor/UnattendGenerator/LICENSE.txt)。第三方记录见 [THIRD_PARTY_NOTICES.md](THIRD_PARTY_NOTICES.md)，固定基线与选择性同步记录见 [UPSTREAM.md](UPSTREAM.md)。本地工具不是原作者的官方桌面发行版。

## 反馈

欢迎提交 Issues，说明工具版本、Windows 环境、复现步骤、预期与实际结果。安装问题请补充目标镜像版本、版本类型、架构与出现阶段。日志或配置只提供脱敏后的最小复现内容。
