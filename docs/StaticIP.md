# 静态 IP 配置（IPv4 / IPv6）

Static IP configuration (IPv4 / IPv6)

## 📖 操作 / Operation

在“网络 → IP 配置”通过铺满卡片宽度的分段条选择“IPv4 配置”或“IPv6 配置”，左右两项等宽，选中项显示圆角底色。支持键盘和设置搜索定位，适配浅色与深色主题。两种协议独立启用、共用一个目标网卡 MAC；切换页签和启用状态不会清空输入。默认均不启用。设置在目标 Windows 的 specialize 阶段生效，Windows PE 驱动及 Wi-Fi 连接另行配置。

Under Network → IP configuration, use the full-width selector with two equally sized segments, IPv4 configuration and IPv6 configuration. The selected segment has a rounded background, with keyboard/search navigation and light/dark theme support. Both protocols have independent enable switches and share one target MAC. Switching tabs or disabling a protocol retains entered values. Both are off by default. Settings apply during specialize in the target Windows installation; PE drivers and Wi-Fi connections are configured separately.

## 子网掩码/前缀长度 / Subnet mask/prefix length

从 V1.11 起，IPv4 使用一个“子网掩码/前缀长度”输入框，填写完整点分十进制掩码（如 255.255.255.0）或带斜线的前缀（如 /24），二者任选一种，不要只填写 24。没有第二个前缀框或输入方式开关。有效输入实时更新摘要、网段预览与导出；正在键入的 /、空白或无效内容原样保留供修正，不能沿用旧前缀导出。检查失败不会停用输入。

From V1.11, IPv4 uses one Subnet mask/prefix length input. Enter a full dotted-decimal mask such as 255.255.255.0, or a slash-prefixed length such as /24; do not enter bare 24. There is no second prefix field or mode switch. Valid input updates the summary, subnet preview and export. An unfinished /, blank or invalid text remains available for correction and cannot silently reuse the previous prefix. A failed check does not disable editing.

旧 JSON 方案按当时实际选用的前缀或掩码显示，未选用的旧值不影响生成；载入不会擅自套用示例或改写方案。新编辑的文本随 JSON 保存，包括格式和未完成输入。XML 始终使用原生地址/前缀格式；含完整本工具还原记录时保留所选输入格式，没有记录时根据实际原生前缀显示 /前缀。只读 XML 不可通过此框修改。

Older JSON plans display their previously active prefix or mask, ignoring inactive legacy values for generation. Loading does not apply examples or rewrite the plan. JSON saves newly edited text, including its format and incomplete edits. XML uses native address/prefix notation. A valid application recovery record retains the chosen input format; XML without that record displays the actual native prefix as /prefix. Read-only XML remains protected.

## 错误定位 / Error navigation

检查与预览及导出遇到已识别的字段错误时，红色原因下方显示设置位置，可点击“前往修改”切换页面、选择协议页并聚焦输入。定位不启用协议、不填写示例、不修改其他值。导入原文的阻止项指向配置方案页并提供“查看导入说明”；无法明确定位的错误保持原说明，不猜测到某个字段。

For recognized field errors during Check and Preview or export, the dialog shows the setting location below the red explanation. Go to edit switches pages/protocol tabs and focuses the input without enabling protocols, applying examples or changing other values. Source XML blockers point to the configuration page through View import details. Errors without an explicit destination retain their explanation without guessing a field.

## IPv4

支持完整点分十进制单播地址，不限于 192.168：10.0.0.0/8、172.16.0.0/12、192.168.0.0/16 及合法公网均可。按目标网络分配填写，示例不是可直接套用的地址。禁止全零、回环主机、组播/保留地址及省略段、十六进制、前导零等有歧义的写法。

Full dotted-decimal unicast addresses are accepted, including the private 10/8, 172.16/12 and 192.168/16 ranges and valid public addresses. Use addresses assigned for the target network. Zero, host-loopback, multicast/reserved and ambiguous abbreviated, hexadecimal or leading-zero forms are rejected.

本工具允许导出的静态地址前缀为 /1–/32，可在同一输入框填写等价连续子网掩码；摘要显示实际前缀及掩码。/1–/30 检查网络/广播地址；/31 用于点对点，两端均可作主机，网关可为另一端；/32 为单地址，本网关输入须留空，需要的其他路由另行配置。

