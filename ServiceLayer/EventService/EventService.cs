using DataLayer.EfClasses;
using DataLayer.EfCore;
using Microsoft.EntityFrameworkCore;
using ServiceLayer.BaseTimeRepository;
using ServiceLayer.Crud;

namespace ServiceLayer.EventService;

public class EventService(EfCoreContext dbContext, IBaseTimeRepository baseTimeRepository)
    : CrudService<SwimEvent, int?>(dbContext), IEventService
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

    public TimeOnly? GetPreviousTime()
    {
        return dbContext.SwimEvents.OrderByDescending(se => se.Order).FirstOrDefault()?.Time;
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

    public async Task CalculateStartTimesAsync(
        IReadOnlyList<int> swimEventIds,
        StartTimeCalculationParameters parameters)
    {
        if (swimEventIds.Count == 0)
            return;

        var events = await dbContext.SwimEvents
            .Where(swimEvent => swimEventIds.Contains(swimEvent.Id))
            .Include(swimEvent => swimEvent.Entries)
            .Include(swimEvent => swimEvent.Heats.OrderBy(heat => heat.Order))
            .ThenInclude(heat => heat.Positions)
            .ThenInclude(position => position.Entry)
            .Include(swimEvent => swimEvent.SwimStyle)
            .Include(swimEvent => swimEvent.AgeGroup)
            .OrderBy(swimEvent => swimEvent.Order)
            .ToListAsync();

        var currentTime = parameters.StartTime;

        foreach (var swimEvent in events)
        {
            swimEvent.Time = currentTime;
            foreach (var heat in swimEvent.Heats)
            {
                heat.DayTime = currentTime;
                var slowestHeatTime = heat.Positions.Max(position => position.Entry.EntryTime) ??
                                      swimEvent.Entries.Max(entry => entry.EntryTime) ??
                                      (int)Math.Truncate(baseTimeRepository.GetBaseTime(swimEvent.Course,
                                          swimEvent.SwimStyle.Distance,
                                          swimEvent.SwimStyle.Stroke, swimEvent.SwimStyle.RelayCount,
                                          swimEvent.AgeGroup.Gender) * 1.5);
                currentTime = currentTime.Add(TimeSpan.FromMilliseconds(slowestHeatTime * 10));
                currentTime = currentTime.Add(parameters.HeatPause);
            }

            currentTime = currentTime.Add(parameters.EventPause);
        }

        await dbContext.SaveChangesAsync();
    }
}