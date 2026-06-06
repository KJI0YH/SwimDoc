using System.Collections.ObjectModel;
using System.Collections.Specialized;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ServiceLayer.EventService;
using UI.Resources;
using UI.Services;

namespace UI.Models.Dialogs;

public sealed class StartTimeCalculationResult(
    TimeOnly startTime,
    TimeSpan heatPause,
    TimeSpan eventPause)
{
    public TimeOnly StartTime { get; } = startTime;
    public TimeSpan HeatPause { get; } = heatPause;
    public TimeSpan EventPause { get; } = eventPause;

    public StartTimeCalculationParameters ToParameters() =>
        new(StartTime, HeatPause, EventPause);
}
