# 进程创建审计

固定上游生成器在 specialize 阶段通过 auditpol 启用进程创建子类别；记录命令行模式另写 ProcessCreationIncludeCmdLine_Enabled=1。默认模式不写入覆盖，单独启用审计也不写入关闭命令行记录的值。

参数可能包含凭据，并明文写入安全日志；本程序只准备安装配置，不收集或读取当前电脑的审计日志。

导入要求完整原生命令与有效提取/specialize 管线。部分、修改、重复、条件执行或其他文件中的相关命令保留原文，不能用本设置覆盖。取消只移除已识别内容，未知命令与文件保留。

参考：[Microsoft：命令行进程审计](https://learn.microsoft.com/en-us/windows-server/identity/ad-ds/manage/component-updates/command-line-process-auditing) · [Microsoft：事件 4688](https://learn.microsoft.com/windows/security/threat-protection/auditing/event-4688)
