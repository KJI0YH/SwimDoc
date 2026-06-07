using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using UI.Resources;
using UI.ViewModels.Pages;

namespace UI.Helpers.Converters;

public sealed class CombinedResultsEventScoringConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not CombinedResultRow row || !TryGetSwimStyleId(parameter, out var swimStyleId))
            return true;
        return !row.IsNonScoringSwimStyle(swimStyleId);
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();

    internal static bool TryGetSwimStyleId(object? parameter, out int swimStyleId)
    {
        switch (parameter)
        {
            case int id:
                swimStyleId = id;
                return true;
            case string text when int.TryParse(text, out var parsed):
                swimStyleId = parsed;
                return true;
            default:
                swimStyleId = 0;
                return false;
        }
    }
}
