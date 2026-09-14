using DataLayer.EfClasses;

namespace ServiceLayer.PointScoreProvider;

public interface IPointScoreProvider
{
    int CalculatePoints(
        Course course,
        int meters,
        Stroke stroke,
        int relayCount,
        Gender sex,
        int? finishTimeHundredths);

    void ApplyEventPoints(SwimEvent swimEvent, IReadOnlyList<Entry> entries);

    /// <summary>
    /// Applies points to <paramref name="entries"/>. In place-table mode, ranks using
    /// <paramref name="rankingEntries"/> (full event) with current heat values from <paramref name="entries"/> overriding.
    /// </summary>
    void ApplyEventPoints(
        SwimEvent swimEvent,
        IReadOnlyList<Entry> entries,
        IReadOnlyList<Entry>? rankingEntries);
}
