# 应答文件 XML 与 ISO 导出

“检查与导出”页提供两种格式，并可选择应答文件名称。

## 文件名称

- 默认 `autounattend.xml`：放在安装介质根目录后，Windows 安装程序可能自动读取。
- 勾选使用 `notautounattend.xml`：需要在 Windows PE 中显式指定，例如 `setup.exe /Unattend:D:\notautounattend.xml`；请按实际安装介质与应答文件盘符调整。此名称选项随 JSON 方案保存，旧方案默认不勾选；它不改变 XML 的安装设置。
- 手动文件名只改变发现方式；显式调用或重命名后仍会执行配置中的操作。其他介质或已缓存的应答文件也可能被安装程序读取。

## ISO 使用方法

1. 配置并检查安装设置，选择需要的文件名，点击“导出应答文件 ISO”。
2. 在虚拟机中保留 Windows 安装 ISO，将应答文件 ISO 挂载为另一台虚拟 CD/DVD 光驱。
3. 从 Windows 安装介质启动。默认文件名可参与安装程序自动发现；手动文件名需显式指定路径。Windows PE 的光驱盘符可能与宿主不同。

ISO 根目录只包含一份完整 XML，保留长文件名。它没有 Windows 安装镜像、引导程序、驱动或 `$OEM$` 资源，不能独立启动；额外资源需自行准备。程序不导入或编辑 ISO。

XML 与 ISO 共用当前配置检查与危险磁盘操作确认，文件先完整写入同目录临时文件后替换目标。取消文件对话框不会写入文件。两种格式包含相同的完整 XML；预览的遮蔽仅用于预览，完整输出可能包含可还原凭据。

## 来源与组件

原作者网站的 Download settings 提供 `notautounattend.xml`，Submit form 提供 XML 与 ISO 下载。本地实现复用既有 XML 生成流程，ISO 封装使用 MIT 授权的 DiscUtils.Iso9660 0.16.13，含传递组件 DiscUtils.Core 与 DiscUtils.Streams；不更新固定 UnattendGenerator 核心。

- https://schneegans.de/windows/unattend-generator/
- https://www.nuget.org/packages/DiscUtils.Iso9660/0.16.13
- https://github.com/DiscUtils/DiscUtils