Static address prefixes allowed for export range from /1 to /32. The same input accepts equivalent contiguous dotted masks. The summary shows the effective prefix and mask. Network/broadcast host checks apply through /30. /31 permits both point-to-point endpoints and the peer as gateway. /32 is a single address and requires an empty gateway field; configure other required routes separately.

| 输入 / Input | 掩码 / Mask | 用途 / Meaning |
| --- | --- | --- |
| /0 | 0.0.0.0 | 覆盖全部 IPv4 地址；显示提示并阻止导出 / Covers all IPv4 addresses; warning and export block |
| /24 | 255.255.255.0 | 常见局域网示例 / Common LAN example |
| /31 | 255.255.255.254 | 点对点 / Point to point |
| /32 | 255.255.255.255 | 单地址 / Single address |

## IPv6

接受压缩或展开的 IPv6 单播地址，导出规范化写法。按微软 UnicastIpAddresses/IpAddress/Value 无人值守设置文档，原生参数范围为 /0–/64；本工具允许导出 /1–/64，静态地址 /0 会提示并阻止导出，此原生输入不接受 /65–/128。地址不含 CIDR、方括号、IPv4 混写或 %区域。全零、回环主机及组播地址不能作静态主机地址。

Compressed and expanded IPv6 unicast forms are accepted and normalized for output. Microsoft documents /0–/64 for this native unattended IpAddress/Value setting. This tool allows /1–/64 for export, blocks a static address /0 with a warning, and rejects /65–/128. Do not include CIDR, brackets, mixed IPv4 notation or a zone suffix. Unspecified, host-loopback and multicast addresses cannot be used as the static host address.

网关可留空；填写时须为同前缀中的其他单播地址，或目标网卡可达的链路本地地址（如 fe80::1），不填 %区域。启用静态 IPv6 时显式关闭 DHCPv6 和路由器发现；留空网关不生成默认路由，不会依靠 RA 自动补齐。

The gateway is optional. It must be another unicast address in the same prefix, or a reachable link-local address such as fe80::1, without a zone suffix. Static IPv6 explicitly disables DHCPv6 and router discovery. An empty gateway field generates no default route and does not rely on RA to supply one.

2001:db8:a::20/64、网关 fe80::1、DNS 2001:db8:a::53 为文档示例，须替换为目标网络信息。

2001:db8:a::20/64, gateway fe80::1 and DNS 2001:db8:a::53 are documentation examples; replace them with the target network values.

## ⚠️ /0 部署检查 / Deployment check

静态 IPv4 的 /0（等价掩码 0.0.0.0）和静态 IPv6 的 /0 覆盖各自协议的全部地址范围，可能导致直连判断或网关通信异常。从 V1.10 起，配置页以红色实时提示，“检查与预览”及导出会停止并以红色说明原因。保留用户输入，须按实际网络修改，不自动转换成 /24 或 /64。关闭静态协议后，其未使用的 /0 输入保留且不应用；旧方案中未选用的兼容字段不参与检查。

Static IPv4 /0 (mask 0.0.0.0) and static IPv6 /0 cover the entire address space of their protocol and may cause incorrect on-link decisions or gateway communication problems. From V1.10, the configuration page displays a live red error, and Check and Preview and export stop with a red explanation. Enter the prefix assigned to the actual network; the tool retains your input and never substitutes /24 or /64. Disabled protocol values and inactive legacy compatibility fields are retained but not applied.

可完整识别的 /0 原 XML 仍可导入，保持原值与编辑基准，修正前缀或明确取消该栈后可重新导出。含 /0 的只读网络原文会给出提示，须先在原工具修正后重新导入。最终检查合并后的原生地址，不能用界面默认值绕过。此规则针对网卡静态地址，不限制正常默认路由的 IPv4 0.0.0.0/0 或 IPv6 ::/0。

Recognized source XML containing /0 remains importable and editable with its original value and baseline. Correct the prefix or explicitly remove that owned stack before exporting. If the network is read-only, correct it in the original tool and import it again. The final check examines merged native addresses, so default form values cannot bypass it. This rule applies to static interface addresses; normal IPv4 0.0.0.0/0 and IPv6 ::/0 default routes remain supported.

