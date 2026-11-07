# 配置集与 AppLocker

## $OEM$ 配置集

开关默认关闭，启用后在 windowsPE 的 Microsoft-Windows-Setup 中设置 UseConfigurationSet=true。与 autounattend.xml 配套准备 $OEM$ 目录：$$ 映射 Windows 目录，$1 映射系统盘根目录。例如 `$OEM$\$1\Users\Public\Desktop\example.txt` 对应目标公共桌面的文件。不要预先创建未来用户的个人目录，以免 Windows 创建用户时采用其他目录名。

工具不创建、打包、复制这些文件，不会自动安装其中的应用。复制可能覆盖同路径内容，实际行为由 Windows Setup 处理。自定义 PE 安装脚本可能自行实现复制，导入时保留原文，不通过此开关覆盖该流程。取消不删除已复制文件。

## AppLocker 策略

默认关闭。导入完整 AppLockerPolicy XML，或载入审核示例后自行调整。使用固定核心的 AppLocker.xsd 结构检查；不支持 DTD、外部实体、处理指令、超过 2 Mi 字符或过深的 XML。结构有效不代表规则适合目标环境。

点击“检查策略结构”可查看各规则集合的执行模式，以及允许、拒绝规则的条数。含有规则的集合即使未配置执行模式，也可能按强制模式处理。仅有拒绝规则的集合若被强制执行，缺少允许规则可能使相应类型的文件无法运行。检查结果依据输入文件，实际效果需结合目标 Windows 上的组策略和应用验证。

原生命令在 specialize 阶段启动 Application Identity（AppIDSvc），执行 Set-AppLockerPolicy -XmlPolicy，不带 -Merge，可能替换已有本地策略。审核或强制模式完全由策略决定；工具不暗中放宽规则。强制策略可能拦截安装脚本、系统工具或管理员程序，应先在虚拟机审核规则，准备管理员恢复与策略回退手段。支持情况受 Windows 版本、组件和组织策略影响；不等于 SAC 或防病毒。

JSON 两种凭据模式均保留策略原文，可能包含路径、SID 和其他自行填写的信息；省略凭据不清除这些内容。预览整体遮蔽策略 XML，实际导出仍含完整策略。取消仅影响新配置，不撤销已应用策略。导入仅编辑完整识别的策略、提取和调用管线，未知修改保留原文并拒绝不可靠覆盖。恢复推荐会关闭两项开关，保留填写的策略内容。

参考：

- [原生成器使用说明](https://schneegans.de/windows/unattend-generator/usage/)
- [微软：UseConfigurationSet](https://learn.microsoft.com/en-us/windows-hardware/customize/desktop/unattend/microsoft-windows-setup-useconfigurationset)
- [微软：Set-AppLockerPolicy](https://learn.microsoft.com/en-us/powershell/module/applocker/set-applockerpolicy)
- [微软：AppLocker 规则集合与执行行为](https://learn.microsoft.com/en-us/windows/security/application-security/application-control/app-control-for-business/applocker/applocker-processes-and-interactions)
