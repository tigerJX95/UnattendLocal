<div align="center">

**简体中文** | [English](./README.en.md)

</div>

# UnattendLocal · Windows 无人值守配置工具

UnattendLocal 是面向 Windows 10/11 的中文本地配置工具，以 Schneegans 的 UnattendGenerator 生成核心为基础，提供中文图形界面，方便创建、检查和导出 Windows 安装应答文件 `autounattend.xml`。

## 主要功能

- 配置语言、区域、账户、自动登录、网络及 OOBE 安装选项。
- 配置安装镜像、目标分区和 CompactOS 系统文件压缩。
- 调整任务栏、开始菜单、资源管理器、桌面外观及常用系统设置。
- 支持自定义安装脚本、JSON 方案保存与恢复，以及配置摘要和 XML 预览。
- 支持导入已有应答 XML，修改已支持的设置；对尚未支持的内容提示并尽量保留，遇到无法可靠合并的冲突时阻止导出。

**当前开发版本：V0.1 pre40。** 项目目前处于持续开发和测试阶段，正在逐步完善原作者功能的本地化与支持。部分配置仍需在目标 Windows 环境中验证。

## 引用与鸣谢

感谢 **Schneegans（GitHub：cschneegans）** 开发并开源 UnattendGenerator，为本项目提供 Windows 应答文件生成核心与功能参考。

- [原作者 GitHub 仓库](https://github.com/cschneegans/unattend-generator)
- [原作者在线应答文件生成器](https://schneegans.de/windows/unattend-generator/)
- [原项目 MIT 许可证](https://github.com/cschneegans/unattend-generator/blob/master/LICENSE.txt)

本项目为独立维护的中文本地化工具，非原作者官方发布版本。引用的上游代码保留原有版权声明和许可证。
