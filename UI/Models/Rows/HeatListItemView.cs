using DataLayer.EfClasses;
using UI.Resources;

namespace UI.Models.Rows;

public sealed class HeatListItemView(Heat entity) : IEntityRowView<Heat>
{
    public Heat Entity { get; } = entity;
    public int Id => Entity.Id;
    public string DisplayDayTime => EntityDisplayFormatter.FormatHeatDayTime(Entity);
    public string DisplayNumber => string.Format(Strings.Fixation_HeatListItem_Format, Entity.Number, Entity.Order);
}
