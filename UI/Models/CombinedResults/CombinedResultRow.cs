namespace UI.Models.CombinedResults;

public sealed class CombinedResultRow
{
    public CombinedResultRow(
        int? place,
        int athleteId,
        string participantName,
        string yearOfBirth,
        string category,
        string clubName,
        int totalPoints,
        IReadOnlyDictionary<int, string> pointsBySwimStyleId,
        IReadOnlyDictionary<int, bool> scoringBySwimStyleId)
    {
        Place = place;
        AthleteId = athleteId;
        ParticipantName = participantName;
        YearOfBirth = yearOfBirth;
        Category = category;
        ClubName = clubName;
        TotalPoints = totalPoints;
        _pointsBySwimStyleId = new Dictionary<int, string>(pointsBySwimStyleId);
        _scoringBySwimStyleId = new Dictionary<int, bool>(scoringBySwimStyleId);
    }

    public int? Place { get; }
    public string PlaceDisplay => Place?.ToString() ?? string.Empty;
    public int AthleteId { get; }
    public string ParticipantName { get; }
    public string YearOfBirth { get; }
    public string Category { get; }
    public string ClubName { get; }
    public int TotalPoints { get; }
    private readonly Dictionary<int, string> _pointsBySwimStyleId;
    private readonly Dictionary<int, bool> _scoringBySwimStyleId;
    public string this[int swimStyleId] =>
        _pointsBySwimStyleId.TryGetValue(swimStyleId, out var points) ? points : string.Empty;

    public bool IsNonScoringSwimStyle(int swimStyleId) =>
        _scoringBySwimStyleId.TryGetValue(swimStyleId, out var scoring) && !scoring;
}
