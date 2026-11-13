# 快捷预设与额外 XML

## 快捷预设

“配置方案”页提供两个独立起点。新建前确认放弃未保存修改，之后仍可通过全部分类页调整。

- **最小配置**：安装时交互选择语言、版本、分区及账户；不沿用有线或 Wi-Fi 推荐策略，不添加应用移除、硬件检查绕过等选项。默认保留安装隐私文件，以避免首次登录清理脚本；这些文件可能含凭据，可在 OOBE 页调整。此预设不是 XML 冗余清理，也不会删除原作者恢复信息。
- **仅创建本地账户**：创建名为 Example 的管理员账户，允许空密码并首次自动登录一次，之后按既有生成流程清理安装隐私文件。账户名、密码、权限均可修改；并非只创建账户而完全没有首次登录辅助逻辑。

## 额外 XML 组件

入口位于“自定义脚本”页下方，也可搜索“额外 XML”“OEMInformation”。

1. 点击“添加 XML 组件”，选择组件和对应执行阶段。
2. 填写组件内部的设置子元素，不包含 `unattend`、`settings` 或 `component` 包装。
3. 使用“检查额外 XML”检查片段结构，然后勾选启用及核对确认。
4. 使用“检查与预览”核对与当前生成内容的重复项，最后导出 XML 或 ISO。

点击“导入 XML 片段”，选取文件后选择组件和执行阶段。阶段列表只显示该组件支持的选项。窗口预选当前条目的组件和阶段，未选中条目时预选 `Microsoft-Windows-Shell-Setup` 和 `specialize`。确认后新增一条停用草稿。文件只应包含设置子元素，可有多个同级子元素，省去 XML 声明和应答文件包装。支持 UTF-8，以及带 BOM 的 UTF-16、UTF-32。

选中条目后，可通过“另存 XML 片段”保存其中的设置子元素。文件使用 UTF-8，保留文本和换行。导入时选择与原片段匹配的组件和阶段。

新增条目默认停用。最多 15 项，每项最多 131,072 个字符、64 层和 2,048 个元素。允许标准 `wcm` 和 `xsi` 命名空间属性；禁止 DTD、外部实体、处理指令及其他元素命名空间。

组件及其阶段来自当前固定的原作者目录。选用某阶段不会自动触发 Sysprep、审核模式或脱机维护。每个参数是否受目标 Windows 版本、版本类型和架构支持，仍需查阅微软文档并用目标镜像在 Windows SIM 中核对。

## 保留与冲突

额外 XML 只添加独立的顶层设置。它可以给已有组件增加尚未配置的子元素，也可以添加尚未生成的组件。

已有界面管理的原生设置（如账户、语言、磁盘目标及注册所有者）应继续通过原控件配置，不通过此入口建立第二个编辑来源。

如果某个顶层设置已由界面、生成核心或另一条额外 XML 生成，则停止导出；不替换、删除或自动合并已有嵌套列表。相关设置请优先通过现有控件修改。

导入第三方 XML 时，仍按已有规则保留未识别原文，不自动把所有未知内容转换为额外 XML 条目。本程序完整导出的 XML 可通过配置还原记录恢复编辑列表；外部修改导致记录失效时，仍以实际 XML 为准，未识别部分保留。

## OEM 支持信息

在“自定义脚本 → 额外 XML”展开“填写 OEM 支持信息”。填写完整的 HTTP 或 HTTPS 支持网址，支持名称可留空。点击“生成 OEM 配置”，将条目添加到列表。检查生成的内容后，勾选该条目的启用项和额外 XML 确认项。支持名称最多 256 个字符，网址最多 2083 个字符。Windows 如何呈现支持信息取决于目标系统版本。

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

页面说明按用途、操作和限制分段，悬停提示补充细节，避免整段重复。说明保持合理行距和段间距；命令、路径和用户输入原文不应用自动分段。其他页面与悬停提示也采用相同的可读性要求。

## 官方来源

- [Schneegans UnattendGenerator 官网](https://schneegans.de/windows/unattend-generator/)
- [原作者额外 XML 组件实现](https://github.com/cschneegans/unattend-generator/blob/master/modifier/Components.cs)
- [微软 OEMInformation](https://learn.microsoft.com/windows-hardware/customize/desktop/unattend/microsoft-windows-shell-setup-oeminformation)
- [微软应答文件与 Windows SIM](https://learn.microsoft.com/windows-hardware/manufacture/desktop/update-windows-settings-and-scripts-create-your-own-answer-file-sxs)

## English

The Configuration page provides minimal and local-account starting points. The advanced editor is below Custom scripts and accepts up to 15 component/pass XML fragments. New entries and the OEM example start disabled. Expand **Enter OEM support information**, provide an HTTP or HTTPS support website and an optional support name, then create and review the entry. Enabling and confirming it adds `OEMInformation` during `specialize`. It adds independent child settings and refuses duplicate top-level settings instead of replacing generated components. The catalog is pinned to the bundled upstream generator; Windows SIM and target-image installation checks remain necessary.

Preview redacts custom XML. JSON saved without sensitive content omits fragments while retaining their enabled state, requiring re-entry before export. Own exported XML can restore editor entries through its intact configuration snapshot. Unknown third-party XML remains preserved through the existing import workflow.

Choose a file with **Import XML fragment**, then select the component and one of its supported passes. The dialog starts with the selected entry’s target, or `Microsoft-Windows-Shell-Setup` and `specialize` when no entry is selected. Confirm to add a disabled draft. Save the child elements as UTF-8 or BOM-marked UTF-16 or UTF-32, omitting the XML declaration and answer-file wrappers.

Use **Save XML fragment as** to save the selected entry’s child settings as a UTF-8 fragment. The file preserves text and line endings. Select the matching component and pass when importing it into another plan.
