using DataLayer.EfClasses;
using UI.Helpers.Display;
using UI.Models.Rows.Projections;

namespace UI.Models.Rows;

public sealed class SwimEventRowView : IEntityRowView<SwimEvent>
{
    public SwimEvent Entity { get; }
    public int Id => Entity.Id;
    public int Order => Entity.Order;
    public string Date => EntityDisplayFormatter.FormatSwimEventDate(Entity);
    public string Time => EntityDisplayFormatter.FormatSwimEventTime(Entity);
    public EventRound Round => Entity.Round;
    public SwimStyle SwimStyle => Entity.SwimStyle;
    public AgeGroup AgeGroup => Entity.AgeGroup;
    public string Lanes => EntityDisplayFormatter.FormatSwimEventLanes(Entity);
    public SwimEventStatus Status => Entity.Status;

    public SwimEventRowView(SwimEvent entity) => Entity = entity;

    public static SwimEventRowView FromProjection(SwimEventRowProjection projection) =>
        new(EntityRowStubBuilder.BuildSwimEvent(projection));
}
