using DataLayer.EfClasses;

namespace DataLayer.Display;

public static class EntryPlaceAssignment
{
    public static bool IsUnranked(Entry entry) =>
        entry.Status != EntryStatus.FINISH || !entry.FinishTime.HasValue || entry.FinishTime.Value <= 0;

    /// <summary>Backward-compatible alias for unranked (DNS/DSQ/DNF/no time). </summary>
    public static bool SharesFinalPlace(Entry entry) => IsUnranked(entry);

    public static IReadOnlyList<Entry> OrderForResults(IEnumerable<Entry> entries) =>
        entries
            .OrderBy(e => IsUnranked(e) ? 1 : 0)
            .ThenBy(e => e.FinishTime ?? int.MaxValue)
            .ThenBy(e => e.Id)
            .ToList();

    public static IReadOnlyList<(Entry Entry, int Place)> AssignPlaces(IReadOnlyList<Entry> entries)
    {
        if (entries.Count == 0)
            return [];

        var rankedPlaces = new Dictionary<int, int>();
        var place = 1;
        Entry? previousRanked = null;
        var lastRankedPlace = 0;

        foreach (var entry in entries)
        {
            if (IsUnranked(entry))
                continue;

            var entryPlace = previousRanked is not null && entry.FinishTime == previousRanked.FinishTime
                ? lastRankedPlace
                : place;
            lastRankedPlace = entryPlace;
            rankedPlaces[entry.Id] = entryPlace;
            previousRanked = entry;
            place++;
        }

        var finalPlace = rankedPlaces.Count > 0 ? place : 1;
        return entries
            .Select(entry => IsUnranked(entry)
                ? (entry, finalPlace)
                : (entry, rankedPlaces[entry.Id]))
            .ToList();
    }
}
