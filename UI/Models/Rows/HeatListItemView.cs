using DataLayer.EfClasses;

namespace UI.Models.Rows;

public sealed class HeatListItemView(Heat entity) : IEntityRowView<Heat>
{
    public Heat Entity { get; } = entity;
    public int Id => Entity.Id;
    public string DisplayNumberWithTime => EntityDisplayFormatter.FormatHeatNumberWithTime(Entity);
    public string DisplayDayTime => EntityDisplayFormatter.FormatHeatDayTime(Entity);
}
