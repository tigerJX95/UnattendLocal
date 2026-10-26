# 实用脚本模板 / Practical script templates

在“自定义脚本”页选择模板，查看用途、执行阶段与使用条件，再点击“添加所选模板”。新条目默认停用并自动选中；核对下方完整脚本后，勾选“启用此脚本”。添加不会执行脚本或修改当前电脑。

模板是普通 PowerShell 脚本，可修改、排序、删除、另存文件以及随方案保存。已添加的脚本不会因模板选择变化而改变。启用后，每个阶段最多 15 条，总计最多 60 条；完全相同的模板内容和阶段不能重复添加。

## 通用应用安装与高级诊断 / Generic installation and advanced diagnostics

安装模板按安装方式分类。具体软件是使用示例，不单独增加一个模板。EXE、WinGet 两种入口添加后都默认停用；先配置并在隔离虚拟机验证，再启用。已有方案与 XML 中保存的脚本仍按原文恢复，不会自动替换为新版通用模板。

### 本地 EXE 静默安装（通用）

执行阶段为 System。修改脚本顶部三个值：

| 字段 | 内容 |
|---|---|
| `installerPath` | 目标 Windows 能访问的本地 EXE 完整路径，支持空格；不支持相对路径和 UNC 路径 |
| `expectedSha256` | 可信来源安装包的 64 位 SHA256 十六进制值 |
| `silentArguments` | 对应厂商提供的静默安装、禁止自动重启等参数 |

缺少配置或找不到文件时停止；只复制明确指定的 EXE，不扫描磁盘、不下载、不自动识别架构。适用于可独立运行的单文件 EXE；依赖同目录 CAB 或其他文件的安装包不属于此模板的支持范围，MSI 不使用此入口。

确认来源后，用 `Get-FileHash -LiteralPath 'D:\Install\Setup.exe' -Algorithm SHA256` 获取哈希值。SHA256 用于确认将运行的文件与准备时信任的文件一致，不能替代对下载来源的核对。

**7-Zip 使用示例**（文件名按实际下载版本修改）：

```powershell
$installerPath = 'D:\Install\7z2601-x64.exe'
$expectedSha256 = '这里替换成该可信安装包的完整64位SHA256'
$silentArguments = '/S'
```

示例哈希占位文字不能直接运行。实际安装介质盘符可能变化，务必填写目标系统执行阶段可访问的路径；由你选择匹配目标架构的安装包。其他 EXE 可能使用 `/quiet /norestart` 或 `/VERYSILENT /NORESTART` 等完全不同的参数，不能照搬 `/S`。

模板复制到独立临时目录并验证副本后才运行，等待退出并清理。接受返回码 0 和 3010，3010 仅提示稍后重启；其他退出码报错，需要结合厂商文档判断。模板不主动重启，但不能阻止第三方安装程序按自身逻辑重启，需核对参数和实际行为。安装进程本身没有超时限制。重复运行会再次调用安装程序，不保证任意软件的升级、降级或幂等行为。

### 高级诊断：防火墙丢弃日志

这是可选的连接故障排查功能，优先级低于常用部署设置。执行阶段为 System，添加后默认停用。适用于服务正在监听、但其他电脑无法连接，需要确认本机防火墙是否丢弃连接的场景；它不自动诊断或修复问题，也不能直接定位具体应用或规则。

配置本地 `PersistentStore` 的 Domain、Private、Public 日志，将 `LogBlocked` 设为 True，默认上限 `16384` KiB。可修改 `$logMaxSizeKilobytes`，有效范围为 1–32767 KiB。保留原有日志路径及成功连接日志设置，不启停防火墙，不修改规则或默认入站出站策略。执行后核验本地配置，实际生效值可能被组织策略覆盖。

Windows 在丢弃流量时记录日志，不保证启用后立即出现文件。用 `Get-NetFirewallProfile | Select-Object Name, LogBlocked, LogFileName, LogMaxSizeKilobytes` 查看实际配置和路径。在隔离虚拟机中产生一条被现有规则阻止的连接，再检查日志。取消模板不撤销已部署设置。

### English