## 🔎 IPv4 网段预览 / IPv4 subnet preview

随地址、前缀或掩码输入实时显示网络前缀、掩码、广播地址及主机位范围；/31 显示两个点对点端点，/32 显示单地址，两者不显示广播地址。预览只按掩码计算范围，不核验真实网络分配、特殊用途地址、地址占用、网关或 DNS 可达性；可计算预览不代表配置整体通过校验。不会填写或修改地址、网关、前缀或掩码。未启用或只读网络不根据默认值显示预览。

The preview updates with the address, prefix or mask and shows the network prefix, mask, broadcast address and host-bit range. /31 shows the two point-to-point endpoints; /32 shows one address; neither has a broadcast entry. It calculates boundaries only and does not verify real network assignments, all special-purpose addresses, address availability, or gateway/DNS reachability. A calculated preview does not mean the whole configuration is valid. No values are filled or changed automatically. Disabled and read-only networks do not show a preview based on form defaults.

## DNS 与双栈 / DNS and dual stack

每种已启用协议最多三个不重复 DNS，空格、逗号或分号分隔。双栈共用一份原生 DNS 列表，先 IPv4 后 IPv6，按唯一递增数字键生成顺序。不配置 DNS 后缀、注册策略或解析服务，也不测试地址占用和服务可达性。

Each enabled protocol accepts up to three distinct DNS addresses separated by spaces, commas or semicolons. Dual stack uses one native DNS list, ordered IPv4 first and IPv6 second with unique increasing numeric keys. DNS suffixes, registration policies and resolver services are not configured. Address availability and service reachability are not probed.

## XML 导入 / XML import

完整识别的单 MAC、每协议一个地址、每协议最多一条默认路由及对应 DNS 可以编辑。大小写 MAC、等价 IPv6 写法、false/0、默认 versionScope 省略及明确数字 DNS 顺序键可识别。未编辑的网络保持原始文本、键及注释；明确修改时才规范化所拥有的网络组件。原文和基准不一致时，网络修改停止导出。

Editable native input is limited to one MAC, one address and at most one default route per protocol, plus corresponding DNS. Equivalent MAC/IPv6 spelling, false/0, omitted default versionScope and unambiguous numeric DNS keys are recognized. Unedited network components retain source text, keys and comments; explicit edits canonicalize only the owned network components. A network edit is blocked if the source no longer agrees with its baseline.

多网卡、别名、额外地址、额外策略、不同路由度量、DHCP/RA 混合流程、无法用两份协议列表表达的 DNS 交错顺序以及未知结构只读保留，避免丢失原配置。关闭一栈仅移除已识别、由本方案拥有的该栈配置；另一栈保留。关闭两栈仅影响新生成配置。IPv6 默认路由明确输出 ::/0，IPv4 沿用原生整数 0。本版只识别明确以 ::/0 指定的 IPv6 默认路由；数字 0 表示的 IPv6 路由前缀保留原文并只读。

Multiple adapters, aliases, extra addresses/policies, custom metrics, mixed DHCP/RA flows, interleaved DNS ordering that the two protocol lists cannot represent, and unknown structures remain read-only. Disabling one owned, recognized stack retains the other. Disabling both affects newly generated configuration only. IPv6 defaults explicitly use ::/0; IPv4 retains the native integer 0. Only an explicit ::/0 IPv6 default route is recognized for editing. Source IPv6 routes with a numeric 0 prefix are preserved read-only.

## 📚 Microsoft references

- https://learn.microsoft.com/en-us/windows-hardware/customize/desktop/unattend/microsoft-windows-tcpip-interfaces-interface-unicastipaddresses-ipaddress-value
- https://learn.microsoft.com/en-us/windows-hardware/customize/desktop/unattend/microsoft-windows-tcpip-interfaces-interface-ipv6settings
- https://learn.microsoft.com/en-us/windows-hardware/customize/desktop/unattend/microsoft-windows-tcpip-interfaces-interface-routes-route
- https://learn.microsoft.com/en-us/windows-hardware/customize/desktop/unattend/microsoft-windows-dns-client-interfaces-interface-dnsserversearchorder
