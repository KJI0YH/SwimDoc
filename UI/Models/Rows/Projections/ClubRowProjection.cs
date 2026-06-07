namespace UI.Models.Rows.Projections;

public sealed class ClubRowProjection
{
    public int Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public int AthleteCount { get; init; }
    public int EntryCount { get; init; }
    public int RelayCount { get; init; }
    public int PointCount { get; init; }
}
