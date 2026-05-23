using DataLayer.EfClasses;
using ServiceLayer.Crud;

namespace ServiceLayer.EventService;

public interface IEventService : ICrudService<SwimEvent, int?>
{
    int GetNextOrderNumber();
    
    (int min, int max) GetPreviousLanes();

    TimeOnly? GetPreviousTime();

    Task CalculateStartTimesAsync(
        IReadOnlyList<int> swimEventIds,
        StartTimeCalculationParameters parameters);
    
    Task<List<SwimEvent>> GetIndividualEventsAsync();
    Task<List<SwimEvent>> GetRelayEventsAsync();
}