using DataLayer.EfClasses;

namespace BizDbAccess.HeatAllocation;

public interface IHeatAllocationDbAccess
{
    ICollection<Entry> GetOrderedEntriesByEventId(int swimEventId);
    bool IsHeatsAllocated(int swimEventId);
    void DeleteExistedHeats(int swimEventId);
    void AddHeats(IEnumerable<Heat> heats);
}
