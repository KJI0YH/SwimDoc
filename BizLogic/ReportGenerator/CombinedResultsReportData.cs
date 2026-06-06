namespace BizLogic.ReportGenerator;

public sealed record CombinedResultsReportEventColumn(int EventId, string Header, bool HasScoringEntries);

public sealed record CombinedResultsReportAthleteRow(
    string ParticipantName,
    int YearOfBirth,
    string ClubName,
    IReadOnlyDictionary<int, int?> PointsByEventId,
    IReadOnlyDictionary<int, bool> ScoringByEventId,
    int TotalPoints,
    bool IsInOfficialStandings,
    int? Place);

public sealed record CombinedResultsReportData(
    IReadOnlyList<CombinedResultsReportEventColumn> EventColumns,
    IReadOnlyList<CombinedResultsReportAthleteRow> Athletes);
