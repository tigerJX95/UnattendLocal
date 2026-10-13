upstream-import.xml：2026-10-13从原作者网站download接口获取的回归样例，包含尚未在界面实现的DisableUac设置及已支持的EnableLongPaths。URL与生成参数保留在样例注释中。账户ImportFixture、密码FixtureOnly123仅为合成测试数据。测试仅读取和比较XML，不执行其中任何安装或注册表命令。
来源：https://schneegans.de/windows/unattend-generator/

upstream-compact.xml：2026-10-13从官网download接口生成，含原作者Generated Windows PE脚本及CompactOs=true。账户CompactFixture/密码FixtureOnly123为合成数据。样例含清空/格式化磁盘命令，回归只读取比较，绝不执行；用于验证实际/Compact识别、只读保留、原文风险确认及无效覆盖阻止。
