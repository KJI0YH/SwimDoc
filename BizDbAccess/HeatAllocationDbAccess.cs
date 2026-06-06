using DataLayer.EfClasses;
using DataLayer.EfCore;
using Microsoft.EntityFrameworkCore;

namespace BizDbAccess;

public class HeatAllocationDbAccess(EfCoreContext context) : IHeatAllocationDbAccess
{
    public ICollection<Entry> GetOrderedEntriesByEventId(int swimEventId)
    {
        return context.Entries
            .AsNoTracking()
            .Where(entry => entry.SwimEventId == swimEventId && entry.Status >= EntryStatus.EVENT)
            .OrderBy(entry => entry.EntryTime == null ? 1 : 0)
            .ThenBy(entry => entry.EntryTime)
            .ToList();
    }

    public bool IsEventStarted(int swimEventId)
    {
        return context.Heats.Any(heat =>
            heat.SwimEventId == swimEventId &&
            (heat.Status == HeatStatus.UNOFFICIAL || heat.Status == HeatStatus.OFFICIAL));
    }

    public bool IsHeatsAllocated(int swimEventId)
    {
        return context.Heats.Any(heat => heat.SwimEventId == swimEventId);
    }

    public void DeleteExistedHeats(int swimEventId)
    {
        context.Entries
            .Where(e => e.SwimEventId == swimEventId && e.Status >= EntryStatus.FINISH)
            .ExecuteUpdate(setters => setters
                .SetProperty(e => e.FinishTime, (int?)null)
                .SetProperty(e => e.Points, (int?)null)
                .SetProperty(e => e.Status, EntryStatus.EVENT));

        context.Heats
            .Where(heat => heat.SwimEventId == swimEventId)
            .ExecuteDelete();
    }

    public void AddHeats(IEnumerable<Heat> heats)
    {
        context.Heats.AddRange(heats);
    }
}
