# XML 导入 / XML import

选择文件后，工具先读取与解析，在替换当前配置前确认：

- 是：导入并替换当前配置，当前未保存修改被放弃。
- 否：取消整次导入，保留当前配置并显示取消提示。
- 读取或解析失败：显示导入失败，当前配置保留。

未知或无法可靠映射的 XML 内容保留原文。已实现的功能也可能遇到不同版本或修改后的脚本，不能仅凭生成参数注释认定已完整还原。应用移除脚本未完整识别时清单只读，原脚本继续随 XML 导出。Remove 参数不是目标镜像的已安装应用清单。

The tool reads and parses the file before asking whether to replace the current configuration. Yes commits the import; No cancels the entire import and retains the current configuration. Read/parse failures show an error and retain the current configuration.

Unknown XML remains intact. An implemented feature can still have historical or modified scripts that cannot be mapped reliably. Form comments do not establish successful restoration. Unrecognized app-removal scripts remain in exported XML and the removal list becomes read-only. Remove parameters are not an inventory of apps installed in the target image.

## V0.14：已识别的旧版移除流程 / Recognized historical removal flows

支持完整识别的 RemovePackages.ps1、RemoveCapabilities.ps1、RemoveFeatures.ps1 及配套 OneDrive 命令。只有选择器、脚本主体、提取与调用阶段等均能可靠识别时才开放清单编辑；不支持的变体继续只读保留。不改清单时保留旧脚本；修改清单后，将已识别的移除流程转换为当前固定生成器格式，其他 XML 内容保留。旧方案的恢复也要求原文和编辑基准完整；迁移冲突时提示重新导入，不覆盖待处理修改。

Fully recognized RemovePackages.ps1, RemoveCapabilities.ps1, RemoveFeatures.ps1 and the matching OneDrive command can be edited. Selectors, helper bodies, extraction and execution stages must be recognized reliably. Other variants remain read-only. Unedited lists retain the historical scripts; edited lists convert the recognized removal flow to the current fixed generator format while preserving other XML. Older plans require the original source and a complete editing baseline; migration conflicts request reimport and retain pending changes.
