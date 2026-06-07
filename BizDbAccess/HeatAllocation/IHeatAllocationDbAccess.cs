using DataLayer.EfClasses;
using DataLayer.EfCore;
using Microsoft.EntityFrameworkCore;

namespace BizDbAccess;

public interface IHeatAllocationDbAccess
{
    ICollection<Entry> GetOrderedEntriesByEventId(int swimEventId);

    bool IsHeatsAllocated(int swimEventId);

    void DeleteExistedHeats(int swimEventId);
    void AddHeats(IEnumerable<Heat> heats);
}
