namespace ServiceLayer.EventService;

public sealed record StartTimeCalculationParameters(
    TimeOnly StartTime,
    TimeSpan HeatPause,
    TimeSpan EventPause);
