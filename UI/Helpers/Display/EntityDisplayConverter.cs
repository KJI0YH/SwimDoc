using System.Globalization;
using System.Windows.Data;
using DataLayer.EfClasses;

namespace UI.Helpers.Display;

public sealed class EntityDisplayConverter : IValueConverter
{
    public const string SwimStyleKind = "SwimStyle";
    public const string AgeGroupKind = "AgeGroup";
    public const string SwimEventKind = "SwimEvent";
    public const string EntrySwimKind = "EntrySwim";
    public static readonly EntityDisplayConverter Instance = new();
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return (parameter as string) switch
        {
            SwimStyleKind => EntityDisplayFormatter.FormatSwimStyle(value as SwimStyle),
            AgeGroupKind => EntityDisplayFormatter.FormatAgeGroup(value as AgeGroup),
            SwimEventKind => EntityDisplayFormatter.FormatSwimEvent(value as SwimEvent),
            EntrySwimKind => EntityDisplayFormatter.FormatEntrySwimName(value as Entry),
            _ => value?.ToString() ?? string.Empty
        };
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
