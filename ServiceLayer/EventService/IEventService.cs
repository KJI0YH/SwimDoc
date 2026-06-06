using DataLayer.EfClasses;
using ServiceLayer.Crud;

namespace ServiceLayer.EventService;

public interface IEventService : ICrudService<SwimEvent, int?>
{
    int GetNextOrderNumber();

    (int min, int max) GetPreviousLanes();

    (int min, int max, string? customLaneNames) GetPreviousLaneSettings();

    Course GetPreviousCourse();

    TimeOnly? GetPreviousTime();

    Task CalculateStartTimesAsync(
        IReadOnlyList<int> swimEventIds,
        StartTimeCalculationParameters parameters,
        CancellationToken cancellationToken = default);

    Task<List<SwimEvent>> GetIndividualEventsAsync();
    Task<List<SwimEvent>> GetRelayEventsAsync();
}
