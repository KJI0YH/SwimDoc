using DataLayer.EfClasses;
using DataLayer.EfCore;
using Microsoft.EntityFrameworkCore;
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

    public (int min, int max) GetPreviousLanes()
    {
        var swimEvent = dbContext.SwimEvents.OrderByDescending(se => se.Order).FirstOrDefault();
        return swimEvent is null ? (0, 0) : (swimEvent.LaneMin, swimEvent.LaneMax);
    }

    public Task<List<SwimEvent>> GetIndividualEventsAsync()
    {
        return dbContext.SwimEvents
            .AsNoTracking()
            .Include(se => se.AgeGroup)
            .Include(se => se.SwimStyle)
            .Where(se => se.SwimStyle.RelayCount == 0)
            .ToListAsync();
    }

    public Task<List<SwimEvent>> GetRelayEventsAsync()
    {
        return dbContext.SwimEvents
            .AsNoTracking()
            .Include(se => se.AgeGroup)
            .Include(se => se.SwimStyle)
            .Where(se => se.SwimStyle.RelayCount > 0)
            .ToListAsync();
    }
}