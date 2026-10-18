# 安装阶段驱动目录

在“镜像与分区”页启用“从指定目录添加安装驱动”，每行填写一个目标 Windows PE 可访问的目录，例如：

```text
E:\Drivers\Storage
\\server\drivers\Network
```

示例不自动填入。默认关闭；支持最多 8 个不重复目录，单条最长 259 字符。8 个是本工具的范围限制。填写已解压驱动所在的文件夹，配齐 INF、SYS、CAT 等文件，不填写单个 INF、EXE 或压缩包。

本版支持盘符绝对目录和不带访问凭据的 UNC 目录。不支持相对路径、环境变量、通配符、设备路径及重复目录。路径按 Windows 的大小写规则去重，末尾反斜线不使同一个目录变成新目录。

设置使用 `Microsoft-Windows-PnpCustomizationsWinPE/DriverPaths`，由 Windows Setup 在 windowsPE 阶段处理。驱动文件不会嵌入应答 XML；需自行放到可访问的介质或共享目录。工具不下载、复制或在当前电脑安装驱动，也不修改 boot.wim。虚拟机工具安装、Wi-Fi 连接和静态 IP 仍分别配置。

请按目标 Windows 镜像、架构、硬件及签名要求准备驱动。PE 中的盘符可能与当前电脑不同；目录不能位于随后要清空的分区。UNC 路径要求 PE 已有网络驱动、网络连接和访问权限；不能依赖尚无法访问的网络来加载连接该网络所需的网卡驱动。修改开关或目录后，磁盘危险操作确认会清除，须重新核对目标。

保存方案会保留目录和开关。关闭后保留输入供下次使用，但不写入额外原生目录；应用推荐配置保留已有驱动选择。目录可能包含服务器名或个人路径，请自行保管方案。

XML 导入只编辑完整识别的本版原生结构：一个 windowsPE 组件、正确架构及组件属性、仅 DriverPaths/PathAndCredentials/Path，列表键从 1 顺序编号，且不含访问凭据。识别成功后可修改和取消；未修改时仍保留原文。含凭据、额外字段、非标准列表键、重复或其他结构的配置保留原文并只读。自定义 PE 流程不由此控件覆盖。只读不表示原配置未生效，界面默认值也不代表原文实际内容。

预览遮蔽该组件中 Credentials/Password 的内容；保存完整原 XML 或导出时仍保留原配置。省略凭据时不保存完整原 XML，下次需重新导入。旧方案仅在原文与缺失字段/编辑基准能可靠对应时补齐；冲突时保留待处理修改并要求重新导入。

XML 结构检查不等于目标镜像兼容验证。请在 Windows SIM 中用对应镜像核对组件，再在一次性虚拟机或测试设备中确认驱动加载、安装和首次启动；本工具不探测驱动是否匹配或目录是否可达。

参考：

- [Microsoft：安装期间添加驱动](https://learn.microsoft.com/en-us/windows-hardware/manufacture/desktop/add-device-drivers-to-windows-during-windows-setup)
- [DriverPaths](https://learn.microsoft.com/en-us/windows-hardware/customize/desktop/unattend/microsoft-windows-pnpcustomizationswinpe-driverpaths)
- [Path 长度与适用阶段](https://learn.microsoft.com/en-us/windows-hardware/customize/desktop/unattend/microsoft-windows-pnpcustomizationswinpe-driverpaths-pathandcredentials-path)
- [用目标镜像验证应答文件](https://learn.microsoft.com/en-us/windows-hardware/customize/desktop/wsim/validate-an-answer-file)

## English

Enable installation driver directories on the Image and partitions page. Enter one directory per line, up to eight unique directories and 259 characters per path. The eight-directory limit is specific to this tool. Use extracted INF driver packages with all companion files, matched to the target image, architecture, hardware and signature requirements.

The setting uses the native Windows Setup DriverPaths component in the windowsPE pass. Supported input is an absolute drive-letter directory or a UNC directory without credentials. Relative paths, variables, wildcards, device paths and individual driver/archive/installer files are not supported. The tool stores paths only; it does not package drivers, modify boot.wim or install drivers on the current computer.

Check drive letters in target PE and avoid directories on partitions that will be wiped. UNC access requires working PE networking and permissions. Changing driver settings clears disk-operation consent; verify the target again. Saved plans retain paths, and recommendations preserve the driver selection.

Complete recognized native XML can be edited. Credentials, additional fields, nonstandard keys, duplicate components or custom PE flows are preserved and kept read-only. The displayed defaults do not describe an unrecognized source. Preview masks native driver-access passwords; full XML retention/export preserves the original credentials. Older plans migrate only when source and baseline can be matched reliably, preserving pending edits when a reimport is required.

Validate against the target image in Windows SIM and test installation/boot in a disposable environment. XML structure checks do not prove driver compatibility or path availability.
