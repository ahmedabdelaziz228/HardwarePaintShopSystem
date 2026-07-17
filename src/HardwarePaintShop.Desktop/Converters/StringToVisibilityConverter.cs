using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace HardwarePaintShop.Desktop.Converters;

/// <summary>
/// Returns Visibility.Visible when the string is non-null and non-empty, Collapsed otherwise.
/// </summary>
[ValueConversion(typeof(string), typeof(Visibility))]
public class StringToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return !string.IsNullOrWhiteSpace(value as string)
            ? Visibility.Visible
            : Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
