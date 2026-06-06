using System.Globalization;
using System.Windows.Data;
using UI.Resources;

namespace UI.Helpers;

public sealed class LocalizedStringFormatConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (parameter is not string key || string.IsNullOrWhiteSpace(key))
            return value?.ToString() ?? string.Empty;

        var uiCulture = CultureInfo.CurrentUICulture;
        var format = Strings.ResourceManager.GetString(key, uiCulture) ?? key;

        return string.Format(uiCulture, format, value);
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
