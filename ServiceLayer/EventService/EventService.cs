using System.Collections.Immutable;
using System.ComponentModel.DataAnnotations;
using DataLayer.EfClasses;
using DataLayer.EfCore;
using Microsoft.EntityFrameworkCore;
using ServiceLayer.BaseTimeRepository;
using ServiceLayer.Crud;
using ServiceLayer.HeatService;
using ServiceLayer.Logging;
using DataLayer;

namespace ServiceLayer.EventService;

public class EventService(EfCoreContext dbContext, IBaseTimeRepository baseTimeRepository, IAppLog log)
    : CrudService<SwimEvent, int?>(dbContext, log), IEventService
{
    public override async Task<(SwimEvent? entity, ImmutableList<ValidationResult> errors)> CreateAsync(
        SwimEvent entity,
        CancellationToken cancellationToken = default)
    {
        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            await SwimEventOrderAdjuster.ShiftOrdersFromAsync(dbContext, entity.Order, cancellationToken);
            await dbContext.SwimEvents.AddAsync(entity, cancellationToken);
            var errors = await dbContext.SaveChangesWithValidationAsync();
            if (errors.Count > 0)
            {
                dbContext.Entry(entity).State = EntityState.Detached;
                await transaction.RollbackAsync(cancellationToken);
                return (entity, errors);
            }
            await HeatNumberOrderAdjuster.RecalculateGlobalOrdersAsync(dbContext, cancellationToken);
            await dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            log.Info(EntityLogFormatter.FormatOperation("Create", entity));
            return (entity, errors);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public override async Task<(SwimEvent? entity, ImmutableList<ValidationResult> errors)> UpdateAsync(
        SwimEvent entity,
        CancellationToken cancellationToken = default)
    {
        if (entity.Id == 0)
            throw new InvalidOperationException("Cannot update entity with null Id.");
        var id = entity.Id;
        var trackedEntity = await dbContext.SwimEvents.FindAsync([id], cancellationToken);
        if (trackedEntity is null)
            throw new InvalidOperationException($"Entity with Id {id} was not found in the database.");
        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            var oldOrder = trackedEntity.Order;
            var newOrder = entity.Order;
            if (oldOrder != newOrder)
                await SwimEventOrderAdjuster.ApplyOrderChangeAsync(dbContext, id, oldOrder, newOrder, cancellationToken);
            dbContext.Entry(trackedEntity).CurrentValues.SetValues(entity);
            trackedEntity.Order = newOrder;
            dbContext.Entry(trackedEntity).State = EntityState.Modified;
            var errors = await dbContext.SaveChangesWithValidationAsync();
            if (errors.Count > 0)
            {
                await dbContext.Entry(trackedEntity).ReloadAsync(cancellationToken).ConfigureAwait(false);
                await transaction.RollbackAsync(cancellationToken);
                return (trackedEntity, errors);
            }
            if (oldOrder != newOrder)
            {
                await HeatNumberOrderAdjuster.RecalculateGlobalOrdersAsync(dbContext, cancellationToken);
                await dbContext.SaveChangesAsync(cancellationToken);
            }
            await transaction.CommitAsync(cancellationToken);
            log.Info(EntityLogFormatter.FormatOperation("Update", trackedEntity));
            return (trackedEntity, errors);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public override async Task DeleteAsync(int? id, CancellationToken cancellationToken = default)
    {
        if (id is not int swimEventId)
            return;
        var entity = await dbContext.SwimEvents.FindAsync([swimEventId], cancellationToken);
        if (entity is null)
            return;
        log.Info(EntityLogFormatter.FormatOperation("Delete", entity));
        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            dbContext.SwimEvents.Remove(entity);
            await dbContext.SaveChangesAsync(cancellationToken);
            await HeatNumberOrderAdjuster.RecalculateGlobalOrdersAsync(dbContext, cancellationToken);
            await dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

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

    public DateOnly GetPreviousDate()
    {
        return dbContext.SwimEvents
            .OrderByDescending(se => se.Order)
            .Select(se => (DateOnly?)se.Date)
            .FirstOrDefault() ?? DateOnly.FromDateTime(DateTime.Today);
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
            .OrderBy(se => se.Order)
            .ThenBy(se => se.Date)
            .ToListAsync();
    }

    public Task<List<SwimEvent>> GetRelayEventsAsync()
    {
        return dbContext.SwimEvents
            .AsNoTracking()
            .Include(se => se.AgeGroup)
            .Include(se => se.SwimStyle)
            .Where(se => se.SwimStyle.RelayCount > 0)
            .OrderBy(se => se.Order)
            .ThenBy(se => se.Date)
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
            foreach (var swimEvent in events)
                log.Info(EntityLogFormatter.FormatOperation("Update", swimEvent));
            log.Info(
                $"Calculate start times: {events.Count} events, start={parameters.StartTime:HH\\:mm}, heatPause={parameters.HeatPause}, eventPause={parameters.EventPause}, swimEventIds=[{EntityLogFormatter.FormatIdList(swimEventIds)}]");
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }
}
