using System.Collections.ObjectModel;
using System.Windows.Documents;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DataLayer.EfClasses;
using Microsoft.EntityFrameworkCore;
using ServiceLayer.EntryService;
using ServiceLayer.EventService;
using UI.Helpers;
using UI.Services;
using UI.ViewModels.Pages.Data;

namespace UI.ViewModels.Pages;

public partial class ResultsViewModel(
    IEventService eventService,
    IEntryService entryService,
    INavigationService navigationService) :
    DataViewModel<SwimEvent, int?>(eventService), INavigationAware
{
    private int? _navigatedEventId;
    private bool _navigatedEventPageAdjusted;

    [ObservableProperty] private SwimEvent? _selectedSwimEvent;
    [ObservableProperty] private ObservableCollection<ResultEntryView> _entries = new();
    [ObservableProperty] private ResultEntryView? _selectedResultEntry;

    public void OnNavigatedTo(object? parameter)
    {
        if (parameter is not int eventId)
            return;

        _navigatedEventId = eventId;
        _navigatedEventPageAdjusted = false;
        _ = SelectNavigatedEventAsync(eventId);
    }

    public Task RefreshAsync() => LoadEntriesAsync();

    protected Task LoadEntriesForEventIdAsync(int eventId) => LoadEntriesCoreAsync(eventId);

    protected override IQueryable<SwimEvent> ApplyQuery(IQueryable<SwimEvent> query)
    {
        return query
            .OrderBy(se => se.Order)
            .Include(se => se.AgeGroup)
            .Include(se => se.SwimStyle);
    }

    protected override void OnItemsLoaded(IReadOnlyList<SwimEvent> items)
    {
        if (items.Count == 0)
        {
            SelectedSwimEvent = null;
            Entries = [];
            return;
        }

        if (_navigatedEventId is int eventId)
        {
            var found = items.FirstOrDefault(e => e.Id == eventId);
            if (found is not null)
            {
                SelectedSwimEvent = found;
                ClearNavigatedEvent();
                return;
            }

            if (SelectedSwimEvent?.Id == eventId)
                return;

            if (!_navigatedEventPageAdjusted)
            {
                _navigatedEventPageAdjusted = true;
                _ = NavigateToEventPageAsync(eventId);
                return;
            }

            _ = LoadAndSelectEventAsync(eventId);
            return;
        }

        if (SelectedSwimEvent is not null && items.Any(e => e.Id == SelectedSwimEvent.Id))
            return;

        SelectedSwimEvent ??= items.OrderBy(e => e.Order).FirstOrDefault();
    }

    private async Task SelectNavigatedEventAsync(int eventId)
    {
        var swimEvent = await ApplyQuery(eventService.Query())
            .FirstOrDefaultAsync(se => se.Id == eventId);

        if (swimEvent is null)
        {
            ClearNavigatedEvent();
            return;
        }

        SelectedSwimEvent = swimEvent;

        if (Items.Count == 0)
            return;

        await EnsureNavigatedEventVisibleAsync(eventId, swimEvent);

        if (Items.FirstOrDefault(e => e.Id == eventId) is { } listed)
            SelectedSwimEvent = listed;

        ClearNavigatedEvent();
    }

    private async Task EnsureNavigatedEventVisibleAsync(int eventId, SwimEvent swimEvent)
    {
        if (Items.Any(e => e.Id == eventId))
            return;

        if (!_navigatedEventPageAdjusted)
        {
            _navigatedEventPageAdjusted = true;
            await NavigateToEventPageAsync(eventId);
        }

        if (Items.All(e => e.Id != eventId))
        {
            Items = new ObservableCollection<SwimEvent>(
                Items.Append(swimEvent).OrderBy(e => e.Order));
        }
    }

    private async Task NavigateToEventPageAsync(int eventId)
    {
        var baseQuery = ApplySorting(ApplySearch(ApplyQuery(eventService.Query())));
        var target = await baseQuery.FirstOrDefaultAsync(se => se.Id == eventId);
        if (target is null)
        {
            ClearNavigatedEvent();
            return;
        }

        var index = await baseQuery.CountAsync(se => se.Order < target.Order);
        var page = PageSize > 0 ? index / PageSize : 0;

        if (CurrentPage != page)
            CurrentPage = page;
        else
            await LoadAndSelectEventAsync(eventId);
    }

    private async Task LoadAndSelectEventAsync(int eventId)
    {
        try
        {
            var swimEvent = await ApplyQuery(eventService.Query())
                .FirstOrDefaultAsync(se => se.Id == eventId);

            if (swimEvent is null)
                return;

            if (Items.All(e => e.Id != eventId))
            {
                var merged = Items.Append(swimEvent).OrderBy(e => e.Order).ToList();
                Items = new ObservableCollection<SwimEvent>(merged);
            }

            SelectedSwimEvent = Items.First(e => e.Id == eventId);
        }
        finally
        {
            ClearNavigatedEvent();
        }
    }

    private void ClearNavigatedEvent()
    {
        _navigatedEventId = null;
        _navigatedEventPageAdjusted = false;
    }

    partial void OnSelectedSwimEventChanged(SwimEvent? value)
    {
        _ = LoadEntriesAsync();
    }

    private Task LoadEntriesAsync()
    {
        if (SelectedSwimEvent?.Id is not int eventId)
        {
            Entries = [];
            return Task.CompletedTask;
        }

        return LoadEntriesCoreAsync(eventId);
    }

    private async Task LoadEntriesCoreAsync(int eventId)
    {
        IsLoading = true;
        try
        {
            var entries = await entryService.GetEntriesByEventIdOrderByFinishTimeAsync(eventId);
            Entries = new ObservableCollection<ResultEntryView>(BuildResultEntryViews(entries));
        }
        finally
        {
            IsLoading = false;
        }
    }

    internal static List<ResultEntryView> BuildResultEntryViews(IReadOnlyList<Entry> entries)
    {
        if (entries.Count == 0)
            return [];

        var place = 1;
        List<ResultEntryView> orderedEntries = [new(place++, entries[0])];
        var prevResult = orderedEntries[0];
        foreach (var entry in entries.Skip(1))
        {
            orderedEntries.Add(entry.FinishTime == prevResult.Entry.FinishTime
                ? new ResultEntryView(prevResult.Place, entry)
                : new ResultEntryView(place, entry));
            prevResult = orderedEntries[^1];
            place++;
        }

        return orderedEntries;
    }

    internal static ResultEntryView? FindAthleteResult(
        IReadOnlyList<ResultEntryView> eventResults,
        int athleteId) =>
        eventResults.FirstOrDefault(r =>
            r.Entry.AthleteId == athleteId ||
            (r.Entry.RelayId != null &&
             r.Entry.Relay?.Positions.Any(p => p.AthleteId == athleteId) == true));

    [RelayCommand]
    private void GoToNextEvent()
    {
        if (Items.Count == 0)
            return;

        var idx = SelectedSwimEvent is null ? -1 : Items.IndexOf(SelectedSwimEvent);
        var nextIdx = Math.Min(idx + 1, Items.Count - 1);
        SelectedSwimEvent = Items[nextIdx];
    }

    [RelayCommand]
    private void GoToPrevEvent()
    {
        if (Items.Count == 0)
            return;

        var idx = SelectedSwimEvent is null ? 0 : Items.IndexOf(SelectedSwimEvent);
        if (idx < 0) idx = 0;
        var prevIdx = Math.Max(idx - 1, 0);
        SelectedSwimEvent = Items[prevIdx];
    }

    partial void OnSelectedResultEntryChanged(ResultEntryView? value) =>
        OpenAthleteDetailsCommand.NotifyCanExecuteChanged();

    private bool CanOpenAthleteDetails() =>
        EntryAthleteNavigationHelper.TryGetAthleteId(SelectedResultEntry?.Entry, out _);

    [RelayCommand(CanExecute = nameof(CanOpenAthleteDetails))]
    private void OpenAthleteDetails()
    {
        if (!EntryAthleteNavigationHelper.TryGetAthleteId(SelectedResultEntry?.Entry, out var athleteId))
            return;

        navigationService.NavigateTo<AthleteDetailsViewModel>(athleteId);
    }
}

public sealed class ResultEntryView(int place, Entry entry) : ObservableObject
{
    public int Place { get; } = place;
    public Entry Entry { get; } = entry;

    public string ParticipantName => Entry.DisplayParticipantName;
    public string ParticipantYearOfBirth => Entry.Athlete?.YearOfBirth.ToString() ?? string.Empty;
    public string ClubName => Entry.DisplayParticipantClubName;

    public string ResultText => Entry.DisplayFinishTime;
    public int? Points => Entry.Points;
}

public sealed class AthleteResultEntryView(ResultEntryView result) : ObservableObject
{
    public int Place => result.Place;
    public Entry Entry => result.Entry;

    public string EventName => Entry.SwimEvent?.DisplayName ?? Entry.DisplaySwimName;
    public string ResultText => result.ResultText;
    public int? Points => result.Points;
}