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

## 通过 WinGet 安装 Google Chrome

执行阶段：FirstLogon。需要 Windows 11 24H2（内部版本 26100）及以上、管理员首次登录、网络和可用的“应用安装程序”。此模板不会自动启用自动登录或修复、重新注册 WinGet。

默认精确包 ID 为 `Google.Chrome`，源固定为 `winget`，作用域为整机。脚本最多等待 300 秒让当前用户的 `winget.exe` 出现；出现后等待安装命令执行结束，安装耗时不计入就绪等待上限。下载源不可达、策略阻止、程序无法启动或安装返回错误时，记录错误供排查。

启用模板表示接受软件包协议和 WinGet 源协议。脚本使用静默及禁止交互参数，不使用忽略哈希校验或允许重启的参数；`--no-upgrade` 保留已有安装，不强制更新。返回码 0 视为成功；“没有适用更新”返回码只记录提示，其他非零码报错。它不保证在所有软件包和组织环境中都能完成无人值守安装。

可在脚本顶部把 `PackageId` 改为其他已核对的社区包 ID，例如 `Microsoft.PowerToys`；包必须支持整机静默安装。不要在 ID 中拼接命令参数，脚本会拒绝空格及参数注入形式。`ReadyTimeoutSeconds` 只控制等待 WinGet 出现的时间，不控制安装进程的总耗时。

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

The Google Chrome template runs at FirstLogon on Windows 11 build 26100 or newer with an administrator, network access and App Installer. It waits up to 300 seconds for the current user's WinGet executable; that limit excludes the installation duration. Enabling it accepts package and source agreements. It installs exact ID `Google.Chrome` from `winget` in machine scope, without bypassing hashes, forcing upgrades or allowing reboots. Other verified package IDs can be edited at the top of the script if they support machine-wide silent installation.

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
