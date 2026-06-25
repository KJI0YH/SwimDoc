using System.Collections.Immutable;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using BizDbAccess;
using BizDbAccess.HeatAllocation;
using BizLogic.HeatAllocation;
using BizLogic.HeatAllocation.Concrete;
using ServiceLayer.Logging;
using DataLayer;
using DataLayer.Display;
using DataLayer.EfClasses;
using DataLayer.EfCore;
using DataLayer.QueryObjects;
using Microsoft.EntityFrameworkCore;
using ServiceLayer.BizRunners;
using ServiceLayer.Crud;
using ServiceLayer.HeatService.Exceptions;
using ServiceLayer.Logging;
using ServiceLayer.Resources;
using ValidationResult = System.ComponentModel.DataAnnotations.ValidationResult;

namespace ServiceLayer.HeatService;

public class HeatService(EfCoreContext dbContext, IAppLog log) : CrudService<Heat, int?>(dbContext, log), IHeatService
{
    private readonly RunnerWriteDb<HeatAllocationInDto, HeatAllocationOutDto> _runner = new(
        new HeatAllocationAction(new HeatAllocationDbAccess(dbContext), new AppBizLog(log)),
        dbContext);

    public HeatAllocationOutDto AllocateEntriesToHeats(HeatAllocationParameters parameters, bool saveChanges = true)
    {
        try
        {
            var swimEvent = dbContext.SwimEvents.AsNoTracking()
                .FirstOrDefault(swimEvent => swimEvent.Id == parameters.SwimEventId);
            if (swimEvent is null) throw new EntityNotFoundException($"No such swim event: {parameters.SwimEventId}");
            var dataIn = new HeatAllocationInDto(parameters, swimEvent);
            var result = _runner.RunAction(dataIn, saveChanges);
            if (_runner.HasErrors)
                throw new HeatAllocationException(_runner.Errors);
            if (saveChanges)
            {
                log.Info(
                    $"Allocate heats SwimEventId={parameters.SwimEventId}, heatOrder={parameters.HeatOrder}, minWeakHeatSize={parameters.MinHeatSize}, created={result.Heats.Count}");
                foreach (var heat in result.Heats)
                    log.Info(EntityLogFormatter.FormatOperation("Create", heat));
                foreach (var warning in result.Warnings)
                    log.Warning($"Allocate heats SwimEventId={parameters.SwimEventId}: {warning}");
            }
            return result;
        }
        finally
        {
            if (saveChanges)
                dbContext.ChangeTracker.Clear();
        }
    }

    public async Task DeleteSwimEventHeatsAsync(int swimEventId)
    {
        var heatCount = await dbContext.Heats.CountAsync(heat => heat.SwimEventId == swimEventId);
        var entriesInEventHeats = await dbContext.Entries
            .Include(e => e.HeatPosition)
            .Where(e => e.HeatPosition != null && e.HeatPosition.Heat.SwimEventId == swimEventId)
            .ToListAsync();
        foreach (var entry in entriesInEventHeats)
        {
            entry.ClearHeatResultData();
            entry.Status = EntryStatus.EVENT;
        }
        dbContext.Heats.RemoveRange(dbContext.Heats.Where(heat => heat.SwimEventId == swimEventId));
        await dbContext.SaveChangesAsync();
        log.Info($"Delete all heats SwimEventId={swimEventId}: {heatCount} heats");
    }

    public async Task DeleteHeatPositionAsync(int heatId, int entryId)
    {
        var position = await dbContext.HeatPositions
            .Include(heatPosition => heatPosition.Entry)
            .FirstOrDefaultAsync(heatPosition => heatPosition.HeatId == heatId && heatPosition.EntryId == entryId);
        if (position is null)
            return;
        log.Info(EntityLogFormatter.FormatOperation("Delete", position));
        position.Entry.ClearHeatResultData();
        dbContext.HeatPositions.Remove(position);
        await dbContext.SaveChangesAsync();
    }

    public async Task DeleteHeatAsync(int heatId)
    {
        var heat = await dbContext.Heats
            .Include(h => h.Positions)
            .ThenInclude(position => position.Entry)
            .FirstOrDefaultAsync(h => h.Id == heatId);
        if (heat is null)
            return;
        log.Info(EntityLogFormatter.FormatOperation("Delete", heat));
        foreach (var position in heat.Positions)
            position.Entry.ClearHeatResultData();
        foreach (var position in heat.Positions)
            position.Entry.Status = EntryStatus.EVENT;
        dbContext.Heats.Remove(heat);
        await dbContext.SaveChangesAsync();
    }

