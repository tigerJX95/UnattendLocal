# 工作组名称 / Workgroup name

在“网络”页勾选“指定工作组名称”。默认不勾选；启用后生成 `Microsoft-Windows-UnattendedJoin/Identification/JoinWorkgroup`，在 `specialize` 阶段应用。

本版采用跨语言部署时较保守的输入范围：1–15 个英文字母、数字或连字符，首尾为字母或数字，至少包含一个字母。这是本工具支持范围，不能当作 Windows 所有合法 NetBIOS 名称的完整规则。超出范围的原 XML 保留为只读。工作组名不能与已知的固定计算机名相同；动态名称需安装时核对。

工作组不同于 Active Directory 域。此项不会加入域、创建账户、自动开启共享或网络发现。已有域加入、加入凭据、离线加入、额外结构或重复字段只读保留；不会同时写入 JoinDomain 和 JoinWorkgroup。未修改时保留原文，无法可靠合并时停止导出。

支持 JSON 保存、摘要、帮助、搜索和原生 XML 导入后修改/取消。取消已识别工作组后清理对应空组件，可重新导入并再次启用。旧方案仅在字段缺失且编辑基准可靠时迁移，保留其他待处理修改；无法迁移时要求重新导入。工作组名称不属于密码凭据，省略凭据保存时仍保留，预览不遮蔽。

Use the Network page to specify a workgroup during `specialize`. The option defaults to off. This version supports 1–15 ASCII letters, digits or hyphens, with alphanumeric ends and at least one letter. This deliberately limited input range is not the full set of legal Windows NetBIOS names. A known fixed computer name must differ from the workgroup name; check dynamically generated names at installation time.

This does not join Active Directory, create users or enable file sharing/discovery. Imported domain joins, credentials, offline joins, additional structures and duplicates remain read-only. Recognized workgroups can be edited or cancelled, including reimport and re-enabling after cancellation. Saved-plan migration preserves pending edits and stops when the baseline cannot be matched reliably. Workgroup names remain visible in saved plans and previews.

依据 / References:

- [JoinWorkgroup](https://learn.microsoft.com/en-us/windows-hardware/customize/desktop/unattend/microsoft-windows-unattendedjoin-identification-joinworkgroup)
- [NetrValidateName2：名称范围及同名检查](https://learn.microsoft.com/en-us/openspecs/windows_protocols/ms-wkst/dee217f5-68f4-4d2f-84b6-cea620898bf9)

本版接入微软已有设置，不表示微软在本次版本新增该能力。生成结构检查不能代替目标 Windows 安装验证。

This integrates an existing Microsoft setting. Generated XML checks do not replace installation testing.
