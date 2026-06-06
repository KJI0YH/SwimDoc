using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using UI.Resources;
using UI.ViewModels.Pages;

namespace UI.Helpers;

public sealed class CombinedResultsEventScoringConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not CombinedResultRow row || !TryGetEventId(parameter, out var eventId))
            return true;

        return !row.IsNonScoringEvent(eventId);
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();

    internal static bool TryGetEventId(object? parameter, out int eventId)
    {
        switch (parameter)
        {
            case int id:
                eventId = id;
                return true;
            case string text when int.TryParse(text, out var parsed):
                eventId = parsed;
                return true;
            default:
                eventId = 0;
                return false;
        }
    }
}