    public Task<int> GetNextHeatNumberAsync(int swimEventId)
    {
        var maxNumber = dbContext.Heats
            .Where(heat => heat.SwimEventId == swimEventId)
            .Select(heat => (int?)heat.Number)
            .Max();
        return Task.FromResult((maxNumber ?? 0) + 1);
    }

    public async Task<(Heat? heat, ImmutableList<ValidationResult> errors)> SaveHeatWithPositionsAsync(Heat heat,
        bool isAdd)
    {
        ArgumentNullException.ThrowIfNull(heat);
        var swimEvent = await dbContext.SwimEvents
            .AsNoTracking()
            .FirstOrDefaultAsync(se => se.Id == heat.SwimEventId);
        if (swimEvent is null)
            return (null, [new ValidationResult(string.Format(
                CultureInfo.CurrentUICulture,
                ServiceErrorStrings.Heat_Save_SwimEventNotFound_Format,
                heat.SwimEventId))]);
        var validationErrors = ValidateHeatWithPositions(heat, swimEvent);
        if (validationErrors.Count > 0)
            return (null, validationErrors);
        var entryIds = (heat.Positions ?? []).Select(position => position.EntryId).ToList();
        await RemoveEntriesFromOtherHeatsAsync(entryIds, isAdd ? null : heat.Id);
        if (isAdd)
        {
            heat.Status = HeatStatus.NOT_STARTED;
            heat.Positions ??= [];
            foreach (var position in heat.Positions)
                position.Heat = heat;
            dbContext.Heats.Add(heat);
            await dbContext.SaveChangesAsync();
            await RecalculateHeatOrdersAsync();
            await dbContext.Entry(heat).ReloadAsync();
            log.Info(EntityLogFormatter.FormatOperation("Create", heat));
            return (heat, ImmutableList<ValidationResult>.Empty);
        }
        DetachHeatGraphIfTracked(heat);
        var trackedHeat = await dbContext.Heats
            .FirstOrDefaultAsync(h => h.Id == heat.Id);
        if (trackedHeat is null)
            return (null, [new ValidationResult(string.Format(
                CultureInfo.CurrentUICulture,
                ServiceErrorStrings.Heat_Save_HeatNotFound_Format,
                heat.Id))]);
        trackedHeat.Number = heat.Number;
        trackedHeat.DayTime = heat.DayTime;
        trackedHeat.Status = heat.Status;
        var existingPositions = await dbContext.HeatPositions
            .Where(position => position.HeatId == heat.Id)
            .ToListAsync();
        if (existingPositions.Count > 0)
            dbContext.HeatPositions.RemoveRange(existingPositions);
        foreach (var position in heat.Positions ?? [])
        {
            dbContext.HeatPositions.Add(new HeatPosition
            {
                HeatId = trackedHeat.Id,
                Lane = position.Lane,
                EntryId = position.EntryId
            });
        }
        await dbContext.SaveChangesAsync();
        await RecalculateHeatOrdersAsync();
        await dbContext.Entry(trackedHeat).ReloadAsync();
        log.Info(EntityLogFormatter.FormatOperation("Update", heat));
        return (trackedHeat, ImmutableList<ValidationResult>.Empty);
    }

    private async Task RecalculateHeatOrdersAsync()
    {
        var heats = await dbContext.Heats
            .Join(
                dbContext.SwimEvents,
                heat => heat.SwimEventId,
                swimEvent => swimEvent.Id,
                (heat, swimEvent) => new { heat, swimEvent.Order })
            .OrderBy(x => x.Order)
            .ThenBy(x => x.heat.Number)
            .ThenBy(x => x.heat.Id)
            .Select(x => x.heat)
            .ToListAsync();
        for (var order = 1; order <= heats.Count; order++)
            heats[order - 1].Order = order;
        if (heats.Count > 0)
            await dbContext.SaveChangesAsync();
    }

