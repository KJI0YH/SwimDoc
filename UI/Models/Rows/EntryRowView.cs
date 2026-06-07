using DataLayer.EfClasses;
using UI.Helpers.Display;
using UI.Models.Rows.Projections;

namespace UI.Models.Rows;

public sealed class EntryRowView : IEntityRowView<Entry>
{
    public Entry Entity { get; }
    public int Id { get; }
    public string SwimName { get; }
    public string ParticipantName { get; }
    public string ParticipantBirthYear { get; }
    public Category? ParticipantCategory { get; }
    public string ParticipantClubName { get; }
    public string EntryTime { get; }
    public string FinishTime { get; }
    public bool Scoring { get; }
    public EntryStatus Status { get; }
    public int? Points { get; }
    public string? Comment { get; }

    public EntryRowView(Entry entity)
    {
        Entity = entity;
        Id = Entity.Id;
        SwimName = EntityDisplayFormatter.FormatEntrySwimName(Entity);
        ParticipantName = EntityDisplayFormatter.FormatEntryParticipantName(Entity);
        ParticipantBirthYear = EntityDisplayFormatter.FormatEntryParticipantBirthYear(Entity);
        ParticipantCategory = Entity.Athlete?.Category;
        ParticipantClubName = EntityDisplayFormatter.FormatEntryParticipantClubName(Entity);
        EntryTime = EntityDisplayFormatter.FormatEntryTime(Entity);
        FinishTime = EntityDisplayFormatter.FormatFinishTime(Entity);
        Scoring = Entity.Scoring;
        Status = Entity.Status;
        Points = Entity.Points;
        Comment = Entity.Comment;
    }

    public static EntryRowView FromProjection(EntryRowProjection projection)
    {
        var entity = EntityRowStubBuilder.BuildEntry(projection);
        return new EntryRowView(entity);
    }
}
