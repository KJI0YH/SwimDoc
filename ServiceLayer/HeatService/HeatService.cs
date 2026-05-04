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
        var swimEvent = dbContext.SwimEvents.AsNoTracking().FirstOrDefault(swimEvent => swimEvent.Id == parameters.SwimEventId);
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

    public async Task UpdateHeatResultsAsync(int heatId, IReadOnlyList<HeatLaneResultIn> results)
    {
        var heat = await dbContext.Heats
            .Include(h => h.Positions)
            .ThenInclude(p => p.Entry)
            .FirstOrDefaultAsync(h => h.Id == heatId);

        if (heat is null)
            throw new EntityNotFoundException($"No such heat: {heatId}");

        if (heat.Status == HeatStatus.OFFICIAL)
            throw new InvalidOperationException("Heat results are official and cannot be modified.");

        var entryById = heat.Positions
            .Where(p => p.Entry is not null)
            .Select(p => p.Entry)
            .DistinctBy(e => e!.Id)
            .ToDictionary(e => e!.Id, e => e!);

        foreach (var r in results)
        {
            if (!entryById.TryGetValue(r.EntryId, out var entry))
                continue;

            if (r.Status is EntryStatus.DNS or EntryStatus.DNF or EntryStatus.DSQ)
            {
                entry.Status = r.Status;
                entry.FinishTime = null;
                entry.Comment = r.Comment ?? string.Empty;
                continue;
            }

            entry.Status = EntryStatus.FINISH;
            entry.FinishTime = r.FinishTime;
            entry.Comment = r.Comment ?? string.Empty;
        }

        if (heat.Status == HeatStatus.NOT_STARTED && results.Count > 0)
            heat.Status = HeatStatus.UNOFFICIAL;

        await dbContext.SaveChangesAsync();
    }

    public async Task ApproveHeatAsync(int heatId)
    {
        var heat = await dbContext.Heats
            .Include(h => h.Positions)
            .ThenInclude(p => p.Entry)
            .FirstOrDefaultAsync(h => h.Id == heatId);

        if (heat is null)
            throw new EntityNotFoundException($"No such heat: {heatId}");

        if (heat.Status == HeatStatus.OFFICIAL)
            return;

        foreach (var pos in heat.Positions)
        {
            var entry = pos.Entry;

            var ok = entry.Status switch
            {
                EntryStatus.FINISH => entry.FinishTime.HasValue,
                EntryStatus.DNS or EntryStatus.DNF or EntryStatus.DSQ => true,
                _ => false
            };

            if (!ok)
                throw new ValidationException("Not all lane results are provided.");
        }

        heat.Status = HeatStatus.OFFICIAL;
        await dbContext.SaveChangesAsync();
    }

    public async Task UnapproveHeatAsync(int heatId)
    {
        var heat = await dbContext.Heats.FirstOrDefaultAsync(h => h.Id == heatId);
        if (heat is null)
            throw new EntityNotFoundException($"No such heat: {heatId}");

        if (heat.Status != HeatStatus.OFFICIAL)
            return;

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