# PE 脚本安装 / PE script installation

## 简体中文

在“镜像与分区”页启用“使用 PE 脚本安装”。默认关闭；已有方案保持原有安装方式，新字段缺省为固定磁盘编号。本程序只生成应答文件，不扫描、分区或格式化当前电脑。

已知旧版 PE XML 的完整生成流程与有效恢复记录一致时，可恢复选盘、容量及分区设置的编辑；再次导出使用当前修复后的脚本。恢复编辑不会授予清盘确认。未识别或被外部修改的流程继续保留原文并限制覆盖。

### 使用范围

支持 x64 PE 和 x64 Windows 镜像，按镜像索引安装，清空一个已确认的目标磁盘，并创建系统、Windows 和独立恢复分区。可指定 UEFI/GPT、BIOS/MBR，或跟随实际启动方式。BIOS/MBR 主要用于兼容的 Windows 10 环境，不代表绕过 Windows 11 的平台要求。

常规 Setup 安装路径保持不变；以下动态选盘和安全检查仅作用于 PE 脚本安装。ARM64、x86、自定义完整 PE 流程及多目标磁盘安装不在本版范围内。

现有“自定义 C 盘分页文件”设置可用于 PE 路径，容量规则与常规安装一致；作用范围及保留条目的区别见[部署设置](DeploymentTuning.md)。

### 配置步骤

1. 使用自定义配置，选择“按镜像索引”并填写介质中实际存在的索引。
2. 选择“清空指定磁盘后自动分区”，启用 PE 脚本安装，选择目标磁盘方式和布局，核对系统与恢复分区大小。自动布局留空系统分区大小时使用 500 MiB，适用于两种布局。
3. 固定编号使用下方目标编号；条件匹配使用独立的可选匹配编号，留空不会自动套用固定编号。按需填写容量范围和勾选安全条件。所有选盘方式（包括自定义 VBScript 和安装时交互选择）的最小、最大容量均可独立留空；留空表示不限制对应方向，填写后仍对最终选中的磁盘进行检查。
4. PE 模式由实际镜像索引决定安装版本，不通过 SKU 或安装密钥选择版本。“版本与密钥方式”需选择“在 Windows 安装程序中手动输入”，这是当前 PE 路径的兼容选项，不会因此增加 PE 脚本的密钥输入步骤。原有选择与草稿保留，不自动修改。独立激活密钥仍可配置，但须与目标版本匹配，不用于选择镜像索引。
5. 首次验证建议启用分区前暂停和重启前暂停。核对清盘风险并确认后，导出 XML 或应答文件 ISO。

`autounattend.xml` 可放在目标 PE 可访问介质根目录；应答文件 ISO 可作为第二张虚拟光盘挂载，它本身不能启动。

### 四种选盘方式

| 方式 | 行为 |
|---|---|
| 固定编号 | 必须填写非负编号，按目标 PE 的编号核对，不假定磁盘 0 是内置硬盘 |
| 条件唯一匹配 | 在所有磁盘中筛选，必须恰好一个匹配；零个或多个匹配均停止，不选第一个 |
| 安装时交互 | 在 PE 控制台列出磁盘，输入一个非负整数编号；空输入、额外字符或不存在的编号停止 |
| 自定义 VBScript | 在目标 PE 执行可信脚本，只接收一个非负整数编号；退出码非零、错误输出、额外文字或超过 60 秒均停止 |

容量范围按实际字节比较：1 GiB = 1073741824 字节，边界包含在内，不将磁盘容量四舍五入。可留空，输入范围为 1–1048576 GiB。容量和“无分区”“IDE/SCSI 接口”“固定硬盘介质”条件适用于所有方式；可选匹配编号只用于条件匹配。

接口和介质条件采用 WMI 原始分类，不保证按接口名称排除所有 USB 桥接或虚拟磁盘。无法获取必需属性、映射分区数与磁盘报告不一致，或已映射卷盘符无法识别时停止。最低容量检查还为系统、恢复分区及 1 GiB 基本空间保留容量；它不估算镜像展开和安装后的全部空间需求，应自行设置足够的最小容量。

### 分区前安全检查

分区前从 DISM 实际输出读取 Edition ID（SKU），并确认报告索引与所选索引一致；输出写入 `X:\UL-imagecheck.out`。版本标识仅用于展示，缺失、重复或格式异常时提示无法识别，但不据此阻止安装，不根据镜像名称或固定索引猜测，也不尝试解析手动密钥所属 SKU。