    private void DetachHeatGraphIfTracked(Heat heat)
    {
        var heatEntry = dbContext.Entry(heat);
        if (heatEntry.State == EntityState.Detached)
            return;
        foreach (var position in heat.Positions ?? [])
        {
            var positionEntry = dbContext.Entry(position);
            if (positionEntry.State != EntityState.Detached)
                positionEntry.State = EntityState.Detached;
        }
        heatEntry.State = EntityState.Detached;
    }

    private ImmutableList<ValidationResult> ValidateHeatWithPositions(Heat heat, SwimEvent swimEvent)
    {
        var errors = new List<ValidationResult>();
        if (heat.Number < 1)
            errors.Add(new ValidationResult(ServiceErrorStrings.Heat_Number_MinOne, [nameof(Heat.Number)]));
        var positions = heat.Positions?.ToList() ?? [];
        var maxPositions = SwimEventLaneNames.GetLaneCount(swimEvent);
        if (positions.Count > maxPositions)
            errors.Add(new ValidationResult(
                string.Format(
                    CultureInfo.CurrentUICulture,
                    ServiceErrorStrings.Heat_Validation_MaxPositions_Format,
                    maxPositions,
                    SwimEventLaneNames.FormatLanesSummary(swimEvent)),
                [nameof(Heat.Positions)]));
        var duplicateLanes = positions.GroupBy(position => position.Lane).Where(g => g.Count() > 1).Select(g => g.Key);
        foreach (var lane in duplicateLanes)
            errors.Add(new ValidationResult(
                string.Format(
                    CultureInfo.CurrentUICulture,
                    ServiceErrorStrings.Heat_Validation_DuplicateLane_Format,
                    SwimEventLaneNames.GetLaneDisplay(swimEvent, lane)),
                [nameof(HeatPosition.Lane)]));
        var duplicateEntries = positions.GroupBy(position => position.EntryId).Where(g => g.Count() > 1)
            .Select(g => g.Key);
        foreach (var entryId in duplicateEntries)
            errors.Add(new ValidationResult(string.Format(
                    CultureInfo.CurrentUICulture,
                    ServiceErrorStrings.Heat_Validation_DuplicateEntry_Format,
                    entryId),
                [nameof(HeatPosition.EntryId)]));
        foreach (var position in positions)
        {
            if (!SwimEventLaneNames.IsLaneInRange(swimEvent, position.Lane))
                errors.Add(new ValidationResult(
                    string.Format(
                        CultureInfo.CurrentUICulture,
                        ServiceErrorStrings.Heat_Validation_LaneOutOfRange_Format,
                        SwimEventLaneNames.GetLaneDisplay(swimEvent, position.Lane),
                        SwimEventLaneNames.FormatLanesSummary(swimEvent)),
                    [nameof(HeatPosition.Lane)]));
            var entryExists = dbContext.Entries.Any(entry =>
                entry.Id == position.EntryId && entry.SwimEventId == swimEvent.Id);
            if (!entryExists)
                errors.Add(new ValidationResult(string.Format(
                        CultureInfo.CurrentUICulture,
                        ServiceErrorStrings.Heat_Validation_EntryNotInEvent_Format,
                        position.EntryId),
                    [nameof(HeatPosition.EntryId)]));
        }
        return errors.ToImmutableList();
    }

    private async Task RemoveEntriesFromOtherHeatsAsync(IReadOnlyCollection<int> entryIds, int? exceptHeatId)
    {
        if (entryIds.Count == 0)
            return;
        var query = dbContext.HeatPositions.Where(position => entryIds.Contains(position.EntryId));
        if (exceptHeatId.HasValue)
            query = query.Where(position => position.HeatId != exceptHeatId.Value);
        var positionsToRemove = await query.ToListAsync();
        if (positionsToRemove.Count == 0)
            return;
        dbContext.HeatPositions.RemoveRange(positionsToRemove);
        await dbContext.SaveChangesAsync();
    }

    public Task<List<Heat>> GetHeatsByEventIdAsync(int eventId) =>
        HeatsByEventQuery(eventId).ToListAsync();

    public Task<List<Heat>> GetHeatsByEventIdPagedAsync(int eventId, int page, int pageSize) =>
        HeatsByEventQuery(eventId).Page(page, pageSize).ToListAsync();

