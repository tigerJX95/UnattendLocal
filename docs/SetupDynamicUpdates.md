# 安装阶段动态更新 / Setup Dynamic Update

在“镜像与分区”页的“安装阶段动态更新”卡片设置。

- **遵循安装程序默认**：不写入 `Enable`，不等于明确启用。
- **启用动态更新**：写入 `Enable=true`，允许支持此设置的 Windows Setup 获取新的安装文件、驱动及其他安装所需文件。
- **禁用动态更新**：写入 `Enable=false`，仅控制该安装阶段，不关闭安装后的 Windows Update。

界面显示方式独立设置。“遵循默认显示行为”省略 `WillShowUI`；可明确选择 `OnError`（仅出错）、`Never`（不显示更新界面）或 `Always`（始终显示）。微软文档中 Enable 默认 false、WillShowUI 默认 OnError；本工具的默认选项均省略设置，保留介质、启动方式及安装程序的原行为。Never 仍可能显示严重错误，不会自动处理错误或重试。

设置写入 `windowsPE / Microsoft-Windows-Setup / DynamicUpdate`，例如：

```xml
<DynamicUpdate>
  <Enable>true</Enable>
  <WillShowUI>Never</WillShowUI>
</DynamicUpdate>
```

需目标安装程序支持此应答设置，并具备必要网络、驱动和访问条件；定制介质、组织策略或离线环境可能改变实际行为。启用不保证能取得更新，不自动配置 PE 网络、不下载到当前电脑、不修改安装镜像或 boot.wim。动态更新不能替代手动准备启动和存储驱动。更改更新方式会取消危险磁盘操作确认，须重新核对目标。

原生 XML 中完整识别的 Enable 和/或 WillShowUI 可恢复并继续编辑；默认值与明确填写的值分别保留。只认实际 XML，不依据注释猜测。未知字段、重复项、不同结构、不能识别的值或自定义 PE/DISM 流程保持原文，并在此区域显示只读。无法可靠迁移旧方案的编辑基准时阻止导出，保留待处理修改，要求重新导入原 XML。

更改此项不会联动自动登录、隐私文件保留或安装后的更新策略。使用匹配镜像在 Windows SIM 验证后，在一次性虚拟机核对联机/离线安装及界面行为。

## English

Find **Setup Dynamic Update** on the Image and partitions page. Leave the enable setting unspecified, explicitly enable it or disable it. Configure UI independently using the unspecified default, OnError, Never or Always. Both defaults omit their XML fields; Microsoft documents Enable=false and WillShowUI=OnError. Never can still allow a critical error message and does not handle errors automatically.

This writes native `windowsPE / Microsoft-Windows-Setup / DynamicUpdate` settings. It can obtain updated installation files and drivers when supported by the target Setup program and environment. It does not control Windows Update after installation, configure PE networking, download to this computer, modify boot.wim or replace preparation of boot/storage drivers. Changing the update mode clears destructive-disk consent for another review.

Fully recognized native Enable and/or WillShowUI settings are restored for editing. Unknown fields, duplicate or unrecognized structures/values and custom PE/DISM flows retain their original XML with a visible read-only state. Older imported plans migrate only when their editing baseline can be matched reliably; otherwise export stops while pending edits are retained. Automatic logon and privacy-file retention remain independent.

Actual support depends on installation media, Setup version, connectivity and policy. Validate against the target image in Windows SIM and test online/offline installation in a disposable VM.

## Microsoft references

- [DynamicUpdate](https://learn.microsoft.com/en-us/windows-hardware/customize/desktop/unattend/microsoft-windows-setup-dynamicupdate)
- [Enable](https://learn.microsoft.com/en-us/windows-hardware/customize/desktop/unattend/microsoft-windows-setup-dynamicupdate-enable)
- [WillShowUI](https://learn.microsoft.com/en-us/windows-hardware/customize/desktop/unattend/microsoft-windows-setup-dynamicupdate-willshowui)

以上为微软已有的无人值守能力在本工具的本地接入，不表示微软在本轮新增加了这些设置。This integrates an existing Microsoft unattended-setting capability; it is not a claim of a newly released Microsoft feature.
