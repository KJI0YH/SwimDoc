using DataLayer.EfClasses;
using DataLayer.EfCore;
using Microsoft.EntityFrameworkCore;

namespace BizDbAccess;

public interface IHeatAllocationDbAccess
{
    ICollection<Entry> GetOrderedEntriesByEventId(int swimEventId);
    bool IsEventStarted(int swimEventId);

    bool IsHeatsAllocated(int swimEventId);

    void DeleteExistedHeats(int swimEventId);
    void AddHeats(IEnumerable<Heat> heats);
}

public class HeatAllocationDbAccess(EfCoreContext context) : IHeatAllocationDbAccess
{
    public ICollection<Entry> GetOrderedEntriesByEventId(int swimEventId)
    {
        return context.Entries
            .AsNoTracking()
            .Where(entry => entry.SwimEventId == swimEventId && entry.Status == EntryStatus.EVENT)
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
        context.Heats
            .Where(heat => heat.SwimEventId == swimEventId)
            .ExecuteDelete();
    }

    public void AddHeats(IEnumerable<Heat> heats)
    {
        context.Heats.AddRange(heats);
    }
}