using DataLayer.EfClasses;
using UI.Helpers;

namespace UI.Models.Rows;

public sealed class SwimEventRowView(SwimEvent entity) : IEntityRowView<SwimEvent>
{
    public SwimEvent Entity { get; } = entity;

    public int Id => Entity.Id;
    public int Order => Entity.Order;
    public string Date => EntityDisplayFormatter.FormatSwimEventDate(Entity);
    public string Time => EntityDisplayFormatter.FormatSwimEventTime(Entity);
    public EventRound Round => Entity.Round;
    public SwimStyle SwimStyle => Entity.SwimStyle;
    public AgeGroup AgeGroup => Entity.AgeGroup;
    public string Lanes => EntityDisplayFormatter.FormatSwimEventLanes(Entity);
    public SwimEventStatus Status => Entity.Status;
}
