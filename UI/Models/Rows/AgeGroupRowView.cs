using DataLayer.EfClasses;
using UI.Helpers;

namespace UI.Models.Rows;

public sealed class AgeGroupRowView(AgeGroup entity) : IEntityRowView<AgeGroup>
{
    public AgeGroup Entity { get; } = entity;

    public int Id => Entity.Id;
    public string DisplayName => EntityDisplayFormatter.FormatAgeGroup(Entity);
    public Gender Gender => Entity.Gender;
    public int? BirthYearMin => Entity.BirthYearMin;
    public int? BirthYearMax => Entity.BirthYearMax;
}
