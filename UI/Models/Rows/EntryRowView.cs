using DataLayer.EfClasses;
using Microsoft.Extensions.DependencyInjection;
using ServiceLayer.RankTimeProvider;
using UI.Helpers.Display;
using UI.Models.Rows.Projections;

namespace UI.Models.Rows;

public sealed class EntryRowView : IEntityRowView<Entry>
{
    private readonly IAchievedRankProvider _achievedRankProvider =
        App.Current.Services.GetRequiredService<IAchievedRankProvider>();

    public Entry Entity { get; }
    public int Id { get; }
    public string SwimName { get; }
    public string ParticipantName { get; }
    public string ParticipantBirthYear { get; }
    public Category? ParticipantCategory { get; }
    public string ParticipantClubName { get; }
    public string EntryTime { get; }
    public string FinishTime { get; }
    public string RankDisplay { get; }
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
        RankDisplay = Entity.SwimEvent is null
            ? string.Empty
            : _achievedRankProvider.Format(Entity.SwimEvent, Entity);
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
