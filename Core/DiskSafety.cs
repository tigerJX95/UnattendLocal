namespace UnattendLocal.Core;
public static class DiskSafety {
 public const string Risk="清空、删除、格式化、转换分区及安装写入可能导致数据永久丢失、系统无法启动或业务中断；本工具不提供数据恢复，也不保证保留原有文件。自定义 DiskPart 脚本可操作任意磁盘，上方安装目标编号不会限制脚本范围。";
 public const string Preparation="使用前请备份重要数据并验证可恢复性，在目标电脑的 Windows PE 中核对磁盘编号、容量及分区；不要按当前电脑的盘符推断。仅对本人设备或已取得明确授权的设备与数据操作。导出文件本身不操作磁盘，将其用于安装时才按配置执行。";
 public const string Responsibility="使用者应对设备与数据的操作授权、备份、目标选择及脚本内容负责；未经授权操作或因错误配置造成损失，相关责任依适用法律和实际过错认定。本提示不免除开发者或其他主体依法不得免除的责任，也不限制使用者依法享有的权利。勾选风险确认仅表示已核对操作，不构成放弃法定权利。";
}
