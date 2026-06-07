using DataLayer.EfClasses;

namespace UI.Models.Rows;

public sealed class ClubRowView(Club entity) : IEntityRowView<Club>
{
    public Club Entity { get; } = entity;
    public int Id => Entity.Id;
    public string Name => Entity.Name;
    public int AthleteCount => EntityDisplayFormatter.FormatClubAthleteCount(Entity);
    public int EntryCount => EntityDisplayFormatter.FormatClubEntryCount(Entity);
    public int RelayCount => EntityDisplayFormatter.FormatClubRelayCount(Entity);
    public int PointCount => EntityDisplayFormatter.FormatClubPointCount(Entity);
}
