using BizLogic.HeatLogic;
using DataLayer.EfClasses;
using ServiceLayer.Crud;

namespace ServiceLayer.HeatService;

public interface IHeatService : ICrudService<Heat, int>
{
    public HeatAllocationOutDto AllocateEntriesToHeats(HeatAllocationParameters parameters);
    public Task DeleteSwimEventHeatsAsync(int swimEventId);
}