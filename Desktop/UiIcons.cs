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
        Add(global::UnattendLocal.Localization.UiText.Source("text.3df6c3d7eec2"), document+"M7,13 L10,16 L15,10", global::UnattendLocal.Localization.UiText.Source("text.5facbc61aa8c"));
        Add(global::UnattendLocal.Localization.UiText.Source("text.d726fe88e3a1"), "M4,2 H13 L18,7 V10 M4,2 V20 H10 M13,2 V7 H18 M11,17 L17,11 L20,14 L14,20 H11 Z", global::UnattendLocal.Localization.UiText.Source("text.b5caca4c563e"));
        Add(global::UnattendLocal.Localization.UiText.Source("text.520eef7974df"), "M2,8 V4 H9 L12,7 H20 V10 M2,8 H21 L18,20 H2 Z", global::UnattendLocal.Localization.UiText.Source("text.f9b7d0a587d7"));
        Add(global::UnattendLocal.Localization.UiText.Source("text.a1de672a7e93"), document+"M1,12 H12 M8,8 L12,12 L8,16", global::UnattendLocal.Localization.UiText.Source("text.1de3f408ee67"));
        Add(global::UnattendLocal.Localization.UiText.Source("text.a6aa8cd3f498"), "M3,2 H17 L20,5 V20 H3 Z M7,2 V8 H16 V2 M7,20 V13 H16 V20", global::UnattendLocal.Localization.UiText.Source("text.1a5a9b1aaa8b"));
        Add(global::UnattendLocal.Localization.UiText.Source("text.ad9fd5bf109f"), "M4,8 A8,8 0 1 1 3,15 M4,8 V2 M4,8 H10 M8,12 H16 M12,8 V16", global::UnattendLocal.Localization.UiText.Source("text.b5c413d90407"));
        Add(global::UnattendLocal.Localization.UiText.Source("text.f0866be5e5a3"), document+"M7,12 H15 M7,16 H12", global::UnattendLocal.Localization.UiText.Source("text.2d0b28cf3383"));
        Add(global::UnattendLocal.Localization.UiText.Source("text.f3393a498bde"), "M4,8 A8,8 0 1 1 3,15 M4,8 V2 M4,8 H10", global::UnattendLocal.Localization.UiText.Source("text.f3393a498bde"));
        Add(global::UnattendLocal.Localization.UiText.Source("text.fac2a67ad878"), "M3,11 L9,17 L20,5", global::UnattendLocal.Localization.UiText.Source("text.fac2a67ad878"));
        Add(global::UnattendLocal.Localization.UiText.Source("text.2cd0f3be8738"), "M4,4 L18,18 M18,4 L4,18", global::UnattendLocal.Localization.UiText.Source("text.2c679ba08e7f"));
        Add(global::UnattendLocal.Localization.UiText.Source("text.798eb07d9bda"), "M4,7 A8,8 0 0 1 18,5 M18,5 V1 M18,5 H14 M18,15 A8,8 0 0 1 4,17 M4,17 V21 M4,17 H8 M7,11 L10,14 L15,8", global::UnattendLocal.Localization.UiText.Source("text.3fdee207d91a"));
        Add(global::UnattendLocal.Localization.UiText.Source("text.0e6299b1deb3"), "M4,8 A8,8 0 1 1 3,15 M4,8 V2 M4,8 H10", global::UnattendLocal.Localization.UiText.Source("text.3147ed3cf77a"));
        Add(global::UnattendLocal.Localization.UiText.Source("text.1d3c7c25917e"), "M11,2 A4,4 0 1 1 10.99,2 Z M2,20 V17 Q2,12 9,12 H12 M17,12 V20 M13,16 H21", global::UnattendLocal.Localization.UiText.Source("text.1667438db639"));
        Add(global::UnattendLocal.Localization.UiText.Source("text.b487f606a41e"), "M11,2 A4,4 0 1 1 10.99,2 Z M2,20 V17 Q2,12 9,12 H12 M13,16 H21", global::UnattendLocal.Localization.UiText.Source("text.51bc34cfa28c"));
        Add(global::UnattendLocal.Localization.UiText.Source("text.3a5040b68abf"), "M3,3 H19 V19 H3 Z M6,11 L10,15 L17,7", global::UnattendLocal.Localization.UiText.Source("text.09622c2506f5"));
        Add(global::UnattendLocal.Localization.UiText.Source("text.ab9084a640fb"), "M3,3 H19 V19 H3 Z M7,11 H15", global::UnattendLocal.Localization.UiText.Source("text.7e173a6405c8"));
        Add(global::UnattendLocal.Localization.UiText.Source("text.cee6e16b5a01"), "M3,4 H19 V18 H3 Z M6,8 L9,11 L6,14 M12,14 H16", global::UnattendLocal.Localization.UiText.Source("text.e64adbe85e1a"));
        Add(global::UnattendLocal.Localization.UiText.Source("text.b901b508491b"), document+"M1,12 H12 M8,8 L12,12 L8,16", global::UnattendLocal.Localization.UiText.Source("text.fb053ae09928"));
        Add(global::UnattendLocal.Localization.UiText.Source("text.cf81a0a44e7e"), document+"M0,12 H12 M8,8 L12,12 L8,16", global::UnattendLocal.Localization.UiText.Source("text.9ff37621d896"));
        Add(global::UnattendLocal.Localization.UiText.Source("text.7ff4f5a5151f"), document+"M10,12 H22 M18,8 L22,12 L18,16", global::UnattendLocal.Localization.UiText.Source("text.ee06ef3cbc8d"));
        Add(global::UnattendLocal.Localization.UiText.Source("text.78d732c019b8"), "M3,3 H21 V19 H3 Z M3,14 H21 M8,17 H14 M18,16 V18 M16,17 H20", global::UnattendLocal.Localization.UiText.Source("text.94496f890917"));
        Add(global::UnattendLocal.Localization.UiText.Source("text.19b1169c38e9"), document+"M8,11 H14 M11,8 V14", global::UnattendLocal.Localization.UiText.Source("text.d080b7a3396a"));
        Add(global::UnattendLocal.Localization.UiText.Source("text.204f0d0a35b5"), "M8,7 H20 V21 H8 Z M4,17 H2 V2 H14 V5", global::UnattendLocal.Localization.UiText.Source("text.5a42fecbec4e"));
        Add(global::UnattendLocal.Localization.UiText.Source("text.b0b7c4970d14"), "M3,3 H19 V19 H3 Z M6,11 L10,15 L17,7", global::UnattendLocal.Localization.UiText.Source("text.fe0530d779a1"));
        Add(global::UnattendLocal.Localization.UiText.Source("text.e91956fbbdc4"), "M3,3 H19 V19 H3 Z M7,11 H15", global::UnattendLocal.Localization.UiText.Source("text.84d5e1be2af2"));
        Add(global::UnattendLocal.Localization.UiText.Source("text.a47cc3117567"), "M9,2 A7,7 0 1 1 8.99,2 Z M14,14 L21,21", global::UnattendLocal.Localization.UiText.Source("text.3cebd72c6f67"));
        Add(global::UnattendLocal.Localization.UiText.Source("text.3fd47edce45b"), "M5,5 L19,19 M19,5 L5,19", global::UnattendLocal.Localization.UiText.Source("text.0fb29186f863"));
        Add(global::UnattendLocal.Localization.UiText.Source("text.0417f10f6716"), "M3,6 H19 M8,6 V3 H14 V6 M5,6 L6,21 H16 L17,6 M9,10 V17 M13,10 V17", global::UnattendLocal.Localization.UiText.Source("text.355051f540a3"));
        Add(global::UnattendLocal.Localization.UiText.Source("text.5dc52de1e037"), "M4,12 L11,5 L18,12 M11,5 V21", global::UnattendLocal.Localization.UiText.Source("text.7502c87ca1cf"));
        Add(global::UnattendLocal.Localization.UiText.Source("text.90edd91dd0ca"), "M4,12 L11,19 L18,12 M11,19 V3", global::UnattendLocal.Localization.UiText.Source("text.dff9aa221dbc"));
        Add(global::UnattendLocal.Localization.UiText.Source("text.cbb22bec8e79"), document+"M7,11 H15 M7,15 H12", global::UnattendLocal.Localization.UiText.Source("text.6b49dc09121a"));
        Add(global::UnattendLocal.Localization.UiText.Source("text.a146f0457cab"), document+"M7,11 H15 M7,15 H12", global::UnattendLocal.Localization.UiText.Source("text.3f88051e4009"));
        Add(global::UnattendLocal.Localization.UiText.Source("text.2b8e47feaf07"), "M8,7 H20 V21 H8 Z M4,17 H2 V2 H14 V5", global::UnattendLocal.Localization.UiText.Source("text.72820b22f156"));
        Add(global::UnattendLocal.Localization.UiText.Source("text.a8e1f400e2fc"), "M9,2 A7,7 0 1 1 8.99,2 Z M14,14 L21,21 M5,9 L8,12 L12,6", global::UnattendLocal.Localization.UiText.Source("text.d0cf25f776ca"));
        Add(global::UnattendLocal.Localization.UiText.Source("text.b10faf01051d"), "M2,11 Q11,0 20,11 Q11,22 2,11 Z M11,7 A4,4 0 1 1 10.99,7 Z", global::UnattendLocal.Localization.UiText.Source("text.1762739ff44a"));
        Add(global::UnattendLocal.Localization.UiText.Source("text.942ce0a533b0"), document+"M10,12 H22 M18,8 L22,12 L18,16", global::UnattendLocal.Localization.UiText.Source("text.fe2c43d1ceaf"));
        Add(global::UnattendLocal.Localization.UiText.Source("text.c6c90fe1aedd"), "M11,1 A10,10 0 1 1 11,21 A10,10 0 1 1 11,1 Z M11,8 A3,3 0 1 1 11,14 A3,3 0 1 1 11,8 Z M5,6 L7,8 M15,14 L17,16", global::UnattendLocal.Localization.UiText.Source("text.bd83649e2a65"));
        Add(global::UnattendLocal.Localization.UiText.Source("text.13cfafa2c1bf"), document+"M7,12 H15", global::UnattendLocal.Localization.UiText.Source("text.aa6faecd0c37"));
        Add(global::UnattendLocal.Localization.UiText.Source("text.8a06256f3427"), "M11,2 A4,4 0 1 1 10.99,2 Z M2,20 V17 Q2,12 10,12 Q18,12 18,17 V20 Z", global::UnattendLocal.Localization.UiText.Source("text.094d4531c89e"));
        Add(global::UnattendLocal.Localization.UiText.Source("text.1c6125cec549"), document+"M8,11 H14 M11,8 V14", global::UnattendLocal.Localization.UiText.Source("text.91e5af32219e"));
        Add(global::UnattendLocal.Localization.UiText.Source("text.f52b0df3fc68"), "M3,6 H19 M8,6 V3 H14 V6 M5,6 L6,21 H16 L17,6", global::UnattendLocal.Localization.UiText.Source("text.349da881548f"));
        Add(global::UnattendLocal.Localization.UiText.Source("text.40f98094d525"), document+"M7,11 H15 M7,15 H12", global::UnattendLocal.Localization.UiText.Source("text.cd7a5268364d"));
        Add(global::UnattendLocal.Localization.UiText.Source("text.60c83758425f"), "M9,2 A7,7 0 1 1 8.99,2 Z M14,14 L21,21 M5,9 L8,12 L12,6", global::UnattendLocal.Localization.UiText.Source("text.a32ecb88ac59"));
        return actions;
    }
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture) =>
        Actions.TryGetValue(value?.ToString() ?? "", out var action) ? action.Icon : Actions[global::UnattendLocal.Localization.UiText.Source("text.f0866be5e5a3")].Icon;
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotSupportedException();
}

public sealed class ActionHelpConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var title = value?.ToString() ?? global::UnattendLocal.Localization.UiText.Source("text.ed31fbb483ee");
        return new HelpTopic("action", title, ActionIconConverter.Actions.TryGetValue(title, out var action) ? HelpText.Paragraphs(action.Body) : title);
    }
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotSupportedException();
}
