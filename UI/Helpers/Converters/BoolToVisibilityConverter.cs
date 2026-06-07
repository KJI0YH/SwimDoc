using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace UI.Helpers.Converters;

public sealed class BoolToVisibilityConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var invert = parameter is string s && string.Equals(s, "Invert", StringComparison.OrdinalIgnoreCase);
        var isTrue = value is true;
        if (invert) isTrue = !isTrue;
        return isTrue ? Visibility.Visible : Visibility.Collapsed;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return Binding.DoNothing;
    }
}
