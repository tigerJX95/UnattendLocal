# 快捷预设与额外 XML

## 快捷预设

“配置方案”页提供两个独立起点。新建前确认放弃未保存修改，之后仍可通过全部分类页调整。

- **最小配置**：安装时交互选择语言、版本、分区及账户；不沿用有线或 Wi-Fi 推荐策略，不添加应用移除、硬件检查绕过等选项。默认保留安装隐私文件，以避免首次登录清理脚本；这些文件可能含凭据，可在 OOBE 页调整。此预设不是 XML 冗余清理，也不会删除原作者恢复信息。
- **仅创建本地账户**：沿用原作者示例，创建名为 Example 的管理员账户，允许空密码并首次自动登录一次，之后按既有生成流程清理安装隐私文件。账户名、密码、权限均可修改；并非只创建账户而完全没有首次登录辅助逻辑。

## 额外 XML 组件

入口位于“自定义脚本”页下方，也可搜索“额外 XML”“OEMInformation”。

1. 点击“添加 XML 组件”，选择组件和对应执行阶段。
2. 填写组件内部的设置子元素，不包含 `unattend`、`settings` 或 `component` 包装。
3. 使用“检查额外 XML”检查片段结构，然后勾选启用及核对确认。
4. 使用“检查与预览”核对与当前生成内容的重复项，最后导出 XML 或 ISO。

新增条目默认停用。最多 15 项，每项最多 131,072 个字符、64 层和 2,048 个元素。允许标准 `wcm` 和 `xsi` 命名空间属性；禁止 DTD、外部实体、处理指令及其他元素命名空间。

组件及其阶段来自当前固定的原作者目录。选用某阶段不会自动触发 Sysprep、审核模式或脱机维护。每个参数是否受目标 Windows 版本、版本类型和架构支持，仍需查阅微软文档并用目标镜像在 Windows SIM 中核对。

## 保留与冲突

额外 XML 只添加独立的顶层设置。它可以给已有组件增加尚未配置的子元素，也可以添加尚未生成的组件。

已有界面管理的原生设置（如账户、语言、磁盘目标及注册所有者）应继续通过原控件配置，不通过此入口建立第二个编辑来源。

如果某个顶层设置已由界面、生成核心或另一条额外 XML 生成，则停止导出；不替换、删除或自动合并已有嵌套列表。相关设置请优先通过现有控件修改。此行为与原作者允许整组件覆盖的高级入口有所区别。

导入第三方 XML 时，仍按已有规则保留未识别原文，不自动把所有未知内容转换为额外 XML 条目。本程序完整导出的 XML 可通过配置还原记录恢复编辑列表；外部修改导致记录失效时，仍以实际 XML 为准，未识别部分保留。

## OEM 支持信息示例

“载入 OEM 支持信息示例”新增一个停用条目：

- 组件：`Microsoft-Windows-Shell-Setup`
- 阶段：`specialize`

```xml
<OEMInformation>
  <SupportProvider>示例技术支持</SupportProvider>
  <SupportURL>https://example.com/support</SupportURL>
</OEMInformation>
```

先替换示例名称和网址，再启用。示例配置支持提供者及支持网站，不用于改写硬件厂商、型号、Logo 或已弃用的联系电话字段。

## 保存与隐私

额外 XML 可能包含凭据、安装命令及敏感配置。界面预览整体遮蔽启用片段，预览内容不能用于安装；实际导出的 XML 和 ISO 保留原配置。

JSON 选择保存敏感内容时保留片段。选择不保存时清空片段，启用状态保留；载入后需补填才能导出，不会静默跳过原本启用的设置。更改条目、组件、阶段、内容或架构会取消确认，需要重新核对。

## 阅读与排版

页面说明按用途、操作和限制分段，悬停提示补充细节，避免整段重复。说明保持合理行距和段间距；命令、路径和用户输入原文不应用自动分段。后续页面与悬停提示沿用此规范，具体项目约定见根目录 AGENTS.md。

## 官方来源

- [Schneegans UnattendGenerator 官网](https://schneegans.de/windows/unattend-generator/)
- [原作者额外 XML 组件实现](https://github.com/cschneegans/unattend-generator/blob/master/modifier/Components.cs)
- [微软 OEMInformation](https://learn.microsoft.com/windows-hardware/customize/desktop/unattend/microsoft-windows-shell-setup-oeminformation)
- [微软应答文件与 Windows SIM](https://learn.microsoft.com/windows-hardware/manufacture/desktop/update-windows-settings-and-scripts-create-your-own-answer-file-sxs)

## English

The Configuration page provides minimal and local-account starting points. The advanced editor is below Custom scripts and accepts up to 15 component/pass XML fragments. New entries and the OEM example start disabled. It adds independent child settings and refuses duplicate top-level settings instead of replacing generated components. The catalog is pinned to the bundled upstream generator; Windows SIM and target-image installation checks remain necessary.

Preview redacts custom XML. JSON saved without sensitive content omits fragments while retaining their enabled state, requiring re-entry before export. Own exported XML can restore editor entries through its intact configuration snapshot. Unknown third-party XML remains preserved through the existing import workflow.
