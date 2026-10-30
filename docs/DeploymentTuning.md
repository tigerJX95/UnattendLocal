# 部署设置 / Deployment settings

[简体中文](#简体中文) | [English](#english) · [下载程序 / Releases](https://github.com/tigerJX95/UnattendLocal/releases/latest)

## 简体中文

在 OOBE 页的系统设置区域配置。两项默认均不勾选，不改变目标系统已有行为；只生成目标系统安装脚本，不在当前电脑执行。

**禁止新建 NTFS 8.3 短文件名**：在 System 阶段设置系统级创建策略。只影响后续短文件名创建，不扫描或删除已有短文件名。例如，新建文件以后不再额外生成旧式短名称。依赖短路径的旧程序应保持此项关闭。设置生效可能需要重启。

**自定义 C 盘分页文件（高级）**：常规安装在 System 阶段关闭自动管理，并创建或更新 `C:\pagefile.sys` 的容量，保留其他盘符现有分页文件。初始值至少 16 MiB，最大值不小于初始值且不超过 1048576 MiB。这里的上限是输入校验范围，不代表磁盘或 Windows 一定能分配该容量。默认输入 2048 / 4096 MiB，例如可将初始值设为 4096、最大值设为 8192；应按实际磁盘空间、工作负载及故障转储需求选择。重启后检查实际配置。此项不提供禁用分页文件选项，也不依赖自动登录。

PE 脚本安装复用相同的初始与最大容量，在映像释放后直接配置目标系统的离线分页文件。它把分页文件列表设为指定的 C 盘文件，不保留自定义镜像中原有的其他分页文件条目；此路径不再生成重复的 System 分页文件脚本。该操作仅在目标 PE 部署时运行。

取消勾选表示本次生成不配置该项，不会在已安装的系统上撤销此前执行过的更改。JSON 方案保存全部取值；保留本程序恢复注释的原始导出 XML 支持恢复设置。第三方 XML 或去掉恢复注释的 XML 保留原脚本，不保证反推为勾选项；再次增加相关设置前应核对原文，避免相互覆盖。

## English

Find these options in the system settings area of the OOBE page. Both are off by default and leave existing target-system settings unchanged. Scripts run on the installation target, never on the authoring computer.

**Disable creation of new NTFS 8.3 short names** sets a system-wide policy during the System phase. It does not remove existing short names. Leave it off for legacy software requiring short paths. A reboot may be required.

For regular installation, **Custom C: page file** disables automatic management and creates or updates `C:\pagefile.sys` during the System phase, preserving page files on other drives. Initial size must be at least 16 MiB; maximum must be no smaller than initial and no larger than 1048576 MiB. The validation limit does not guarantee that Windows or the disk can allocate that size. The suggested input starts at 2048 / 4096 MiB; 4096 / 8192 MiB is another example, subject to workload, free space and crash-dump requirements. Reboot and inspect the resulting configuration. No automatic logon is required.

PE installation uses the same sizes through the native offline paging file configuration after image application, replacing the paging file list with the specified C: file. It does not preserve other paging entries from the image and omits the duplicate System paging script. This executes only on the deployment target.

Unchecking an option omits it from the new installation script; it does not undo a previous deployment. JSON plans preserve the values. Original XML exported by this app retains editable settings through its restoration comment. Third-party XML and XML without that comment preserve original scripts without guaranteed conversion into checkboxes; inspect existing scripts before adding overlapping settings.


## 最小 PE 验证 / Minimal PE validation

在带快照的可丢弃虚拟机中，使用原有 PE 配置并启用自定义分页文件，设置初始 2048 MiB、最大 4096 MiB。部署后重启，在目标 Windows 中只读核对：

```powershell
Get-CimInstance Win32_PageFileSetting | Select-Object Name, InitialSize, MaximumSize
```

预期 C 盘条目为 `C:\pagefile.sys`、2048、4096；实际可用性仍受磁盘空间和目标系统约束。此项仅用于验证新分页处理，不把此前的磁盘部署回归重新计为本轮实测。

For a disposable snapshot VM, enable custom paging in a PE plan with 2048 / 4096 MiB. After deployment and restart, run the read-only command above in the target Windows and verify its C: entry. Generation tests do not prove that the target system applied the setting.

依据 / Reference: [Win32_PageFileSetting](https://learn.microsoft.com/en-us/windows/win32/cimwin32prov/win32-pagefilesetting).

## V1.31 分页文件入口 / Paging file settings

分页文件仍在 OOBE 的“系统性能与存储”中编辑。“镜像与分区”的 PE 区域显示状态并提供“前往设置”，只定位和高亮，不改变配置；不勾选表示保留目标镜像的分页文件设置。

Edit the paging file under System performance and storage in OOBE. The PE area in Image and partition shows status and links to the same editor without changing values. Unchecked retains the target image paging file settings.
