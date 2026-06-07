namespace UI.Models.CombinedResults;

public sealed class CombinedResultRow
{
    public CombinedResultRow(
        int? place,
        int athleteId,
        string participantName,
        string yearOfBirth,
        string clubName,
        int totalPoints,
        IReadOnlyDictionary<int, int?> pointsByEventId,
        IReadOnlyDictionary<int, bool> scoringByEventId,
        bool isOutOfScoring)
    {
        Place = place;
        AthleteId = athleteId;
        ParticipantName = participantName;
        YearOfBirth = yearOfBirth;
        ClubName = clubName;
        TotalPoints = totalPoints;
        IsOutOfScoring = isOutOfScoring;
        _pointsByEventId = new Dictionary<int, int?>(pointsByEventId);
        _scoringByEventId = new Dictionary<int, bool>(scoringByEventId);
    }

    public int? Place { get; }
    public string PlaceDisplay => Place?.ToString() ?? string.Empty;
    public int AthleteId { get; }
    public string ParticipantName { get; }
    public string YearOfBirth { get; }
    public string ClubName { get; }
    public int TotalPoints { get; }
    public bool IsOutOfScoring { get; }
    private readonly Dictionary<int, int?> _pointsByEventId;
    private readonly Dictionary<int, bool> _scoringByEventId;
    public int? this[int swimEventId] =>
        _pointsByEventId.TryGetValue(swimEventId, out var points) ? points : null;

    public bool IsNonScoringEvent(int swimEventId) =>
        _scoringByEventId.TryGetValue(swimEventId, out var scoring) && !scoring;
}
