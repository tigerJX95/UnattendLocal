# 静态 IPv4 / Static IPv4

## 📖 使用

在“网络 → 静态 IPv4（目标系统）”启用。默认不启用；输入示例只是占位提示，不自动写入方案。

| 字段 | 范围与含义 |
| --- | --- |
| 目标 MAC | 目标电脑/虚拟机网卡的单播 MAC，支持横线、冒号或连续 12 位；导出统一大写横线格式。 |
| IPv4 与前缀 | 一个点分十进制单播 IPv4，前缀 1–30；拒绝本子网的网络、广播地址。/31 和 /32 不在本版范围。 |
| 网关 | 可留空；填写时须为同子网的其他有效主机，生成一条默认路由。 |
| DNS | 可留空；最多三个不重复的 IPv4，以逗号、分号或空格分隔，按顺序编号。 |

配置通过 Microsoft-Windows-TCPIP / Microsoft-Windows-DNS-Client 在 specialize 阶段应用。不配置 Windows PE，不改变 IPv6，不创建 Wi-Fi 连接，不扫描当前电脑或探测地址占用。需目标网卡驱动、MAC 匹配与无地址冲突；自定义脚本或组织策略可能覆盖设置。网关留空不生成默认路由，DNS 留空不写入 DNS 覆盖，不保证名称解析或互联网连接。推荐场景不会重置用户单独填写的静态网络。

方案会保存 MAC、IP 和 DNS；“省略凭据”不会省略这些网络标识。分享前请核对。含新功能的方案请使用 V1.0 或更新版本，旧版可能忽略新字段。关闭选项只影响新生成配置，不恢复已安装电脑的网络。

完整符合本工具输出范围的实际 XML 可还原编辑；多网卡、IPv6、别名标识、DNS 后缀策略或改写过的流程只读保留。关闭已识别静态网络会从导出配置移除相应组件；未知网络内容不会被默认值覆盖。旧方案若没有新字段且原文是完整可识别配置，会补齐编辑基准并保留待处理修改；无法可靠迁移则要求重新导入原 XML。

## 📖 Usage and scope

Enable static IPv4 on the Network page. It is off by default; example placeholders are not saved values. Provide the target adapter's unicast MAC, one dotted-decimal IPv4 and a prefix of 1–30. The optional gateway must be another valid host in the same subnet. Up to three distinct IPv4 DNS addresses are accepted, separated by spaces, commas or semicolons.

Settings use the native TCPIP/DNS-Client components during specialize. They do not configure Windows PE, change IPv6, establish Wi-Fi or probe the current computer. Verify the target MAC, driver and address availability. Scripts and organization policies can override the result. An empty gateway generates no default route; empty DNS generates no DNS override and does not guarantee name resolution. Recommendations do not reset the explicit target network.

Plans retain MAC, IP and DNS even when credentials are omitted. Disabling the option affects new output, not an installed computer. Complete recognized XML is editable; multi-adapter, IPv6, aliases, extra policies or modified forms retain their original content with this editor read-only. Older plans migrate only when the source and baseline can be handled reliably, preserving pending edits; otherwise reimport is required.

## 参考 / References

- https://learn.microsoft.com/en-us/windows-hardware/customize/desktop/unattend/microsoft-windows-tcpip-interfaces-interface
- https://learn.microsoft.com/en-us/windows-hardware/customize/desktop/unattend/microsoft-windows-tcpip-interfaces-interface-identifier
- https://learn.microsoft.com/en-us/windows-hardware/customize/desktop/unattend/microsoft-windows-tcpip-interfaces-interface-routes-route
- https://learn.microsoft.com/en-us/windows-hardware/customize/desktop/unattend/microsoft-windows-dns-client-interfaces-interface-dnsserversearchorder
