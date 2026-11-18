# 查看 Windows 安装日志

在“检查与导出”点击“查看安装日志”，选择一份日志文件。输入关键词可筛选包含该文字的行，匹配不区分大小写。清空关键词可恢复预览。

| 文件 | 内容 | 常见位置 |
| --- | --- | --- |
| `setupact.log` | 安装过程 | `%WINDIR%\Panther` |
| `setuperr.log` | 安装错误 | `%WINDIR%\Panther` |
| `setupapi.dev.log` | 即插即用设备和驱动安装 | `%WINDIR%\INF` |
| `setupapi.offline.log` | 安装时的离线驱动操作 | `%WINDIR%\INF` |
| `cbs_unattend.log` | 无人值守组件服务 | `%WINDIR%\Panther` |

可打开从测试机复制出来的日志。窗口以只读方式打开文件，并显示文件名。支持 UTF-8 及带 BOM 的 UTF-16、UTF-32。较大的日志显示末尾 256 KiB 内的内容，筛选也仅作用于这个范围。可再次选择同一文件，读取安装过程中追加的记录。

## English

Under **Check and export**, select **View setup logs** and open a log file. Enter a keyword to show lines containing that text, ignoring case. Clear the filter to restore the preview.

Supported files include `setupact.log`, `setuperr.log`, and `cbs_unattend.log` from `%WINDIR%\Panther`, and `setupapi.dev.log` and `setupapi.offline.log` from `%WINDIR%\INF`. You can also open copies collected from a test computer.

The viewer opens files in read-only mode. It supports UTF-8 and BOM-marked UTF-16 and UTF-32. For larger files, the preview and keyword filter cover the final 256 KiB. Reopen the same file to refresh a log that Setup has appended to.

[Microsoft Learn · Deployment Troubleshooting and Log Files](https://learn.microsoft.com/en-us/windows-hardware/manufacture/desktop/deployment-troubleshooting-and-log-files)

AppLocker 事件 XML 的复核入口位于“OOBE → AppLocker”，详见[配置集目录与 AppLocker 事件复核](DeploymentReview.md)。
