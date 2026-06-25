using DataLayer.EfClasses;
using UI.Helpers.Display;
using UI.Models.Rows.Projections;
using UI.Resources;

namespace UI.Models.Rows;

public sealed class SwimEventRowView : IEntityRowView<SwimEvent>
{
    public SwimEvent Entity { get; }
    public int Id => Entity.Id;
    public int Order => Entity.Order;
    public string Date => EntityDisplayFormatter.FormatSwimEventDate(Entity);
    public string Time => EntityDisplayFormatter.FormatSwimEventTime(Entity);
    public EventRound Round => Entity.Round;
    public Course Course => Entity.Course;
    public SwimStyle SwimStyle => Entity.SwimStyle;
    public AgeGroup AgeGroup => Entity.AgeGroup;
    public string Lanes => EntityDisplayFormatter.FormatSwimEventLanes(Entity);
    public string RoundParticipantsCount =>
        Entity.RoundParticipantsCount?.ToString() ?? Strings.Common_All;
    public SwimEventStatus Status => Entity.Status;
    public int EntryCount { get; }
    public int HeatCount { get; }

    public SwimEventRowView(SwimEvent entity, int entryCount = 0, int heatCount = 0)
    {
        Entity = entity;
        EntryCount = entryCount;
        HeatCount = heatCount;
    }

    public static SwimEventRowView FromProjection(SwimEventRowProjection projection) =>
        new(EntityRowStubBuilder.BuildSwimEvent(projection), projection.EntryCount, projection.HeatCount);
}
