# 配置集目录与 AppLocker 事件复核

## $OEM$ 文件目标

在“镜像与分区 → 安装介质配置集”点击“预览 $OEM$ 复制目标”，选择已准备的 `$OEM$` 文件夹。窗口列出相对路径和常规 Windows Setup 的目标路径，不读取文件内容，也不复制或运行文件。选择文件夹不会改变“使用 $OEM$ 配置集”开关。

| 相对路径示例 | 常规 Setup 目标 |
| --- | --- |
| `$1\Tools\install.cmd` | `%SystemDrive%\Tools\install.cmd` |
| `$$\Setup\Scripts\SetupComplete.cmd` | `%WINDIR%\Setup\Scripts\SetupComplete.cmd` |
| `D\Data\example.txt` | `D:\Data\example.txt` |
| `Other\example.txt` | 未映射，需要核对目录结构 |

窗口顶部显示当前方案的安装路径和配置集状态。常规 Windows Setup 通过 `windowsPE` 阶段的 `UseConfigurationSet` 使用配置集。启用本工具生成的 PE 安装脚本时，由脚本处理复制，仍须打开配置集开关。原生成器新版的自定义 `pe.cmd` 可独立处理 `$OEM$`，导入这类应答文件后，应核对最终 XML 和配套脚本。

预览不假定系统盘是 C 盘。它不检查目标是否已有同名文件，也不判断文件内容是否适合目标系统。不要通过配置集覆盖 Windows 系统文件。启用 `UseConfigurationSet` 后，应答文件所在介质根目录的其他内容也可能被复制，不能将预览中的 `$OEM$` 文件列表视为整个介质的完整复制清单。映射表以常规 Windows Setup 为准。

路径中含 Windows 保留设备名、末尾空格或句点的文件和目录标为未映射。

最多扫描 2000 个条目和 32 层子目录，列出 500 个文件。符号链接、目录联接、不可访问或过深的子目录会跳过。遇到限制或跳过内容时，窗口明确标示结果不完整。关闭窗口可取消后台复核。

## AppLocker 事件 XML

在目标 Windows 的事件查看器中，打开“应用程序和服务日志 → Microsoft → Windows → AppLocker”，按所需时间范围筛选，再将事件另存为 XML。也可在事件详情的 XML 视图中复制单个 `Event` 到 XML 文件。

在“OOBE → AppLocker”点击“复核 AppLocker 事件”，打开该 XML。支持单个 Windows `Event` 或 `Events` 集合，不支持 EVTX 和 PowerShell 的 CLIXML 序列化文件。只统计 `Microsoft-Windows-AppLocker` 提供程序；相同编号的其他提供程序事件会忽略。

| 结果 | 事件编号 |
| --- | --- |
| 已允许 | 8002、8005、8020、8023 |
| 审计命中：本次已允许，强制执行时会阻止 | 8003、8006、8021、8024 |
| 实际阻止 | 8004、8007、8022、8025 |
| 策略或环境错误 | 8000、8008、8027 |
| 其他事件 | 其余编号，包括 Config CI 相关事件 |

窗口分别统计这些结果，并列出前 500 条审计命中、阻止或错误事件的编号、时间和文件路径。计数覆盖已导入的全部事件。没有发现上述事件不能说明策略已生效；时间筛选、日志来源和日志保留范围都会影响结果。

事件文件上限为 4 MiB、20000 条事件，XML 另有深度和元素数量限制。DTD 和外部实体不被接受。文件路径可能包含个人信息，分享截图或复制结果前请自行检查。复核不会应用策略或修改当前方案。

## English

**Image and partitions → Installation media configuration set → Preview $OEM$ copy destinations** reads directory metadata and maps `$1` to `%SystemDrive%`, `$$` to `%WINDIR%`, and single-letter directories to their drive roots. Unknown layouts remain unmapped. It does not read payloads, copy files, enable a configuration set, or inspect existing destination files. Configuration sets can also copy other contents at the answer-file media root; this preview is not an inventory of that entire copy operation.