    private IQueryable<Heat> HeatsByEventQuery(int eventId) =>
        dbContext.Heats
            .AsNoTracking()
            .Where(heat => heat.SwimEventId == eventId)
            .OrderBy(heat => heat.Number)
            .Include(heat => heat.Positions.OrderBy(hp => hp.Lane))
            .ThenInclude(hp => hp.Entry)
            .ThenInclude(entry => entry.Athlete!)
            .ThenInclude(athlete => athlete.Club)
            .Include(heat => heat.Positions.OrderBy(hp => hp.Lane))
            .ThenInclude(hp => hp.Entry)
            .ThenInclude(entry => entry.Relay!)
            .ThenInclude(relay => relay.Positions)
            .ThenInclude(p => p.Athlete)
            .ThenInclude(a => a.Club)
            .Include(heat => heat.Positions.OrderBy(hp => hp.Lane))
            .ThenInclude(hp => hp.Entry)
            .ThenInclude(entry => entry.Relay!)
            .ThenInclude(relay => relay.Club)
            .Include(heat => heat.Positions.OrderBy(hp => hp.Lane))
            .ThenInclude(hp => hp.Entry)
            .ThenInclude(entry => entry.SwimStyle)
            .Include(heat => heat.Positions.OrderBy(hp => hp.Lane))
            .ThenInclude(hp => hp.Entry)
            .ThenInclude(entry => entry.SwimEvent);

    public async Task ApproveHeatAsync(Heat incomingHeat)
    {
        ArgumentNullException.ThrowIfNull(incomingHeat);
        if (incomingHeat.Positions is null)
            throw new ValidationException(ServiceErrorStrings.Heat_Approve_PositionsMissing);
        var trackedHeat = await dbContext.Heats
            .Include(h => h.Positions)
            .ThenInclude(p => p.Entry)
            .FirstOrDefaultAsync(h => h.Id == incomingHeat.Id);
        if (trackedHeat is null)
            throw new EntityNotFoundException($"No such heat: {incomingHeat.Id}");
        if (trackedHeat.Status == HeatStatus.OFFICIAL)
            return;
        var incomingByEntryId = incomingHeat.Positions.ToDictionary(p => p.EntryId, p => p.Entry);
        foreach (var trackedPosition in trackedHeat.Positions)
        {
            if (!incomingByEntryId.TryGetValue(trackedPosition.EntryId, out var incomingEntry))
                throw new ValidationException(string.Format(
                    CultureInfo.CurrentUICulture,
                    ServiceErrorStrings.Heat_Approve_NoResultForEntry_Format,
                    trackedPosition.EntryId));
            trackedPosition.Entry.Status = incomingEntry.Status;
            trackedPosition.Entry.FinishTime = incomingEntry.FinishTime;
            trackedPosition.Entry.Comment = incomingEntry.Comment;
            trackedPosition.Entry.Points = incomingEntry.Points;
            trackedPosition.Entry.ApplyNonFinishResultRules();
            dbContext.NormalizeEntry(trackedPosition.Entry);
        }
        foreach (var position in trackedHeat.Positions)
        {
            var entry = position.Entry;
            var isResultProvided = entry.Status switch
            {
                EntryStatus.FINISH => entry.FinishTime.HasValue,
                EntryStatus.DNS or EntryStatus.DNF or EntryStatus.DSQ => true,
                _ => false
            };
            if (!isResultProvided)
                throw new ValidationException(ServiceErrorStrings.Heat_Approve_NotAllLaneResultsProvided);
        }
        trackedHeat.Status = HeatStatus.OFFICIAL;
        await dbContext.SaveChangesAsync();
        log.Info(EntityLogFormatter.FormatOperation("Approve", trackedHeat));
    }

    public async Task UnapproveHeatAsync(int heatId)
    {
        var heat = await dbContext.Heats.FirstOrDefaultAsync(h => h.Id == heatId);
        if (heat is null) throw new EntityNotFoundException($"No such heat: {heatId}");
        if (heat.Status != HeatStatus.OFFICIAL) return;
        heat.Status = HeatStatus.UNOFFICIAL;
        await dbContext.SaveChangesAsync();
        log.Info(EntityLogFormatter.FormatOperation("Unapprove", heat));
    }

    public int GetTotalHeats()
    {
        return dbContext.Heats.Count();
    }

    public int GetTotalHeatsInEvent(int swimEventId)
    {
        return dbContext.Heats.Count(heat => heat.SwimEventId == swimEventId);
    }
}
