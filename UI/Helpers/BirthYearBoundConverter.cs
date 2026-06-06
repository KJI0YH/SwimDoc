using System.Globalization;
using System.Windows.Data;
using UI.Resources;

namespace UI.Helpers;

public sealed class BirthYearBoundConverter : IValueConverter
{
    public const string Min = "Min";
    public const string Max = "Max";

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is int year)
            return year.ToString();

        var kind = parameter as string;

        return kind switch
        {
            Min => Strings.Get("AgeGroup_YearRange_OlderThan"),
            Max => Strings.Get("AgeGroup_YearRange_YoungerThan"),
            _ => string.Empty
        };
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
