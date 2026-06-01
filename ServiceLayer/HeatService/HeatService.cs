using System.Collections.Immutable;
using System.ComponentModel.DataAnnotations;
using BizDbAccess;
using BizLogic.HeatLogic;
using BizLogic.HeatLogic.Concrete;
using DataLayer;
using DataLayer.EfClasses;
using DataLayer.EfCore;
using DataLayer.QueryObjects;
using Microsoft.EntityFrameworkCore;
using ServiceLayer.BizRunners;
using ServiceLayer.Crud;
using ServiceLayer.HeatService.Exceptions;
using ValidationResult = System.ComponentModel.DataAnnotations.ValidationResult;

namespace ServiceLayer.HeatService;

public class HeatService(EfCoreContext dbContext) : CrudService<Heat, int?>(dbContext), IHeatService
{
    private readonly RunnerWriteDb<HeatAllocationInDto, HeatAllocationOutDto> _runner = new(
        new HeatAllocationAction(new HeatAllocationDbAccess(dbContext)),
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
            return _runner.HasErrors ? throw new HeatAllocationException(_runner.Errors) : result;
        }
        finally
        {
            if (saveChanges)
                dbContext.ChangeTracker.Clear();
        }
    }

    public async Task DeleteSwimEventHeatsAsync(int swimEventId)
    {
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
    }

    public async Task DeleteHeatPositionAsync(int heatId, int entryId)
    {
        var position = await dbContext.HeatPositions
            .Include(heatPosition => heatPosition.Entry)
            .FirstOrDefaultAsync(heatPosition => heatPosition.HeatId == heatId && heatPosition.EntryId == entryId);
        if (position is null)
            return;

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
            return (null, [new ValidationResult($"Событие {heat.SwimEventId} не найдено.")]);

        var validationErrors = ValidateHeatWithPositions(heat, swimEvent, isAdd);
        if (validationErrors.Count > 0)
            return (null, validationErrors);

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
            return (heat, ImmutableList<ValidationResult>.Empty);
        }

        DetachHeatGraphIfTracked(heat);

        var trackedHeat = await dbContext.Heats
            .FirstOrDefaultAsync(h => h.Id == heat.Id);
        if (trackedHeat is null)
            return (null, [new ValidationResult($"Заплыв {heat.Id} не найден.")]);

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

    private ImmutableList<ValidationResult> ValidateHeatWithPositions(Heat heat, SwimEvent swimEvent, bool isAdd)
    {
        var errors = new List<ValidationResult>();

        if (heat.Number < 1)
            errors.Add(new ValidationResult("Номер заплыва должен быть не меньше 1.", [nameof(Heat.Number)]));

        var positions = heat.Positions?.ToList() ?? [];
        var maxPositions = SwimEventLaneNames.GetLaneCount(swimEvent);
        if (positions.Count > maxPositions)
            errors.Add(new ValidationResult(
                $"В заплыве может быть не более {maxPositions} позиций (дорожки {SwimEventLaneNames.FormatLanesSummary(swimEvent)}).",
                [nameof(Heat.Positions)]));
        var duplicateLanes = positions.GroupBy(position => position.Lane).Where(g => g.Count() > 1).Select(g => g.Key);
        foreach (var lane in duplicateLanes)
            errors.Add(new ValidationResult(
                $"Дорожка {SwimEventLaneNames.GetLaneDisplay(swimEvent, lane)} указана более одного раза.",
                [nameof(HeatPosition.Lane)]));

        var duplicateEntries = positions.GroupBy(position => position.EntryId).Where(g => g.Count() > 1)
            .Select(g => g.Key);
        foreach (var entryId in duplicateEntries)
            errors.Add(new ValidationResult($"Заявка {entryId} указана более одного раза.",
                [nameof(HeatPosition.EntryId)]));

        foreach (var position in positions)
        {
            if (!SwimEventLaneNames.IsLaneInRange(swimEvent, position.Lane))
                errors.Add(new ValidationResult(
                    $"Дорожка {SwimEventLaneNames.GetLaneDisplay(swimEvent, position.Lane)} вне диапазона {SwimEventLaneNames.FormatLanesSummary(swimEvent)}.",
                    [nameof(HeatPosition.Lane)]));

            var entryExists = dbContext.Entries.Any(entry =>
                entry.Id == position.EntryId && entry.SwimEventId == swimEvent.Id);
            if (!entryExists)
                errors.Add(new ValidationResult($"Заявка {position.EntryId} не принадлежит событию.",
                    [nameof(HeatPosition.EntryId)]));

            var entryInOtherHeat = dbContext.HeatPositions.Any(existing =>
                existing.EntryId == position.EntryId &&
                existing.HeatId != (isAdd ? 0 : heat.Id));
            if (entryInOtherHeat)
                errors.Add(new ValidationResult($"Заявка {position.EntryId} уже назначена в другой заплыв.",
                    [nameof(HeatPosition.EntryId)]));
        }

        return errors.ToImmutableList();
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
            throw new ValidationException("Не переданы позиции заплыва.");

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
                throw new ValidationException($"Нет данных результата для заявки {trackedPosition.EntryId}.");

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

            if (!isResultProvided) throw new ValidationException("Not all lane results are provided.");
        }

        trackedHeat.Status = HeatStatus.OFFICIAL;
        await dbContext.SaveChangesAsync();
    }

    public async Task UnapproveHeatAsync(int heatId)
    {
        var heat = await dbContext.Heats.FirstOrDefaultAsync(h => h.Id == heatId);
        if (heat is null) throw new EntityNotFoundException($"No such heat: {heatId}");

        if (heat.Status != HeatStatus.OFFICIAL) return;

        heat.Status = HeatStatus.UNOFFICIAL;
        await dbContext.SaveChangesAsync();
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