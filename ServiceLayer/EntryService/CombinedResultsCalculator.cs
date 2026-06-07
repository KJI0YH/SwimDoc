using DataLayer.EfClasses;

namespace ServiceLayer.EntryService;

internal static class CombinedResultsCalculator
{
    public static IReadOnlyList<IGrouping<int, SwimEvent>> GroupEventsBySwimStyle(IReadOnlyList<SwimEvent> events) =>
        events
            .GroupBy(swimEvent => swimEvent.SwimStyleId)
            .OrderBy(group => group.Min(swimEvent => swimEvent.Order))
            .ToList();

    public static Entry? GetHighestRoundEntry(
        IReadOnlyList<SwimEvent> groupEvents,
        IReadOnlyDictionary<int, Entry> entriesByEventId)
    {
        Entry? highestRoundEntry = null;
        var highestRoundRank = -1;
        foreach (var swimEvent in groupEvents)
        {
            if (!entriesByEventId.TryGetValue(swimEvent.Id, out var entry))
                continue;
            var roundRank = GetRoundRank(swimEvent.Round);
            if (roundRank <= highestRoundRank)
                continue;
            highestRoundRank = roundRank;
            highestRoundEntry = entry;
        }
        return highestRoundEntry;
    }

    public static string FormatPoints(Entry? entry) =>
        entry?.Points is int points ? points.ToString() : string.Empty;

    public static int GetTotalContribution(Entry? highestRoundEntry) =>
        highestRoundEntry is { Scoring: true } ? highestRoundEntry.Points ?? 0 : 0;

    private static int GetRoundRank(EventRound round) =>
        round switch
        {
            EventRound.FIN => 5,
            EventRound.SOS => 4,
            EventRound.SEM => 3,
            EventRound.SOP => 2,
            EventRound.PRE => 1,
            _ => 0
        };
}
