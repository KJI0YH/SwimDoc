using BizLogic.HeatLogic;
using DataLayer.EfCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;
using ServiceLayer.HeatService;
using ServiceLayer.HeatService.Exceptions;
using UI.ViewModels.Pages;

namespace UI.Services;

public sealed class HeatAllocationBatchSession : IAsyncDisposable
{
    private readonly IServiceScope _scope = App.Current.Services.CreateScope();
    private readonly EfCoreContext _dbContext;
    private readonly HeatService _heatService;
    private readonly HeatOrder _heatOrder;
    private readonly int _minHeatSize;

    public HeatAllocationBatchSession(HeatOrder heatOrder, int minHeatSize)
    {
        _heatOrder = heatOrder;
        _minHeatSize = minHeatSize;
        _dbContext = _scope.ServiceProvider.GetRequiredService<EfCoreContext>();

        _heatService = new HeatService(_dbContext);
    }

    public OperationItemOutcome AllocateEvent(int swimEventId)
    {
        var parameters = new HeatAllocationParameters(swimEventId, _heatOrder, _minHeatSize);
        using var transaction = _dbContext.Database.BeginTransaction();
        try
        {
            var result = _heatService.AllocateEntriesToHeats(parameters, saveChanges: true);
            transaction.Commit();
            return EventsViewModel.OutcomeFromHeatAllocation(result);
        }
        catch (HeatAllocationException ex)
        {
            transaction.Rollback();
            return EventsViewModel.OutcomeFromHeatAllocationException(ex);
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }

    public ValueTask DisposeAsync()
    {
        _scope.Dispose();
        return ValueTask.CompletedTask;
    }
}
