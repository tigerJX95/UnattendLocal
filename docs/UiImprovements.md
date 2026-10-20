# 界面操作说明

## 📖 安装目标提示

导出前检查区与配置摘要将磁盘操作和 Windows 安装目标分别说明：自动分区不等于必然已选好安装目标；指定已有分区不等于自动分区；InstallToAvailablePartition 表示安装程序选择可用分区。没有指定目标时，提示“可能需要选择”，不宣称一定手动选盘。

生成后的预览顶部及导出危险操作提示读取实际 XML 的目标配置。导入的保留目标不能被界面的手动模式默认值误判为尚未指定。复杂、不完整或重复目标要求核对原文；提示不代替镜像、目标盘和真实安装验证。

## 🎨 可视化调色板

“配色与背景”保留常用强调色列表与 HEX 输入。点击强调色或纯色背景色块，打开独立选色窗口：

- 拖动二维色板调整饱和度和亮度，直接点击或拖动色相彩条选中该处颜色，不再显示独立的下方色相滑条；上方色板同步，可继续调整深浅。
- RGB 分量和 #RRGGBB 双向同步，预览即时更新；无效 HEX 不能作为自定义色确定。
- 色板可用方向键调整，Shift 加大步长，色相条支持方向键、Home/End，Shift 加大步长；RGB 滑块与 HEX 输入也可通过键盘操作。
- 确定才写回方案；取消、Esc 或关闭窗口不改变打开前的值。
- “恢复 Windows 默认”先进入待确认状态，再点击确定生效。强调色对应恢复整组 Windows 默认配色，纯色背景对应恢复 Windows 默认背景；保留原填色与其他页面内容。

本功能选择目标 Windows 的初始颜色，不改变本工具主题。窗口沿用工具的浅色/深色配色。JSON、XML 的已有颜色字段及生成语义保持，未增加透明通道或颜色名称支持。

## 💡 文件夹位置示意

在“桌面与文件夹 → 开始菜单电源按钮旁的文件夹”标题右侧，点击图片图标查看离线位置示意。高亮区域位于 Windows 11 开始菜单底部、电源按钮左侧，与固定应用区和任务栏不同。

图形为本地静态示意，明确标为非实机截图，仅用文档与下载作示例，不代表当前已勾选这两项。仅查看说明，不修改勾选、联网或截取屏幕。实际布局可能随 Windows 版本变化。

## 📦 脚本与应用列表

脚本导入和另存按钮采用不同方向的文档箭头。提示分别为“导入脚本文件，新增条目并默认停用”“导出选中脚本内容到文件”。导入后新增停用条目，另存输出选中条目的脚本内容。

应用移除使用双列复选框，详细影响说明在悬停提示中；“匹配项：”独立起行，下方标识逐项换行；说明仍参与搜索，筛选不会清除隐藏项目的选择。保持原有默认不勾选、批量选择确认和未知脚本只读保护，不改变实际移除脚本。

应用描述使用独立的纯文本提示模板，避免被全局“标题＋正文”模板当作对象解析而显示空框。选色底部三个按钮统一使用标准图标布局、专用图形和单行提示；恢复默认不再被过大的内边距裁切。

## 📚 References

- https://learn.microsoft.com/en-us/windows-hardware/customize/desktop/unattend/microsoft-windows-setup-imageinstall-osimage-installto
- https://learn.microsoft.com/en-us/windows-hardware/customize/desktop/unattend/microsoft-windows-setup-imageinstall-osimage-installtoavailablepartition
- https://support.microsoft.com/zh-cn/windows/experience/personalization/customize-the-windows-start-menu
