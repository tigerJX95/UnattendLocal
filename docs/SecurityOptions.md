# 应用与内核保护、系统盘权限

四项均来自固定原生生成核心，默认不勾选；不勾选表示不添加覆盖，而非强制启用。只在目标安装过程中应用，不修改当前电脑。

| 设置 | 目标阶段 | 作用及限制 |
| --- | --- | --- |
| 关闭智能应用控制（SAC） | specialize | VerifiedAndReputablePolicyState=0；Windows 11 的应用信任保护降低，重新开启可能需重置或重装。 |
| 关闭 Windows 与 Edge 的 SmartScreen | specialize、默认用户 | Windows/WTDS 相关信誉、钓鱼与密码风险项、默认用户的 Edge/网页内容评估项，并隐藏安全中心托盘图标；不关闭 Defender 防病毒引擎。 |
| 关闭核心隔离（VBS / 内存完整性） | specialize | 设置 VBS 与 HVCI 相关注册表项为关闭；不卸载 Hyper-V、WSL2、Docker 或虚拟机工具，也不调整固件虚拟化，不保证性能收益。 |
| 收紧 C 盘根目录写入权限 | specialize | 执行 `icacls.exe C:\ /remove:g "*S-1-5-11"`；移除已验证用户组在 C 盘根目录的显式允许权限，不递归重置整个磁盘 ACL。其他组仍可能授予写入，旧软件可能受影响。 |

系统版本、组织策略、固件锁定或后续更新可能影响结果。取消勾选只改变新配置，不还原已安装系统；不要把关闭保护作为默认优化。恢复推荐会取消这些覆盖。

JSON 保存两种凭据模式均保留四项选择。XML 导入以实际命令及提取/调用管线为准，只有完整识别的固定原生组才开放编辑；缺失、重复、条件执行、不同阶段或被修改的命令保留原文并阻止不可靠覆盖。旧版导入 JSON 可补齐新状态与编辑基准，保留无关待处理修改和显式取消。

参考：

- [原生成器](https://schneegans.de/windows/unattend-generator/)
- [微软：智能应用控制常见问题](https://support.microsoft.com/en-us/windows/security/threat-malware-protection/smart-app-control-frequently-asked-questions)
- [微软：应用和浏览器控制](https://support.microsoft.com/en-us/windows/security/windows-security/app-browser-control-in-the-windows-security-app)
- [微软：内存完整性](https://learn.microsoft.com/en-us/windows/security/hardware-security/enable-virtualization-based-protection-of-code-integrity)
- [微软：icacls](https://learn.microsoft.com/en-us/windows-server/administration/windows-commands/icacls)
