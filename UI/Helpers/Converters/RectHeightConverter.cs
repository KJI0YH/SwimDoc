using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace UI.Helpers.Converters;

public sealed class RectHeightConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not Rect rect) return Binding.DoNothing;
        var multiplier = 1.0;
        if (parameter is string s && double.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out var m))
            multiplier = m;
        return Math.Max(0, rect.Height * multiplier);
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return Binding.DoNothing;
    }
}
