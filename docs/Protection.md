# UAC 与 WPBT

两项通过固定上游生成器在 specialize 阶段配置，默认不勾选。

- UAC：EnableLUA=0，关闭用户账户控制。这不是只隐藏权限提升提示；管理员审批/权限提升保护降低，可能影响依赖 UAC 的应用。
- WPBT：DisableWpbtExecution=1，阻止 Windows 执行 Windows Platform Binary Table 提供的程序。不修改 BIOS/UEFI 固件、不卸载已有软件，也不全面阻止厂商应用或驱动安装。

导入识别实际完整原生命令和有效执行管线。修改、重复、部分或停用的内容保留原文，不能用开关覆盖；旧导入方案可迁移，显式保存的取消选择不被覆盖。

参考：[上游生成器](https://schneegans.de/windows/unattend-generator/) · [Microsoft：UAC 配置](https://learn.microsoft.com/en-us/windows/security/application-security/application-control/user-account-control/settings-and-configuration)
