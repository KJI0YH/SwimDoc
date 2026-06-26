using DataLayer.EfClasses;
using DataLayer.EfCore;
using Microsoft.EntityFrameworkCore;

namespace ServiceLayer.EventService;

internal static class SwimEventStatusAdjuster
{
    public static async Task RefreshAsync(
        EfCoreContext dbContext,
        int swimEventId,
        CancellationToken cancellationToken = default)
    {
        var newStatus = await CalculateStatusAsync(dbContext, swimEventId, cancellationToken);
        var rowsUpdated = await dbContext.Database.ExecuteSqlInterpolatedAsync(
            $"""
             UPDATE SwimEvents
             SET Status = {(int)newStatus}
             WHERE Id = {swimEventId}
               AND Status != {(int)newStatus}
             """,
            cancellationToken);
        if (rowsUpdated == 0)
            return;
        var tracked = dbContext.SwimEvents.Local.FirstOrDefault(swimEvent => swimEvent.Id == swimEventId);
        if (tracked is not null)
            tracked.Status = newStatus;
    }

    public static async Task<SwimEventStatus> CalculateStatusAsync(
        EfCoreContext dbContext,
        int swimEventId,
        CancellationToken cancellationToken = default)
    {
        var entryCount = await dbContext.Entries
            .AsNoTracking()
            .CountAsync(entry => entry.SwimEventId == swimEventId, cancellationToken);
        var heatStatuses = await dbContext.Heats
            .AsNoTracking()
            .Where(heat => heat.SwimEventId == swimEventId)
            .Select(heat => heat.Status)
            .ToListAsync(cancellationToken);
        if (entryCount == 0 && heatStatuses.Count == 0)
            return SwimEventStatus.EMPTY;
        if (heatStatuses.Count == 0 && entryCount > 0)
            return SwimEventStatus.ENTRY;
        if (heatStatuses.Count > 0 && heatStatuses.All(status => status == HeatStatus.NOT_STARTED))
            return SwimEventStatus.NOT_STARTED;
        if (heatStatuses.Count > 0 && heatStatuses.All(status => status == HeatStatus.OFFICIAL))
            return SwimEventStatus.OFFICIAL;
        return SwimEventStatus.RUNNING;
    }
}
