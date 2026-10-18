# Windows 注册信息 / Windows registration information

入口：“基础设置”页底部的“Windows 注册信息”。

- **设置注册所有者**：生成 `Microsoft-Windows-Shell-Setup/RegisteredOwner`。
- **设置组织名称**：生成 `Microsoft-Windows-Shell-Setup/RegisteredOrganization`。

两项独立，默认不勾选。不勾选时省略字段；勾选后允许留空，以明确生成空元素。取消只改变新配置，不清除已安装电脑上的信息。每项最多 256 字符；本工具按 .NET 字符串长度验证，补充平面字符占两位；不支持控制字符或首尾空白，不会自动修剪输入。

这些是 Windows 的注册信息，不创建或重命名登录账户，不修改计算机名、不加入域，也不影响激活或证明许可证所有权。新配置写在 specialize 阶段；仅生成配置，不修改当前电脑。

## XML 导入与方案

以实际原生 XML 为准，不从注释猜测。完整识别的单个所有者与单个组织字段可导入并修改；支持 specialize、oobeSystem、offlineServicing、generalize、auditUser，两个字段可位于不同阶段，编辑后仍保留原阶段。保留是否存在字段、明确空值以及是否省略的区别。

取消一个字段时保留另一个字段和同组件的其他设置。重复或跨阶段重复字段、额外结构、未识别阶段/属性、超出本工具输入范围的值保留原文，本区域明确只读。实际执行效果取决于目标 Windows、安装阶段是否运行及其他配置；请用匹配镜像验证。

旧导入方案只有在新增字段缺失且原文与编辑基准可可靠对应时迁移，同时保留待处理的其他修改。无法可靠迁移时保留方案并阻止导出，要求重新导入原 XML。原文省略的方案不能用界面默认值代替原文继续导出。

JSON 保存已输入的名称，包括未勾选时保留的输入；“省略凭据”不移除这些名称。预览不会遮蔽已启用的注册名称；未启用的名称不写入 XML 还原记录。分享方案或 XML 前，请按需要处理个人和单位信息。

## English

Find Windows registration information at the bottom of Basic settings. RegisteredOwner and RegisteredOrganization have independent switches, both off by default. Unchecked omits the field; checked with empty text writes an explicit empty element. The editor allows up to 256 .NET string code units per value and rejects control characters and leading/trailing whitespace.

These fields are registration information. They do not create or rename accounts, change the computer name, join a domain, activate Windows or prove license ownership. New configurations write them in specialize; the tool does not modify this computer.

Fully recognized single native fields are imported from specialize, oobeSystem, offlineServicing, generalize or auditUser and retain their individual original passes. Edits and removal preserve the other registration field and unrelated component settings. Duplicates, additional structures, unsupported values or unrecognized metadata remain visibly read-only with the original XML preserved.

Older imported plans migrate only when missing fields and their editing baseline can be matched reliably, retaining other pending changes. Otherwise export stops and requests a fresh XML import. Omitted source XML is never replaced by editor defaults.

JSON retains entered names even when unchecked or credentials are omitted. Enabled names are visible in previews; unchecked names are excluded from the XML restore note. Review personal and organization information before sharing. Validate using the target image in Windows SIM and a disposable VM; the selected installation pass must actually execute.

## Microsoft references

- [RegisteredOwner](https://learn.microsoft.com/en-us/windows-hardware/customize/desktop/unattend/microsoft-windows-shell-setup-registeredowner)
- [RegisteredOrganization](https://learn.microsoft.com/en-us/windows-hardware/customize/desktop/unattend/microsoft-windows-shell-setup-registeredorganization)

本功能接入微软已有的无人值守设置，不表示微软在本轮新增了这些设置。This integrates existing Microsoft unattended settings, not a newly announced Microsoft capability.
