using System.Collections.Immutable;
using System.ComponentModel.DataAnnotations;
using BizDbAccess;
using BizLogic.HeatLogic;
using BizLogic.HeatLogic.Concrete;
using DataLayer.EfClasses;
using DataLayer.EfCore;
using Microsoft.EntityFrameworkCore;
using ServiceLayer.BizRunners;
using ServiceLayer.Crud;
using ServiceLayer.HeatService.Exceptions;

namespace ServiceLayer.HeatService;

public class HeatService(EfCoreContext dbContext) : CrudService<Heat, int?>(dbContext), IHeatService
{
    private readonly RunnerWriteDb<HeatAllocationInDto, HeatAllocationOutDto> _runner = new(
        new HeatAllocationAction(new HeatAllocationDbAccess(dbContext)),
        dbContext);

    public HeatAllocationOutDto AllocateEntriesToHeats(HeatAllocationParameters parameters)
    {
        var swimEvent = dbContext.SwimEvents.AsNoTracking()
            .FirstOrDefault(swimEvent => swimEvent.Id == parameters.SwimEventId);
        if (swimEvent is null) throw new EntityNotFoundException($"No such swim event: {parameters.SwimEventId}");
        var dataIn = new HeatAllocationInDto(parameters, swimEvent.LaneMin, swimEvent.LaneMax);
        var result = _runner.RunAction(dataIn);
        return _runner.HasErrors ? throw new HeatAllocationException(_runner.Errors) : result;
    }

    public async Task DeleteSwimEventHeatsAsync(int swimEventId)
    {
        dbContext.Heats.RemoveRange(dbContext.Heats.Where(heat => heat.SwimEventId == swimEventId));
        await dbContext.SaveChangesAsync();
    }

    public async Task DeleteHeatPositionAsync(int heatId, int entryId)
    {
        var position = await dbContext.HeatPositions
            .FirstOrDefaultAsync(heatPosition => heatPosition.HeatId == heatId && heatPosition.EntryId == entryId);
        if (position is null)
            return;

        dbContext.HeatPositions.Remove(position);
        await dbContext.SaveChangesAsync();
    }

    public Task<List<Heat>> GetHeatsByEventIdAsync(int eventId)
    {
        return dbContext.Heats
            .AsNoTracking()
            .Where(heat => heat.SwimEventId == eventId)
            .OrderBy(heat => heat.Order)
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
            .ThenInclude(entry => entry.SwimEvent)
            .ToListAsync();
    }
    
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