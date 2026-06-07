using DataLayer.EfClasses;
using DataLayer.EfCore;
using Microsoft.EntityFrameworkCore;
using ServiceLayer.BaseTimeRepository;
using ServiceLayer.Crud;
using DataLayer;

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

    public (int min, int max, string? customLaneNames) GetPreviousLaneSettings()
    {
        var swimEvent = dbContext.SwimEvents
            .OrderByDescending(se => se.Order)
            .Select(se => new { se.LaneMin, se.LaneMax, se.CustomLaneNames })
            .FirstOrDefault();
        return swimEvent is null ? (0, 0, null) : (swimEvent.LaneMin, swimEvent.LaneMax, swimEvent.CustomLaneNames);
    }

    public TimeOnly? GetPreviousTime()
    {
        return dbContext.SwimEvents.OrderByDescending(se => se.Order).FirstOrDefault()?.Time;
    }

    public Course GetPreviousCourse()
    {
        return dbContext.SwimEvents
            .OrderByDescending(se => se.Order)
            .Select(se => (Course?)se.Course)
            .FirstOrDefault() ?? Course.LCM;
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
        StartTimeCalculationParameters parameters,
        CancellationToken cancellationToken = default)
    {
        if (swimEventIds.Count == 0)
            return;
        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            var events = await dbContext.SwimEvents
                .Where(swimEvent => swimEventIds.Contains(swimEvent.Id))
                .Include(swimEvent => swimEvent.Entries)
                .Include(swimEvent => swimEvent.Heats.OrderBy(heat => heat.Order))
                .ThenInclude(heat => heat.Positions)
                .ThenInclude(position => position.Entry)
                .Include(swimEvent => swimEvent.SwimStyle)
                .Include(swimEvent => swimEvent.AgeGroup)
                .OrderBy(swimEvent => swimEvent.Order)
                .ToListAsync(cancellationToken);
            var currentTime = parameters.StartTime;
            foreach (var swimEvent in events)
            {
                cancellationToken.ThrowIfCancellationRequested();
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
            cancellationToken.ThrowIfCancellationRequested();
            await dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }
}
