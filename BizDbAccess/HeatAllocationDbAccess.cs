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

public class HeatAllocationDbAccess : IHeatAllocationDbAccess
{
    private readonly EfCoreContext _context;

    public HeatAllocationDbAccess(EfCoreContext context)
    {
        _context = context;
    }

    public ICollection<Entry> GetOrderedEntriesByEventId(int swimEventId)
    {
        return _context.Entries
            .AsNoTracking()
            .Where(entry => entry.SwimEventId == swimEventId)
            .OrderBy(entry => entry.EntryTime == null ? 1 : 0)
            .ThenBy(entry => entry.EntryTime)
            .ToList();
    }

    public bool IsHeatsExists(int swimEventId)
    {
        return _context.Heats.Any(heat => heat.SwimEventId == swimEventId);
    }

    public void AddHeats(IEnumerable<Heat> heats)
    {
        _context.Heats.AddRange(heats);
    }
}