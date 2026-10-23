# 实用脚本模板 / Practical script templates

在“自定义脚本”页选择模板，查看用途、执行阶段与使用条件，再点击“添加所选模板”。新条目默认停用并自动选中；核对下方完整脚本后，勾选“启用此脚本”。添加不会执行脚本或修改当前电脑。

模板是普通 PowerShell 脚本，可修改、排序、删除、另存文件以及随方案保存。已添加的脚本不会因模板选择变化而改变。启用后，每个阶段最多 15 条，总计最多 60 条；完全相同的模板内容和阶段不能重复添加。

## 离线安装 .NET Framework 3.5

执行阶段：FirstLogon。需要管理员首次登录，并在安装后仍可访问匹配目标 Windows 版本、语言及架构的安装介质。

脚本检查 NetFx3 是否已启用，然后查找可用驱动器中的 `sources\sxs` 和 NetFx3 CAB 包。依次尝试本地源，使用 `-LimitAccess` 阻止 Windows Update 回退，使用 `-NoRestart` 避免立即重启。所有源失败或不存在时记录错误；文件存在不代表版本匹配，请查看 DISM 日志排查。

Windows 内部版本 28000 及以上改用独立安装程序，本模板会在修改功能前停止。它不安装 .NET 10 Desktop Runtime，也不能替代 UnattendLocal 框架依赖版需要的运行时。

## 安装微软 Visual C++ v14 运行库

执行阶段：FirstLogon。需要管理员首次登录和网络。x86 系统安装 x86 包；AMD64 系统安装 x86 和 x64 包；ARM64 系统安装 x86 和 ARM64 包。目标架构在安装时检测，不取生成配置的电脑架构。

脚本通过微软官方固定下载入口获取安装包，验证 Authenticode 状态为 Valid 且签名证书组织为 Microsoft Corporation。下载或签名检查失败时不运行该安装包。它使用随机命名的临时目录，等待安装结束并在退出时清理；不绕过证书校验。

接受返回码 0（成功）、3010（之后需要重启）和 1638（已安装另一版本）；其他返回码记录错误，不主动重启。它不替代所有年代的 VC++ 运行库，不安装旧版 VC++ 2013 或更早版本。ARM64 上若另外需要 x64 仿真应用运行库，可自行调整脚本。

## 禁用所有驱动器的 AutoRun

执行阶段：System。在创建用户前写入全机 Explorer 策略的 `NoDriveAutoRun=0x03ffffff` 与 `NoDriveTypeAutoRun=0xff`，影响所有用户。

该设置控制驱动器的 AutoRun 自动执行命令，不替代恶意软件检测，也不保证隐藏所有 AutoPlay 提示。组织策略可能覆盖本设置。

## 执行与日志

FirstLogon 不会因为添加模板而自动启用管理员自动登录；请在账户设置中安排首次管理员登录。脚本仍经过原生成器的 System / FirstLogon 执行链；脚本报错会进入该链的日志，不保证停止其他安装命令。

编辑脚本后请自行核对内容。模板测试覆盖生成、保存与导入，以及模拟下载、签名、安装返回码和本地源失败；实际安装结果取决于目标 Windows、安装介质、网络和策略。

## English

Choose a template on the custom scripts page, review its purpose, stage and requirements, then add it. The new entry is selected and disabled until explicitly enabled. Adding a template does not execute it on the current computer. Entries remain ordinary editable PowerShell scripts supported by existing plan and XML workflows.

Offline NetFx3 runs at FirstLogon with administrator privileges and matching local installation media. It checks CAB availability, tries local sources, blocks Windows Update fallback and avoids restarting. Build 28000 and newer require a standalone installer and are rejected before feature modification.

VC++ v14 runs at FirstLogon with administrator privileges and network access. It selects packages using the target OS architecture, includes x86 on 64-bit systems, downloads through Microsoft's official URLs, and requires a valid Microsoft Authenticode signature. It waits for installers, handles success/restart-needed/already-installed results and cleans its private temporary directory. It does not install all historical VC++ runtimes.

AutoRun policy runs at System and disables AutoRun commands for every drive letter and drive type. It does not replace malware detection or suppress every AutoPlay prompt. Organization policies may override it.

FirstLogon templates do not enable automatic logon. Arrange an administrator's first logon in the account settings. Script failures are recorded through the existing generator pipeline and do not guarantee that subsequent setup commands stop.

## 参考资料 / References

- [原作者示例脚本 / Upstream samples](https://schneegans.de/windows/unattend-generator/samples/)
- [微软：Windows 11 安装 .NET Framework 3.5](https://learn.microsoft.com/dotnet/framework/install/dotnet-35-windows-11)
- [微软：VC++ v14 运行库下载](https://learn.microsoft.com/cpp/windows/latest-supported-vc-redist)
- [下载程序 / Download releases](https://github.com/tigerJX95/UnattendLocal/releases/latest)
