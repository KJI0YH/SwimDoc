namespace UI.Models.Rows.Projections;

public sealed class ClubRowProjection
{
    public int Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public int AthleteCount { get; init; }
    public int EntryScoringCount { get; init; }
    public int EntryPersonalCount { get; init; }
    public int RelayScoringCount { get; init; }
    public int RelayPersonalCount { get; init; }
    public int PointCount { get; init; }
}
