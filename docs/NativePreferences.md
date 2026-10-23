# 首选电源计划与 Windows 错误报告

两项设置均位于 **OOBE** 页，可通过设置搜索定位。默认“不指定”，不写入对应覆盖；本工具不读取或修改当前电脑的电源或错误报告设置。

## 首选电源计划

选择平衡、高性能、节能，或填写已有电源计划的 GUID。本功能生成 `specialize` 阶段的 `Microsoft-Windows-powercpl/PreferredPlan`。

| 计划 | GUID |
| --- | --- |
| 平衡 | `381b4222-f694-41f0-9685-ff5bb260df2e` |
| 高性能 | `8c5e7fda-e8bf-4a96-9a85-a6e23a8c635c` |
| 节能 | `a1841308-3541-4fab-bc81-f71556f20b4a` |

自定义值须为 36 个字符的 GUID，不带大括号、空格或命令参数，不接受全零值。可用目标系统的 `powercfg /list` 查询计划标识；当前电脑有此计划不代表目标安装镜像也有。

指定计划须已经存在于目标镜像中，不会自动导入 `.pow` 文件，也不会创建缺失计划。不存在时可能无法应用或导致安装错误。高性能可能增加耗电与发热，节能可能降低性能。Modern Standby 设备通常仅允许平衡或基于平衡的计划；实际能力受硬件、镜像和厂商策略影响。

首选电源计划与 Windows 设置中的“电源模式”滑块不同。本功能不单独修改交流供电、电池、睡眠或合盖参数；这些默认行为取决于所选计划。安装后可用 `powercfg /getactivescheme` 核对实际活动计划。

## Windows 错误报告（WER）

本功能生成 `specialize` 阶段的 `Microsoft-Windows-ErrorReportingCore/DisableWER`。

| 界面选项 | XML |
| --- | --- |
| 不指定 | 不添加 DisableWER |
| 启用 Windows 错误报告 | `<DisableWER>0</DisableWER>` |
| 禁用 Windows 错误报告 | `<DisableWER>1</DisableWER>` |

启用不意味着强制上传所有报告，实际收集及发送仍受 Windows、组织策略和用户设置影响。禁用可能减少故障报告和相关解决方案信息，不等于关闭事件日志、全部诊断数据或第三方应用报告。

本功能不停止或卸载服务；不要仅以 WerSvc 是否运行判断设置是否应用。实际效果需使用目标 Windows 镜像验证。

## XML 导入与原文保留

- 完整识别的 specialize 阶段设置可以修改或取消；电源计划 GUID 大小写保留，避免无意义变更。
- 不认识的值、重复字段、额外属性、嵌套结构、错误命名空间及其他执行阶段只读保留，不使用界面默认值覆盖。PreferredPlan 官方也支持 generalize，但本页仅编辑 specialize，不转换原文件阶段。
- 同组件其他未识别的子元素继续保留。取消已识别设置只移除相应标量项，不删除整个组件的其他内容。
- V1.14 额外 XML 中已配置的组件继续由原条目管理，不自动转换为专用控件。需要切换编辑方式时，先停用相应额外 XML 条目，再在本页选择目标设置。
- 同一组件存在已启用额外 XML 与非默认专用设置时，阻止导出。可将专用选项改为“不指定”，或停用相应额外 XML 条目。

## JSON 与旧方案

普通 JSON 保存保留电源计划和 WER 选择，不将 GUID 当作密码省略。导入 XML 的完整原文仍受原有敏感内容保存选择控制；原文未保存时，需要重新导入才能继续导出。

旧 JSON 中原本只读保留、现在可完整识别的设置会迁移编辑基准。额外 XML 的编辑来源和已停用状态继续保留；无法确定待处理修改的迁移停止导出并要求重新导入原 XML，不静默替换当前修改。

## 建议核对

先在测试虚拟机选择“平衡”和一个明确的 WER 选项，检查导出的 XML 组件、架构与 specialize 阶段。安装后核对实际电源计划及错误报告行为。使用自定义 GUID、Modern Standby 或组织策略环境时，另行确认目标支持情况。

结构检查不代替 Windows SIM 对目标镜像的检查及实际部署验证。

## 微软官方文档

- [PreferredPlan](https://learn.microsoft.com/windows-hardware/customize/desktop/unattend/microsoft-windows-powercpl-preferredplan)
- [DisableWER](https://learn.microsoft.com/windows-hardware/customize/desktop/unattend/microsoft-windows-errorreportingcore-disablewer)
- [Power policy settings / Modern Standby](https://learn.microsoft.com/windows/win32/power/power-policy-settings)

## English

The OOBE page provides optional native power-plan and WER settings. Neither overrides Windows defaults unless explicitly selected. PreferredPlan requires an existing target plan GUID and does not import plans or change the Windows Power mode slider. DisableWER uses 0 for enabled and 1 for disabled; it does not disable all diagnostics or stop services.

Recognized specialize settings can be edited or cleared while preserving unrelated component content. Unknown or duplicate settings remain read-only. Existing V1.14 extra-XML owners and pending disabled entries retain their original editing source. Validate target-image support and actual deployment behavior.
