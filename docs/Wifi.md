# 无线网络 / Wi-Fi

V0.8 保持 WPA2 兼容，新增 WPA3-Personal（AES，启用过渡模式）、开放网络及完整 WLANProfile XML。推荐无线场景仍采用 WPA2，不强制更改既有配置。

| 模式 | 认证与凭据 | 连接方式 |
| --- | --- | --- |
| WPA2-Personal（AES） | WPA2PSK / AES，8–63 位可打印 ASCII 密码 | 参数决定隐藏网络与自动连接 |
| WPA3-Personal（AES，启用过渡模式） | WPA3SAE / AES，沿用原生 transitionMode，8–63 位可打印 ASCII 密码 | 需目标系统、网卡驱动及接入点支持 |
| 开放 Wi-Fi（无密码、不加密） | open / none，不写 sharedKey，不使用保留密码 | 不加密无线链路，可能仍需门户认证 |
| WLANProfile XML | 完整 XML 决定认证及凭据 | XML 的 connectionMode 决定是否连接，上方参数不参与生成 |

开放网络是原生成器已有的 Open 认证方式，不是本工具自行扩展的认证机制。它不提供无线链路加密，仅在确认网络和用途后使用；个人网络优先选择 WPA2 / WPA3。

名称按微软文档 WPA3-Personal with transition mode 表述为“WPA3-Personal（AES，启用过渡模式）”。网络也支持过渡模式时，可兼容仅支持 WPA2-PSK 的客户端；实际连接取决于系统、驱动及接入点。

SSID 为 1–32 UTF-8 字节；配置名称和 SSID 不接受控制字符、双引号、美元符号、反引号，以兼容固定安装脚本。XML 自动连接时配置名称需与 SSID 一致，这是当前原生连接脚本的限制；名称不同时可设置 connectionMode=manual 并在目标系统手动连接。

在已配置网络的 Windows 上可用 `netsh wlan export profile key=clear` 导出 WLANProfile。导出文件可能含明文密码，请妥善保管。加密密钥不能可靠迁移；XML 结构合法并不保证目标 Windows 支持对应认证、具备证书或安装无线驱动。

XML 编辑器支持文件导入、粘贴和检查；限制为 512 Ki 字符，拒绝 DTD、处理指令、过深及过多元素，使用固定生成核心的 WLAN schema 验证。结构检查不连接无线网络，不安装驱动或证书。

省略凭据时清空个人网络密码及完整无线 XML，包括未启用模式中保留的 XML；XML 模式标记待重新导入。选择保存凭据时保留完整内容。预览整体遮蔽 Wifi.xml，实际导出不改变安装数据；任意自定义脚本中的秘密仍需自行核对。

XML 导入核对实际 Wifi.xml、服务等待、配置导入、连接命令，以及脚本提取和 specialize 执行管线。仅完整原生参数配置还原成 WPA2/WPA3/开放参数；其他可验证配置使用 XML 模式保留。修改、重复、缺失、停用或不同阶段的无线脚本保留原文并拒绝网络页覆盖。旧导入 JSON 的新字段缺失时，可迁移原 XML 中现已支持的无线配置及基准，保留待处理的其他修改；显式保存的取消和待处理网络模式编辑不被替换。

WPA3, open Wi-Fi, and WLANProfile XML use the fixed native generator. Existing WPA2 plans and the WPA2 recommendation remain compatible. XML owns its authentication and connection settings. Saving without credentials omits the whole XML profile, including inactive retained profiles; preview hides it completely. Import checks active native scripts before allowing edits, preserving ambiguous payloads.

参考：

- [原生成器](https://schneegans.de/windows/unattend-generator/)
- [WLAN sharedKey / Microsoft Learn](https://learn.microsoft.com/en-us/windows/win32/nativewifi/wlan-profileschema-sharedkey-security-element)
- [WPA3-Personal（AES，启用过渡模式） transition profile / Microsoft Learn](https://learn.microsoft.com/en-us/windows/win32/nativewifi/wpa3-personal-transition-profile-sample)
- 固定源码：vendor/UnattendGenerator/modifier/Wifi.cs、resource/WLAN_profile_v1.xsd。
