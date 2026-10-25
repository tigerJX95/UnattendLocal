using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Markup;
using UnattendLocal.Localization;

namespace UnattendLocal.Desktop;

// Source is deliberately invariant: ComboBoxItem.Content and action IDs retain
// their original values. Only the presentation binding is language-dependent.
[MarkupExtensionReturnType(typeof(string))]
public sealed class SourceExtension(string key) : MarkupExtension
{
    public override object ProvideValue(IServiceProvider serviceProvider) => UiText.Source(key);
}
[MarkupExtensionReturnType(typeof(string))]
public sealed class TextExtension(string key) : MarkupExtension
{
    public override object ProvideValue(IServiceProvider serviceProvider) => new Binding("[" + key + "]")
    { Source = UiText.State, Mode = BindingMode.OneWay }.ProvideValue(serviceProvider);
}
[MarkupExtensionReturnType(typeof(string))]
public sealed class DisplayExtension : MarkupExtension
{
    public BindingBase? Value { get; set; }
    public string? FormatKey { get; set; }
    public override object ProvideValue(IServiceProvider serviceProvider)
    {
        var binding = new MultiBinding { Mode = BindingMode.OneWay, Converter = new DisplayConverter(), ConverterParameter = FormatKey };
        binding.Bindings.Add(Value ?? new Binding());
        binding.Bindings.Add(new Binding(nameof(UiText.LanguageState.Revision)) { Source = UiText.State });
        return binding.ProvideValue(serviceProvider);
    }
}
public sealed class DisplayConverter : IMultiValueConverter
{
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        if (values.Length == 0 || values[0] == DependencyProperty.UnsetValue) return "";
        string translated = UiText.Translate(values[0]?.ToString());
        return parameter is string key && key.Length > 0 ? string.Format(CultureInfo.CurrentCulture, UiText.Get(key), translated) : translated;
    }
    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture) => targetTypes.Select(_ => Binding.DoNothing).ToArray();
}
