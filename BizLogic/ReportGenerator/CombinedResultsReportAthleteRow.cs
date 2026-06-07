namespace BizLogic.ReportGenerator;

public sealed record CombinedResultsReportAthleteRow(
    string ParticipantName,
    int YearOfBirth,
    string ClubName,
    IReadOnlyDictionary<int, string> PointsBySwimStyleId,
    IReadOnlyDictionary<int, bool> ScoringBySwimStyleId,
    int TotalPoints,
    bool IsInOfficialStandings,
    int? Place);
