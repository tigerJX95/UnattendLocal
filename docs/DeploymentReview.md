# 配置集目录与 AppLocker 事件复核

## $OEM$ 文件目标

在“镜像与分区 → 安装介质配置集”点击“预览 $OEM$ 复制目标”，选择已准备的 `$OEM$` 文件夹。窗口列出相对路径和常规 Windows Setup 的目标路径，不读取文件内容，也不复制或运行文件。选择文件夹不会改变“使用 $OEM$ 配置集”开关。

| 相对路径示例 | 常规 Setup 目标 |
| --- | --- |
| `$1\Tools\install.cmd` | `%SystemDrive%\Tools\install.cmd` |
| `$$\Setup\Scripts\SetupComplete.cmd` | `%WINDIR%\Setup\Scripts\SetupComplete.cmd` |
| `D\Data\example.txt` | `D:\Data\example.txt` |
| `Other\example.txt` | 未映射，需要核对目录结构 |

预览不假定系统盘是 C 盘。它不检查目标是否已有同名文件，也不判断文件内容是否适合目标系统。不要通过配置集覆盖 Windows 系统文件。启用 `UseConfigurationSet` 后，应答文件所在介质根目录的其他内容也可能被复制，不能将预览中的 `$OEM$` 文件列表视为整个介质的完整复制清单。自定义 PE 脚本的复制行为由脚本决定。

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

**Image and partitions → Installation media configuration set → Preview $OEM$ copy destinations** reads directory metadata and maps `$1` to `%SystemDrive%`, `$$` to `%WINDIR%`, and single-letter directories to their drive roots. Unknown layouts remain unmapped. It does not read payloads, copy files, enable a configuration set, or inspect existing destination files. Configuration sets can also copy other contents at the answer-file media root; this preview is not an inventory of that entire copy operation. Custom PE scripts control their own copy behavior.

The scan is limited to 2000 entries and 32 subdirectory levels, with 500 file rows. Links, inaccessible directories, and deeper directories are skipped. Partial results are labeled. Close the window to cancel.

**OOBE → AppLocker → Review AppLocker events** accepts XML saved from Event Viewer, either one Windows `Event` or an `Events` collection. EVTX and PowerShell CLIXML are not supported. Only the `Microsoft-Windows-AppLocker` provider is counted. Audit findings (8003/8006/8021/8024), actual blocks (8004/8007/8022/8025), and policy/environment errors (8000/8008/8027) are separate. Allowed events (8002/8005/8020/8023) and other IDs have separate counts; Config CI IDs are not classified as AppLocker blocks.

All imported events contribute to counts; only the first 500 findings, blocks, or errors are listed. No findings does not establish that the policy is active. Input is limited to 4 MiB and 20000 events, with XML complexity limits and no DTD or external entities. Review does not apply policy or change the current plan. Check paths for personal information before sharing results.

## 参考 / References

- [Microsoft: Distribution Shares and Configuration Sets Overview](https://learn.microsoft.com/en-us/windows-hardware/customize/desktop/wsim/distribution-shares-and-configuration-sets-overview)
- [Microsoft: UseConfigurationSet](https://learn.microsoft.com/en-us/windows-hardware/customize/desktop/unattend/microsoft-windows-setup-useconfigurationset)
- [Microsoft: Using Event Viewer with AppLocker](https://learn.microsoft.com/en-us/windows/security/application-security/application-control/app-control-for-business/applocker/using-event-viewer-with-applocker)