**Local EXE silent installation (generic)** runs during System and requires an explicit local absolute `installerPath`, trusted `expectedSha256` and vendor-specific `silentArguments`. No scanning, downloading or architecture detection. Only standalone single-file EXE installers are supported, not MSI or installers requiring adjacent files. Paths refer to the target Windows system; select the correct architecture yourself. A private copy is verified before execution. The script waits, accepts exit codes 0/3010 and cleans up. Other codes fail; there is no installation timeout. It does not request a reboot but cannot prevent an installer from rebooting itself. Verify vendor no-restart options and behavior. A hash confirms identity with a trusted file, not publisher authenticity by itself.

7-Zip is an example: set the path to your actual official installer, fill its SHA256 and use `/S`. Other installers require their own documented arguments; do not assume `/S` is universal. Example filenames and hash placeholders are not ready-to-run configuration.

**Advanced diagnostics: firewall dropped-packet logging** is optional troubleshooting for blocked connections. Runs during System and configures local PersistentStore logging for Domain, Private and Public with a default 16384 KiB limit (1–32767 supported). Preserves existing paths, allowed-traffic logging, firewall state and filtering rules. Organization policy may override effective settings. Log entries are written when traffic is dropped; enabling logging does not fix connectivity or directly identify the application or rule. Disabling the template does not undo a deployed change.

Existing script bodies in plans and original XML remain unchanged by template reorganization.

