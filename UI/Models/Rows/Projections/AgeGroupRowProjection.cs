using DataLayer.EfClasses;

namespace UI.Models.Rows.Projections;

public sealed class AgeGroupRowProjection
{
    public int Id { get; init; }
    public string? Name { get; init; }
    public Gender Gender { get; init; }
    public int? BirthYearMin { get; init; }
    public int? BirthYearMax { get; init; }
}
