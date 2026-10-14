# V0.2 系统维护设置依据

生成内容直接使用当前固定上游核心，未修改 vendor 源码。两项默认关闭。

- 更新重启：AUOptions=4、NoAutoRebootWithLoggedOnUsers=1，以及上游 MoveActiveHours.xml 计划任务。微软说明登录用户策略受条件约束，用户安排的重启可能覆盖；部分策略在不同 Windows 版本的适用性不同，不宣称完全阻止重启。
- NTFS 最后访问时间：上游命令为 `fsutil.exe behavior set disableLastAccess 1;`，不调整创建时间或修改时间。不要把此选项解释为文件删除或备份功能。

参考：

- [原生成器](https://schneegans.de/windows/unattend-generator/)
- [上游任务说明](https://schneegans.de/windows/unattend-generator/usage/)
- [Microsoft：更新后的设备重启](https://learn.microsoft.com/en-us/windows/deployment/update/waas-restart)
- [Microsoft：fsutil behavior](https://learn.microsoft.com/en-us/windows-server/administration/windows-commands/fsutil-behavior)

导入仅识别已知完整命令组、原始任务内容、有效的提取及 specialize 调用。部分、修改、重复、停用或其他阶段的相关内容保留原文，拒绝开关覆盖；不通过注释中的参数推断已经执行的设置。取消已识别选项只移除对应已知内容，其他文件与命令保留。
