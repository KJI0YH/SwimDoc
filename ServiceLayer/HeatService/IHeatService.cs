using BizLogic.HeatLogic;
using DataLayer.EfClasses;
using ServiceLayer.Crud;

namespace ServiceLayer.HeatService;

public interface IHeatService : ICrudService<Heat, int?>
{
    public HeatAllocationOutDto AllocateEntriesToHeats(HeatAllocationParameters parameters);
    public Task DeleteSwimEventHeatsAsync(int swimEventId);
    public Task DeleteHeatPositionAsync(int heatId, int entryId);

    public Task<List<Heat>> GetHeatsByEventIdAsync(int eventId);
    public Task ApproveHeatAsync(Heat heat);
    public Task UnapproveHeatAsync(int heatId);
    
    public int GetTotalHeats();
    
    public int GetTotalHeatsInEvent(int swimEventId);
}