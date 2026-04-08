using System.Globalization;
using System.Windows.Data;
using DataLayer;

namespace UI.Helpers;

public class EnumDescriptionConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value == null)
            return string.Empty;

        return value is Enum e ? EnumDisplay.GetDescription(e) : value.ToString() ?? string.Empty;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value;
    }
}