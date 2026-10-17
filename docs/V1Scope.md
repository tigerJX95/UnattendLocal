# V1.0 定位 / Release scope

V1.0 采用“成熟核心功能版”定位：以固定 UnattendGenerator 核心为基础，提供常用 Windows 安装选项的中文本地界面、可复用方案、可靠的配置导入、检查与导出。固定基线与选择性同步见 UPSTREAM.md。

已包含账户与首次登录、语言区域、镜像分区、Wi-Fi 与单网卡静态 IPv4、常用系统与外观、应用移除、自定义脚本等主要流程。默认行为保持保守，危险磁盘操作需明确确认；未知 XML 保留原文，无法可靠合并时停止导出。

V1.0 不等于覆盖原作者全部高级设置。按条件筛选目标磁盘、更复杂的 PE 安装流程、多网卡与高级网络策略可在后续版本继续完善。当前固定 vendor 不因本次版本号升级而变更。

自动化检查验证生成、导入合并、方案与状态联动，不代替 Windows 安装实测。目标镜像、驱动、固件、网卡及组织策略需要在实际部署环境核对；本环境的 Windows UI 与安装验证状态见根目录 验证结果.txt 和 VisualStudio使用说明.md。

V1.0 is the core-feature release: a Chinese local interface, reusable plans and reliable configuration import/check/export built on a fixed UnattendGenerator core. It includes common setup, account, network, appearance, app-removal and script workflows without claiming complete coverage of every upstream advanced setting.

Conditional disk targeting, more complex PE workflows and multi-adapter network policies may follow later. Automated checks validate configuration behavior and do not substitute for installation testing with the target image, drivers, firmware and policies. The fixed vendor baseline remains unchanged.
