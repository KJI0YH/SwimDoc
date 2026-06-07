using DataLayer.EfClasses;

namespace UI.Models.Rows;

public sealed class SwimStyleRowView(SwimStyle entity) : IEntityRowView<SwimStyle>
{
    public SwimStyle Entity { get; } = entity;
    public int Id => Entity.Id;
    public string DisplayName => EntityDisplayFormatter.FormatSwimStyle(Entity);
    public int Distance => Entity.Distance;
    public Stroke Stroke => Entity.Stroke;
}
