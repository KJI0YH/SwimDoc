using DataLayer.Display;
using DataLayer.EfClasses;
using ServiceLayer.BaseTimeRepository;
using ServiceLayer.RankTimeProvider;
using ServiceLayer.Scoring;

namespace ServiceLayer.PointScoreProvider;

public sealed class PointScoreProvider(
    IBaseTimeRepository baseTimeRepository,
    IScoringSettingsService scoringSettings,
    IAchievedRankProvider achievedRankProvider) : IPointScoreProvider
{
    public int CalculatePoints(
        Course course,
        int meters,
        Stroke stroke,
        int relayCount,
        Gender sex,
        int? finishTime)
    {
        var settings = scoringSettings.Current;
        if (settings.Mode == ScoringMode.PlaceTable)
            return 0;
        if (finishTime is null or <= 0)
            return 0;
        var baseTime = GetScoringBaseTime(course, meters, stroke, relayCount, sex);
        if (baseTime <= 0)
            return 0;
        return (int)Math.Truncate(1000 * Math.Pow(baseTime / (double)finishTime, 3));
    }

    public void ApplyEventPoints(SwimEvent swimEvent, IReadOnlyList<Entry> entries) =>
        ApplyEventPoints(swimEvent, entries, rankingEntries: null);

    public void ApplyEventPoints(
        SwimEvent swimEvent,
        IReadOnlyList<Entry> entries,
        IReadOnlyList<Entry>? rankingEntries)
    {
        ArgumentNullException.ThrowIfNull(swimEvent);
        ArgumentNullException.ThrowIfNull(entries);
        var settings = scoringSettings.Current;
        if (settings.Mode == ScoringMode.PlaceTable)
        {
            var forRanking = rankingEntries is { Count: > 0 }
                ? MergeHeatEntriesForRanking(rankingEntries, entries)
                : entries;
            ApplyPlaceTablePoints(forRanking, settings.PlacePoints);
            ApplyMinimumCategory(swimEvent, forRanking, settings.MinimumCategory);
            return;
        }

        foreach (var entry in entries)
        {
            if (!entry.Scoring || entry.Status != EntryStatus.FINISH || entry.FinishTime is null or <= 0)
            {
                entry.Points = 0;
                continue;
            }

            var swimStyle = entry.SwimStyle ?? swimEvent.SwimStyle;
            var gender = swimEvent.AgeGroup?.Gender ?? Gender.Mixed;
            entry.Points = CalculatePoints(
                swimEvent.Course,
                swimStyle.Distance,
                swimStyle.Stroke,
                swimStyle.RelayCount,
                gender,
                entry.FinishTime);
        }

        ApplyMinimumCategory(swimEvent, entries, settings.MinimumCategory);
    }

    private void ApplyMinimumCategory(SwimEvent swimEvent, IReadOnlyList<Entry> entries, Category? minimum)
    {
        if (minimum is null or Category.NoCategory)
            return;
        foreach (var entry in entries)
        {
            if (entry.Points is > 0 && !achievedRankProvider.MeetsCategory(swimEvent, entry, minimum.Value))
                entry.Points = 0;
        }
    }

    public static IReadOnlyList<Entry> MergeHeatEntriesForRanking(
        IReadOnlyList<Entry> eventEntries,
        IReadOnlyList<Entry> heatEntries)
    {
        var heatIds = heatEntries.Select(e => e.Id).ToHashSet();
        var merged = new List<Entry>(eventEntries.Count + heatEntries.Count);
        foreach (var entry in eventEntries)
        {
            if (!heatIds.Contains(entry.Id))
                merged.Add(entry);
        }

        merged.AddRange(heatEntries);
        return merged;
    }

    private static void ApplyPlaceTablePoints(IReadOnlyList<Entry> entries, IReadOnlyList<int> placePoints)
    {
        foreach (var entry in entries.Where(e => !e.Scoring))
            entry.Points = 0;
        var scoringOrdered = EntryPlaceAssignment.OrderForResults(entries.Where(e => e.Scoring));
        foreach (var (entry, place) in EntryPlaceAssignment.AssignPlaces(scoringOrdered))
        {
            entry.Points = EntryPlaceAssignment.IsUnranked(entry)
                ? 0
                : GetPointsForPlace(placePoints, place);
        }
    }

    public static int GetPointsForPlace(IReadOnlyList<int> placePoints, int place)
    {
        if (place <= 0 || place > placePoints.Count)
            return 0;
        return placePoints[place - 1];
    }

    private int GetScoringBaseTime(Course course, int meters, Stroke stroke, int relayCount, Gender sex)
    {
        var baseTime = baseTimeRepository.GetBaseTime(course, meters, stroke, relayCount, sex);
        if (baseTime > 0 || relayCount == 0)
            return baseTime;
        if (sex != Gender.Mixed)
            return 0;
        baseTime = baseTimeRepository.GetBaseTime(course, meters, stroke, relayCount, Gender.Male);
        if (baseTime > 0)
            return baseTime;
        return baseTimeRepository.GetBaseTime(course, meters, stroke, relayCount, Gender.Female);
    }
}
