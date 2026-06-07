using DataLayer.EfClasses;

namespace UI.Models.Rows.Projections;

public sealed class SwimStyleRowProjection
{
    public int Id { get; init; }
    public int Distance { get; init; }
    public Stroke Stroke { get; init; }
    public int RelayCount { get; init; }
    public bool IsRelay { get; init; }
}
