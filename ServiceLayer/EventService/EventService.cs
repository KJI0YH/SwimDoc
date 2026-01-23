using DataLayer.EfClasses;
using DataLayer.EfCore;
using ServiceLayer.Crud;

namespace ServiceLayer.EventService;

public class EventService(EfCoreContext dbContext) : CrudService<SwimEvent, int?>(dbContext), IEventService
{
    public int GetNextOrderNumber()
    {
        var total = dbContext.SwimEvents.Count();
        var max = total > 0 ? dbContext.SwimEvents.Max(se => se.Order) : 0;
        return Math.Max(total, max) + 1;
    }
}