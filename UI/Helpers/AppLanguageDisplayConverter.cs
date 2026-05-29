using System.Globalization;
using System.Windows.Data;
using UI.Resources;
using UI.Services;

namespace UI.Helpers;

public sealed class AppLanguageDisplayConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not AppLanguage language)
            return string.Empty;

        return language switch
        {
            AppLanguage.Russian => Strings.Language_Russian,
            AppLanguage.English => Strings.Language_English,
            _ => language.ToString()
        };
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
