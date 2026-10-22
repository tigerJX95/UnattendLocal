using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace UnattendLocal.Desktop;

// 矢量轮廓不依赖 Segoe 图标字体；按钮原有 Content 保留为无障碍名称。
public sealed class ActionIconConverter : IValueConverter
{
    internal static readonly Dictionary<string, (Geometry Icon, string Body)> Actions = Create();
    private static Dictionary<string, (Geometry, string)> Create()
    {
        var actions = new Dictionary<string, (Geometry, string)>();
        void Add(string names, string path, string body)
        {
            var geometry = Geometry.Parse(path); geometry.Freeze();
            foreach (var name in names.Split('|')) actions.Add(name, (geometry, body));
        }
        const string document = "M4,2 H13 L18,7 V20 H4 Z M13,2 V7 H18 ";
        Add("新建推荐配置", document+"M7,13 L10,16 L15,10", "新建一份可修改的推荐配置。切换前会确认是否放弃当前修改。");
        Add("新建自定义配置", "M4,2 H13 L18,7 V10 M4,2 V20 H10 M13,2 V7 H18 M11,17 L17,11 L20,14 L14,20 H11 Z", "从基础值新建自定义配置，逐项填写设置。切换前会确认是否放弃当前修改。");
        Add("打开方案", "M2,8 V4 H9 L12,7 H20 V10 M2,8 H21 L18,20 H2 Z", "打开已保存的 JSON 配置方案；未保存的密码和密钥需要重新输入。");
        Add("导入应答 XML", document+"M1,12 H12 M8,8 L12,12 L8,16", "导入已有 Windows 应答 XML 为自定义配置。已识别的设置可编辑，其余原文保留；存在冲突时停止导出。导入失败不替换当前方案。");
        Add("保存方案", "M3,2 H17 L20,5 V20 H3 Z M7,2 V8 H16 V2 M7,20 V13 H16 V20", "保存当前配置为 JSON 方案。保存时可选择是否写入密码和自定义产品密钥。");
        Add("从基础值重新开始自定义配置|从基础值重新开始", "M4,8 A8,8 0 1 1 3,15 M4,8 V2 M4,8 H10 M8,12 H16 M12,8 V16", "从基础值重新开始自定义配置，会清空当前填写的账户、凭据和其他配置。");
        Add("保留当前设置", document+"M7,12 H15 M7,16 H12", "保留当前设置，进入自定义配置继续调整，不自动应用推荐值。");
        Add("恢复 Windows 默认", "M4,8 A8,8 0 1 1 3,15 M4,8 V2 M4,8 H10", "恢复 Windows 默认");
        Add("确定", "M3,11 L9,17 L20,5", "确定");
        Add("取消", "M4,4 L18,18 M18,4 L4,18", "取消本次操作。");
        Add("应用 / 恢复整套推荐值", "M4,7 A8,8 0 0 1 18,5 M18,5 V1 M18,5 H14 M18,15 A8,8 0 0 1 4,17 M4,17 V21 M4,17 H8 M7,11 L10,14 L15,8", "应用当前场景的整套推荐值；保留账户、凭据等填写内容，磁盘恢复为手动模式。操作前会显示确认说明。");
        Add("恢复此项", "M4,8 A8,8 0 1 1 3,15 M4,8 V2 M4,8 H10", "只将当前这一项恢复为推荐值。");
        Add("添加账户", "M11,2 A4,4 0 1 1 10.99,2 Z M2,20 V17 Q2,12 9,12 H12 M17,12 V20 M13,16 H21", "添加一个本地账户，随后填写账户名称、权限及密码设置。");
        Add("移除账户", "M11,2 A4,4 0 1 1 10.99,2 Z M2,20 V17 Q2,12 9,12 H12 M13,16 H21", "移除当前选中的本地账户。");
        Add("全选", "M3,3 H19 V19 H3 Z M6,11 L10,15 L17,7", "选中当前这一组的全部设置。");
        Add("全不选", "M3,3 H19 V19 H3 Z M7,11 H15", "取消选中当前这一组的全部设置。");
        Add("安装脚本与隐私文件设置…", "M3,4 H19 V18 H3 Z M6,8 L9,11 L6,14 M12,14 H16", "跳转到安装脚本与隐私文件设置，调整脚本窗口和安装凭据文件保留策略。");
        Add("导入 XML…|导入 JSON…", document+"M1,12 H12 M8,8 L12,12 L8,16", "从文件导入当前布局编辑器内容，并检查结构。覆盖已有内容前会确认。");
        Add("导入脚本文件…", document+"M0,12 H12 M8,8 L12,12 L8,16", "导入脚本文件，新增条目并默认停用");
        Add("另存选中脚本…", document+"M10,12 H22 M18,8 L22,12 L18,16", "导出选中脚本内容到文件");
        Add("查看文件夹位置示意", "M3,3 H21 V19 H3 Z M3,14 H21 M8,17 H14 M18,16 V18 M16,17 H20", "查看 Windows 11 开始菜单电源按钮左侧文件夹入口的位置示意；只显示说明，不修改配置。");
        Add("添加脚本", document+"M8,11 H14 M11,8 V14", "新增脚本，选择执行阶段、格式并填写内容。");
        Add("复制脚本", "M8,7 H20 V21 H8 Z M4,17 H2 V2 H14 V5", "复制已选脚本到原条目下方，副本默认停用；核对内容后自行启用。");
        Add("勾选当前筛选结果", "M3,3 H19 V19 H3 Z M6,11 L10,15 L17,7", "勾选应用移除页当前显示的项目，隐藏项目保持原选择。操作前确认影响范围。");
        Add("取消当前筛选结果", "M3,3 H19 V19 H3 Z M7,11 H15", "取消当前显示项目的移除选择，隐藏项目保持原选择。");
        Add("定位所选设置", "M9,2 A7,7 0 1 1 8.99,2 Z M14,14 L21,21", "切换到选中的设置页面，滚动定位并短暂高亮；不自动修改设置。");
        Add("关闭", "M5,5 L19,19 M19,5 L5,19", "关闭搜索窗口。");
        Add("删除脚本", "M3,6 H19 M8,6 V3 H14 V6 M5,6 L6,21 H16 L17,6 M9,10 V17 M13,10 V17", "删除当前选中的脚本。");
        Add("上移脚本", "M4,12 L11,5 L18,12 M11,5 V21", "将当前脚本上移，同阶段按列表顺序执行。");
        Add("下移脚本", "M4,12 L11,19 L18,12 M11,19 V3", "将当前脚本下移，同阶段按列表顺序执行。");
        Add("目录命令示例|网络配置示例", document+"M7,11 H15 M7,15 H12", "新增示例脚本，默认关闭。核对说明与脚本后自行启用。");
        Add("载入示例", document+"M7,11 H15 M7,15 H12", "将当前布局编辑器填入可修改的示例。覆盖已有内容前会确认。");
        Add("复制示例", "M8,7 H20 V21 H8 Z M4,17 H2 V2 H14 V5", "复制完整示例，不覆盖当前内容。复制成功后显示已复制提示。");
        Add("检查 XML|检查 JSON", "M9,2 A7,7 0 1 1 8.99,2 Z M14,14 L21,21 M5,9 L8,12 L12,6", "检查当前布局文档的结构；应用是否存在及实际效果仍需在目标 Windows 验证。");
        Add("检查与预览", "M2,11 Q11,0 20,11 Q11,22 2,11 Z M11,7 A4,4 0 1 1 10.99,7 Z", "检查当前配置并预览应答文件，预览会遮蔽密码和产品密钥。");
        Add("导出 autounattend.xml|导出 notautounattend.xml", document+"M10,12 H22 M18,8 L22,12 L18,16", "检查并导出 autounattend.xml；自动磁盘操作会要求再次核对目标。");
        Add("导出应答文件 ISO", "M11,1 A10,10 0 1 1 11,21 A10,10 0 1 1 11,1 Z M11,8 A3,3 0 1 1 11,14 A3,3 0 1 1 11,8 Z M5,6 L7,8 M15,14 L17,16", "将应答文件导出为 ISO，供虚拟机挂载。");
        Add("新建最小配置", document+"M7,12 H15", "从最小配置起点新建方案。保留安装隐私文件，避免添加首次登录清理脚本。可在 OOBE 页调整；不是删除原作者恢复信息或压缩现有 XML。新建前会确认是否放弃当前修改。");
        Add("新建本地账户预设", "M11,2 A4,4 0 1 1 10.99,2 Z M2,20 V17 Q2,12 10,12 Q18,12 18,17 V20 Z", "首次登录后沿用现有清理流程处理安装隐私文件。确认新建会替换当前方案，不会修改当前电脑；其余系统选项仍可继续调整。");
        Add("添加 XML 组件", document+"M8,11 H14 M11,8 V14", "新增停用的 XML 条目，最多 15 项。组件和执行阶段须匹配当前组件目录；设置内容以微软对应版本文档为准。");
        Add("移除 XML 组件", "M3,6 H19 M8,6 V3 H14 V6 M5,6 L6,21 H16 L17,6", "移除当前 XML 条目。导入文件中由其他内容保留的设置仍需核对完整原文。");
        Add("载入 OEM 支持信息示例", document+"M7,11 H15 M7,15 H12", "新增 OEMInformation 示例并保持停用。示例使用 specialize 阶段，填写 SupportProvider 和 SupportURL；请替换示例值后启用，不包含已弃用的厂商标志或联系电话字段。");
        Add("检查额外 XML", "M9,2 A7,7 0 1 1 8.99,2 Z M14,14 L21,21 M5,9 L8,12 L12,6", "检查当前片段的 XML 结构和组件阶段。与当前配置的重复设置在检查与预览时核对；微软参数值与目标镜像兼容性需另行验证。");
        return actions;
    }
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture) =>
        Actions.TryGetValue(value?.ToString() ?? "", out var action) ? action.Icon : Actions["保留当前设置"].Icon;
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotSupportedException();
}

public sealed class ActionHelpConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var title = value?.ToString() ?? "操作";
        return new HelpTopic("action", title, ActionIconConverter.Actions.TryGetValue(title, out var action) ? HelpText.Paragraphs(action.Body) : title);
    }
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotSupportedException();
}
