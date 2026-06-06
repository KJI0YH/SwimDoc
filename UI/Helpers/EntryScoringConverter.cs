using System.Globalization;
using System.Windows.Data;
using DataLayer.EfClasses;
using UI.ViewModels.Pages;
using UI.Models.Rows;

namespace UI.Helpers;

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
            EntryRowView entryRow => entryRow.Scoring,
            IEntityRowView<Entry> entryRow => entryRow.Entity.Scoring,
            ResultEntryView result => result.Entry.Scoring,
            ParticipantResultEntryView participantResult => participantResult.Entry.Scoring,
            HeatPositionView heat => heat.Entry.Scoring,
            FixationHeatPositionView fixation => fixation.Entry.Scoring,
            _ => true
        };
}
