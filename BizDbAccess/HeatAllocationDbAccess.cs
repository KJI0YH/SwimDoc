using DataLayer.EfClasses;
using DataLayer.EfCore;
using Microsoft.EntityFrameworkCore;

namespace BizDbAccess;

public interface IHeatAllocationDbAccess
{
    ICollection<Entry> GetOrderedEntriesByEventId(int swimEventId);
    bool IsHeatsExists(int swimEventId);
    void AddHeats(IEnumerable<Heat> heats);
}

public class HeatAllocationDbAccess(EfCoreContext context) : IHeatAllocationDbAccess
{
    public ICollection<Entry> GetOrderedEntriesByEventId(int swimEventId)
    {
        return context.Entries
            .AsNoTracking()
            .Where(entry => entry.SwimEventId == swimEventId)
            .OrderBy(entry => entry.EntryTime == null ? 1 : 0)
            .ThenBy(entry => entry.EntryTime)
            .ToList();
    }

    public bool IsHeatsExists(int swimEventId)
    {
        return context.Heats.Any(heat => heat.SwimEventId == swimEventId);
    }

    public void AddHeats(IEnumerable<Heat> heats)
    {
        context.Heats.AddRange(heats);
    }
}