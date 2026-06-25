using DataLayer.EfClasses;
using UI.Helpers.Display;
using UI.Models.Rows.Projections;

namespace UI.Models.Rows;

public sealed class AthleteRowView : IEntityRowView<Athlete>
{
    public Athlete Entity { get; }
    public int Id { get; }
    public string FirstName { get; }
    public string LastName { get; }
    public int YearOfBirth { get; }
    public Gender Gender { get; }
    public Category Category { get; }
    public string ClubName { get; }
    public int PointCount { get; }

    public AthleteRowView(Athlete entity)
    {
        Entity = entity;
        Id = Entity.Id;
        FirstName = Entity.FirstName;
        LastName = Entity.LastName;
        YearOfBirth = Entity.YearOfBirth;
        Gender = Entity.Gender;
        Category = Entity.Category;
        ClubName = EntityDisplayFormatter.FormatAthleteClubName(Entity);
        PointCount = EntityDisplayFormatter.FormatAthletePointCount(Entity);
    }

    public static AthleteRowView FromProjection(AthleteRowProjection projection, int pointCount = 0)
    {
        var entity = EntityRowStubBuilder.BuildAthlete(projection);
        return new AthleteRowView(
            entity,
            pointCount,
            projection.ClubName ?? EntityDisplayFormatter.FormatAthleteClubName(entity));
    }

    private AthleteRowView(Athlete entity, int pointCount, string clubName)
    {
        Entity = entity;
        Id = entity.Id;
        FirstName = entity.FirstName;
        LastName = entity.LastName;
        YearOfBirth = entity.YearOfBirth;
        Gender = entity.Gender;
        Category = entity.Category;
        ClubName = clubName;
        PointCount = pointCount;
    }
}
