using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace UnattendLocal.Desktop;

// Empty optional numeric editors must write null back to the plan. UnsetValue
// makes invalid nonempty input a binding error, blocking export of stale data.
public sealed class OptionalIntegerConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        => value is int number ? number.ToString(culture) : string.Empty;

    public object? ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is not string text) return DependencyProperty.UnsetValue;
        if (string.IsNullOrWhiteSpace(text)) return null;
        return int.TryParse(text, NumberStyles.Integer, culture, out int number)
            ? number : DependencyProperty.UnsetValue;
    }
}
