using DataLayer.EfClasses;

namespace DataLayer.Scoring;

public static class ScoringPointsCalculator
{
    public static int CalculateAthleteScoringPoints(IEnumerable<Entry> entries)
    {
        var scoringEntries = entries.Where(e => e.Scoring).ToList();
        if (scoringEntries.Count == 0)
            return 0;

        var withEvent = scoringEntries.Where(e => e.SwimEvent is not null).ToList();
        var withoutEvent = scoringEntries.Where(e => e.SwimEvent is null).ToList();

        var fromGrouped = withEvent
            .GroupBy(e => (e.SwimStyleId, e.SwimEvent!.AgeGroupId))
            .Sum(group => GetHighestRoundEntry(group).Points ?? 0);

        var fromStandalone = withoutEvent.Sum(e => e.Points ?? 0);
        return fromGrouped + fromStandalone;
    }

    public static Entry GetHighestRoundEntry(IEnumerable<Entry> entries)
    {
        Entry? highestRoundEntry = null;
        var highestRoundRank = -1;
        foreach (var entry in entries)
        {
            var roundRank = entry.SwimEvent is not null
                ? GetRoundRank(entry.SwimEvent.Round)
                : 0;
            if (roundRank <= highestRoundRank)
                continue;
            highestRoundRank = roundRank;
            highestRoundEntry = entry;
        }

        return highestRoundEntry ?? entries.First();
    }

    public static int GetRoundRank(EventRound round) =>
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
