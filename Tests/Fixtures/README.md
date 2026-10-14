upstream-import.xml：2026-10-13从原作者网站download接口获取的回归样例，包含尚未在界面实现的DisableUac设置及已支持的EnableLongPaths。URL与生成参数保留在样例注释中。账户ImportFixture、密码FixtureOnly123仅为合成测试数据。测试仅读取和比较XML，不执行其中任何安装或注册表命令。
来源：https://schneegans.de/windows/unattend-generator/

upstream-compact.xml：2026-10-13从官网download接口生成，含原作者Generated Windows PE脚本及CompactOs=true。账户CompactFixture/密码FixtureOnly123为合成数据。样例含清空/格式化磁盘命令，回归只读取比较，绝不执行；用于验证实际/Compact识别、只读保留、原文风险确认及无效覆盖阻止。

upstream-system-options.xml：2026-10-14 从原作者网站 download 接口生成，启用 DisableAutomaticRestartSignOn、PreventDeviceApps、DeleteWindowsOld，并保留独立的首次自动登录。SystemOptionsFixture / FixtureOnly123 是合成测试账户及密码。用于实际 XML 还原、往返保留和取消设置检查；测试不执行注册表、目录清理或其他安装命令。

upstream-image-backgrounds.xml：2026-10-14 从官网 download 接口通过无账户、无密码的合成参数生成。WallpaperScript 和 LockScreenScript 返回占位字节数组，仅供检查生成、识别、往返修改和取消；不是有效图片，不用于真实安装，不执行其中的命令。

upstream-app-removal.xml：2026-10-14 从官网 download 接口通过无账户、无密码合成参数生成，选择 Photos/Teams/Copilot/OneDrive/Notepad/Recall。验证实际移除 helper 和侧效应的识别、往返、增删及全部取消，不执行其中的命令。

upstream-advanced-system.xml：2026-10-14 从官网 download 接口通过无账户、无密码合成参数生成，选择 EnableRemoteDesktop/AllowPowerShellScripts。验证原始命令、阶段、完整识别、往返与取消，不执行其中的命令。
