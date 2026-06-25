using System.Collections.ObjectModel;
using System.Collections.Specialized;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DataLayer.EfClasses;
using Microsoft.EntityFrameworkCore;
using ServiceLayer.EventService;
using UI.Resources;
using UI.Models;
using UI.Services.Navigation;

namespace UI.ViewModels.Dialogs.LoadEntriesFromPreviousEvent;

public partial class LoadEntriesFromPreviousEventViewModel : ViewModelBase, IWindowResult, INavigationContextAware
{
    private readonly IEventService _eventService;
    private int? _contextEventId;
    [ObservableProperty] private ObservableCollection<SearchableItem> _officialPreviousEvents = new();
    [ObservableProperty] private ObservableCollection<SearchableItem> _targetEvents = new();
    [ObservableProperty] private SearchableItem? _selectedPreviousEvent;
    [ObservableProperty] private SearchableItem? _selectedTargetEvent;
    [ObservableProperty] private ObservableCollection<string> _validationErrors = [];
    public LoadEntriesFromPreviousEventViewModel(IEventService eventService)
    {
        _eventService = eventService;
        ValidationErrors.CollectionChanged += OnValidationErrorsChanged;
    }

    public Task InitializeAsync()
    {
        LoadEvents();
        return Task.CompletedTask;
    }

    public string WindowTitle => Strings.LoadPrev_WindowTitle;
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
        foreach (var swimEvent in swimEvents)
        {
            OfficialPreviousEvents.Add(new SearchableItem
            {
                Value = swimEvent,
                DisplayText = FormatPreviousEventOption(swimEvent)
            });
        }
        TargetEvents.Clear();
        foreach (var swimEvent in swimEvents)
        {
            TargetEvents.Add(new SearchableItem
            {
                Value = swimEvent,
                DisplayText = EntityDisplayFormatter.FormatSwimEvent(swimEvent)
            });
        }
        ApplyDefaultSelections();
    }

    public void ApplyContext(NavigationContext context)
    {
        _contextEventId = context.EventId ?? context.Id;
        ApplyDefaultSelections();
    }

    private void ApplyDefaultSelections()
    {
        if (!_contextEventId.HasValue)
            return;
        SelectedTargetEvent = TargetEvents.FirstOrDefault(item =>
            item.Value is SwimEvent swimEvent && swimEvent.Id == _contextEventId.Value);
        if (SelectedTargetEvent?.Value is not SwimEvent targetEvent)
            return;
        if (!targetEvent.PreviousSwimEventId.HasValue)
            return;
        SelectedPreviousEvent = OfficialPreviousEvents.FirstOrDefault(item =>
            item.Value is SwimEvent swimEvent && swimEvent.Id == targetEvent.PreviousSwimEventId.Value);
    }

    [RelayCommand]
    private void Save()
    {
        ValidationErrors.Clear();
        if (SelectedPreviousEvent?.Value is not SwimEvent previousEvent)
        {
            ValidationErrors.Add(Strings.LoadPrev_Validation_SelectOfficialPreviousEvent);
            return;
        }
        if (previousEvent.Status != SwimEventStatus.OFFICIAL)
        {
            ValidationErrors.Add(Strings.LoadPrev_Validation_SelectOfficialPreviousEvent);
            return;
        }
        if (SelectedTargetEvent?.Value is not SwimEvent targetEvent)
        {
            ValidationErrors.Add(Strings.LoadPrev_Validation_SelectCurrentEvent);
            return;
        }
        if (previousEvent.Id == targetEvent.Id)
        {
            ValidationErrors.Add(Strings.LoadPrev_Validation_EventsMustDiffer);
            return;
        }
        if (targetEvent.RoundParticipantsCount is null or <= 0)
        {
            ValidationErrors.Add(Strings.LoadPrev_Validation_RoundParticipantsRequired);
            return;
        }
        Result = new LoadEntriesFromPreviousEventResult(
            previousEvent.Id,
            targetEvent.Id,
            EntityDisplayFormatter.FormatSwimEvent(previousEvent),
            EntityDisplayFormatter.FormatSwimEvent(targetEvent));
        CloseRequested?.Invoke(this, EventArgs.Empty);
    }

    private static string FormatPreviousEventOption(SwimEvent swimEvent)
    {
        var name = EntityDisplayFormatter.FormatSwimEvent(swimEvent);
        return swimEvent.Status == SwimEventStatus.OFFICIAL
            ? name
            : $"{name} ({EntityDisplayFormatter.FormatSwimEventStatus(swimEvent)})";
    }

    [RelayCommand]
    private void Cancel()
    {
        Result = null;
        CloseRequested?.Invoke(this, EventArgs.Empty);
    }
}