The window identifies the current plan's configuration-set state and installation path. Regular Windows Setup reads `UseConfigurationSet` in the `windowsPE` pass. An UnattendLocal-generated PE script handles copying when the plan enables it. New upstream custom `pe.cmd` scripts can process `$OEM$` independently. For imported plans, verify the final XML and accompanying scripts.

The scan is limited to 2000 entries and 32 subdirectory levels, with 500 file rows. Links, inaccessible directories, and deeper directories are skipped. Partial results are labeled. Close the window to cancel.

Paths with reserved Windows device names or segments ending in a space or period are marked as unmapped.

**OOBE → AppLocker → Review AppLocker events** accepts XML saved from Event Viewer, either one Windows `Event` or an `Events` collection. EVTX and PowerShell CLIXML are not supported. Only the `Microsoft-Windows-AppLocker` provider is counted. Audit findings (8003/8006/8021/8024), actual blocks (8004/8007/8022/8025), and policy/environment errors (8000/8008/8027) are separate. Allowed events (8002/8005/8020/8023) and other IDs have separate counts; Config CI IDs are not classified as AppLocker blocks.

All imported events contribute to counts; only the first 500 findings, blocks, or errors are listed. No findings does not establish that the policy is active. Input is limited to 4 MiB and 20000 events, with XML complexity limits and no DTD or external entities. Review does not apply policy or change the current plan. Check paths for personal information before sharing results.

## 导出阶段摘要 / Export phase summary

“检查与导出”读取最终应答文件中的 `windowsPE` 镜像选择配置，区分按索引、名称和描述选择。按索引时显示编号，按名称或描述时只显示方式。遇到多个或无法识别的条件时提示核对。请用实际安装介质确认目标镜像。

导入按描述选择或无法识别的选择规则后，程序保留原 XML，并提示该规则只读。若尝试在原规则之外指定索引或名称，导出会提示恢复“由安装程序决定”以保留原规则。

`oobeSystem` 摘要列出隐藏网络页和在线登录页的请求，实际显示哪些页面取决于目标 Windows 和安装环境。

**Checks and export** reads the final answer file's `windowsPE` image selection and distinguishes index, name, and description. It shows the index number and keeps custom names and descriptions out of the summary. Multiple or unrecognized selectors prompt a manual check against the installation media.

On import, description-based or unrecognized selectors are preserved as read-only XML. A new index or name conflicts with the preserved selector. Set image selection to **Let Setup decide** to keep the original rule when exporting.

The `oobeSystem` summary lists requests to hide the network and online sign-in pages. The pages shown depend on the target Windows version and installation environment.

## 参考 / References

- [Microsoft: Distribution Shares and Configuration Sets Overview](https://learn.microsoft.com/en-us/windows-hardware/customize/desktop/wsim/distribution-shares-and-configuration-sets-overview)
- [Microsoft: UseConfigurationSet](https://learn.microsoft.com/en-us/windows-hardware/customize/desktop/unattend/microsoft-windows-setup-useconfigurationset)
- [Upstream: configuration-set handling in Windows Setup and pe.cmd](https://github.com/cschneegans/unattend-generator/commit/8fdf1b99af95977ea38f4922662964dc6324b0df)
- [Microsoft: Using Event Viewer with AppLocker](https://learn.microsoft.com/en-us/windows/security/application-security/application-control/app-control-for-business/applocker/using-event-viewer-with-applocker)
- [Microsoft Learn · OSImage/InstallFrom/MetaData](https://learn.microsoft.com/windows-hardware/customize/desktop/unattend/microsoft-windows-setup-imageinstall-osimage-installfrom-metadata)
- [Microsoft Learn · Naming Files, Paths, and Namespaces](https://learn.microsoft.com/windows/win32/fileio/naming-a-file)
- [Microsoft Learn · HideWirelessSetupInOOBE](https://learn.microsoft.com/windows-hardware/customize/desktop/unattend/microsoft-windows-shell-setup-oobe-hidewirelesssetupinoobe)
- [Microsoft Learn · HideOnlineAccountScreens](https://learn.microsoft.com/windows-hardware/customize/desktop/unattend/microsoft-windows-shell-setup-oobe-hideonlineaccountscreens)
