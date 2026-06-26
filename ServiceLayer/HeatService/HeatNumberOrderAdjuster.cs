using DataLayer.EfClasses;
using DataLayer.EfCore;
using Microsoft.EntityFrameworkCore;

namespace ServiceLayer.HeatService;

internal static class HeatNumberOrderAdjuster
{
    public static async Task ShiftNumbersFromAsync(
        EfCoreContext dbContext,
        int swimEventId,
        int fromNumber,
        CancellationToken cancellationToken = default)
    {
        var hasConflict = await dbContext.Heats
            .AnyAsync(heat => heat.SwimEventId == swimEventId && heat.Number == fromNumber, cancellationToken);
        if (!hasConflict)
            return;
        var heats = await dbContext.Heats
            .Where(heat => heat.SwimEventId == swimEventId && heat.Number >= fromNumber)
            .ToListAsync(cancellationToken);
        foreach (var heat in heats)
            heat.Number++;
    }

    public static async Task ApplyNumberChangeAsync(
        EfCoreContext dbContext,
        int swimEventId,
        int heatId,
        int oldNumber,
        int newNumber,
        CancellationToken cancellationToken = default)
    {
        if (oldNumber == newNumber)
            return;
        var heats = await dbContext.Heats
            .Where(heat => heat.SwimEventId == swimEventId)
            .ToListAsync(cancellationToken);
        if (!heats.Any(heat => heat.Id != heatId && heat.Number == newNumber))
            return;
        if (newNumber < oldNumber)
        {
            foreach (var heat in heats.Where(heat =>
                         heat.Id != heatId &&
                         heat.Number >= newNumber &&
                         heat.Number < oldNumber))
                heat.Number++;
            return;
        }
        foreach (var heat in heats.Where(heat =>
                     heat.Id != heatId &&
                     heat.Number > oldNumber &&
                     heat.Number <= newNumber))
            heat.Number--;
    }

    public static async Task RecalculateGlobalOrdersAsync(
        EfCoreContext dbContext,
        CancellationToken cancellationToken = default)
    {
        var heats = await dbContext.Heats
            .Join(
                dbContext.SwimEvents,
                heat => heat.SwimEventId,
                swimEvent => swimEvent.Id,
                (heat, swimEvent) => new { heat, swimEvent.Order })
            .OrderBy(x => x.Order)
            .ThenBy(x => x.heat.Number)
            .ThenBy(x => x.heat.Id)
            .Select(x => x.heat)
            .ToListAsync(cancellationToken);
        for (var order = 1; order <= heats.Count; order++)
            heats[order - 1].Order = order;
    }
}
