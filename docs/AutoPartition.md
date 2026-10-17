# 自动分区设置 / Automatic partition options

“镜像与分区” → “清空指定磁盘后自动分区”。本轮在原有本地 Windows Setup / DiskPart 路径中接入原作者的系统分区大小、分区前暂停选项，不切换到完整的 GeneratePESettings 安装流程。

| 设置 | 默认与范围 | 作用 |
| --- | --- | --- |
| 系统分区大小 | 留空；GPT 默认 300 MB、可填 300–8192 MB；MBR 默认 500 MB、可填 100–8192 MB | GPT 的 EFI/FAT32 或 MBR 的活动系统/NTFS 分区，不是 Windows 主分区容量 |
| 执行自动分区前暂停 | 关闭 | 目标 Windows PE 显示目标磁盘编号，执行 DiskPart 前 pause，按任意键继续 |

8192 MB 为本工具的输入上限，不是 Windows 最大分区限制。GPT 下限采用 300 MB，工具不枚举目标扇区大小，以覆盖官方当前列出的 4K 原生扇区最低要求。固件、实际磁盘、镜像及恢复工具仍须匹配部署环境。

两项不影响手动/已有分区/自定义 DiskPart 模式，也不会修改当前电脑。暂停前会写临时命令文件，尚未调用 DiskPart；暂停不自动判断磁盘是否正确。大小、暂停、布局或目标等变更会取消会话中的危险操作确认，保存与载入也不会恢复确认。

XML 仅完整识别本工具的命令及对应安装目标。额外命令、阶段/目标不一致、重复调用等只读保留。未知完整 PE 脚本保留原文，不能叠加本工具的自动清空模式。旧 JSON 缺少新字段时默认行为不变；旧导入方案若可可靠还原，仅迁移新字段与编辑基准，保留待处理修改，否则提示重新导入。

Both controls are under Image and partitions, in automatic disk-wiping/partitioning mode. This extends the existing local Windows Setup / DiskPart path with upstream system-size and pause options; it does not enable the complete GeneratePESettings workflow.

A blank system size retains GPT 300 MB / MBR 500 MB. This tool accepts GPT 300–8192 MB and MBR 100–8192 MB. The 8192 MB cap is an application input limit, not a Windows filesystem limit. The GPT minimum covers the current official minimum for 4K-native sectors without inspecting the target hardware.

The optional pause displays the target disk number in target Windows PE before DiskPart runs. Pressing a key continues. It does not validate the disk, replace backups or grant destructive-operation consent. Changes clear consent. Manual, existing-partition and custom DiskPart modes are unaffected; the current computer is not modified.

Only a complete recognized command and matching install target are editable. Altered, duplicate or inconsistent flows stay protected. Older JSON plans preserve defaults; reliable imported plans migrate only the newly supported fields/baseline while retaining pending edits.

V1.1 方案请使用 V1.1 或更高版本继续编辑；旧程序可能忽略新增字段。

Continue editing V1.1 plans in V1.1 or later; older applications may ignore the new fields.

## 来源 / Sources

- [原作者项目与 Disk.cs](https://github.com/cschneegans/unattend-generator/blob/27237f82e9a8498d9c5bab7d398dc34b7682a1e8/modifier/Disk.cs)
- [UEFI/GPT 分区要求](https://learn.microsoft.com/en-us/windows-hardware/manufacture/desktop/configure-uefigpt-based-hard-drive-partitions)
- [BIOS/MBR 分区要求](https://learn.microsoft.com/en-us/windows-hardware/manufacture/desktop/configure-biosmbr-based-hard-drive-partitions)
