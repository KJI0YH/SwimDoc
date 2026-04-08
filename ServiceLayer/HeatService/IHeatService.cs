using BizLogic.HeatLogic;
using DataLayer.EfClasses;
using ServiceLayer.Crud;

namespace ServiceLayer.HeatService;

public sealed class HeatLaneResultIn
{
    public int EntryId { get; set; }
    public int? FinishTime { get; set; }
    public EntryStatus Status { get; set; }
    public string? Comment { get; set; }
}

public interface IHeatService : ICrudService<Heat, int?>
{
    public HeatAllocationOutDto AllocateEntriesToHeats(HeatAllocationParameters parameters);
    public Task DeleteSwimEventHeatsAsync(int swimEventId);
    public Task DeleteHeatPositionAsync(int heatId, int entryId);

    public Task UpdateHeatResultsAsync(int heatId, IReadOnlyList<HeatLaneResultIn> results);
    public Task ApproveHeatAsync(int heatId);
    public Task UnapproveHeatAsync(int heatId);
    
    public int GetTotalHeats();
    
    public int GetTotalHeatsInEvent(int swimEventId);
}