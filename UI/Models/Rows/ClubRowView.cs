using DataLayer.EfClasses;
using UI.Helpers.Display;
using UI.Models.Rows.Projections;

namespace UI.Models.Rows;

public sealed class ClubRowView : IEntityRowView<Club>
{
    public Club Entity { get; }
    public int Id { get; }
    public string Name { get; }
    public int AthleteCount { get; }
    public int EntryCount { get; }
    public int RelayCount { get; }
    public int PointCount { get; }

    public ClubRowView(Club entity)
    {
        Entity = entity;
        Id = Entity.Id;
        Name = Entity.Name;
        AthleteCount = EntityDisplayFormatter.FormatClubAthleteCount(Entity);
        EntryCount = EntityDisplayFormatter.FormatClubEntryCount(Entity);
        RelayCount = EntityDisplayFormatter.FormatClubRelayCount(Entity);
        PointCount = EntityDisplayFormatter.FormatClubPointCount(Entity);
    }

    public static ClubRowView FromProjection(ClubRowProjection projection)
    {
        var entity = EntityRowStubBuilder.BuildClub(projection);
        return new ClubRowView(entity, projection.AthleteCount, projection.EntryCount, projection.RelayCount,
            projection.PointCount);
    }

    private ClubRowView(Club entity, int athleteCount, int entryCount, int relayCount, int pointCount)
    {
        Entity = entity;
        Id = entity.Id;
        Name = entity.Name;
        AthleteCount = athleteCount;
        EntryCount = entryCount;
        RelayCount = relayCount;
        PointCount = pointCount;
    }
}