镜像检查通过现有 cscript/VBScript 解析 DISM 英文输出，不依赖 findstr.exe。DISM 读取失败、架构字段缺失或重复、未知架构均停止；明确非 x64 时单独提示不兼容。日志保存在 `X:\UL-imageinfo.txt` 与 `X:\UL-imagecheck.out`。

PE 键盘命令仅接受普通键盘布局标识，输入法 GUID 配置保留当前 PE 布局；普通布局设置失败时给出警告，不修改目标 Windows 的输入配置。

先检查 x64 PE、镜像索引及镜像架构，识别 BIOS/UEFI。固定布局与启动方式不一致时停止；自动布局将 BIOS 对应到 MBR、UEFI 对应到 GPT。涉及 `{fwbootmgr}` 的 `bcdedit` 仅在 GPT 路径执行。

选盘后记录编号、设备 PNP 标识、容量和启动方式。每次调用破坏性 DiskPart 脚本前重新枚举并检查，复核位于可选暂停之后。目标变化、容量不符、条件改变、零/多匹配、身份无法确认或查询失败时停止，不调用该 DiskPart 脚本。包含已识别安装镜像、应答文件或 PE 系统盘的目标磁盘会被排除。

检查不是备份，也不能消除最后一次检查与实际分区之间设备再次变化的风险。不要在部署中插拔或重映射磁盘；安装介质、挂载文件与实际物理磁盘的关系仍须人工核对。镜像释放保留 `/CheckIntegrity /Verify`，不保证所有后续安装条件均满足。

### 自定义脚本的信任边界

只使用可信 VBScript。脚本本身可运行其他命令并操作磁盘，既不是安全沙盒，也不受后续清盘检查约束。该门禁保护的是工具生成的分区调用，不能使恶意脚本变安全。

脚本向标准输出写一个编号，标准错误为空，以退出码 0 结束，例如 `WScript.Echo 0`。允许末尾一个换行，不允许多个编号、额外说明、正负号、小数或命令字符。最多 65536 字符、1024 行，每行最多 400 字符；导出时按短片段写入，不改变脚本正文。不在配置电脑执行或预先验证脚本的实际选盘结果。

### 保存、恢复与兼容性

XML 恢复记录仅包含实际启用的自定义选盘脚本，不包含停用草稿。JSON 保存选盘方式、容量、安全条件和脚本；旧 JSON 不自动启用 PE，也不改成自动匹配。修改相关参数会取消清盘确认，保存或导入不会恢复确认状态。

本程序导出的原始 XML，只有有效恢复记录及完整匹配的 PE 流程才可恢复编辑。修改过的、无法识别的流程保留原文。较早版本 PE XML 可能因流程基准变化而无法恢复编辑或安全导出，原始内容仍保留在导入记录中；此时用原 JSON 方案继续编辑。切回常规 Setup 时，需要重新填写常规路径所需的固定目标和布局，不能沿用动态选盘的假定。

安装驱动目录、动态更新、PE 日志目录、硬件检查绕过、Compact OS 和 windowsPE 额外 XML 仍不能混用；导出时明确报错。PE 驱动可按介质要求放入 `$WinPEDriver$`。目标 PE 必须包含 CMD、DISM、DiskPart、WMI、cscript 与 VBScript 所需组件。

初次部署先使用带快照的临时虚拟机。六项最小实机验收及当前状态见 [部署验证](PeDeploymentValidation.md)。

## English

Enable **Install using a PE script** on Image and partitions. It is off by default. Existing plans retain their installation path, and missing selector fields default to fixed numbering. This application generates answer files; it does not inspect or modify disks on the configuration computer.

### Scope and configuration

Supports x64 PE, x64 images selected by index, one explicitly confirmed target and separate system, Windows and recovery partitions. Choose UEFI/GPT, BIOS/MBR or follow firmware. BIOS/MBR primarily targets compatible Windows 10 installations and does not bypass Windows 11 platform requirements. The ordinary Setup path is unchanged; the dynamic selectors and gates described here apply only to PE script installation.

Use custom configuration, select an actual image index, choose automatic disk wiping, enable PE installation and review the selector, layout and partition sizes. Automatic layout defaults to a 500 MiB system partition when unspecified. The actual image index selects the edition; SKU and installation-key selection are not supported by this PE path. Keep the mode at Enter manually in Windows Setup, a compatibility setting that does not add a key prompt to the PE script. Existing inputs are preserved. A separate activation key must match the target edition and does not select the image index. Enable both pauses for initial testing, review erasure consent, then export XML or an answer-file ISO. The answer-file ISO is not bootable.

