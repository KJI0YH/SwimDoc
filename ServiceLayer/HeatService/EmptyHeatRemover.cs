using DataLayer.EfCore;
using Microsoft.EntityFrameworkCore;
using ServiceLayer.EventService;

namespace ServiceLayer.HeatService;

internal static class EmptyHeatRemover
{
    public static async Task RemoveEmptyHeatsAndRefreshStatusAsync(
        EfCoreContext dbContext,
        int swimEventId,
        CancellationToken cancellationToken = default)
    {
        await RemoveOrphansAndEmptyHeatsAsync(dbContext, swimEventId, cancellationToken);
        if (dbContext.ChangeTracker.HasChanges())
            await dbContext.SaveChangesAsync(cancellationToken);
        await SwimEventStatusAdjuster.RefreshAsync(dbContext, swimEventId, cancellationToken);
    }

    public static async Task<int> RemoveOrphansAndEmptyHeatsAsync(
        EfCoreContext dbContext,
        int swimEventId,
        CancellationToken cancellationToken = default)
    {
        var orphanPositions = await dbContext.HeatPositions
            .Where(position => position.Heat.SwimEventId == swimEventId)
            .Where(position => !dbContext.Entries.Any(entry => entry.Id == position.EntryId))
            .ToListAsync(cancellationToken);
        if (orphanPositions.Count > 0)
            dbContext.HeatPositions.RemoveRange(orphanPositions);

        var emptyHeats = await dbContext.Heats
            .Where(heat => heat.SwimEventId == swimEventId)
            .Where(heat => !dbContext.HeatPositions.Any(position => position.HeatId == heat.Id))
            .ToListAsync(cancellationToken);
        if (emptyHeats.Count == 0)
            return 0;
        dbContext.Heats.RemoveRange(emptyHeats);
        return emptyHeats.Count;
    }
}
