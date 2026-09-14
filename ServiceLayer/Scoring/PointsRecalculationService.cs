using DataLayer.EfClasses;
using DataLayer.EfCore;
using Microsoft.EntityFrameworkCore;
using ServiceLayer.Logging;
using ServiceLayer.PointScoreProvider;

namespace ServiceLayer.Scoring;

public interface IPointsRecalculationService
{
    Task RecalculateAllAsync(CancellationToken cancellationToken = default);
    Task RecalculateSwimEventAsync(int swimEventId, CancellationToken cancellationToken = default);
}

public sealed class PointsRecalculationService(
    EfCoreContext dbContext,
    IPointScoreProvider pointScoreProvider,
    IAppLog log) : IPointsRecalculationService
{
    public async Task RecalculateAllAsync(CancellationToken cancellationToken = default)
    {
        var swimEvents = await dbContext.SwimEvents
            .Include(e => e.SwimStyle)
            .Include(e => e.AgeGroup)
            .Include(e => e.Entries)
            .ThenInclude(entry => entry.SwimStyle)
            .Where(e => e.Entries.Any(entry => entry.Status >= EntryStatus.FINISH))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        foreach (var swimEvent in swimEvents)
            pointScoreProvider.ApplyEventPoints(swimEvent, swimEvent.Entries.ToList());

        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        log.Info($"Recalculated points for {swimEvents.Count} swim event(s)");
    }

    public async Task RecalculateSwimEventAsync(int swimEventId, CancellationToken cancellationToken = default)
    {
        var swimEvent = await dbContext.SwimEvents
            .Include(e => e.SwimStyle)
            .Include(e => e.AgeGroup)
            .Include(e => e.Entries)
            .ThenInclude(entry => entry.SwimStyle)
            .FirstOrDefaultAsync(e => e.Id == swimEventId, cancellationToken)
            .ConfigureAwait(false);
        if (swimEvent is null)
            return;

        pointScoreProvider.ApplyEventPoints(swimEvent, swimEvent.Entries.ToList());
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        log.Info($"Recalculated points for swim event {swimEventId}");
    }
}
