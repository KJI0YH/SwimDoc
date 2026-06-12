using DataLayer.EfClasses;

namespace UI.Models.Rows.Projections;

public sealed class SwimEventRowProjection
{
    public int Id { get; init; }
    public int Order { get; init; }
    public Course Course { get; init; }
    public DateOnly Date { get; init; }
    public TimeOnly? Time { get; init; }
    public EventRound Round { get; init; }
    public int LaneMin { get; init; }
    public int LaneMax { get; init; }
    public string? CustomLaneNames { get; init; }
    public SwimEventStatus Status { get; init; }
    public int SwimStyleId { get; init; }
    public int SwimStyleDistance { get; init; }
    public Stroke SwimStyleStroke { get; init; }
    public int SwimStyleRelayCount { get; init; }
    public bool SwimStyleIsRelay { get; init; }
    public int AgeGroupId { get; init; }
    public string? AgeGroupName { get; init; }
    public Gender AgeGroupGender { get; init; }
    public int? AgeGroupBirthYearMin { get; init; }
    public int? AgeGroupBirthYearMax { get; init; }
    public int EntryCount { get; init; }
    public int HeatCount { get; init; }
}
