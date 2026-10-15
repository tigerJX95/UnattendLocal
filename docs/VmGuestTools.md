# 虚拟机集成工具

| 选项 | 挂载介质中需要的内容 | 执行阶段 |
| --- | --- | --- |
| VMware Tools | 产品名为 VMware Tools 的 setup.exe | 全机首次登录 |
| VirtualBox 增强功能 | VBoxWindowsAdditions.exe、cert 目录 | 全机首次登录 |
| VirtIO Guest Tools | virtio-win-guest-tools.exe | 全机首次登录 |
| Parallels Tools | PTAgent.exe | 全机首次登录 |

四项默认关闭，沿用固定生成核心的安装脚本和静默参数；安装前从虚拟机平台挂载对应官方工具介质，确认 Windows 版本/架构兼容。脚本扫描 D:–Z: 光驱/盘符，缺少介质时输出提示，不下载、不安装。不附带第三方安装器。

VirtualBox 脚本从介质导入工具证书。Parallels 部分功能需重启。VirtIO 的首次登录工具不替代 Windows PE 驱动，安装阶段无法识别磁盘或网络时需另行准备驱动；导入 XML 中已有 PE 驱动脚本按原文保留。

只编辑完整识别的原生首次登录调用与助手脚本。修改、缺失、重复、停用、执行阶段不同的内容保留原文，不能用开关覆盖。取消不卸载已安装工具，不删除导入 XML 的未知内容。

固定核心依据：[原生成器](https://schneegans.de/windows/unattend-generator/)、[使用说明](https://schneegans.de/windows/unattend-generator/usage/)，以及 vendor/UnattendGenerator 内对应资源文件。
