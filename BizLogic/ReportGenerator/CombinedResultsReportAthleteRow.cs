namespace BizLogic.ReportGenerator;

public sealed record CombinedResultsReportAthleteRow(
    string ParticipantName,
    int YearOfBirth,
    string ClubName,
    IReadOnlyDictionary<int, int?> PointsByEventId,
    IReadOnlyDictionary<int, bool> ScoringByEventId,
    int TotalPoints,
    bool IsInOfficialStandings,
    int? Place);
