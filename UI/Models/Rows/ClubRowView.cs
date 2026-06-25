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
    public string EntryCount { get; }
    public string RelayCount { get; }
    public int PointCount { get; }

    public ClubRowView(Club entity)
    {
        Entity = entity;
        Id = Entity.Id;
        Name = Entity.Name;
        AthleteCount = EntityDisplayFormatter.FormatClubAthleteCount(Entity);
        EntryCount = EntityDisplayFormatter.FormatClubEntryCountDisplay(Entity);
        RelayCount = EntityDisplayFormatter.FormatClubRelayCountDisplay(Entity);
        PointCount = EntityDisplayFormatter.FormatClubPointCount(Entity);
    }

    public static ClubRowView FromProjection(ClubRowProjection projection, int pointCount = 0)
    {
        var entity = EntityRowStubBuilder.BuildClub(projection);
        return new ClubRowView(
            entity,
            projection.AthleteCount,
            EntityDisplayFormatter.FormatScoringPersonalCount(
                projection.EntryScoringCount,
                projection.EntryPersonalCount),
            EntityDisplayFormatter.FormatScoringPersonalCount(
                projection.RelayScoringCount,
                projection.RelayPersonalCount),
            pointCount);
    }

    private ClubRowView(Club entity, int athleteCount, string entryCount, string relayCount, int pointCount)
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
