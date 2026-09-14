using DataLayer.EfClasses;
using ServiceLayer.PointScoreProvider;

namespace Tests.Helpers;

internal sealed class NoOpPointScoreProvider : IPointScoreProvider
{
    public int CalculatePoints(
        Course course,
        int meters,
        Stroke stroke,
        int relayCount,
        Gender sex,
        int? finishTimeHundredths) => 0;

    public void ApplyEventPoints(SwimEvent swimEvent, IReadOnlyList<Entry> entries)
    {
    }

    public void ApplyEventPoints(
        SwimEvent swimEvent,
        IReadOnlyList<Entry> entries,
        IReadOnlyList<Entry>? rankingEntries)
    {
    }
}
