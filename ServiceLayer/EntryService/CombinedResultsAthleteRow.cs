using DataLayer.EfClasses;

namespace ServiceLayer.EntryService;

public sealed record CombinedResultsAthleteRow(
    Athlete Athlete,
    IReadOnlyDictionary<int, int?> PointsByEventId,
    IReadOnlyDictionary<int, bool> ScoringByEventId,
    int TotalPoints,
    bool IsInOfficialStandings);
