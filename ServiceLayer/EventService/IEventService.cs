using DataLayer.EfClasses;
using ServiceLayer.Crud;

namespace ServiceLayer.EventService;

public interface IEventService : ICrudService<SwimEvent, int?>
{
    int GetNextOrderNumber();
    
    (int min, int max) GetPreviousLanes();
}