Fixed mode requires the target PE disk number. Matching mode uses its own optional index filter and must find exactly one disk; zero or multiple matches stop. Interactive mode lists disks and accepts one unsigned number. Custom mode executes a trusted VBScript that must return exactly one unsigned index, optionally followed by one newline, with exit code 0 and empty stderr within 60 seconds.

Capacity bounds are inclusive, compared directly in bytes using 1 GiB = 1073741824 bytes, without rounding. Either field may remain blank in every selection mode, including custom VBScript and interactive selection. Blank means no corresponding bound; accepted input is 1–1048576 GiB. Capacity, unpartitioned, IDE/SCSI-interface and fixed-media conditions apply to all modes. Interface/media names are WMI classifications, not a guarantee of excluding all USB bridges or virtual disks. Required properties and existing partition mappings must be readable. Mapping counts must agree with the disk report, and mapped volume identifiers must be valid drive letters. A basic space floor reserves the system/recovery partitions plus 1 GiB, not the full expanded-image and operating-system requirement; configure a suitable minimum yourself.

### Safety and trust boundaries

Before partitioning, read the actual Edition ID (SKU) from DISM and confirm the reported index matches the selected index. Report both in X:\UL-imagecheck.out. Edition identifiers are informational only; missing, duplicate or malformed values are reported as unavailable and do not block deployment. Do not infer the edition from the image name or a fixed index mapping, and do not infer an arbitrary key's SKU.

Image validation parses English DISM output using cscript/VBScript, without findstr.exe. Missing, duplicated or unknown architecture fields fail closed; known non-x64 architectures have a separate incompatibility error. Keep X:\UL-imageinfo.txt and X:\UL-imagecheck.out for diagnosis. IME GUID profiles retain the current PE keyboard layout; ordinary layout failures produce a warning without changing the target Windows input configuration.

Check PE architecture, image readability and image architecture before selection. Determine BIOS or UEFI; reject a conflicting fixed layout or map automatic layout to MBR/GPT. Firmware-boot-manager `bcdedit` commands remain GPT-only.

Record index, PNP device identity, capacity and firmware, then re-enumerate and check immediately before the destructive DiskPart invocation, after any pause. Changed identities, invalid capacities, changed conditions, zero/multiple matches and query failures stop that invocation. Disks mapped to the identified image, answer-file or PE system volumes are excluded. Keep devices and mappings unchanged during deployment: checks are not backups and cannot eliminate a device change after the final check. Verify mounted installation media and physical-disk relationships manually. Image application retains `/CheckIntegrity /Verify`.

Custom scripts are executable trusted code, not sandboxed. They can run disk commands themselves, outside the protection of the generated partition gate. Review the entire script. Limits are 65,536 characters, 1,024 lines and 400 characters per line; short-fragment export preserves the original text. The application does not execute or validate the script’s actual selection on the configuration host.

### Persistence and validation

XML recovery metadata omits inactive selector scripts. JSON retains selector settings and drafts without restoring erasure consent. Older plans do not automatically switch installation paths or enable matching. Original app-exported XML is editable only with valid restoration metadata and a matching complete PE pipeline. Modified or unrecognized flows remain preserved. Older PE XML may no longer be editable or safely exportable when its pipeline no longer matches; its source remains retained in the import record. Use its original JSON plan. Switching to ordinary Setup requires a valid fixed target and supported ordinary layout.

Setup driver directories, Dynamic Update, PE log paths, hardware bypass, Compact OS and extra windowsPE XML remain incompatible. The target PE needs CMD, DISM, DiskPart, WMI, cscript and VBScript. Prepare PE drivers through `$WinPEDriver$` when appropriate. Test in a snapshot VM first; see [Deployment validation](PeDeploymentValidation.md).

## 来源 / Sources

- [Dynamic selection, 8a362f0](https://github.com/cschneegans/unattend-generator/commit/8a362f03a8b97bc7e583643818be33444636bb43)
- [Removal of old assertions, 28c2d0f](https://github.com/cschneegans/unattend-generator/commit/28c2d0f951f69caa3b0099b5eaab4dc02fee514b)
- [GPT-only bcdedit, 22b7922](https://github.com/cschneegans/unattend-generator/commit/22b79226057e6fc522236ee99f133eafee1cf748)
- [Reviewed current Disk.cs](https://github.com/cschneegans/unattend-generator/blob/82d4ffdd11ea3266f8f3b29d4fcd80f66bfdbaf6/modifier/Disk.cs)

Recognized older PE XML remains editable when its complete generated pipeline matches its valid recovery metadata. Re-export upgrades it to the current scripts without granting erase consent. Unknown or externally modified pipelines remain protected.
