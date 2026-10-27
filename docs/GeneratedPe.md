# PE 脚本安装 / PE script installation

[简体中文](#简体中文) | [English](#english)

## 简体中文

### 用途与范围

在“镜像与分区”页，启用“使用 PE 脚本安装”。默认关闭，旧方案继续使用常规 Windows Setup 路径。

该模式通过生成核心提供安装脚本，运行于目标 Windows PE，依次定位安装镜像、选择目标磁盘、分区、释放 Windows 镜像、写入启动文件、应用安装配置并重启，随后继续 Windows 安装。配置工具不在当前电脑执行这些命令，也不修改安装 ISO 或制作启动介质。

首阶段仅支持 x64 Windows 10/11 安装环境、UEFI/GPT、清空一个明确指定的目标磁盘、按镜像索引选择 x64 镜像，并创建独立恢复分区。“单个目标磁盘”不表示自动识别系统盘，也不保证磁盘 0 是内置硬盘；须核对实际设备。容量与接口筛选、交互选盘、MBR、自定义完整 PE 脚本及其他架构不在当前编辑范围。

### 配置步骤

1. 选择自定义配置。在“镜像与分区”中选择“按镜像索引”，填写实际索引。可对安装介质运行只读命令 `dism.exe /Get-ImageInfo /ImageFile:"D:\sources\install.wim"` 核对版别；盘符、文件类型和索引按介质实际情况填写。
2. 选择“清空指定磁盘后自动分区”，明确填写目标磁盘编号，选择 UEFI/GPT，核对系统与恢复分区大小。
3. 启用 PE 脚本安装。安装密钥选项须保留默认的“安装时输入”状态：此模式不使用 Windows Setup 的安装密钥流程，安装版别由镜像索引决定；如有合法的独立激活密钥，仍可另行配置。
4. 按需启用“执行自动分区前暂停”和“PE 阶段完成后、重新启动前暂停”。核对清盘风险后勾选确认，再导出应答 XML 或应答文件 ISO。
5. 按应答文件的正常使用方式准备安装介质。常用方式是将 `autounattend.xml` 放在可访问介质根目录；应答文件 ISO 可作为第二张虚拟光盘，与 Windows 安装 ISO 一起挂载。工具生成的应答文件 ISO 本身不能启动。

PE 执行前会核对 x64 环境与 UEFI 启动方式，并以英文 DISM 输出读取所选镜像索引和 x64 架构；失败时停止，不进入分区。目标磁盘查询无匹配时也会停止。这些检查不替代对目标设备、备份、版别和磁盘容量的核对，也不能预先验证整个镜像文件或所有后续安装条件。释放镜像阶段仍保留 `/CheckIntegrity /Verify` 镜像完整性检查参数。

### 与其他设置的关系

- 安装驱动目录、安装阶段动态更新、PE 日志目录、硬件检查绕过、Compact OS 和 windowsPE 额外 XML 暂不能与此模式混用；导出时明确报错。
- PE 存储驱动可在介质准备 `$WinPEDriver$` 目录。PE 需要支持安装脚本使用的 CMD、DISM、DiskPart、WMI 与 VBScript 流程；精简 PE 可能缺少所需组件。
- 其他阶段的脚本仍沿用 System、DefaultUser、FirstLogon、UserOnce，不新增“封装前／中／后”分类。
- 磁盘编号、布局、分区大小、镜像索引、安装模式或暂停设置变更后需重新确认危险操作。保存或导入不会保留确认状态。

### 保存与 XML 编辑

JSON 方案保存 PE 模式与参数。旧 JSON 缺少新字段时保持常规安装方式，不自动切换。已导入的常规 XML 不直接切换为 PE 模式；此时请使用新方案或 JSON 方案。

本工具导出的原始 XML 在完整恢复记录有效、PE 流程与编辑基准一致时可恢复参数，支持修改镜像索引及切换回常规安装。修改过的、缺少有效恢复记录的或不能识别的 PE 脚本按原文保留，不通过此选项覆盖。带有原始 XML 的导入方案仍需核对原文操作范围并重新进行导入风险确认。

### 初次验证

先在带快照的临时虚拟机中使用一块可丢弃虚拟磁盘和 x64 Windows 安装介质，启用 UEFI。首次打开两个暂停选项，核对分区前提示和目标磁盘，再确认镜像释放、引导创建、重启及进入 OOBE 的完整过程。随后在同一方案上核对 JSON 保存、XML 导入与再导出。程序的自动生成与模拟检查不能代替目标环境的完整安装验证。

## English

### Purpose and scope

Enable **Install using a PE script** on Image and partitions. This is off by default; existing plans retain the ordinary Windows Setup path.

The generation core creates a script for the target Windows PE. It locates installation media, selects the target disk, partitions it, applies Windows, creates boot files, applies installation configuration and restarts into the remaining setup. The configuration tool does not run these commands on the current computer, modify installation ISOs or create bootable media.

The initial editor supports x64 Windows 10/11 setup environments, UEFI/GPT, wiping one specified target disk, selecting an x64 image by index and creating a separate recovery partition. It does not identify the system disk automatically. Disk 0 is not guaranteed to be an internal drive. Capacity/interface filters, interactive disk selection, MBR, custom complete PE scripts and other architectures are outside the initial editor scope.

### Configuration

1. Use custom configuration, select an image by index and enter its actual index. Inspect the media with a read-only command such as `dism.exe /Get-ImageInfo /ImageFile:"D:\sources\install.wim"`; adjust the drive, format and index to the actual media.
2. Select automatic wiping and partitioning, enter the target disk number, select UEFI/GPT, and review the system and recovery partition sizes.
3. Enable PE script installation. Keep the installation-key option at its interactive default: this mode does not use the Windows Setup installation-key path. The image index determines the edition. A separate, legitimately licensed activation key remains supported.
4. Optionally pause before partitioning and before the final PE restart. Review the disk-erasure risk, confirm it, then export XML or an answer-file ISO.
5. Prepare the answer file normally. For example, place `autounattend.xml` at the root of accessible media, or attach an answer-file ISO as a second virtual CD alongside the Windows installation ISO. The answer-file ISO is not bootable.

Before partitioning, PE checks x64 execution, UEFI boot and the selected image index/architecture using English DISM output. Failed checks stop the script before partitioning. A missing target disk also stops execution. These checks do not replace verification of the actual target, backups, edition or capacity, and do not validate the entire image or every later installation condition. The `/CheckIntegrity /Verify` switches remain enabled during image application.

### Compatibility and persistence

Setup driver directories, Dynamic Update, a PE log directory, hardware-check bypass, Compact OS and extra windowsPE XML are rejected in this mode. Prepare `$WinPEDriver$` on the media if needed. The PE must support the CMD, DISM, DiskPart, WMI and VBScript workflow; stripped-down PE images may lack required components.

Other scripts retain their existing System, DefaultUser, FirstLogon and UserOnce phases. Changes to the target, layout, partition sizes, image index, mode or pause settings clear destructive-operation consent. Saving or importing never restores consent.

JSON retains the new mode and parameters. Older JSON defaults to ordinary Setup. Imported ordinary XML cannot be switched directly to PE mode; use a new or JSON plan. An original app-exported XML with an intact restoration record and matching PE pipeline is editable, including changing the index and switching back to ordinary Setup. Modified or unrecognized scripts remain preserved and protected. Imported XML also requires its existing source-risk confirmation.

Use a disposable snapshot VM with x64 Windows media, UEFI and one disposable virtual disk for initial installation testing. Enable both pauses, verify the target before partitioning, and check image application, boot files, restart and OOBE. Then verify JSON and XML roundtrips. Generation and simulated checks do not establish successful installation on the target environment.

## 官方来源 / Official references

- [Upstream PE settings](https://schneegans.de/windows/unattend-generator/)
- [Pinned upstream Disk.cs](https://github.com/cschneegans/unattend-generator/blob/97c1141d51a9cdae6831107cb3311c2f9eb8121b/modifier/Disk.cs)
- [DISM image management](https://learn.microsoft.com/en-us/windows-hardware/manufacture/desktop/dism-image-management-command-line-options-s14)
- [DISM global options, including /English](https://learn.microsoft.com/en-us/windows-hardware/manufacture/desktop/dism-global-options-for-command-line-syntax)
