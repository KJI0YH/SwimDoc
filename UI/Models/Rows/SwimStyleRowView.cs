using DataLayer.EfClasses;
using UI.Helpers.Display;
using UI.Models.Rows.Projections;

namespace UI.Models.Rows;

public sealed class SwimStyleRowView : IEntityRowView<SwimStyle>
{
    public SwimStyle Entity { get; }
    public int Id => Entity.Id;
    public string DisplayName => EntityDisplayFormatter.FormatSwimStyle(Entity);
    public int Distance => Entity.Distance;
    public Stroke Stroke => Entity.Stroke;

    public SwimStyleRowView(SwimStyle entity) => Entity = entity;

    public static SwimStyleRowView FromProjection(SwimStyleRowProjection projection) =>
        new(EntityRowStubBuilder.BuildSwimStyle(projection));
}
