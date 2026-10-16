# XML 导入 / XML import

选择文件后，工具先读取与解析，在替换当前配置前确认：

- 是：导入并替换当前配置，当前未保存修改被放弃。
- 否：取消整次导入，保留当前配置并显示取消提示。
- 读取或解析失败：显示导入失败，当前配置保留。

未知或无法可靠映射的 XML 内容保留原文。已实现的功能也可能遇到不同版本或修改后的脚本，不能仅凭生成参数注释认定已完整还原。应用移除脚本未完整识别时清单只读，原脚本继续随 XML 导出。Remove 参数不是目标镜像的已安装应用清单。

The tool reads and parses the file before asking whether to replace the current configuration. Yes commits the import; No cancels the entire import and retains the current configuration. Read/parse failures show an error and retain the current configuration.

Unknown XML remains intact. An implemented feature can still have historical or modified scripts that cannot be mapped reliably. Form comments do not establish successful restoration. Unrecognized app-removal scripts remain in exported XML and the removal list becomes read-only. Remove parameters are not an inventory of apps installed in the target image.
