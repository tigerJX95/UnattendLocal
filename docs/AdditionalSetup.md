# 辅助安装与系统恢复选项 / Additional setup options

V0.7 新增三项，均默认不勾选，在 OOBE 页配置。沿用固定生成核心，安装阶段不改名、不挪动。本工具不运行所生成的命令。

| 选项 | 安装阶段 | 作用与边界 |
| --- | --- | --- |
| 讲述人辅助安装 / Narrator-assisted setup | windowsPE、specialize、每位用户首次登录 | 启动 Narrator 并配置辅助功能项；需介质、语音及音频支持，安装后可手动调整，不自动关闭 |
| 允许卸载 Edge / Make Edge uninstallable | specialize | 运行 MakeEdgeUninstallable.ps1，尝试修改 IntegratedServicesRegionPolicySet.json 的对应策略；不卸载 Edge 或 WebView2，受版本、权限及更新影响 |
| 关闭 C 盘系统还原 / Disable System Restore on C: | 全机首次登录 | Disable-ComputerRestore -Drive 'C:\'；可能删除已有还原点，不关闭其他盘、WinRE、文件历史记录或所有备份 |

三项与首次自动登录独立。首次登录选项需实际运行该阶段；取消仅影响新生成配置，不撤销已安装系统设置。未勾选表示不添加覆盖，不表示强制开启系统还原或强制关闭讲述人。

支持 JSON、摘要、推荐差异和单项恢复、动态帮助、预览失效及 XML 导入编辑。导入基于实际有效载荷、阶段调用和脚本提取，不单凭表单参数或快照勾选。部分、修改、重复、停用、不同阶段的脚本保留原文，开关不覆盖未可靠识别的内容。

讲述人必须完整识别 PE、specialize、UserOnce 三部分，以及默认用户配置加载/执行/卸载及 RunOnce 注册。完整原生讲述人启动命令可免于单独的 PE 风险提示；磁盘操作、未知及被修改的 PE 命令仍触发核对。旧导入 JSON 缺失新字段时从原 XML 迁移状态及编辑基准，保留待保存修改；显式 false 不被覆盖。

All three settings default to off. They use the fixed native payloads and stages, and do not change the current computer. Import verifies full scripts and their active pipelines before exposing an editable switch; ambiguous content stays intact. Older imported JSON plans migrate missing fields and their baseline while retaining pending edits and explicit choices.

参考：

- 固定源码：vendor/UnattendGenerator/modifier/Accessibility.cs、modifier/Optimizations.cs、resource/MakeEdgeUninstallable.ps1。
- [Disable-ComputerRestore / Microsoft Learn](https://learn.microsoft.com/en-us/powershell/module/microsoft.powershell.management/disable-computerrestore?view=powershell-5.1)
- [生成器说明](https://schneegans.de/windows/unattend-generator/)
