using System.Globalization;
using System.Windows.Data;

namespace HardwarePaintShop.Desktop.Converters;

/// <summary>
/// Selects one of two strings based on a bool value.
/// ConverterParameter format: "TrueString|FalseString"
/// Example: ConverterParameter='تعديل|إضافة'
/// </summary>
[ValueConversion(typeof(bool), typeof(string))]
public class BoolToStringConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool b && parameter is string param)
        {
            var parts = param.Split('|');
            if (parts.Length == 2)
                return b ? parts[0] : parts[1];
        }
        return string.Empty;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
