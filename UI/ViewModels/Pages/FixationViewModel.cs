using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DataLayer;
using DataLayer.EfClasses;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ServiceLayer.EventService;
using ServiceLayer.HeatService;
using ServiceLayer.PointScoreProvider;
using UI.Helpers.Threading;
using UI.Resources;
using UI.ViewModels.Pages.Data;
using UI.Models.Rows;
using UI.Models.Rows.Projections;
using UI.Models;

namespace UI.ViewModels.Pages;

public partial class FixationViewModel(
    IEventService eventService,
    IHeatService heatService,
    IPointScoreProvider pointScoreProvider,
    INavigationService navigationService)
    : DataViewModel<SwimEvent, SwimEventRowView, int?>(eventService)
{
    protected override bool UsesPaging => false;

    private IHeatService HeatService =>
        App.Current.Services.GetRequiredService<IHeatService>();
    private IPointScoreProvider PointScoreProvider =>
        App.Current.Services.GetRequiredService<IPointScoreProvider>();
    public event Action<int>? EventResultsChanged;
    [ObservableProperty] private SwimEvent? _selectedSwimEvent;
    [ObservableProperty] private SearchableItem? _selectedSwimEventOption;
    [ObservableProperty] private ObservableCollection<SearchableItem> _swimEventOptions = new();
    [ObservableProperty] private HeatListItemView? _selectedHeatItem;
    [ObservableProperty] private ObservableCollection<HeatListItemView> _eventHeats = new();
    private Heat? SelectedHeat => SelectedHeatItem?.Entity;
    private SwimEvent? _fixationSwimEvent;
    [ObservableProperty] private ObservableCollection<FixationHeatPositionView> _fixationHeatPositionViews = new();
    [ObservableProperty] private FixationHeatPositionView? _selectedFixationPosition;
    [ObservableProperty] private bool _canApprove;
    [ObservableProperty] private bool _canUnapprove;
    public bool CanEditHeat => SelectedHeat?.Status != HeatStatus.OFFICIAL;
    public string SelectedHeatHeader
    {
        get
        {
            if (SelectedHeat is null || SelectedSwimEvent is null) return string.Empty;
            var heatsInEvent = EventHeats.Count;
            var heatsTotal = HeatService.GetTotalHeats();
            return string.Format(
                Strings.Get("Fixation_SelectedHeatHeader_NoEvent_Format"),
                SelectedHeat.Number,
                heatsInEvent,
                SelectedHeat.Order,
                heatsTotal,
                EntityDisplayFormatter.FormatHeatDayTime(SelectedHeat));
        }
    }

    public string SelectedHeatStatus => SelectedHeat?.Status.ToString() ?? string.Empty;
    protected override void ResetForNewCompetition()
    {
        base.ResetForNewCompetition();
        SelectedSwimEvent = null;
        SelectedSwimEventOption = null;
        SwimEventOptions = [];
        EventHeats = [];
        SelectedHeatItem = null;
        FixationHeatPositionViews = [];
    }

    protected override IQueryable<SwimEvent> ApplyQuery(IQueryable<SwimEvent> query) =>
        query.OrderBy(se => se.Order);

    protected override async Task<List<SwimEventRowView>> LoadPageRowsAsync(
        IQueryable<SwimEvent> query,
        IServiceProvider serviceProvider)
    {
        var projections = await RowProjectionQueries.SelectSwimEvent(query).ToListAsync().ConfigureAwait(false);
        return projections.Select(SwimEventRowView.FromProjection).ToList();
    }

    protected override void OnItemsLoaded(IReadOnlyList<SwimEvent> items)
    {
        if (items.Count == 0)
        {
            SelectedSwimEvent = null;
            SelectedSwimEventOption = null;
            SwimEventOptions = [];
            EventHeats = [];
            SelectedHeatItem = null;
            FixationHeatPositionViews = [];
            return;
        }
        SelectedSwimEvent ??= items.OrderBy(e => e.Order).FirstOrDefault();
        SwimEventOptions = SearchableItem.ToSwimEventOptions(items);
        if (SelectedSwimEvent is not null && items.Any(e => e.Id == SelectedSwimEvent.Id))
        {
            SyncSelectedSwimEventOption();
            return;
        }
        SelectedSwimEvent ??= items.OrderBy(e => e.Order).FirstOrDefault();
    }

    private void SyncSelectedSwimEventOption()
    {
        if (SelectedSwimEvent?.Id is int eventId)
        {
            var option = SwimEventOptions.FirstOrDefault(o =>
                o.Value is SwimEvent swim && swim.Id == eventId);
            if (!ReferenceEquals(SelectedSwimEventOption, option))
                SelectedSwimEventOption = option;
        }
        else if (SelectedSwimEventOption is not null)
            SelectedSwimEventOption = null;
    }

    partial void OnSelectedSwimEventChanged(SwimEvent? value)
    {
        SyncSelectedSwimEventOption();
        _ = LoadEventHeatsAsync();
        OnPropertyChanged(nameof(SelectedHeatHeader));
        OnPropertyChanged(nameof(SelectedHeatStatus));
        OnPropertyChanged(nameof(CanEditHeat));
    }

    partial void OnSelectedSwimEventOptionChanged(SearchableItem? value)
    {
        if (value?.Value is SwimEvent swimEvent && SelectedSwimEvent?.Id != swimEvent.Id)
            SelectedSwimEvent = swimEvent;
    }

    partial void OnSelectedHeatItemChanged(HeatListItemView? value)
    {
        LoadHeatPositions();
        RefreshButtons();
        OnPropertyChanged(nameof(CanEditHeat));
        OnPropertyChanged(nameof(SelectedHeatHeader));
        OnPropertyChanged(nameof(SelectedHeatStatus));
    }

    private async Task LoadEventHeatsAsync()
    {
        if (SelectedSwimEvent?.Id is not int eventId)
        {
            _fixationSwimEvent = null;
            EventHeats = [];
            SelectedHeatItem = null;
            FixationHeatPositionViews = [];
            return;
        }
        await YieldToBackgroundAsync();
        var keepHeatId = SelectedHeat?.Id;
        var eventIdCopy = eventId;
        _fixationSwimEvent = await CrudService.Query()
            .Include(se => se.SwimStyle)
            .Include(se => se.AgeGroup)
            .AsNoTracking()
            .FirstOrDefaultAsync(se => se.Id == eventIdCopy)
            .ConfigureAwait(false);
        var heats = await HeatService.GetHeatsByEventIdAsync(eventIdCopy).ConfigureAwait(false);
        await DispatcherUiHelper.InvokeOnUiAsync(() =>
        {
            EventHeats = new ObservableCollection<HeatListItemView>(heats.Select(h => new HeatListItemView(h)));
            SelectedHeatItem = keepHeatId is int id
                ? EventHeats.FirstOrDefault(h => h.Entity.Id == id) ?? EventHeats.FirstOrDefault()
                : EventHeats.FirstOrDefault();
        });
    }

    private void LoadHeatPositions()
    {
        if (SelectedHeat?.Positions is null || SelectedHeat.Positions.Count == 0)
        {
            FixationHeatPositionViews = [];
            RefreshButtons();
            return;
        }
        var swimEvent = _fixationSwimEvent ?? SelectedSwimEvent;
        if (swimEvent is null)
        {
            FixationHeatPositionViews = [];
            RefreshButtons();
            return;
        }
        var positions = SelectedHeat.Positions.OrderBy(p => p.Lane).ToList();
        FixationHeatPositionViews = new ObservableCollection<FixationHeatPositionView>(positions
            .Select(p => new FixationHeatPositionView(p, swimEvent, OnRowChanged, PointScoreProvider)));
        RefreshButtons();
    }

    private void OnRowChanged()
    {
        RefreshButtons();
    }

    private void RefreshButtons()
    {
        CanUnapprove = SelectedHeat?.Status == HeatStatus.OFFICIAL;
        CanApprove = SelectedHeat is not null
                     && CanEditHeat
                     && FixationHeatPositionViews.Count > 0
                     && FixationHeatPositionViews.All(v => v.IsCompleteForApproval());
        OnPropertyChanged(nameof(CanEditHeat));
        OnPropertyChanged(nameof(SelectedHeatHeader));
        OnPropertyChanged(nameof(SelectedHeatStatus));
    }

    [RelayCommand]
    private async Task ApproveHeat()
    {
        if (SelectedHeat is null) return;
        if (!CanApprove) return;
        try
        {
            await HeatService.ApproveHeatAsync(SelectedHeat);
            await LoadEventHeatsAsync();
            if (SelectedSwimEvent?.Id is int eventId)
                EventResultsChanged?.Invoke(eventId);
        }
        catch
        {
        }
    }

    [RelayCommand]
    private async Task UnapproveHeat()
    {
        if (SelectedHeat?.Id is not int heatId)
            return;
        try
        {
            await HeatService.UnapproveHeatAsync(heatId);
            await LoadEventHeatsAsync();
            if (SelectedSwimEvent?.Id is int eventId)
                EventResultsChanged?.Invoke(eventId);
        }
        catch
        {
        }
    }

    [RelayCommand]
    private void GoToNextEvent()
    {
        if (Items.Count == 0) return;
        var entities = Items.Select(row => row.Entity).ToList();
        var idx = SelectedSwimEvent is null ? -1 : entities.IndexOf(SelectedSwimEvent);
        var nextIdx = (idx + 1) % entities.Count;
        SelectedSwimEvent = entities[nextIdx];
    }

    [RelayCommand]
    private void GoToPrevEvent()
    {
        if (Items.Count == 0) return;
        var entities = Items.Select(row => row.Entity).ToList();
        var idx = SelectedSwimEvent is null ? 1 : entities.IndexOf(SelectedSwimEvent);
        var prevIdx = idx - 1 + (idx - 1 < 0 ? entities.Count : 0);
        SelectedSwimEvent = entities[prevIdx];
    }

    [RelayCommand]
    private void GoToNextHeat()
    {
        if (EventHeats.Count == 0) return;
        var idx = SelectedHeatItem is null ? -1 : EventHeats.IndexOf(SelectedHeatItem);
        var nextIdx = (idx + 1);
        if (nextIdx >= EventHeats.Count)
            GoToNextEvent();
        else
            SelectedHeatItem = EventHeats[nextIdx];
    }

    [RelayCommand]
    private void GoToPrevHeat()
    {
        if (EventHeats.Count == 0) return;
        var idx = SelectedHeatItem is null ? 0 : EventHeats.IndexOf(SelectedHeatItem);
        var prevIdx = idx - 1;
        if (prevIdx < 0)
            GoToPrevEvent();
        else
            SelectedHeatItem = EventHeats[prevIdx];
    }

    partial void OnSelectedFixationPositionChanged(FixationHeatPositionView? value) =>
        OpenAthleteDetailsCommand.NotifyCanExecuteChanged();

    private bool CanOpenAthleteDetails() =>
        EntryAthleteNavigationHelper.TryGetAthleteId(SelectedFixationPosition?.Entry, out _);

    [RelayCommand(CanExecute = nameof(CanOpenAthleteDetails))]
    private void OpenAthleteDetails()
    {
        if (!EntryAthleteNavigationHelper.TryGetAthleteId(SelectedFixationPosition?.Entry, out var athleteId))
            return;
        navigationService.NavigateTo<AthleteDetailsViewModel>(athleteId);
    }
}
