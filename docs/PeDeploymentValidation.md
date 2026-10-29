# PE 部署验证 / Deployment validation

版本重点：PE 目标磁盘选择、分区前安全检查与启动兼容性。

## 当前状态

六项真实虚拟机安装验收：**5/6 用户反馈通过，第六项修复后待复测**。自动生成、JSON/XML 往返和参数边界检查不代表已经成功安装 Windows。

`verify.ps1` 运行自动检查。新增 `Tests/PeSafety.Runtime.Tests.ps1` 在 Windows 存在 cscript 时运行实际 VBScript，但用模拟磁盘、启动模式及安装介质，不枚举或修改真实磁盘，不执行 DiskPart。在非 Windows 环境明确报告 NOT RUN，不将它记作实机通过。

`Tests/PeCommandChain.Runtime.Tests.ps1` 在 Windows 上执行实际生成的 CMD 检查段，以无磁盘操作的宿主替身验证正值、负值返回码、空结果和旧编号残留。此检查不运行 DiskPart，也不替代虚拟机验收。

第六项复测时，除检查原系统和校验文件保持完整外，还必须确认选盘失败后没有进入 DiskPart。仅看到清盘失败或原系统仍可启动，不能证明提前停止有效。分别验证 `WScript.Echo 99`（确保不存在磁盘 99）、`WScript.Echo "0abc"`、两行输出 `0` 与 `1`、`WScript.Quit 1`、`Err.Raise 53`。另用有效编号完成一次选择，在分区前暂停期间删除 `X:\UL-target.snapshot`；继续后应由复核停止，不进入 DiskPart。

各版本的自动检查执行结果及环境限制见 [项目进度](../PROJECT_PROGRESS.md)。

## 最小实机验收

只使用带快照的临时虚拟机和可丢弃虚拟磁盘。Windows 安装 ISO 与应答文件 ISO 分开挂载，首次开启分区前及重启前暂停。镜像索引以实际介质为准，不在宿主机或有重要数据的磁盘上验证。

| 场景 | 配置示例与预期 | 状态 |
|---|---|---|
| 单磁盘 UEFI/GPT | x64 Windows 11、UEFI、80 GiB 目标盘；完成分区、释放镜像、引导、重启及 OOBE | 用户反馈通过 |
| 单磁盘 BIOS/MBR | 兼容的 x64 Windows 10、BIOS、80 GiB 目标盘；完成安装，GPT 专用 bcdedit 不执行 | 用户反馈通过 |
| 多盘唯一匹配 | 80 GiB 与 20 GiB 两盘，范围 64–128 GiB；只选择 80 GiB 盘，另一盘保持完整 | 用户反馈通过 |
| 多盘同时匹配 | 两块 80 GiB 盘，同样容量范围；在分区前停止，两盘数据保持完整 | 用户反馈通过 |
| 无匹配磁盘 | 20 GiB 盘，范围 64–128 GiB；在分区前停止，不调用破坏性 DiskPart | 用户反馈通过 |
| 交互或脚本异常 | 不存在的编号、额外字符、脚本多行输出、非零退出码（包括负值运行错误）或缺失目标记录；均须在进入 DiskPart 前停止 | 修复后待复测 |

为需要保留的虚拟盘预先放置校验文件，停止或部署后核对分区表、文件及 SHA256。检查目标磁盘实际编号，不假定磁盘 0 正确。每个成功场景还应检查实际 EFI/活动系统分区、恢复分区和 Windows 启动结果。

补充安全检查：固定布局与启动方式不匹配；磁盘容量恰好处于边界或差一个字节；安装源位于候选目标；WMI 属性不可读；暂停期间更换或新增匹配盘。最后一种仅在可丢弃、带快照的环境中进行，预期重新检查失败并停止。

记录目标系统/PE 版本、固件、虚拟磁盘布局、选择条件、实际选中编号、停止位置、返回结果及分区前后校验。失败时保留 PE 中的 `X:\pe.cmd`、`X:\target.vbs`、`X:\target.out`、`X:\UL-verify.out`、`X:\UL-imageinfo.txt`、`X:\UL-imagecheck.out` 和 DISM 输出，分享前脱敏；X: 为临时盘，重启后通常不保留。

## English

Actual VM installation acceptance is **5/6 reported passed by the user; the sixth requires retesting after the fix**. Automated generation and persistence checks do not prove installation success. The Windows-only runtime test executes the production selector with mocked disk/firmware I/O, without DiskPart or real disk enumeration. Non-Windows runs explicitly report NOT RUN.

Use disposable snapshot VMs only. Cover UEFI/GPT Windows 11 success, compatible BIOS/MBR Windows 10 success, a unique match among multiple disks, ambiguous matches, no match, and invalid interactive/custom-script output. Verify that nontarget disks, partitions and checksum files remain intact. Check image application, boot files, restart, recovery and OOBE on successful installations. Also test firmware conflicts, exact capacity boundaries, installation-source exclusion, unreadable WMI properties and target changes during a pause. Record environment, selected disk, stop location and before/after integrity checks; preserve and redact PE logs before restarting.

For the sixth case, an intact old OS alone is insufficient: confirm that no DiskPart invocation follows selection failure. Test a nonexistent index, malformed or multiline output, a positive exit code and an unhandled VBScript runtime error. Also delete the target snapshot during the pre-partition pause after a valid selection; the recheck must stop before DiskPart.
