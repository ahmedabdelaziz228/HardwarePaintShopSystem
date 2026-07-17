using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace HardwarePaintShop.Desktop.Converters;

/// <summary>
/// Inverts a bool: true → false, false → true.
/// Also supports Visibility: true → Collapsed, false → Visible.
/// </summary>
[ValueConversion(typeof(bool), typeof(bool))]
public class InverseBoolConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool b)
        {
            if (targetType == typeof(Visibility))
                return b ? Visibility.Collapsed : Visibility.Visible;
            return !b;
        }
        return value;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool b) return !b;
        return value;
    }
}
