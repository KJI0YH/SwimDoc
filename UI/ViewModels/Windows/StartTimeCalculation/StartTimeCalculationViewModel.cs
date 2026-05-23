using System.Collections.ObjectModel;
using System.Collections.Specialized;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ServiceLayer.EventService;
using UI.Services;

namespace UI.ViewModels.Windows.StartTimeCalculation;

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

public partial class StartTimeCalculationViewModel : ViewModelBase, IWindowResult
{
    [ObservableProperty] private int _eventPauseMinutes = 1;
    [ObservableProperty] private int _heatPauseMinutes = 1;
    [ObservableProperty] private ObservableCollection<string> _validationErrors = [];

    public StartTimeCalculationViewModel(IEventService eventService)
    {
        ValidationErrors.CollectionChanged += OnValidationErrorsChanged;
        var previousTime = eventService.GetPreviousTime();
        if (previousTime.HasValue)
            UpdateTime(previousTime.Value.Hour, previousTime.Value.Minute);
    }

    public IReadOnlyList<string> HourOptions { get; } = CreateTimePartOptions(24);
    public IReadOnlyList<string> MinuteOptions { get; } = CreateTimePartOptions(60);

    public string WindowTitle => "Расчёт времени старта";

    public bool HasErrors => ValidationErrors.Count > 0;

    public StartTimeCalculationResult? Result { get; private set; }

    object? IWindowResult.Result => Result;

    public event EventHandler? CloseRequested;

    public string HourText
    {
        get => _hour?.ToString("00") ?? string.Empty;
        set
        {
            if (!TryParseTimePart(value, 23, out var hour))
                return;

            UpdateTime(hour, _minute);
        }
    }

    public string MinuteText
    {
        get => _minute?.ToString("00") ?? string.Empty;
        set
        {
            if (!TryParseTimePart(value, 59, out var minute))
                return;

            UpdateTime(_hour, minute);
        }
    }

    private int? _hour = 9;
    private int? _minute = 0;

    private void OnValidationErrorsChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        OnPropertyChanged(nameof(HasErrors));
    }

    [RelayCommand]
    private void Save()
    {
        ValidationErrors.Clear();

        if (!_hour.HasValue || !_minute.HasValue)
        {
            ValidationErrors.Add("Укажите время начала.");
            return;
        }

        if (HeatPauseMinutes < 0)
        {
            ValidationErrors.Add("Пауза между заплывами не может быть отрицательной.");
            return;
        }

        if (EventPauseMinutes < 0)
        {
            ValidationErrors.Add("Пауза между событиями не может быть отрицательной.");
            return;
        }

        Result = new StartTimeCalculationResult(
            new TimeOnly(_hour.Value, _minute.Value),
            TimeSpan.FromMinutes(HeatPauseMinutes),
            TimeSpan.FromMinutes(EventPauseMinutes));
        CloseRequested?.Invoke(this, EventArgs.Empty);
    }

    [RelayCommand]
    private void Cancel()
    {
        Result = null;
        CloseRequested?.Invoke(this, EventArgs.Empty);
    }

    private void UpdateTime(int? hour, int? minute)
    {
        _hour = hour;
        _minute = minute;
        OnPropertyChanged(nameof(HourText));
        OnPropertyChanged(nameof(MinuteText));
    }

    private static bool TryParseTimePart(string? text, int max, out int? value)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            value = null;
            return true;
        }

        if (int.TryParse(text.Trim(), out var parsed) && parsed >= 0 && parsed <= max)
        {
            value = parsed;
            return true;
        }

        value = null;
        return false;
    }

    private static List<string> CreateTimePartOptions(int count) =>
        Enumerable.Range(0, count).Select(value => value.ToString("00")).ToList();
}
