using DataLayer.EfCore;
using Microsoft.EntityFrameworkCore;

namespace ServiceLayer.EventService;

internal static class SwimEventOrderAdjuster
{
    public static async Task ShiftOrdersFromAsync(
        EfCoreContext dbContext,
        int fromOrder,
        CancellationToken cancellationToken = default)
    {
        var hasConflict = await dbContext.SwimEvents
            .AnyAsync(swimEvent => swimEvent.Order == fromOrder, cancellationToken);
        if (!hasConflict)
            return;
        var events = await dbContext.SwimEvents
            .Where(swimEvent => swimEvent.Order >= fromOrder)
            .ToListAsync(cancellationToken);
        foreach (var swimEvent in events)
            swimEvent.Order++;
    }

    public static async Task ApplyOrderChangeAsync(
        EfCoreContext dbContext,
        int swimEventId,
        int oldOrder,
        int newOrder,
        CancellationToken cancellationToken = default)
    {
        if (oldOrder == newOrder)
            return;
        var events = await dbContext.SwimEvents.ToListAsync(cancellationToken);
        if (!events.Any(swimEvent => swimEvent.Id != swimEventId && swimEvent.Order == newOrder))
            return;
        if (newOrder < oldOrder)
        {
            foreach (var swimEvent in events.Where(swimEvent =>
                         swimEvent.Id != swimEventId &&
                         swimEvent.Order >= newOrder &&
                         swimEvent.Order < oldOrder))
                swimEvent.Order++;
            return;
        }
        foreach (var swimEvent in events.Where(swimEvent =>
                     swimEvent.Id != swimEventId &&
                     swimEvent.Order > oldOrder &&
                     swimEvent.Order <= newOrder))
            swimEvent.Order--;
    }
}
