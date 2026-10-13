using UnattendLocal.Core;

namespace UnattendLocal.Desktop;

public record HelpTopic(string Id, string Title, string Body);

// 中文整理：上游网站操作说明 + 当前固定版本的生成器行为。
// 提示只描述设置，不读取或显示账户 / Wi-Fi 密码。
public static class HelpCatalog
{
    public const string SourceUrl = "https://schneegans.de/windows/unattend-generator/";
    private static readonly HelpTopic[][] Pages =
    [
        [
            new("lockkeys", "锁定键初始状态与按键行为", "默认模式不写入覆盖；自定义模式配置登录界面及新建用户的初始状态，以及每个键是否忽略按键。忽略按键使用系统级扫描码映射，影响目标系统所有用户；通常不能再通过该物理键切换状态，应先确认所需初始状态。固件、驱动、快速启动及后续用户设置可能影响实际效果。不需要自动登录，不修改当前电脑。切回默认只影响新生成的配置，不自动恢复已安装系统中的扫描码映射。"),
            new("numlock", "Num Lock：数字锁定", "开启时数字小键盘通常输入数字；关闭时通常用于方向或导航。自定义初始状态的可修改起点为开启 Num Lock、关闭另两个键；只有明确启用自定义才参与生成。没有数字小键盘的设备，效果由键盘与驱动决定。"),
            new("capslock", "Caps Lock：大写锁定", "开启时英文字母通常默认大写，关闭时通常默认小写。初始状态与忽略按键分别选择；实际输入也受输入法、Shift 键和应用影响。"),
            new("scrolllock", "Scroll Lock：滚动锁定", "设置 Scroll Lock 的初始开关；此键的用途由应用决定，例如某些表格软件的方向键滚动行为。常用起点为关闭，不代表禁用这个按键。"),
            new("lockbehavior", "忽略锁定键按键", "仅自定义锁定键模式下生效。勾选后在目标系统写入系统级 Scancode Map，将对应物理键映射为空，通常无法再用该键切换锁定状态；未勾选时保持正常切换。此设置影响所有用户和登录界面，不能与其他扫描码映射合并，可能覆盖其他键盘重映射设置。默认模式保留选择但不生成覆盖；取消勾选或切回默认不会清除已经安装的系统上的映射。"),
            new("pointerprecision", "鼠标提高指针精确度", "勾选后关闭新建用户的鼠标提高指针精确度，将默认用户的 MouseSpeed、MouseThreshold1、MouseThreshold2 设为 0，减少 Windows 鼠标加速；不调整 DPI、指针速度或游戏内设置。默认不写入覆盖。鼠标驱动与应用可能采用独立设置；不修改当前电脑，不依赖自动登录。取消只影响新生成的安装配置。"),
            new("stickykeys", "粘滞键", "粘滞键是便于依次按下组合快捷键的辅助功能。默认模式不写入覆盖；禁用模式关闭粘滞键及连续五次 Shift 的快捷切换。自定义可分别配置六个选项，不会自动开启粘滞键，且不调整筛选键或切换键。设置写入目标系统的默认用户与登录界面，影响新建用户和登录界面；需要键盘辅助功能的用户应谨慎选择禁用。非自定义模式保留六个选项但不参与生成。不修改当前电脑；切回默认只影响新生成的配置。"),
            new("stickyhotkey", "连续五次 Shift 切换", "自定义模式下允许连续按 Shift 五次打开或关闭粘滞键；不勾选时关闭这个快捷方式，其他选项仍独立保留。自定义模式不自动开启粘滞键。"),
            new("stickyhotkeysound", "快捷切换提示音", "自定义模式下使用连续五次 Shift 切换粘滞键时播放提示音；只有快捷切换被启用并实际使用时，该提示音才有意义。"),
            new("stickyindicator", "粘滞键任务栏图标", "自定义模式下允许显示粘滞键任务栏图标；实际显示与粘滞键当前状态及 Windows 版本有关。"),
            new("stickyaudible", "快捷键按下与释放提示音", "自定义模式下使用粘滞键时，按下或释放快捷键会播放提示音；与连续五次 Shift 切换时的提示音分别设置。"),
            new("stickytristate", "连续两次锁定快捷键", "自定义模式下使用粘滞键时，连续按同一修饰键两次可将其锁定，便于连续输入组合快捷键。"),
            new("stickytwokeysoff", "同时按两键关闭", "自定义模式下，同时按下两个键时关闭粘滞键。需要依次输入组合快捷键的用户可根据操作习惯选择。"),
            new("computer", "计算机名", "留空时由 Windows 生成随机名称。自定义名称应符合 Windows 计算机名规则，不能与本地用户名相同。"),
            new("architecture", "目标安装架构", "amd64 用于常见的 Intel / AMD 64 位电脑；arm64 用于 ARM 64 位电脑；x86 用于 32 位镜像。这里选择的是待安装系统的架构。"),
            new("language", "语言、区域与键盘", "显示语言须与 Windows 安装镜像一致。区域格式决定日期、数字与货币的显示方式；键盘 / 输入法决定输入方式。默认不勾选手动设置；请提前选好语言与区域。明确勾选手动设置后，安装时再选择。"),
            new("region", "国家、地区与时区", "国家或地区用于地区内容和部分系统设置。时区单独指定；安装工具所在电脑的时区不会自动替代这里的选择。")
        ],
        [
            new("accountlockout", "账户锁定策略", "使用 Windows 默认策略时不写入锁定策略覆盖，实际默认值由目标 Windows 版本及已有组织策略决定。禁用时不按登录失败次数锁定账户，会降低防密码试探能力。自定义以失败次数、失败计数重置等待时间、锁定持续时间配置本机账户策略，在安装 specialize 阶段执行，不依赖自动登录。本组不修改微软在线账户或域端认证服务的锁定策略，组织策略也可能覆盖本机配置。"),
            new("lockoutthreshold", "登录失败次数", "自定义模式填写 0–999 次。正数表示达到该失败次数后触发账户锁定；0 表示禁用按失败次数锁定，此时两项时间不会触发锁定。过低阈值可能增加误锁或被恶意触发锁定的风险。"),
            new("lockoutreset", "失败计数重置等待时间", "自定义模式填写 1–99999 分钟。它表示登录失败计数重置前的等待时间，不是账户被锁定后的解锁时间；不得大于锁定持续时间。"),
            new("lockoutduration", "锁定持续时间", "自定义模式填写 1–99999 分钟，锁定后经过此时间自动解锁；不得小于失败计数重置等待时间。本版不提供持续时间 0（直到管理员手动解锁）。"),
            new("account", "账户创建方式", "自定义基础配置默认自动创建本地账户，账户列表由你填写。自动创建模式使用左侧账户列表，至少需要一个管理员。手动创建本地账户或添加微软账户时，安装过程会显示相应引导，左侧列表及首次自动登录选项不参与账户创建。微软账户需要联网。"),
            new("user", "用户名与显示名", "用户名用于创建本地账户。同一方案中的用户名不能重复，大小写不同也算重复。显示名可留空；只有希望显示名称不同于用户名时才需要填写。"),
            new("empty", "设置密码 / 允许空密码", "使用密码时，在密码框输入并取消“明确允许空密码”。不使用密码时，先清空密码框，再勾选“明确允许空密码”。空密码与密码到期策略是两个独立设置。"),
            new("logon", "首次自动登录一次", "首次自动登录管理员账户，以完成首次登录阶段的设置。本版使用列表中的第一个管理员，且只自动登录一次，不影响后续正常登录。"),
            new("expiration", "密码到期策略", "“永不过期”不要求定期更换密码；“Windows 默认”对应默认 42 天到期；“指定天数”允许 1–999 天。这些设置适用于本地账户；内置 Administrator 账户的密码永不过期。"),
            new("secrets", "密码编码与隐私文件", "Base64 可还原，不是加密。隐私文件保留选项位于 OOBE 页的“安装脚本与隐私文件”。默认在首次管理员登录阶段清理目标系统的应答文件与 Wi-Fi 文件；自动创建本地账户并关闭首次自动登录时需明确选择保留。安装介质上的原始 XML 需另行保管。")
        ],
        [
            new("wifi", "无线网络配置方式", "新建推荐与自定义基础配置默认选择有线局域网，跳过 Wi-Fi 配置，有线网络通过 DHCP 获取地址。首页选择推荐场景会立即同步此处的网络模式，切换为预设 Wi-Fi 时默认自动连接。手动选择会保留安装时的无线网络引导。预设模式将 WPA2-Personal 无线配置写入应答文件。若镜像不含无线网卡驱动，需把相应驱动放到安装介质的 $WinPEDriver$ 文件夹。"),
            new("ssid", "Wi-Fi 名称与密码", "SSID 应为 1–32 字节，中文字符会占多个字节。本版不接受控制字符、双引号、美元符号或反引号。WPA2 密码支持 8–63 位可打印 ASCII 字符。"),
            new("hidden", "隐藏网络与自动连接", "只有路由器不广播 SSID 时才需要勾选“隐藏网络”。自动连接选项决定是否自动连接预设 Wi-Fi；这些选项仅在预设 Wi-Fi 模式下生效。"),
            new("offline", "允许无网络安装", "离线安装与本地账户分别配置；隐藏无线页面不等于绕过联网要求。添加微软账户需要网络，不能同时选择允许离线安装。"),
            new("driver", "无线驱动", "若 Windows 安装镜像不包含无线网卡驱动，预设 Wi-Fi 也无法连接。可把相应驱动放在安装介质的 $WinPEDriver$ 文件夹。本版有线网络使用默认 DHCP。")
        ],
        [
            new("privacy", "快捷隐私设置", "全部关闭会将 ProtectYourPC 设置为 3，关闭 OOBE 快捷隐私设置并跳过手动选择；手动选择保留隐私引导；全部开启启用快捷设置。该选项覆盖 Windows 应答文件所定义的快捷设置，具体表现随目标 Windows 版本变化。"),
            new("online", "在线账户页面", "隐藏在线账户页面用于本地账户流程；选择添加微软账户时，本版不隐藏该页面。该选项不会创建账户，也不会断开网络。"),
            new("eula", "许可协议页面", "此选项只控制安装引导中是否显示许可协议页面。Windows 的使用仍受相应许可条款约束。"),
            new("oem", "OEM 注册页面", "勾选后隐藏电脑厂商的 OEM 注册页面，用于减少首次开机引导中的交互。新建推荐与自定义基础配置默认勾选，减少安装期间的交互。"),
            new("appsuggestions", "应用建议与自动推广", "勾选后减少 Windows 的应用建议、提示和自动推广安装，配置内容分发与消费者功能相关设置；不会卸载现有应用，也不阻止用户主动安装。此设置也可能使 Windows 设置中移动设备的管理设备按钮不可用，请需要手机连接等功能的用户谨慎选择。未勾选时不写入覆盖，遵循 Windows 与组织策略；取消只影响新生成的安装配置，不自动恢复已安装系统。实际覆盖范围与效果随 Windows 版本和版本类型而异。不会修改当前电脑。"),
            new("windowsupdate", "Windows 更新暂停", "勾选后在目标系统注册名为 PauseWindowsUpdate 的计划任务，在开机时运行并按天重复，将更新暂停期设为未来 7 天。此项反复延长暂停期，不是停用更新服务。长期暂停会延迟安全补丁与质量更新。临时更新可在 Windows 更新设置中选择恢复更新；任务再次运行时仍可能暂停。要永久恢复，请先在任务计划程序中禁用或删除 PauseWindowsUpdate，再恢复更新。未勾选时不写入此项覆盖，遵循 Windows 与组织策略；取消勾选只影响新生成的配置，不会删除已安装系统里的任务。不会修改当前电脑，实际效果需在目标 Windows 验证。"),
            new("edgefre", "Edge 首次运行引导", "勾选后跳过目标系统中 Edge 首次运行的欢迎引导和启动画面。此项不阻止浏览器账户登录，也不等于关闭同步；仍可能出现同步确认或其他提示。未勾选时不写入此项覆盖，遵循 Edge 默认行为。需目标 Edge 版本支持，不会修改当前电脑。"),
            new("edgebackground", "Edge 启动加速及后台运行", "勾选后将目标系统 Edge 的启动加速与关闭窗口后继续后台运行两项默认设为关闭；浏览器启动速度可能变慢。安装后用户仍可在 Edge 系统设置中调整，组织的强制策略可能覆盖此默认值。不卸载 Edge、不删除浏览数据，也不保证系统组件调用时没有 Edge 进程。未勾选时不写入覆盖，遵循 Edge 默认行为。需目标 Edge 版本支持，不会修改当前电脑。"),
            new("longpaths", "长路径支持", "勾选后在安装时启用 Windows 长路径支持，适用于 Windows 10 1607 及以后版本和 Windows 11。应用本身也须支持长路径；旧程序和资源管理器的部分操作仍可能受限制，单个文件名长度限制不变。未勾选时不写入覆盖，遵循 Windows 默认行为。不会修改当前电脑。"),
            new("systemsounds", "系统声音", "勾选后为目标系统的用户配置关闭系统提示音方案，同时关闭系统启动声音。不会静音扬声器，不影响音乐、视频和应用自行播放的音频；系统不在声音控制面板中展示的个别事件可能保留。安装后可在 Windows 声音设置中自行调整。未勾选时不写入此项覆盖，遵循 Windows 默认行为；不会修改当前电脑。"),
            new("faststartup", "快速启动", "勾选后在安装时关闭目标系统的快速启动；关机后的开机可能变慢。未勾选时不写入此项覆盖，遵循 Windows 默认行为，并非强制开启。此设置不关闭休眠功能，也不修改当前电脑。"),
            new("deviceencryption", "禁止 BitLocker 自动设备加密", "启用后，在目标 Windows 安装阶段阻止自动启用设备加密。新建推荐与自定义配置默认启用；可取消勾选以遵循 Windows 默认行为。这不会解密已有加密卷，也不会禁用 BitLocker 功能或阻止安装后手动配置加密。效果需在目标 Windows 版本与硬件上验证。"),
            new("hardware", "Windows 11 安装检查", "在版本与密钥页分别选择绕过 TPM 2.0、安全启动或内存容量检查。默认全部关闭，按 Windows 默认要求检查。设置在 Windows PE 安装阶段应用；不关闭固件中的 TPM 或安全启动，也不提升硬件能力。只配置这三项，不保证绕过 CPU 指令集或其他版本要求；须以目标安装镜像实测为准。"),
            new("hardwaretpm", "绕过 TPM 2.0 检查", "启用后，安装阶段配置跳过 TPM 要求检查。不会关闭或模拟 TPM，不改变设备加密默认设置。取消时不生成此项覆盖。"),
            new("hardwareboot", "绕过安全启动检查", "启用后，安装阶段配置跳过安全启动要求检查。不会更改 BIOS/UEFI 的安全启动开关，不会转换系统分区格式。取消时不生成此项覆盖。"),
            new("hardwareram", "绕过内存容量检查", "启用后，安装阶段配置跳过内存容量要求检查。不会增加实际内存；安装后的可用性和性能仍受硬件限制。取消时不生成此项覆盖。"),
            new("powershell", "隐藏 PowerShell 窗口", "勾选后，为安装和首次登录阶段的 PowerShell 调用使用 -WindowStyle Hidden；取消后使用 Normal，便于观察和调试。新建方案默认开启。若脚本包含 Read-Host 等交互输入，请保持窗口可见，否则无法回答提示，安装可能停滞。"),
            new("sensitivefiles", "保留安装隐私文件", "勾选后保留目标系统的以下文件：C:\\Windows\\Panther\\unattend.xml、C:\\Windows\\Panther\\unattend-original.xml、C:\\Windows\\Setup\\Scripts\\Wifi.xml。它们可能包含账户密码、Wi-Fi 密码和产品密钥。取消后，在首次管理员登录阶段执行清理；原始安装介质上的 XML 需另行保管。自动创建本地账户并关闭首次自动登录时，本版要求明确选择保留。")
        ],
        [
            new("preview", "检查与预览", "检查当前配置并验证 XML 结构，预览会遮蔽账户与 Wi-Fi 密码。预览文本不能直接用于安装；修改配置后应重新检查。"),
            new("export", "导出与安装", "导出会重新检查当前设置，并生成完整 autounattend.xml。将文件放到安装 U 盘根目录或虚拟机可读取的安装介质。安装镜像与分区可在“镜像与分区”页配置，默认手动选择。清空磁盘或格式化时必须再次核对风险。"),
            new("save", "方案保存与载入", "默认不保存密码和自定义产品密钥；载入后需重新输入。选择包含凭据时，JSON 会明文保存这些内容。导出的 XML 可能含可还原凭据，请妥善保管。"),
            new("verify", "安装验证", "结构检查不能替代真实安装。先在虚拟机核对用户、密码、首次登录、网络与 OOBE。工具生成安装配置，不会修改当前电脑的账户或网络。")
        ],
        [
            new("custom", "新建自定义配置", "从独立基础值开始，不沿用推荐策略或预填账户。默认预设语言、自动创建本地账户、有线网络和关闭快捷隐私；语言区域、SKU、账户及凭据需在工具内填写。密码到期使用 Windows 默认策略，时区由 Windows 决定；磁盘保持手动，避免误删数据。切换模式时可重新开始或保留当前设置，重新开始会清空已有配置。"),
            new("timezone", "时区设置", "由 Windows 决定时不写入固定时区；指定时区时必须选择目标时区。自定义基础配置默认由 Windows 决定，推荐配置默认使用所选固定时区。"),
            new("image", "镜像选择", "按名称或索引选择安装介质中的镜像。索引从 1 开始，可使用 DISM /Get-ImageInfo 核对；镜像必须与产品密钥、SKU、语言和架构匹配。此工具不读取实际镜像内容。"),
            new("customscripts", "按阶段执行自定义脚本", "System 在 specialize 阶段以 SYSTEM 身份执行；DefaultUser 随后修改默认用户注册表模板；FirstLogon 在全机首个用户登录时执行；UserOnce 在每个用户首次登录时执行，不保证桌面已加载完成。支持 CMD、PowerShell、REG、VBScript，DefaultUser 仅支持前三种。同阶段按列表顺序；必须自行处理等待与错误。空白启用脚本不能导出，淡色示例仅供参考。JScript（.js）已停止支持；旧方案内容保留但自动停用，请移除或手动改写。"),
            new("scriptcontent", "脚本内容与保存", "脚本嵌入应答文件，安装时按所选阶段执行，本工具不会本地执行。关闭某条脚本只保留编辑内容，不参与生成。方案会原样保存脚本；省略凭据与预览遮蔽不解析脚本内部的密码、令牌。"),
            new("diskscript", "自定义 DiskPart 脚本", "在 Windows PE 中先执行分区脚本，成功后才交由安装程序将镜像安装到明确填写的磁盘 / 分区。此目标不能限制脚本操作范围。仅支持 ASCII DiskPart 命令；不支持 CMD、PowerShell、变量或 noerr。命令错误时显示 X:\\UnattendLocal-diskpart.log 并停留，需人工排查；本工具不会在当前电脑执行脚本。"),
            new("diskscriptexample", "载入分区示例", "淡色示例仅供参考，不保存、不参与生成。点击此图标才填入编辑器。示例使用 UEFI/GPT，清空磁盘 0，Windows 为第 3 个分区；恢复分区 1000 MB，需核对空间要求。不会替你填写或修改安装目标，也不会自动勾选风险确认。"),
            new("diskmode", "磁盘与分区模式", "默认手动选择。已有分区模式仅指定安装目标，默认不格式化；清空模式会删除指定磁盘全部分区并创建新布局，必须明确勾选确认。"),
            new("disktarget", "目标磁盘与分区编号", "磁盘编号从 0 开始，分区编号从 1 开始。必须核对目标电脑 Windows PE 中的编号；当前电脑磁盘编号和盘符不能用于推断安装目标。修改编号会取消危险操作确认。"),
            new("diskformat", "格式化已有分区", "默认关闭。开启后将目标已有分区格式化为 NTFS，删除该分区全部数据，需独立确认。不格式化也不保证安装后原有文件完整保留。"),
            new("disklayout", "自动分区布局", "UEFI / GPT 创建 EFI、MSR、Windows 和恢复分区；BIOS / MBR 创建活动系统、Windows 和恢复分区，主要适用于 Windows 10 传统 BIOS。恢复分区默认 1536 MB，可调整；须核对固件启动方式、磁盘容量及恢复镜像大小。"),
            new("diskconfirm", "危险操作确认", "确认只在当前会话有效，不保存到方案。改变脚本、目标、布局或操作模式会取消确认，重新打开方案后必须再次确认。导出时再次核对目标及删除范围。"),
            new("edition", "安装版本与密钥方式", "安装时输入：由安装程序询问密钥。通用安装密钥：按所选 SKU 写入公开安装密钥，不提供激活授权。自定义密钥：用于安装和激活配置。固件密钥：由目标电脑安装程序使用 BIOS/UEFI 中的密钥。"),
            new("sku", "Windows 10 / 11 与 SKU", "Windows 10 或 Windows 11 由安装介质决定；密钥用于选择镜像中的 SKU。同名 SKU 可能共用密钥，密钥仍必须匹配版本、授权渠道和镜像。LTSC / IoT 等未列出的 SKU 可使用匹配的自定义密钥与安装镜像。"),
            new("installkey", "自定义产品密钥", "输入五组、每组五位字母或数字，以短横线分隔。工具会去除首尾空格并转成大写，只检查格式；授权有效性及镜像 SKU 匹配由 Windows 安装和激活流程判断。"),
            new("activation", "单独指定激活密钥", "安装密钥用于选择安装的 SKU，激活密钥写入安装后的激活配置。两者可以不同，但须匹配安装的版本和授权渠道；MAK 密钥应填在激活配置中。未单独指定时，自定义安装密钥也用作激活密钥；通用安装模式不额外写入激活密钥。")
        ],
        [
            new("taskbarsearch", "任务栏搜索样式", "完整搜索框、图标和文字、仅图标、隐藏，对应四种显示样式。默认完整搜索框沿用 Windows 默认输出；其余样式在每位用户首次登录时设置。图标和文字适用于支持该样式的 Windows 11；不同版本的可用样式与实际外观可能不同。隐藏搜索入口不会禁用 Windows 搜索。"),
            new("taskbaricons", "任务栏固定图标", "默认图标沿用 Windows 布局；移除全部只移除固定的应用图标，不影响开始、搜索、任务视图、托盘或正在运行的应用。自定义 XML 使用 LayoutModificationTemplate 和 CustomTaskbarLayoutCollection，可编辑、导入、检查或载入示例。在安装阶段写入布局并安排解除锁定，之后可手动增删。"),
            new("starttiles", "Windows 10 开始菜单磁贴", "配置初始磁贴，可使用默认、全部移除或从 Windows 10 的 Export-StartLayout 导出的 XML。Windows 11 忽略此设置。此选项不卸载应用，安装后可增删磁贴。"),
            new("startpins", "Windows 11 开始菜单固定应用", "配置初始固定应用，可使用默认、全部移除或从 Windows 11 的 Export-StartLayout 导出的 JSON。JSON 根对象须包含 pinnedList 数组。Windows 10 忽略此设置。此选项不卸载应用，安装后可增删固定项。将来使用应用移除设置时，默认固定项可能仍留下已移除应用的入口。"),
            new("widgets", "禁用小组件", "隐藏 Windows 11 的新闻和天气小组件入口，通过安装阶段的策略设置完成。取消时沿用 Windows 默认，不生成反向启用策略。"),
            new("taskbaralignment", "任务栏左对齐", "设置 Windows 11 任务栏图标左对齐。关闭时保留系统默认；Windows 10 的布局不因此改变。"),
            new("taskview", "隐藏任务视图按钮", "隐藏任务栏上的任务视图入口，不禁用虚拟桌面或任务视图功能。取消时保留 Windows 默认。"),
            new("trayicons", "始终显示全部托盘图标", "Windows 10 设置 EnableAutoTray；Windows 11 创建周期性计划任务，将通知区域的图标设为可见。影响系统托盘，不影响任务栏固定应用。"),
            new("bingsearch", "搜索中不显示 Bing 结果", "限制开始菜单和搜索栏中的联网搜索建议。不会禁用本地搜索，也不会卸载 Bing 应用。效果需用目标 Windows 版本验证。")
        ],
        [
            new("explorerhidden", "隐藏文件显示方式", "使用 Windows 默认设置：隐藏具有 Hidden 属性的文件。隐藏受保护的操作系统文件：显示普通隐藏文件，仅隐藏同时带 Hidden 和 System 属性的文件。显示全部文件：同时显示普通隐藏文件与受保护的操作系统文件，适合需要查看系统文件的用户。显示文件不改变文件属性或访问权限，也不会删除或修改文件。"),
            new("explorerextensions", "始终显示文件扩展名", "启用后显示已知文件类型的扩展名，例如 .txt、.exe、.pdf，便于确认完整文件名。关闭时沿用 Windows 默认隐藏已知类型扩展名的行为。该设置不会重命名文件，也不会改变默认打开方式。"),
            new("explorercontext", "Windows 11 经典右键菜单", "在 Windows 11 中使用经典右键菜单，减少进入“显示更多选项”的操作。在每位用户首次登录时应用并刷新资源管理器；不同 Windows 11 版本的实际表现可能不同。Windows 10 本来使用经典菜单。取消时沿用目标系统默认菜单。"),
            new("explorertips", "隐藏文件夹与桌面项目的悬停说明", "启用后关闭鼠标悬停在文件夹和桌面项目上时的弹出说明。不影响本工具的悬停提示；不会关闭 Windows 通知，也不会禁止打开属性窗口。关闭时沿用 Windows 默认显示行为。"),
            new("explorerlaunch", "打开资源管理器时进入此电脑", "启用后，新打开资源管理器时进入“此电脑”，而不是“快速访问”或 Windows 11 的“主页”。不删除快速访问中的固定项目，不改变默认保存位置；系统版本和打开方式会影响实际行为。"),
            new("taskbarendtask", "任务栏显示结束任务", "在支持此功能的 Windows 11 版本中，为任务栏应用图标的右键菜单显示“结束任务”。是否显示取决于目标版本；Windows 10 不因这一项获得相同入口。勾选只配置菜单，不会在安装过程中结束任何应用。使用“结束任务”可能丢失该应用未保存的内容。")
        ],
        [
            new("desktopicons", "系统桌面图标", "默认模式沿用 Windows 的系统桌面图标。自定义模式中，勾选的系统图标显示，未勾选的隐藏；全不选时隐藏这组系统图标。不会删除普通桌面文件或应用快捷方式，不改变文件夹内容。图库、主页等图标需要目标 Windows 版本支持。设置在每位用户首次登录时应用并刷新资源管理器。切换默认后保留勾选但不参与生成。"),
            new("edgeicon", "删除 Edge 桌面快捷方式", "启用后删除公共桌面和每位用户桌面上名为 Microsoft Edge.lnk 的快捷方式。该选项独立于系统桌面图标模式；不会卸载 Edge，也不会删除浏览器数据。桌面重定向或快捷方式名称不同时可能无法匹配。取消时不生成删除命令，保留 Windows 默认行为。"),
            new("startfolders", "开始菜单电源按钮旁的文件夹", "仅适用于 Windows 11，配置开始菜单电源按钮旁的文档、下载、文件资源管理器、音乐、网络、个人文件夹、图片、设置、视频入口。默认模式沿用 Windows；自定义中仅显示勾选项目，全不选时隐藏这组入口。不会删除文件夹、修改内容或更改开始菜单固定应用。Windows 10 忽略此设置；切换默认后保留勾选但不参与生成。")
        ]
        ,[
            new("visualeffects", "视觉效果设置方式", "Windows 默认由目标系统自行选择，不写入本组覆盖。最佳外观开启本组全部 17 项；最佳性能关闭全部 17 项，包括字体平滑和文件缩略图。自定义时逐项选择，未勾选的效果关闭，全不选时全部关闭。自定义初始未勾选，需自行选择；可用全选作为起点。切换模式或恢复推荐保留个人勾选，只有自定义模式使用这些勾选。效果设置会在安装阶段写入默认值，并在每位用户首次登录时设置模式；实际效果取决于目标 Windows 版本。"),
            ..VisualEffectsSettings.Items.Select(item=>new HelpTopic(item.HelpId,item.Label,item.Description+" 仅自定义模式可逐项调整；最佳外观统一开启，最佳性能统一关闭，Windows 默认由系统决定。设置应用于目标 Windows，不改变本工具界面。"))
        ]
        ,[
            new("wallpaper", "桌面背景设置方式", "使用 Windows 默认背景时，不写入本组覆盖。纯色背景在每位用户首次登录时应用，并替换背景图片的显示方式，不删除图片文件。背景与系统/应用颜色模式、强调色独立配置。切回默认或恢复推荐会保留选色，但不参与生成；安装后仍可自行更改背景。"),
            new("wallpapercolor", "纯色桌面背景", "可以选择常用色，或输入六位十六进制 #RRGGBB，例如 #003366。输入大小写与首尾空格均可，生成时规范为大写。只接受不透明 RGB 颜色，不接受颜色名称、缩写、透明度或脚本。色块仅预览选色，不会修改当前电脑的背景，也不显示目标 Windows 的实际桌面。"),
            new("windowscolors", "Windows 配色设置方式", "默认沿用目标 Windows 配色，不写入本组覆盖。自定义时设置系统与应用的浅深模式、强调色和三项开关；关闭的开关会明确写为关闭。切回默认或恢复推荐时保留个人填写内容，但不参与生成。本页设置目标 Windows 的初始外观，独立于顶部月亮/太阳切换的工具主题。"),
            new("systemcolortheme", "系统颜色模式", "选择开始菜单、任务栏等系统界面的浅色或深色模式。系统浅色时不应用开始菜单和任务栏强调色；相关勾选会保留，切回深色时恢复。应用颜色模式单独选择，不必与系统相同。"),
            new("appscolortheme", "应用颜色模式", "选择支持 Windows 颜色模式的应用使用浅色或深色。应用可以自行设置主题，部分应用不支持跟随 Windows；不保证所有窗口外观一致。"),
            new("accentcolor", "Windows 强调色", "可选择常用颜色，或输入六位十六进制 #RRGGBB（例如 #0078D4）。色块只显示当前选色；不是目标系统截图。输入框可使用大小写字母，生成时规范为大写。不接受颜色名称、缩写、透明度或脚本。自定义模式下必须填写有效颜色。"),
            new("colortransparency", "透明效果", "控制 Windows 窗口背景和部分界面的透明效果。自定义模式下，勾选开启，不勾选关闭；实际显示受版本、硬件和系统设置影响。与视觉效果页的半透明选择框为不同设置。"),
            new("accentstart", "开始菜单和任务栏的强调色", "系统深色模式下，可将强调色显示于开始菜单和任务栏。系统浅色时该项显示为关闭并禁用，导出不应用，原先勾选保留以便切回深色恢复。不会更改任务栏固定图标、搜索样式或排列。"),
            new("accentborders", "标题栏和窗口边框的强调色", "控制目标 Windows 在标题栏和窗口边框上显示强调色。自定义模式下，勾选开启，不勾选关闭；支持程度取决于窗口和应用实现。独立于开始菜单和任务栏强调色。")
        ]
    ];

