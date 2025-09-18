using System.Collections.Immutable;
using System.ComponentModel.DataAnnotations;
using BizDbAccess;
using BizLogic.HeatLogic;
using DataLayer.EfCore;
using ServiceLayer.BizRunners;

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

    public void AllocateEntriesToHeats(HeatAllocationParameters parameters)
    {
        var swimEvent = _context.SwimEvents.FirstOrDefault(swimEvent => swimEvent.Id == parameters.SwimEventId);
        if (swimEvent == null) return;
        var dataIn = new HeatAllocationInDto(parameters, swimEvent.LaneMin, swimEvent.LaneMax);
        var result = _runner.RunAction(dataIn);
        if (_runner.HasErrors) throw new Exception();
    }
}