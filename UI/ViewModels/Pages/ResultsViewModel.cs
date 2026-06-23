using System.Collections.ObjectModel;
using System.Windows.Documents;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DataLayer.EfClasses;
using DataLayer.Display;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ServiceLayer.AgeGroupService;
using ServiceLayer.EntryService;
using ServiceLayer.EventService;
using UI.ViewModels.Pages.Data;
using UI.Models.Rows;
using UI.Models.Rows.Projections;
using UI.Models;
using UI.Services.Navigation;

namespace UI.ViewModels.Pages;

public partial class ResultsViewModel(
    IEventService eventService,
    IEntryService entryService,
    IAgeGroupService ageGroupService,
    INavigationService navigationService) :
    DataViewModel<SwimEvent, SwimEventRowView, int?>(eventService), INavigationAware, INavigationTabState
{
    private int? _navigatedEventId;
    private bool _navigatedEventPageAdjusted;
    private int? _persistedSwimEventId;
    private IEventService EventService =>
        App.Current.Services.GetRequiredService<IEventService>();
    private IEntryService EntryService =>
        App.Current.Services.GetRequiredService<IEntryService>();
    public CombinedResultsViewModel CombinedResults { get; } = new();
    [ObservableProperty] private int _selectedTabIndex;
    public int NavigationTabIndex
    {
        get => SelectedTabIndex;
        set => SelectedTabIndex = value;
    }
    [ObservableProperty] private SwimEvent? _selectedSwimEvent;
    [ObservableProperty] private SearchableItem? _selectedSwimEventOption;
    [ObservableProperty] private ObservableCollection<SearchableItem> _swimEventOptions = new();
    [ObservableProperty] private ObservableCollection<ResultEntryView> _entries = new();
    [ObservableProperty] private ResultEntryView? _selectedResultEntry;
    protected override void ResetForNewCompetition()
    {
        base.ResetForNewCompetition();
        SelectedSwimEvent = null;
        SelectedSwimEventOption = null;
        SwimEventOptions = [];
        Entries = [];
        SelectedResultEntry = null;
        _persistedSwimEventId = null;
        ClearNavigatedEvent();
        _ = CombinedResults.InitializeAsync();
    }

    public void OnNavigatedTo(object? parameter)
    {
        if (NavigationContext.Parse(parameter)?.ResolveId() is int eventId)
        {
            _navigatedEventId = eventId;
            _navigatedEventPageAdjusted = false;
            _ = InitializeForEventAsync(eventId);
            return;
        }
        _ = CombinedResults.InitializeAsync();
        EnsureDataLoaded();
    }

    private async Task InitializeForEventAsync(int eventId)
    {
        RequestReload();
        await LoadDataAsync();
        var swimEvent = await QuerySwimEventAsync(eventId);
        if (swimEvent?.AgeGroupId is int ageGroupId)
            CombinedResults.SetPreferredAgeGroupId(ageGroupId);
        await CombinedResults.InitializeAsync();
        await SelectNavigatedEventAsync(eventId);
    }

    public Task RefreshAsync() => LoadEntriesAsync();
    protected Task LoadEntriesForEventIdAsync(int eventId) => LoadEntriesCoreAsync(eventId);
    protected override IQueryable<SwimEvent> ApplyQuery(IQueryable<SwimEvent> query) =>
        query.OrderBy(se => se.Order);

    protected override async Task<List<SwimEventRowView>> LoadPageRowsAsync(IQueryable<SwimEvent> query)
    {
        var projections = await RowProjectionQueries.SelectSwimEvent(query).ToListAsync();
        return projections.Select(SwimEventRowView.FromProjection).ToList();
    }

    private async Task<SwimEvent?> QuerySwimEventAsync(int eventId)
    {
        var projection = await RowProjectionQueries.SelectSwimEvent(ApplyQuery(EventService.Query()))
            .FirstOrDefaultAsync(se => se.Id == eventId);
        return projection is null ? null : SwimEventRowView.FromProjection(projection).Entity;
    }

    public void OnNavigationRestored()
    {
        var eventId = _persistedSwimEventId ?? SelectedSwimEvent?.Id;
        if (eventId is not int selectedEventId)
            return;
        var pageItems = Items.Count > 0
            ? Items.Select(row => row.Entity).ToList()
            : SelectedSwimEvent?.Id == selectedEventId
                ? new List<SwimEvent> { SelectedSwimEvent }
                : [];
        ApplySwimEventSelectionFromId(
            selectedEventId,
            pageItems,
            SelectedSwimEvent?.Id == selectedEventId ? SelectedSwimEvent : null);
        _ = RestoreAfterNavigationAsync(selectedEventId);
    }

    protected override void OnItemsLoaded(IReadOnlyList<SwimEvent> items)
    {
        if (items.Count == 0)
        {
            if (_persistedSwimEventId is null)
            {
                SelectedSwimEvent = null;
                SelectedSwimEventOption = null;
                SwimEventOptions = [];
                Entries = [];
            }
            return;
        }
        if (_navigatedEventId is int eventId)
        {
            var found = items.FirstOrDefault(e => e.Id == eventId);
            if (found is not null)
            {
                SelectedSwimEvent = found;
                ClearNavigatedEvent();
                SyncSwimEventSelection(items);
                return;
            }
            if (SelectedSwimEvent?.Id == eventId)
            {
                SyncSwimEventSelection(items);
                return;
            }
            if (!_navigatedEventPageAdjusted)
            {
                _navigatedEventPageAdjusted = true;
                _ = NavigateToEventPageAsync(eventId);
                return;
            }
            _ = LoadAndSelectEventAsync(eventId);
            return;
        }
        SyncSwimEventSelection(items);
        if (SelectedSwimEvent is null)
            SelectedSwimEvent = items.OrderBy(e => e.Order).FirstOrDefault();
    }

    private async Task RestoreAfterNavigationAsync(int eventId)
    {
        SwimEvent? swimEvent = SelectedSwimEvent?.Id == eventId
            ? SelectedSwimEvent
            : Items.FirstOrDefault(row => row.Id == eventId)?.Entity;
        swimEvent ??= await QuerySwimEventAsync(eventId);
        if (swimEvent is null)
            return;
        var pageItems = Items.Count > 0
            ? Items.Select(row => row.Entity).ToList()
            : new List<SwimEvent> { swimEvent };
        ApplySwimEventSelectionFromId(eventId, pageItems, swimEvent);
        await LoadEntriesForEventIdAsync(eventId);
    }

    private void ApplySwimEventSelectionFromId(
        int eventId,
        IReadOnlyList<SwimEvent> pageItems,
        SwimEvent? preferredEvent = null)
    {
        var swimEvent = preferredEvent
            ?? pageItems.FirstOrDefault(e => e.Id == eventId)
            ?? (SelectedSwimEvent?.Id == eventId ? SelectedSwimEvent : null);
        if (swimEvent is null)
            return;
        var options = pageItems.Count > 0 ? pageItems.ToList() : [swimEvent];
        if (options.All(e => e.Id != eventId))
            options.Add(swimEvent);
        options = options.OrderBy(e => e.Order).ToList();
        SwimEventOptions = new ObservableCollection<SearchableItem>(
            options.Select(e => new SearchableItem
            {
                Value = e,
                DisplayText = EntityDisplayFormatter.FormatSwimEvent(e)
            }));
        SelectedSwimEvent = options.First(e => e.Id == eventId);
        SelectedSwimEventOption = SwimEventOptions.FirstOrDefault(o =>
            o.Value is SwimEvent swim && swim.Id == eventId);
    }

    private void SyncSwimEventSelection()
    {
        var eventId = _persistedSwimEventId ?? SelectedSwimEvent?.Id;
        if (eventId is not int selectedEventId)
            return;
        ApplySwimEventSelectionFromId(selectedEventId, Items.Select(row => row.Entity).ToList());
    }

    private void SyncSwimEventSelection(IReadOnlyList<SwimEvent> pageItems)
    {
        var eventId = _persistedSwimEventId ?? SelectedSwimEvent?.Id;
        if (eventId is not int selectedEventId)
        {
            if (pageItems.Count == 0)
                return;
            var first = pageItems.OrderBy(e => e.Order).First();
            ApplySwimEventSelectionFromId(first.Id, pageItems, first);
            return;
        }
        ApplySwimEventSelectionFromId(selectedEventId, pageItems);
    }

    private async Task SelectNavigatedEventAsync(int eventId)
    {
        var swimEvent = await QuerySwimEventAsync(eventId);
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
            SelectedSwimEvent = listed.Entity;
        SyncSwimEventSelection();
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
            Items = new ObservableCollection<SwimEventRowView>(
                Items.Append(new SwimEventRowView(swimEvent)).OrderBy(e => e.Order));
        }
    }

    private async Task NavigateToEventPageAsync(int eventId)
    {
        var baseQuery = ApplySorting(ApplySearch(ApplyQuery(EventService.Query())));
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
            var swimEvent = await QuerySwimEventAsync(eventId);
            if (swimEvent is null)
                return;
            if (Items.All(e => e.Id != eventId))
            {
                var merged = Items.Append(new SwimEventRowView(swimEvent)).OrderBy(e => e.Order).ToList();
                Items = new ObservableCollection<SwimEventRowView>(merged);
            }
            SelectedSwimEvent = Items.First(e => e.Id == eventId).Entity;
            SyncSwimEventSelection();
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
        _persistedSwimEventId = value?.Id;
        if (value?.Id is int eventId)
        {
            var option = SwimEventOptions.FirstOrDefault(o =>
                o.Value is SwimEvent swim && swim.Id == eventId);
            if (!ReferenceEquals(SelectedSwimEventOption, option))
                SelectedSwimEventOption = option;
        }
        else if (SelectedSwimEventOption is not null)
            SelectedSwimEventOption = null;
        if (value?.AgeGroupId is int ageGroupId)
            CombinedResults.TrySelectAgeGroup(ageGroupId);
        _ = LoadEntriesAsync();
    }

    partial void OnSelectedSwimEventOptionChanged(SearchableItem? value)
    {
        if (value?.Value is SwimEvent swimEvent && SelectedSwimEvent?.Id != swimEvent.Id)
            SelectedSwimEvent = swimEvent;
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
            var entries = await EntryService.GetEntriesByEventIdOrderByFinishTimeAsync(eventId);
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
        return EntryPlaceAssignment.AssignPlaces(entries)
            .Select(item => new ResultEntryView(item.Place, item.Entry))
            .ToList();
    }

    internal static ResultEntryView? FindAthleteResult(
        IReadOnlyList<ResultEntryView> eventResults,
        int athleteId) =>
        eventResults.FirstOrDefault(r =>
            r.Entry.AthleteId == athleteId ||
            (r.Entry.RelayId != null &&
             r.Entry.Relay?.Positions.Any(p => p.AthleteId == athleteId) == true));

    internal static bool IsClubEntry(Entry entry, int clubId) =>
        entry.Athlete?.ClubId == clubId || entry.Relay?.ClubId == clubId;

    internal static IEnumerable<ResultEntryView> FindClubResults(
        IReadOnlyList<ResultEntryView> eventResults,
        int clubId) =>
        eventResults.Where(r => IsClubEntry(r.Entry, clubId));

    [RelayCommand]
    private void GoToNextEvent()
    {
        if (Items.Count == 0)
            return;
        var idx = SelectedSwimEvent is null ? -1 : Items.ToList().FindIndex(row => row.Id == SelectedSwimEvent.Id);
        var nextIdx = Math.Min(idx + 1, Items.Count - 1);
        SelectedSwimEvent = Items[nextIdx].Entity;
    }

    [RelayCommand]
    private void GoToPrevEvent()
    {
        if (Items.Count == 0)
            return;
        var idx = SelectedSwimEvent is null ? 0 : Items.ToList().FindIndex(row => row.Id == SelectedSwimEvent.Id);
        if (idx < 0) idx = 0;
        var prevIdx = Math.Max(idx - 1, 0);
        SelectedSwimEvent = Items[prevIdx].Entity;
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
