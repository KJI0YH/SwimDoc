using DataLayer.EfClasses;

namespace UI.Models.Rows.Projections;

public sealed class AthleteRowProjection
{
    public int Id { get; init; }
    public string FirstName { get; init; } = string.Empty;
    public string LastName { get; init; } = string.Empty;
    public int YearOfBirth { get; init; }
    public Gender Gender { get; init; }
    public Category Category { get; init; }
    public string? ClubName { get; init; }
    public int PointCount { get; init; }
}
