using System.Collections.ObjectModel;
using System.Collections.Specialized;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DataLayer.EfClasses;
using Microsoft.EntityFrameworkCore;
using ServiceLayer.EventService;
using UI.Services;
using UI.Views.Controls.SearchableComboBox;

namespace UI.ViewModels.Windows.LoadEntriesFromPreviousEvent;

public partial class LoadEntriesFromPreviousEventViewModel : ViewModelBase, IWindowResult
{
    private readonly IEventService _eventService;

    [ObservableProperty] private ObservableCollection<SearchableItem> _officialPreviousEvents = new();
    [ObservableProperty] private ObservableCollection<SearchableItem> _targetEvents = new();
    [ObservableProperty] private SearchableItem? _selectedPreviousEvent;
    [ObservableProperty] private SearchableItem? _selectedTargetEvent;
    [ObservableProperty] private ObservableCollection<string> _validationErrors = [];

    public LoadEntriesFromPreviousEventViewModel(IEventService eventService)
    {
        _eventService = eventService;
        ValidationErrors.CollectionChanged += OnValidationErrorsChanged;
        LoadEvents();
    }

    public string WindowTitle => "Загрузка заявок из предыдущего события";

    public bool HasErrors => ValidationErrors.Count > 0;

    public LoadEntriesFromPreviousEventResult? Result { get; private set; }

    object? IWindowResult.Result => Result;

    public event EventHandler? CloseRequested;

    private void OnValidationErrorsChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        OnPropertyChanged(nameof(HasErrors));
    }

    private void LoadEvents()
    {
        var swimEvents = _eventService.Query()
            .Include(swimEvent => swimEvent.AgeGroup)
            .Include(swimEvent => swimEvent.SwimStyle)
            .OrderBy(swimEvent => swimEvent.Order)
            .ThenBy(swimEvent => swimEvent.Date)
            .ToList();

        OfficialPreviousEvents.Clear();
        foreach (var swimEvent in swimEvents.Where(e => e.Status == SwimEventStatus.OFFICIAL))
        {
            OfficialPreviousEvents.Add(new SearchableItem
            {
                Value = swimEvent,
                DisplayText = swimEvent.DisplayName
            });
        }

        TargetEvents.Clear();
        foreach (var swimEvent in swimEvents)
        {
            TargetEvents.Add(new SearchableItem
            {
                Value = swimEvent,
                DisplayText = swimEvent.DisplayName
            });
        }
    }

    [RelayCommand]
    private void Save()
    {
        ValidationErrors.Clear();

        if (SelectedPreviousEvent?.Value is not SwimEvent previousEvent)
        {
            ValidationErrors.Add("Выберите предыдущее событие в статусе OFFICIAL.");
            return;
        }

        if (SelectedTargetEvent?.Value is not SwimEvent targetEvent)
        {
            ValidationErrors.Add("Выберите текущее событие.");
            return;
        }

        if (previousEvent.Id == targetEvent.Id)
        {
            ValidationErrors.Add("Предыдущее и текущее события должны отличаться.");
            return;
        }

        if (targetEvent.RoundParticipantsCount is null or <= 0)
        {
            ValidationErrors.Add("Для текущего события задайте количество участников раунда.");
            return;
        }

        Result = new LoadEntriesFromPreviousEventResult(
            previousEvent.Id,
            targetEvent.Id,
            previousEvent.DisplayName,
            targetEvent.DisplayName);
        CloseRequested?.Invoke(this, EventArgs.Empty);
    }

    [RelayCommand]
    private void Cancel()
    {
        Result = null;
        CloseRequested?.Invoke(this, EventArgs.Empty);
    }
}
