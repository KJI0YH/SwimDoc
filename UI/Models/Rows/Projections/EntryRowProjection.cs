using DataLayer.EfClasses;

namespace UI.Models.Rows.Projections;

public sealed class EntryRowProjection
{
    public int Id { get; init; }
    public bool Scoring { get; init; }
    public EntryStatus Status { get; init; }
    public int? EntryTime { get; init; }
    public int? FinishTime { get; init; }
    public int? Points { get; init; }
    public string? Comment { get; init; }
    public int? AthleteId { get; init; }
    public int? RelayId { get; init; }
    public int? SwimEventId { get; init; }
    public bool HasHeatPosition { get; init; }
    public string? AthleteFirstName { get; init; }
    public string? AthleteLastName { get; init; }
    public int? AthleteYearOfBirth { get; init; }
    public Category? AthleteCategory { get; init; }
    public string? AthleteClubName { get; init; }
    public string? RelayClubName { get; init; }
    public int? RelayNumber { get; init; }
    public int SwimStyleId { get; init; }
    public int SwimStyleDistance { get; init; }
    public Stroke SwimStyleStroke { get; init; }
    public int SwimStyleRelayCount { get; init; }
    public bool SwimStyleIsRelay { get; init; }
    public int? SwimEventOrder { get; init; }
    public EventRound? SwimEventRound { get; init; }
    public int? SwimEventAgeGroupId { get; init; }
    public string? SwimEventAgeGroupName { get; init; }
    public Gender? SwimEventAgeGroupGender { get; init; }
    public int? SwimEventAgeGroupBirthYearMin { get; init; }
    public int? SwimEventAgeGroupBirthYearMax { get; init; }
    public List<RelayPositionRowProjection> RelayPositions { get; set; } = [];
}

public sealed class RelayPositionRowProjection
{
    public int RelayId { get; init; }
    public int Order { get; init; }
    public string? AthleteFirstName { get; init; }
    public string? AthleteLastName { get; init; }
    public int? AthleteYearOfBirth { get; init; }
}
