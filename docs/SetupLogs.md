# 查看 Windows 安装日志

在「检查与导出」点击「查看安装日志」，用窗口右上角的「打开日志文件」按钮选择日志。输入关键词后，窗口显示当前预览范围内匹配的行和数量，匹配不区分大小写。清空关键词可恢复预览。

| 文件 | 内容 | 常见位置 |
| --- | --- | --- |
| `setupact.log` | 安装过程 | `%WINDIR%\Panther` |
| `setuperr.log` | 安装错误 | `%WINDIR%\Panther` |
| `setupapi.dev.log` | 即插即用设备和驱动安装 | `%WINDIR%\INF` |
| `setupapi.offline.log` | 安装时的离线驱动操作 | `%WINDIR%\INF` |
| `cbs_unattend.log` | 无人值守组件服务 | `%WINDIR%\Panther` |
| `DISM.log` | 映像服务、驱动及语言包操作 | `%WINDIR%\Logs\DISM` |
| `CBS.log` | 组件服务操作 | `%WINDIR%\Logs\CBS` |
| `OfflineUpdates.log` | PE 脚本安装离线更新包 | 目标 Windows 分区的 `Windows\Logs\DISM` |

可打开从测试机复制出来的日志。窗口以只读方式打开文件，并显示文件名。支持 UTF-8 及带 BOM 的 UTF-16、UTF-32。较大的日志显示末尾 256 KiB 内的内容，筛选也仅作用于这个范围。点击「重新读取当前日志」可读取追加的记录，并保留筛选词。

## English

Under **Check and export**, select **View setup logs** and use the **Open a log file** button in the top right. Enter a keyword to show and count matching lines in the preview, ignoring case. Clear the filter to restore the preview.

Supported files include `setupact.log`, `setuperr.log`, and `cbs_unattend.log` from `%WINDIR%\Panther`, `setupapi.dev.log` and `setupapi.offline.log` from `%WINDIR%\INF`, `DISM.log` from `%WINDIR%\Logs\DISM`, and `CBS.log` from `%WINDIR%\Logs\CBS`. You can also open copies collected from a test computer.

PE script installation writes `OfflineUpdates.log` under `Windows\Logs\DISM` on the target Windows partition when applying offline updates. Open it in the same viewer.

The viewer opens files in read-only mode. It supports UTF-8 and BOM-marked UTF-16 and UTF-32. For larger files, the preview and keyword filter cover the final 256 KiB. Use **Reload current log** to read appended records and keep the current keyword.

[Microsoft Learn · Deployment Troubleshooting and Log Files](https://learn.microsoft.com/en-us/windows-hardware/manufacture/desktop/deployment-troubleshooting-and-log-files)

AppLocker 事件 XML 的复核入口位于“OOBE → AppLocker”，详见[配置集目录与 AppLocker 事件复核](DeploymentReview.md)。
