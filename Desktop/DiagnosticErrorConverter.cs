using System.Globalization;
using System.Windows.Data;

namespace UnattendLocal.Desktop;

public sealed class DiagnosticErrorConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture) => DiagnosticText.IsErrorSummary(value as string);
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => Binding.DoNothing;
}
