# PE 部署验证 / Deployment validation

版本重点：PE 目标磁盘选择、分区前安全检查与启动兼容性。

## 当前状态

六项真实虚拟机安装验收：**0/6 已完成，全部待验证**。自动生成、JSON/XML 往返和参数边界检查不代表已经成功安装 Windows。

`verify.ps1` 运行自动检查。新增 `Tests/PeSafety.Runtime.Tests.ps1` 在 Windows 存在 cscript 时运行实际 VBScript，但用模拟磁盘、启动模式及安装介质，不枚举或修改真实磁盘，不执行 DiskPart。在非 Windows 环境明确报告 NOT RUN，不将它记作实机通过。

各版本的自动检查执行结果及环境限制见 [项目进度](../PROJECT_PROGRESS.md)。

## 最小实机验收

只使用带快照的临时虚拟机和可丢弃虚拟磁盘。Windows 安装 ISO 与应答文件 ISO 分开挂载，首次开启分区前及重启前暂停。镜像索引以实际介质为准，不在宿主机或有重要数据的磁盘上验证。

| 场景 | 配置示例与预期 | 状态 |
|---|---|---|
| 单磁盘 UEFI/GPT | x64 Windows 11、UEFI、80 GiB 目标盘；完成分区、释放镜像、引导、重启及 OOBE | 待实机验证 |
| 单磁盘 BIOS/MBR | 兼容的 x64 Windows 10、BIOS、80 GiB 目标盘；完成安装，GPT 专用 bcdedit 不执行 | 待实机验证 |
| 多盘唯一匹配 | 80 GiB 与 20 GiB 两盘，范围 64–128 GiB；只选择 80 GiB 盘，另一盘保持完整 | 待实机验证 |
| 多盘同时匹配 | 两块 80 GiB 盘，同样容量范围；在分区前停止，两盘数据保持完整 | 待实机验证 |
| 无匹配磁盘 | 20 GiB 盘，范围 64–128 GiB；在分区前停止，不调用破坏性 DiskPart | 待实机验证 |
| 交互或脚本异常 | 不存在的编号、额外字符、脚本多行输出或非零退出码；均拒绝工具生成的分区调用 | 待实机验证 |

为需要保留的虚拟盘预先放置校验文件，停止或部署后核对分区表、文件及 SHA256。检查目标磁盘实际编号，不假定磁盘 0 正确。每个成功场景还应检查实际 EFI/活动系统分区、恢复分区和 Windows 启动结果。

补充安全检查：固定布局与启动方式不匹配；磁盘容量恰好处于边界或差一个字节；安装源位于候选目标；WMI 属性不可读；暂停期间更换或新增匹配盘。最后一种仅在可丢弃、带快照的环境中进行，预期重新检查失败并停止。

记录目标系统/PE 版本、固件、虚拟磁盘布局、选择条件、实际选中编号、停止位置、返回结果及分区前后校验。失败时保留 PE 中的 `X:\pe.cmd`、`X:\target.vbs`、`X:\target.out`、`X:\UL-verify.out` 和 DISM 输出，分享前脱敏；X: 为临时盘，重启后通常不保留。

## English

Actual VM installation acceptance is **0/6 complete; all six remain pending**. Automated generation and persistence checks do not prove installation success. The Windows-only runtime test executes the production selector with mocked disk/firmware I/O, without DiskPart or real disk enumeration. Non-Windows runs explicitly report NOT RUN.

Use disposable snapshot VMs only. Cover UEFI/GPT Windows 11 success, compatible BIOS/MBR Windows 10 success, a unique match among multiple disks, ambiguous matches, no match, and invalid interactive/custom-script output. Verify that nontarget disks, partitions and checksum files remain intact. Check image application, boot files, restart, recovery and OOBE on successful installations. Also test firmware conflicts, exact capacity boundaries, installation-source exclusion, unreadable WMI properties and target changes during a pause. Record environment, selected disk, stop location and before/after integrity checks; preserve and redact PE logs before restarting.
