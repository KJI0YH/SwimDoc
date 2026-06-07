using DataLayer.EfClasses;

namespace ServiceLayer.EntryService;

public sealed record CombinedResultsAthleteRow(
    Athlete Athlete,
    IReadOnlyDictionary<int, string> PointsBySwimStyleId,
    IReadOnlyDictionary<int, bool> ScoringBySwimStyleId,
    int TotalPoints,
    bool IsInOfficialStandings);
