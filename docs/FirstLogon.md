# 首次登录 / First logon

## 💡 内置账户说明

选择此方式会启用内置 Administrator，并沿用原生成器的首次自动登录及次数重置流程；本工具不在之后自动停用该账户。请自行安排后续使用。Base64 只是可还原的编码，保存或分享实际 XML 前需核对凭据。

本选项仅在自动配置本地账户时生效。旧方案中的普通自动登录设置保持；旧方案若包含此前未还原的内置账户原文，需要重新导入原 XML。无法可靠识别的登录次数、域信息、密码不一致或执行链修改会保留原文并限制编辑。

## 💡 Built-in account behavior

This option enables the built-in Administrator and uses the fixed generator's first-logon and counter-reset flow. The tool does not automatically disable that account afterward; plan its subsequent use. Base64 is reversible encoding. Review credentials before saving or sharing the actual XML.

The option applies only to automatic local-account configuration. Ordinary sign-in settings in older plans are retained. Older saved plans containing a previously unrecognized built-in-account flow require reimporting the original XML. Unrecognized logon counts, domain information, conflicting passwords and altered execution chains remain preserved with restricted editing.

## 使用步骤 / Steps

在“用户与密码”选择“自动创建本地账户”，再选择首次登录方式。使用内置 Administrator 时在专用密码框填写密码，或清空密码框后明确允许空密码。若不使用内置账户首次登录，本地账户列表仍须至少有一个管理员。不自动登录时，保留安装隐私文件的确认规则继续适用。

Select automatic local-account configuration on the Accounts page, then choose the first-logon mode. For built-in Administrator, enter its password in the separate field or explicitly allow an empty password after clearing that field. Other modes still require an administrator in the local-account list. No automatic sign-in still requires the existing sensitive-file retention consent.

微软说明 / Microsoft reference:
- https://learn.microsoft.com/en-us/windows-hardware/customize/desktop/unattend/microsoft-windows-shell-setup-autologon-username
- https://learn.microsoft.com/en-us/windows-hardware/customize/desktop/unattend/microsoft-windows-shell-setup-useraccounts-administratorpassword
