using System.Collections.Immutable;
using System.ComponentModel.DataAnnotations;
using BizLogic.HeatLogic;
using DataLayer.EfClasses;
using ServiceLayer.Crud;

namespace ServiceLayer.HeatService;

public interface IHeatService : ICrudService<Heat, int?>
{
    HeatAllocationOutDto AllocateEntriesToHeats(HeatAllocationParameters parameters, bool saveChanges = true);
    public Task DeleteSwimEventHeatsAsync(int swimEventId);
    public Task DeleteHeatPositionAsync(int heatId, int entryId);
    public Task DeleteHeatAsync(int heatId);

    public Task<List<Heat>> GetHeatsByEventIdAsync(int eventId);
    public Task<List<Heat>> GetHeatsByEventIdPagedAsync(int eventId, int page, int pageSize);
    public Task ApproveHeatAsync(Heat heat);
    public Task UnapproveHeatAsync(int heatId);

    public Task<int> GetNextHeatNumberAsync(int swimEventId);
    public Task<(Heat? heat, ImmutableList<ValidationResult> errors)> SaveHeatWithPositionsAsync(Heat heat, bool isAdd);

    public int GetTotalHeats();

    public int GetTotalHeatsInEvent(int swimEventId);
}