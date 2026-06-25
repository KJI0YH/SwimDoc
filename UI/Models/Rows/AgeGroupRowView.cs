using DataLayer.EfClasses;
using UI.Helpers.Display;
using UI.Models.Rows.Projections;

namespace UI.Models.Rows;

public sealed class AgeGroupRowView : IEntityRowView<AgeGroup>
{
    public AgeGroup Entity { get; }
    public int Id => Entity.Id;
    public string DisplayName => EntityDisplayFormatter.FormatAgeGroup(Entity);
    public Gender Gender => Entity.Gender;
    public int? BirthYearMin => Entity.BirthYearMin;
    public int? BirthYearMax => Entity.BirthYearMax;
    public int ParticipantCount { get; }

    public AgeGroupRowView(AgeGroup entity) => Entity = entity;

    public static AgeGroupRowView FromProjection(AgeGroupRowProjection projection, int participantCount = 0) =>
        new(EntityRowStubBuilder.BuildAgeGroup(projection), participantCount);

    private AgeGroupRowView(AgeGroup entity, int participantCount)
    {
        Entity = entity;
        ParticipantCount = participantCount;
    }
}
