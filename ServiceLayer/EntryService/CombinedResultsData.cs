using DataLayer.EfClasses;

namespace ServiceLayer.EntryService;

public sealed record CombinedResultsEventColumn(int EventId, string Header, bool HasScoringEntries);

public sealed record CombinedResultsData(
    IReadOnlyList<CombinedResultsEventColumn> EventColumns,
    IReadOnlyList<CombinedResultsAthleteRow> Athletes);

public sealed record CombinedResultsAthleteRow(
    Athlete Athlete,
    IReadOnlyDictionary<int, int?> PointsByEventId,
    IReadOnlyDictionary<int, bool> ScoringByEventId,
    int TotalPoints,
    bool IsInOfficialStandings);
