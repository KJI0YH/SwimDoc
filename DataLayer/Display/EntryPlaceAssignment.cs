using DataLayer.EfClasses;

namespace DataLayer.Display;

public static class EntryPlaceAssignment
{
    public static bool SharesFinalPlace(Entry entry) =>
        entry.Status > EntryStatus.FINISH || (entry.Points ?? 0) == 0;

    public static IReadOnlyList<Entry> OrderForResults(IEnumerable<Entry> entries) =>
        entries
            .OrderBy(e => SharesFinalPlace(e) ? 1 : 0)
            .ThenByDescending(e => e.Points ?? 0)
            .ThenBy(e => e.FinishTime ?? int.MaxValue)
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
            if (SharesFinalPlace(entry))
                continue;

            var entryPlace = previousRanked is not null && entry.Points == previousRanked.Points
                ? lastRankedPlace
                : place;
            lastRankedPlace = entryPlace;
            rankedPlaces[entry.Id] = entryPlace;
            previousRanked = entry;
            place++;
        }

        var finalPlace = rankedPlaces.Count > 0 ? place : 1;
        return entries
            .Select(entry => SharesFinalPlace(entry)
                ? (entry, finalPlace)
                : (entry, rankedPlaces[entry.Id]))
            .ToList();
    }
}
