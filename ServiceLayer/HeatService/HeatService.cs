using System.Collections.Immutable;
using System.ComponentModel.DataAnnotations;
using BizDbAccess;
using BizLogic.HeatLogic;
using BizLogic.HeatLogic.Concrete;
using DataLayer.EfCore;
using ServiceLayer.BizRunners;
using ServiceLayer.HeatService.Exceptions;

namespace ServiceLayer.HeatService;

public class HeatService
{
    private readonly EfCoreContext _context;
    private readonly RunnerWriteDb<HeatAllocationInDto, HeatAllocationOutDto> _runner;
    public IImmutableList<ValidationResult> Errors => _runner.Errors;

    public HeatService(EfCoreContext context)
    {
        _context = context;
        _runner = new RunnerWriteDb<HeatAllocationInDto, HeatAllocationOutDto>(
            new HeatAllocationAction(new HeatAllocationDbAccess(context)),
            context);
    }

    public HeatAllocationOutDto AllocateEntriesToHeats(HeatAllocationParameters parameters)
    {
        var swimEvent = _context.SwimEvents.FirstOrDefault(swimEvent => swimEvent.Id == parameters.SwimEventId);
        if (swimEvent == null) return new HeatAllocationOutDto([]);
        var dataIn = new HeatAllocationInDto(parameters, swimEvent.LaneMin, swimEvent.LaneMax);
        var result = _runner.RunAction(dataIn);
        return _runner.HasErrors ? throw new HeatAllocationException(Errors) : result;
    }

    public async Task DeleteSwimEventHeatsAsync(int swimEventId)
    {
        _context.Heats.RemoveRange(_context.Heats.Where(heat => heat.SwimEventId == swimEventId));
        await _context.SaveChangesAsync();
    }
}