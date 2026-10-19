# Windows PE 日志目录 / Windows PE log directory

在“镜像与分区”页勾选“指定 PE 日志目录”，生成 `Microsoft-Windows-Setup/LogPath`，仅作用于 `windowsPE` 阶段。默认不勾选，不覆盖安装程序默认位置。

这不是本程序运行日志，也不会改变安装后系统 `%WINDIR%\Panther` 或 `$Windows.~BT\Sources\Panther` 等日志位置。它不收集全部安装日志、不复制、上传或清理日志。

目录必须是目标电脑固定磁盘上的本地绝对路径，最长 259 字符，例如 `D:\SetupLogs`。不支持 UNC、相对路径、环境变量、设备路径、通配符或单个日志文件；本工具拒绝常见 PE 内存盘 `X:`。目录名也不能使用保留设备名、相对层级、重复分隔符或末尾空格/点。

请提前准备可写目录，并按目标 PE 的实际盘符核对固定磁盘。不要使用随后被清空或格式化的磁盘/分区。当前电脑上存在该路径不能证明目标 PE 可用；本工具不读取宿主磁盘、判断磁盘类型或保证安装时可写。修改开关或目录会取消已有磁盘风险确认。

支持方案保存、摘要、帮助、搜索及完整原生 XML 的导入、修改和取消。重复、额外结构、非本版支持的路径或自定义 PE/DISM 安装流程只读保留。旧方案仅在编辑基准可靠时迁移；分享日志或配置前检查设备、路径和部署信息。

Use Image and partition settings to specify `Microsoft-Windows-Setup/LogPath` for `windowsPE` only. The option defaults to off. It does not relocate normal Windows Panther logs or provide logging for this application.

Use a fully qualified local directory on a fixed disk of the target computer, up to 259 characters, such as `D:\SetupLogs`. UNC paths, variables, device paths and individual log files are unsupported. This tool also rejects the common PE RAM-drive letter `X:` and invalid/reserved directory names. Prepare a writable directory, check PE drive-letter assignments and avoid disks/partitions that will be wiped or formatted. The tool cannot verify target disk type or access from the current computer. Changes reset existing disk-risk consent.

Saved plans, summaries, help, search and recognized native XML editing are supported. Unknown structures and custom PE flows stay read-only. Migration requires a reliable editing baseline. Review logs and configuration for sensitive deployment information before sharing.

依据 / Reference:

- [Microsoft Learn：LogPath](https://learn.microsoft.com/en-us/windows-hardware/customize/desktop/unattend/microsoft-windows-setup-logpath)

此项是微软已有的部署设置；实际目录写入和日志范围需要使用匹配镜像及一次性虚拟机核对。

This is an existing Microsoft deployment setting. Verify actual directory writes and log scope with the target image in a test VM.