    public static int PageCount => Pages.Length;
    public static IReadOnlyList<HelpTopic> ForPage(int page) => Pages[Math.Clamp(page, 0, Pages.Length - 1)];

    public static HelpTopic Current(int page, string? id, InstallPlan plan, UserEntry? user)
    {
        var topics = ForPage(page);
        var topic = topics.FirstOrDefault(t => t.Id == id) ?? topics[0];
        string state = topic.Id switch
        {
            "accountlockout" => $"当前：{AccountLockoutPolicy.ModeSummary(plan)}。",
            "lockoutthreshold" or "lockoutreset" or "lockoutduration" => plan.AccountLockoutMode!=AccountLockoutPolicy.Custom?"当前：非自定义模式，保留的参数不参与生成。":$"当前：{AccountLockoutPolicy.ModeSummary(plan)}。",
            "pointerprecision" => plan.DisablePointerPrecision?"当前：关闭提高指针精确度。":"当前：遵循 Windows 默认行为。",
            "stickykeys" => $"当前：{StickyKeySettings.ModeSummary(plan)}。",
            "stickyhotkey" or "stickyhotkeysound" or "stickyindicator" or "stickyaudible" or "stickytristate" or "stickytwokeysoff" => plan.StickyKeysMode!=StickyKeySettings.Custom?"当前：非自定义模式，保留的选项不参与生成。":$"当前：{StickyKeySettings.ModeSummary(plan)}。",
            "lockbehavior" => plan.LockKeyMode==KeyboardLockSettings.Default?"当前：Windows 默认状态，保留的忽略按键选择不参与生成。":$"当前：{KeyboardLockSettings.ModeSummary(plan)}。",
            "lockkeys" => $"当前：{KeyboardLockSettings.ModeSummary(plan)}。",
            "numlock" => KeyboardLockSettings.CurrentKey(plan, plan.InitialNumLockOn,plan.IgnoreNumLock),
            "capslock" => KeyboardLockSettings.CurrentKey(plan, plan.InitialCapsLockOn,plan.IgnoreCapsLock),
            "scrolllock" => KeyboardLockSettings.CurrentKey(plan, plan.InitialScrollLockOn,plan.IgnoreScrollLock),
            "wallpaper" => $"当前：{WallpaperSettings.ModeSummary(plan)}。",
            "wallpapercolor" => !WallpaperSettings.IsSolid(plan)?"当前：Windows 默认背景，保留的颜色不参与生成。":ColorSettings.TryNormalize(plan.WallpaperColorHex,out var backgroundColor)?$"当前：纯色背景 {backgroundColor}。":"当前：背景颜色无效，请输入 #RRGGBB。",
            "windowscolors" => $"当前：{ColorSettings.ModeSummary(plan)}。",
            "systemcolortheme" => ColorSettings.IsCustom(plan)?$"当前：系统{plan.WindowsColorTheme}。":"当前：由 Windows 决定，保留的系统模式不参与生成。",
            "appscolortheme" => ColorSettings.IsCustom(plan)?$"当前：应用{plan.AppsColorTheme}。":"当前：由 Windows 决定，保留的应用模式不参与生成。",
            "accentcolor" => !ColorSettings.IsCustom(plan)?"当前：由 Windows 决定，保留的颜色不参与生成。":ColorSettings.TryNormalize(plan.AccentColorHex,out var color)?$"当前：{color}。":"当前：颜色无效，请输入 #RRGGBB。",
            "colortransparency" => !ColorSettings.IsCustom(plan)?"当前：由 Windows 决定。":$"当前：透明效果{(plan.EnableColorTransparency?"开启":"关闭")}。",
            "accentstart" => !ColorSettings.IsCustom(plan)?"当前：由 Windows 决定。":plan.WindowsColorTheme==ColorSettings.Light?"当前：系统浅色，此项不生效；原有选择保留，切回深色时恢复。":$"当前：开始菜单和任务栏强调色{(ColorSettings.StartAccentEnabled(plan)?"开启":"关闭")}。",
            "accentborders" => !ColorSettings.IsCustom(plan)?"当前：由 Windows 决定。":$"当前：标题栏和边框强调色{(plan.AccentColorOnBorders?"开启":"关闭")}。",
            "visualeffects" => $"当前：{VisualEffectsSettings.ModeSummary(plan)}。",
            _ when VisualEffectsSettings.Items.Any(item=>item.HelpId==topic.Id) => $"当前：{VisualEffectsSettings.ItemState(plan,VisualEffectsSettings.Items.First(item=>item.HelpId==topic.Id))}。",
            "desktopicons" => $"当前：{DesktopSettings.IconSummary(plan)}。",
            "edgeicon" => $"当前：删除 Edge 桌面快捷方式{(plan.DeleteEdgeDesktopIcon?"开启":"关闭")}。",
            "startfolders" => $"当前：{DesktopSettings.FolderSummary(plan)}（仅 Windows 11）。",
            "explorerhidden" => $"当前：{plan.HideFilesMode}。",
            "explorerextensions" => $"当前：始终显示文件扩展名{(plan.ShowFileExtensions?"开启":"关闭")}。",
            "explorercontext" => $"当前：经典右键菜单{(plan.ClassicContextMenu?"开启":"关闭")}。",
            "explorertips" => $"当前：隐藏悬停说明{(plan.HideInfoTip?"开启":"关闭")}。",
            "explorerlaunch" => $"当前：打开资源管理器时{(plan.LaunchToThisPC?"进入此电脑":"使用 Windows 默认入口")}。",
            "taskbarendtask" => $"当前：任务栏显示结束任务{(plan.ShowEndTask?"开启":"关闭")}。",
            "taskbarsearch" => $"当前：{plan.TaskbarSearch}。",
            "taskbaricons" => $"当前：{plan.TaskbarIconsMode}；{(plan.TaskbarIconsMode==ShellSettings.CustomXml?"自定义 XML 参与生成":"编辑器内容不参与生成")}。",
            "starttiles" => $"当前：{plan.StartTilesMode}（仅 Windows 10）。",
            "startpins" => $"当前：{plan.StartPinsMode}（仅 Windows 11）。",
            "widgets" => $"当前：禁用小组件{(plan.DisableWidgets?"开启":"关闭")}。",
            "taskbaralignment" => $"当前：任务栏左对齐{(plan.LeftTaskbar?"开启":"关闭")}。",
            "taskview" => $"当前：隐藏任务视图{(plan.HideTaskViewButton?"开启":"关闭")}。",
            "trayicons" => $"当前：显示全部托盘图标{(plan.ShowAllTrayIcons?"开启":"关闭")}。",
            "bingsearch" => $"当前：不显示 Bing 搜索结果{(plan.DisableBingResults?"开启":"关闭")}。",
            "account" => $"当前：{plan.AccountMode}。",
            "empty" => user is null ? "请先选择或添加账户。" : user.AllowEmptyPassword
                ? "当前已允许空密码；请确保该账户的密码框为空。"
                : "当前使用密码；请填写密码，或明确选择允许空密码。",
            "expiration" => plan.PasswordExpiration switch
            {
                "Windows 默认" => "当前：Windows 默认，通常为 42 天到期；本工具不额外覆盖系统默认策略。",
                "指定天数" => $"当前：{plan.PasswordDays} 天到期，仅 1–999 天有效。",
                _ => "当前：密码永不过期。"
            },
            "logon" => plan.AccountMode != "自动创建本地账户" ? "当前为手动创建账户模式，此选项不参与账户创建。"
                : plan.AutoLogon ? "当前：首次自动登录一次。" : "当前：关闭首次自动登录；需勾选保留安装凭据文件。",
            "wifi" => plan.WifiMode=="隐藏无线网络选择页" ? "当前：有线局域网（跳过 Wi-Fi 配置）。" : $"当前：{plan.WifiMode}。",
            "offline" => plan.BypassNetwork ? "当前已允许无网络安装；微软账户模式与此设置冲突。" : "当前未启用无网络安装绕过。",
            "privacy" => $"当前：{plan.PrivacyMode}。",
            "online" => plan.AccountMode == "安装时添加微软账户" ? "当前为微软账户模式，隐藏选项关闭且不可用。" : $"当前为本地账户流程；隐藏在线账户页面：{(plan.HideOnlineAccount?"开启":"关闭")}。",
            "language" => plan.InteractiveLanguage ? "当前：安装时手动选择语言与区域。" : $"当前：预设语言与区域；{(new[]{plan.ImageLanguage,plan.Locale,plan.Keyboard,plan.GeoId}.Any(string.IsNullOrWhiteSpace)?"请补齐本页的语言、区域、键盘和国家或地区":"已填写，请核对安装镜像")}。",
            "region" or "timezone" => plan.TimeZoneMode=="由 Windows 决定" ? "当前：由 Windows 决定时区。" : $"当前：指定时区；{(string.IsNullOrWhiteSpace(plan.TimeZone)?"尚未选择":ChoiceLocalization.Get("TimeOffset",plan.TimeZone,plan.TimeZone))}。",
            "hidden" => plan.WifiMode!="预设 WPA2 Wi-Fi" ? "当前未启用预设 Wi-Fi，这些选项不生效。" : $"当前：隐藏网络{(plan.HiddenWifi?"开启":"关闭")}；自动连接{(plan.ConnectWifi?"开启":"关闭")}。",
            "eula" => $"当前：{(plan.HideEula?"隐藏":"显示")}许可协议页面。",
            "oem" => $"当前：{(plan.HideOEMRegistration?"隐藏":"显示")} OEM 注册页面。",
            "appsuggestions" => plan.DisableAppSuggestions?"当前：禁用应用建议和自动推广；移动设备管理入口可能受影响。":"当前：遵循 Windows 默认行为。",
            "windowsupdate" => plan.DisableWindowsUpdate?"当前：循环暂停更新；永久恢复需停用 PauseWindowsUpdate 任务。":"当前：遵循 Windows 默认行为。",
            "edgefre" => plan.HideEdgeFre?"当前：跳过 Edge 首次运行引导。":"当前：遵循 Edge 默认行为。",
            "edgebackground" => plan.DisableEdgeStartupBoost?"当前：启动加速与后台运行默认关闭，用户仍可调整。":"当前：遵循 Edge 默认行为。",
            "longpaths" => plan.EnableLongPaths?"当前：启用长路径支持，仍需应用支持。":"当前：遵循 Windows 默认行为。",
            "systemsounds" => plan.TurnOffSystemSounds?"当前：关闭系统提示音方案与系统启动声音。":"当前：遵循 Windows 默认行为。",
            "faststartup" => plan.DisableFastStartup?"当前：在目标系统关闭快速启动。":"当前：遵循 Windows 默认行为。",
            "deviceencryption" => plan.PreventDeviceEncryption?"当前：禁止自动启用 BitLocker 设备加密。":"当前：遵循 Windows 默认设备加密行为。",
            "hardware" => $"当前：{HardwareSettings.Summary(plan)}。",
            "hardwaretpm" => $"当前：TPM 2.0 检查绕过{(plan.BypassHardware||plan.BypassTPMCheck?"开启":"关闭")}。",
            "hardwareboot" => $"当前：安全启动检查绕过{(plan.BypassHardware||plan.BypassSecureBootCheck?"开启":"关闭")}。",
            "hardwareram" => $"当前：内存容量检查绕过{(plan.BypassHardware||plan.BypassRAMCheck?"开启":"关闭")}。",
            "architecture" => $"当前目标架构：{ChoiceLocalization.Architecture(plan.Architecture)}。",
            "computer" => string.IsNullOrEmpty(plan.ComputerName)?"当前：由 Windows 自动生成计算机名。":$"当前计算机名：{plan.ComputerName}。",
            "image" => $"当前：{InstallationSettings.ImageSummary(plan)}。",
            "diskmode" or "disktarget" or "diskformat" or "disklayout" => $"当前：{InstallationSettings.DiskSummary(plan)}。",
            "edition" or "sku" => $"当前：{plan.EditionMode}；SKU {(string.IsNullOrWhiteSpace(plan.EditionId)?"尚未选择":plan.EditionId)}。",
            "activation" => plan.UseActivationKey?"当前：单独指定激活密钥，生成前需填写并检查格式。":"当前：未单独指定激活密钥。",
            "powershell" => plan.HidePowerShellWindows ? "当前：隐藏 PowerShell 窗口。" : "当前：显示 PowerShell 窗口。",
            "sensitivefiles" => plan.KeepSensitiveFiles ? "当前：保留安装隐私文件。" : "当前：在首次管理员登录阶段清理安装隐私文件。",
            _ => ""
        };
        return topic with { Body = string.IsNullOrEmpty(state) ? topic.Body : state + "\n\n" + topic.Body };
    }
}