References: [upstream samples](https://schneegans.de/windows/unattend-generator/samples/), [7-Zip silent installation](https://7-zip.org/faq.html), [Get-FileHash](https://learn.microsoft.com/en-us/powershell/module/microsoft.powershell.utility/get-filehash), [Set-NetFirewallProfile](https://learn.microsoft.com/en-us/powershell/module/netsecurity/set-netfirewallprofile).

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

## 允许局域网 Ping（IPv4 / IPv6）

执行阶段：System。允许同一子网的设备向本机发送 Ping 请求，用于安装后的基本连通性检查。

模板仅为专用和域网络添加两条入站放行规则：ICMPv4 类型 8、ICMPv6 类型 128，来源为 `LocalSubnet`；不关闭防火墙，不新增公用网络放行。现有规则可能另有放行或阻止行为，此模板不清理其他程序或系统规则。

规则使用 `UnattendLocal-ICMPv4-Echo` 与 `UnattendLocal-ICMPv6-Echo` 名称，归属组为 `UnattendLocal`。重复执行会更新该组的同名规则；如果同名规则属于其他组，会在写入前停止。写入过程中遇到系统错误会停止，但不保证撤销之前已成功写入的规则。

这不是跨网段 Ping 模板。若当前网络仍被 Windows 识别为公用网络，或域策略禁止本地规则合并，添加后也可能无法 Ping 通；模板不自动改变网络类别。测试可在可信局域网内将目标网络设为专用后，由同子网另一台电脑运行 `ping <目标 IPv4 地址>` 或 `ping -6 <目标 IPv6 地址>`。

## 关闭受邀 Windows 远程协助

执行阶段：System。将 `HKLM\SYSTEM\CurrentControlSet\Control\Remote Assistance` 下的 DWORD 值 `fAllowToGetHelp` 设为 `0`，关闭传统 Windows 远程协助的本机受邀连接开关。

它不是“关闭所有远程控制”的总开关；不会停用远程桌面 RDP、快速助手、企业主动提供协助策略或第三方远控软件，不删除服务或现有防火墙规则。组织策略仍可能覆盖本机设置。

## 脚本名称

“脚本名称”用于区分列表中的多个条目，最多 80 个字符，可留空。模板添加时自动带入名称；旧方案没有名称时保留原来的阶段与格式显示，不自动改写原有脚本。复制条目保留名称，副本仍默认停用。

名称保存在 JSON 方案，以及本工具有效的 XML 配置还原记录中，不拼接进可执行命令、不改变生成的脚本文件名。把 XML 交给其他工具编辑后，还原记录可能失效，名称不保证保留；脚本实际执行内容以 XML 为准。单独另存脚本文件只保存正文，不保存名称、执行阶段或启用状态。

## WinGet 应用安装（通用）

执行阶段为 FirstLogon，需要 Windows 11 24H2（内部版本 26100）及以上、管理员首次登录、网络和可用的应用安装程序。模板顶部 `PackageId` 默认为空，必须填写经过核对的软件包 ID；未填写时停止，不默认安装任何软件。

**Chrome 使用示例**：把参数默认值改为 `[string]$PackageId = 'Google.Chrome'`。安装其他应用时，先用 `winget search` 和 `winget show --id 包ID --exact --source winget` 核对 ID、来源及整机静默安装支持。具体软件仅作为示例，不需要重复新增模板入口。安装多个应用可以添加多条普通脚本并分别配置 ID；不要在一个 ID 中拼接额外命令参数。

保留最多 300 秒的 WinGet 就绪等待、精确 ID 与固定 `winget` 源、整机静默安装、返回码检查及 `--no-upgrade`。不绕过哈希验证，不允许自动重启；启用即接受包和源协议。就绪等待上限不包含安装耗时，不保证每个软件包都支持整机无人值守安装。

## 创建公共桌面快捷方式

执行阶段：System。Windows 自动提供公共桌面目录，默认创建 `System Properties.lnk` 和 `UnattendLocal Releases.url`；所有用户均可看到。名称是脚本内可修改的示例文件名，程序界面说明保持中文。

在脚本顶部的 `Shortcuts` 列表中修改 `Name`（含 `.lnk` 或 `.url` 扩展名）与 `Target`。程序目标是绝对文件或目录路径，支持环境变量展开；不将命令行参数拼进目标路径。网页目标仅支持绝对 HTTP / HTTPS 地址。模板不检查程序是否已经安装，只创建链接，不打开网页、不下载或执行目标内容。

已有同名文件或文件夹会保留并记录提示，重复执行不会覆盖。文件名不允许路径分隔符、保留设备名或跨目录写入；定义列表先整体校验，再逐项写入。临时文件与目标位于同目录，完整生成后再移动；发生错误时清理临时文件，但不撤销之前已成功创建的其他链接。

程序链接依赖 Windows Script Host 的 `WScript.Shell` COM 组件；被组织策略禁用、公共桌面不可写或磁盘写入失败时会报错。网页链接使用标准 InternetShortcut 格式。安装后可在桌面查看链接，再按需要手动打开。

## 执行与日志

FirstLogon 不会因为添加模板而自动启用管理员自动登录；请在账户设置中安排首次管理员登录。脚本仍经过原生成器的 System / FirstLogon 执行链；脚本报错会进入该链的日志，不保证停止其他安装命令。

编辑脚本后请自行核对内容。模板测试覆盖生成、保存与导入，以及模拟下载、签名、安装返回码和本地源失败；实际安装结果取决于目标 Windows、安装介质、网络和策略。

## English

Choose a template on the custom scripts page, review its purpose, stage and requirements, then add it. The new entry is selected and disabled until explicitly enabled. Adding a template does not execute it on the current computer. Entries remain ordinary editable PowerShell scripts supported by existing plan and XML workflows.

Offline NetFx3 runs at FirstLogon with administrator privileges and matching local installation media. It checks CAB availability, tries local sources, blocks Windows Update fallback and avoids restarting. Build 28000 and newer require a standalone installer and are rejected before feature modification.

VC++ v14 runs at FirstLogon with administrator privileges and network access. It selects packages using the target OS architecture, includes x86 on 64-bit systems, downloads through Microsoft's official URLs, and requires a valid Microsoft Authenticode signature. It waits for installers, handles success/restart-needed/already-installed results and cleans its private temporary directory. It does not install all historical VC++ runtimes.

AutoRun policy runs at System and disables AutoRun commands for every drive letter and drive type. It does not replace malware detection or suppress every AutoPlay prompt. Organization policies may override it.

FirstLogon templates do not enable automatic logon. Arrange an administrator's first logon in the account settings. Script failures are recorded through the existing generator pipeline and do not guarantee that subsequent setup commands stop.

Local Ping runs at System and allows only ICMPv4 type 8 and ICMPv6 type 128, from LocalSubnet, on Private and Domain profiles. It adds no Public exception and does not disable the firewall or change network categories. Owned rules are updated on rerun; collisions with another application's rule names stop before writes. Existing policies and network devices may still block traffic.

Solicited Remote Assistance sets the local `fAllowToGetHelp` toggle to zero during System. It does not disable RDP, Quick Assist, corporate Offer Remote Assistance policy or third-party tools. Organization policy can override it.

Script names are optional, limited to 80 characters and preserved by plans, duplication and valid local XML restoration records. They do not enter commands or affect generated filenames. Standalone script export contains only the script body; names may be lost when another tool edits the XML.

The generic WinGet template runs at FirstLogon on Windows 11 build 26100 or later with an administrator, network access and App Installer. PackageId starts empty; set a verified ID before enabling. Chrome is a documentation example (`[string]$PackageId = 'Google.Chrome'`), not the default. It waits up to 300 seconds for WinGet, installs an exact ID from the winget source in machine scope, preserves hash checks and avoids forced upgrades or requested reboots. Enabling accepts package/source agreements. The readiness timeout excludes installation time. The selected package must support machine-wide silent installation.

The public desktop template runs at System. Edit the `Shortcuts` list for `.lnk` program links or `.url` HTTP/HTTPS links. Windows resolves the common desktop directory. Definitions are validated before any files are created, existing names are preserved, and temporary files are cleaned after failure. Previously created links are not rolled back if a later item fails. The default links target System Properties and this project's release page; adding the template never executes either target.

## 参考资料 / References

- [原作者示例脚本 / Upstream samples](https://schneegans.de/windows/unattend-generator/samples/)
- [微软：Windows 11 安装 .NET Framework 3.5](https://learn.microsoft.com/dotnet/framework/install/dotnet-35-windows-11)
- [微软：VC++ v14 运行库下载](https://learn.microsoft.com/cpp/windows/latest-supported-vc-redist)
- [下载程序 / Download releases](https://github.com/tigerJX95/UnattendLocal/releases/latest)

- [Microsoft: New-NetFirewallRule](https://learn.microsoft.com/powershell/module/netsecurity/new-netfirewallrule)
- [Microsoft: Set-NetFirewallRule](https://learn.microsoft.com/powershell/module/netsecurity/set-netfirewallrule)
- [Microsoft: Remote Assistance policies](https://learn.microsoft.com/windows/client-management/mdm/policy-csp-remoteassistance)

- [Microsoft: WinGet install](https://learn.microsoft.com/windows/package-manager/winget/install)
- [Microsoft: WinGet return codes](https://github.com/microsoft/winget-cli/blob/master/doc/windows/package-manager/winget/returnCodes.md)
- [Microsoft: Create desktop shortcuts with WSH](https://learn.microsoft.com/troubleshoot/windows-client/admin-development/create-desktop-shortcut-with-wsh)

## V1.22：系统盘卷标与卓越性能 / Volume label and Ultimate Performance

两项均保留为原作者示例能力对应的普通脚本模板，在自定义脚本页选择后添加，默认停用。核对完整脚本、名称与阶段后再启用，继续支持 JSON 方案保存及本程序原始 XML 恢复。

### 设置系统盘卷标

在 System 阶段识别目标 Windows 的实际系统盘符，仅对一个 NTFS 系统卷执行重命名，不格式化或修改分区。默认脚本为 `$label = 'Windows'`，例如可改为 `$label = '系统'`。卷标必须非空、最长 32 个字符，不包含路径保留字符或控制字符。已有相同卷标时不重复修改。

### 启用卓越性能电源计划

在 System 阶段从 Windows 的卓越性能计划创建专用计划，再激活。使用固定的本模板 GUID 和名称，重复运行时复用；该 GUID 被其他计划占用时停止。每个 powercfg 命令均检查返回码，不支持的 Windows 或硬件会明确报错，不静默切换为其他计划。计划可能增加耗电和发热，笔记本/现代待机设备不保证支持。

此模板与“首选电源计划”均控制活动计划，请只选一种。该模板后执行时会覆盖此前选择的活动计划，取消模板不会撤销已经应用到目标系统的更改。

### English

**Set the system volume label** runs during System, detects the installed Windows drive and renames its NTFS volume without formatting or repartitioning. Change `$label = 'Windows'` to your desired non-empty label, up to 32 characters without path/control characters. An identical label is left unchanged.

**Enable Ultimate Performance** duplicates the Windows source scheme to a managed GUID, names and activates it during System, and reuses its own scheme on repeat runs. A GUID collision or failed powercfg command stops execution. Windows edition and hardware determine support; no fallback is activated. Energy consumption and heat may increase. Use this template or Preferred power plan, not both.

Both are added disabled as editable named PowerShell scripts, preserving plan and original-XML roundtrips. Unchecking a script does not undo a previous deployment.

参考 / Reference: [原作者示例脚本 / upstream samples](https://schneegans.de/windows/unattend-generator/samples/), [Set-Volume](https://learn.microsoft.com/en-us/powershell/module/storage/set-volume), [powercfg](https://learn.microsoft.com/en-us/windows-hardware/design/device-experiences/powercfg-command-line-options).
