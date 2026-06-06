using System.Globalization;
using System.Windows.Data;

namespace UI.Helpers;

public class NullableIntConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value == null)
            return string.Empty;

        if (value is int intValue)
            return intValue.ToString();

        return string.Empty;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value == null || string.IsNullOrWhiteSpace(value.ToString()))
            return null;

        if (int.TryParse(value.ToString(), out var result))
            return result;

        return null;
    }
}
