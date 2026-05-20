using System.Globalization;
using System.Windows.Data;
using DataLayer.EfClasses;
using UI.ViewModels.Pages;

namespace UI.Helpers;

/// <summary>
/// Resolves whether a grid row's linked <see cref="Entry"/> participates in scoring.
/// </summary>
public sealed class EntryScoringConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        ResolveScoring(value);

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();

    internal static bool ResolveScoring(object? row) =>
        row switch
        {
            null => true,
            Entry entry => entry.Scoring,
            ResultEntryView result => result.Entry.Scoring,
            HeatPositionView heat => heat.Entry.Scoring,
            FixationHeatPositionView fixation => fixation.Entry.Scoring,
            _ => true
        };
}
