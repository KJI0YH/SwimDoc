using DataLayer.EfClasses;
using UI.Helpers;

namespace UI.Models.Rows;

public sealed class EntryRowView(Entry entity) : IEntityRowView<Entry>
{
    public Entry Entity { get; } = entity;

    public int Id => Entity.Id;
    public string SwimName => EntityDisplayFormatter.FormatEntrySwimName(Entity);
    public string ParticipantName => EntityDisplayFormatter.FormatEntryParticipantName(Entity);
    public string ParticipantBirthYear => EntityDisplayFormatter.FormatEntryParticipantBirthYear(Entity);
    public string ParticipantClubName => EntityDisplayFormatter.FormatEntryParticipantClubName(Entity);
    public string EntryTime => EntityDisplayFormatter.FormatEntryTime(Entity);
    public string FinishTime => EntityDisplayFormatter.FormatFinishTime(Entity);
    public bool Scoring => Entity.Scoring;
    public EntryStatus Status => Entity.Status;
}
