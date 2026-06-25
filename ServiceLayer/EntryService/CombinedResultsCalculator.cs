using DataLayer.EfClasses;
using DataLayer.Scoring;

namespace ServiceLayer.EntryService;

public static class CombinedResultsCalculator
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
            var roundRank = ScoringPointsCalculator.GetRoundRank(swimEvent.Round);
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

    public static IEnumerable<(CombinedResultsAthleteRow AthleteRow, int Place)> AssignPlaces(
        IReadOnlyList<CombinedResultsAthleteRow> athletes)
    {
        if (athletes.Count == 0)
            yield break;
        var place = 1;
        var previousTotal = athletes[0].TotalPoints;
        for (var index = 0; index < athletes.Count; index++)
        {
            var athleteRow = athletes[index];
            if (index > 0 && athleteRow.TotalPoints != previousTotal)
            {
                place = index + 1;
                previousTotal = athleteRow.TotalPoints;
            }
            yield return (athleteRow, place);
        }
    }
}
