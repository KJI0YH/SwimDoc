using System.Globalization;
using System.Windows.Data;
using UI.Resources;

namespace UI.Helpers;

public class EnumDescriptionConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value == null)
            return string.Empty;

        if (value is Enum e)
            return Strings.GetEnumDisplay(e);

        return value.ToString() ?? string.Empty;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value;
    }
}