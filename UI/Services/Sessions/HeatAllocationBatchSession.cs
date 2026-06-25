using BizLogic.HeatAllocation;
using DataLayer.EfCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;
using ServiceLayer.HeatService;
using ServiceLayer.HeatService.Exceptions;
using ServiceLayer.Logging;
using UI.ViewModels.Pages;

namespace UI.Services.Sessions;

/// <summary>
/// Batch heat allocation must use a single <see cref="EfCoreContext"/> for the transaction and service.
/// Do not resolve <see cref="IHeatService"/> from DI here — it would get a different context.
/// </summary>
public sealed class HeatAllocationBatchSession : IAsyncDisposable
{
    private readonly IServiceScope _scope = App.Current.Services.CreateScope();
    private readonly EfCoreContext _dbContext;
    private readonly IHeatService _heatService;
    private readonly IAppLog _log;
    private readonly HeatOrder _heatOrder;
    private readonly int _minHeatSize;

    public HeatAllocationBatchSession(HeatOrder heatOrder, int minHeatSize)
    {
        _heatOrder = heatOrder;
        _minHeatSize = minHeatSize;
        _dbContext = _scope.ServiceProvider.GetRequiredService<EfCoreContext>();
        _log = _scope.ServiceProvider.GetRequiredService<IAppLog>();
        _heatService = new HeatService(_dbContext, _log);
    }

    public OperationItemOutcome AllocateEvent(int swimEventId)
    {
        var parameters = new HeatAllocationParameters(swimEventId, _heatOrder, _minHeatSize);
        using var transaction = _dbContext.Database.BeginTransaction();
        try
        {
            var result = _heatService.AllocateEntriesToHeats(parameters, saveChanges: true);
            transaction.Commit();
            var outcome = EventsViewModel.OutcomeFromHeatAllocation(result);
            _log.Info($"Batch heat allocation succeeded for SwimEventId={swimEventId}, heats={result.Heats.Count}");
            return outcome;
        }
        catch (HeatAllocationException ex)
        {
            transaction.Rollback();
            _log.Warning($"Batch heat allocation failed for SwimEventId={swimEventId}: {ex.Errors.Count} errors");
            return EventsViewModel.OutcomeFromHeatAllocationException(ex);
        }
        catch
        {
            transaction.Rollback();
            _log.Error($"Batch heat allocation unexpected error for SwimEventId={swimEventId}");
            throw;
        }
    }

    public ValueTask DisposeAsync()
    {
        _scope.Dispose();
        return ValueTask.CompletedTask;
    }
}
