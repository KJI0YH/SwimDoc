using DataLayer.EfClasses;

namespace UI.Models.Rows;

public sealed class AthleteRowView(Athlete entity) : IEntityRowView<Athlete>
{
    public Athlete Entity { get; } = entity;
    public int Id => Entity.Id;
    public string FirstName => Entity.FirstName;
    public string LastName => Entity.LastName;
    public int YearOfBirth => Entity.YearOfBirth;
    public Gender Gender => Entity.Gender;
    public string ClubName => EntityDisplayFormatter.FormatAthleteClubName(Entity);
    public int PointCount => EntityDisplayFormatter.FormatAthletePointCount(Entity);
}
