using System.Collections.ObjectModel;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Windows.Data;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DataLayer;
using DataLayer.EfClasses;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ServiceLayer.EventService;
using ServiceLayer.HeatService;
using UI.Helpers;
using UI.Resources;
using UI.Services;
using UI.ViewModels.Pages.Data;
using UI.Views.Controls.SearchableComboBox;
using UI.Views.Windows.AddEdit;

namespace UI.ViewModels.Pages;

public partial class HeatsViewModel(
    IEventService eventService,
    IHeatService heatService,
    INavigationService navigationService)
    : DataViewModel<SwimEvent, int?>(eventService)
{
    private readonly IAddEditWindowFactory _windowFactory =
        App.Current.Services.GetRequiredService<IAddEditWindowFactory>();

    private readonly IPagingSettingsService _pagingSettings =
        App.Current.Services.GetRequiredService<IPagingSettingsService>();

    protected IHeatService HeatService { get; } = heatService;

    private int HeatPageSize => _pagingSettings.GetPageSize(PagingPage.Heats);

    protected virtual bool UsesHeatPaging => true;

    protected override bool UsesPaging => false;

    [ObservableProperty] ObservableCollection<HeatPositionView> _heatPositions = new();

    private bool _searchHandlerAttached;
    private bool _suppressHeatPageLoad;
    private bool _heatPagingSubscribed;

    private void EnsureSearchHandlerAttached()
    {
        if (_searchHandlerAttached)
            return;

        _searchHandlerAttached = true;
        PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(SearchText))
            {
                ResetHeatPaging();
                _ = LoadHeatPositionsAsync();
            }
        };
    }

    private void EnsureHeatPagingSubscription()
    {
        if (_heatPagingSubscribed)
            return;

        _heatPagingSubscribed = true;
        _pagingSettings.PageSizeChanged += OnHeatPagingSettingsChanged;
    }

    private void OnHeatPagingSettingsChanged(PagingPage page)
    {
        if (page != PagingPage.Heats)
            return;

        ResetHeatPaging();
        _ = LoadHeatPositionsAsync();
    }

    [ObservableProperty] private SwimEvent? _selectedSwimEvent;
    [ObservableProperty] private ObservableCollection<SearchableItem> _swimEventOptions = new();

    [ObservableProperty] private HeatPositionView? _selectedHeatPosition;

    [ObservableProperty] private int? _selectedHeatId;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(DeleteToolTip))]
    private bool _isWholeHeatSelected;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HeatsItemsInfo))]
    private int _heatCurrentPage;

    [ObservableProperty] private ObservableCollection<int> _heatPageOptions = new();

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HeatsItemsInfo), nameof(IsHeatPagingEnabled))]
    private int _heatTotalPages;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HeatsItemsInfo))]
    private int _heatTotalItems;

    private ObservableCollection<HeatPositionView>? _heatPositionsGroupedSource;

    public string DeleteToolTip => IsWholeHeatSelected ? Strings.Heats_DeleteTooltip_Heat : Strings.Heats_DeleteTooltip_Position;
    private ListCollectionView? _heatPositionsGroupedView;

    public string HeatsItemsInfo
    {
        get
        {
            if (HeatTotalItems <= 0)
                return string.Format(Strings.Paging_ItemsInfo_EmptyFormat, HeatTotalItems);

            var from = HeatCurrentPage * HeatPageSize + 1;
            var to = Math.Min(HeatTotalItems, HeatCurrentPage * HeatPageSize + HeatPageSize);
            return string.Format(Strings.Paging_ItemsInfo_RangeFormat, from, to, HeatTotalItems);
        }
    }

    public bool IsHeatPagingEnabled => HeatTotalPages > 0;

    public ICollectionView HeatPositionsView
    {
        get
        {
            if (_heatPositionsGroupedSource == HeatPositions) return _heatPositionsGroupedView!;
            _heatPositionsGroupedSource = HeatPositions;
            _heatPositionsGroupedView = new ListCollectionView(HeatPositions);
            _heatPositionsGroupedView.GroupDescriptions?.Add(
                new PropertyGroupDescription(nameof(HeatPositionView.HeatId)));
            return _heatPositionsGroupedView!;
        }
    }

    protected override IQueryable<SwimEvent> ApplyQuery(IQueryable<SwimEvent> query)
    {
        return query
            .OrderBy(se => se.Order)
            .Include(e => e.AgeGroup)
            .Include(e => e.SwimStyle);
    }

    protected override void OnItemsLoaded(IReadOnlyList<SwimEvent> items)
    {
        EnsureSearchHandlerAttached();
        EnsureHeatPagingSubscription();

        if (items.Count == 0)
        {
            SelectedSwimEvent = null;
            SwimEventOptions = [];
            HeatPositions = [];
            UpdateHeatPaging(0);
            return;
        }

        SwimEventOptions = new ObservableCollection<SearchableItem>(
            items.Select(e => new SearchableItem
            {
                Value = e,
                DisplayText = EntityDisplayFormatter.FormatSwimEvent(e)
            }));

        if (SelectedSwimEvent is not null && items.Any(e => e.Id == SelectedSwimEvent.Id))
            return;

        SelectedSwimEvent ??= items.OrderBy(e => e.Order).FirstOrDefault();
    }

    public Task RefreshAsync() => LoadHeatPositionsAsync();

    partial void OnSelectedSwimEventChanged(SwimEvent? value)
    {
        CreateHeatCommand.NotifyCanExecuteChanged();
        ResetHeatPaging();
        _ = LoadHeatPositionsAsync();
    }

    partial void OnHeatPositionsChanged(ObservableCollection<HeatPositionView> value)
    {
        if (SelectedHeatId is int heatId && !value.Any(position => position.HeatId == heatId))
        {
            SelectedHeatId = null;
            SelectedHeatPosition = null;
        }

        OnPropertyChanged(nameof(HeatPositionsView));
    }

    partial void OnHeatCurrentPageChanged(int value)
    {
        GoToFirstHeatPageCommand.NotifyCanExecuteChanged();
        GoToPreviousHeatPageCommand.NotifyCanExecuteChanged();
        GoToNextHeatPageCommand.NotifyCanExecuteChanged();
        GoToLastHeatPageCommand.NotifyCanExecuteChanged();
        OnPropertyChanged(nameof(HeatsItemsInfo));

        if (!_suppressHeatPageLoad)
            _ = LoadHeatPositionsAsync();
    }

    partial void OnHeatTotalPagesChanged(int value)
    {
        GoToFirstHeatPageCommand.NotifyCanExecuteChanged();
        GoToPreviousHeatPageCommand.NotifyCanExecuteChanged();
        GoToNextHeatPageCommand.NotifyCanExecuteChanged();
        GoToLastHeatPageCommand.NotifyCanExecuteChanged();

        HeatPageOptions = value > 0
            ? new ObservableCollection<int>(Enumerable.Range(1, value))
            : new ObservableCollection<int>();
    }

    protected virtual async Task LoadHeatPositionsAsync()
    {
        if (SelectedSwimEvent?.Id is not int eventId)
        {
            HeatPositions = [];
            UpdateHeatPaging(0);
            return;
        }

        IsLoading = true;
        try
        {
            var heatsInEvent = HeatService.GetTotalHeatsInEvent(eventId);
            var heatsTotal = HeatService.GetTotalHeats();
            List<Heat> heats;

            if (!UsesHeatPaging || !string.IsNullOrWhiteSpace(SearchText))
            {
                heats = await HeatService.GetHeatsByEventIdAsync(eventId);
                UpdateHeatPaging(heatsInEvent, resetPage: false);
            }
            else
            {
                UpdateHeatPaging(heatsInEvent, resetPage: false);
                if (heatsInEvent == 0)
                {
                    HeatPositions = [];
                    return;
                }

                heats = await HeatService.GetHeatsByEventIdPagedAsync(eventId, HeatCurrentPage, HeatPageSize);
            }

            var swimEvent = SelectedSwimEvent;
            var heatPositionViews = heats.SelectMany(h =>
                h.Positions.Select(p => new HeatPositionView(
                    p,
                    swimEvent,
                    h.Number,
                    heatsInEvent,
                    h.Order,
                    heatsTotal,
                    h.Status,
                    h.DisplayDayTime)));

            HeatPositions = new ObservableCollection<HeatPositionView>(
                FilterHeatPositions(heatPositionViews));
        }
        finally
        {
            IsLoading = false;
        }
    }

    private void ResetHeatPaging()
    {
        if (HeatCurrentPage == 0)
            return;

        _suppressHeatPageLoad = true;
        HeatCurrentPage = 0;
        _suppressHeatPageLoad = false;
    }

    protected void UpdateHeatPaging(int totalHeats, bool resetPage = true)
    {
        _suppressHeatPageLoad = true;
        HeatTotalItems = totalHeats;
        HeatTotalPages = totalHeats > 0 ? (int)Math.Ceiling(totalHeats / (double)HeatPageSize) : 0;

        if (resetPage)
            HeatCurrentPage = 0;
        else if (HeatCurrentPage >= HeatTotalPages && HeatTotalPages > 0)
            HeatCurrentPage = HeatTotalPages - 1;

        _suppressHeatPageLoad = false;
        OnPropertyChanged(nameof(HeatsItemsInfo));
    }

    [RelayCommand(CanExecute = nameof(CanGoToFirstHeatPage))]
    private void GoToFirstHeatPage() => HeatCurrentPage = 0;

    private bool CanGoToFirstHeatPage() => HeatCurrentPage > 0;

    [RelayCommand(CanExecute = nameof(CanGoToPreviousHeatPage))]
    private void GoToPreviousHeatPage() => HeatCurrentPage--;

    private bool CanGoToPreviousHeatPage() => HeatCurrentPage > 0;

    [RelayCommand(CanExecute = nameof(CanGoToNextHeatPage))]
    private void GoToNextHeatPage() => HeatCurrentPage++;

    private bool CanGoToNextHeatPage() => HeatTotalPages > 0 && HeatCurrentPage < HeatTotalPages - 1;

    [RelayCommand(CanExecute = nameof(CanGoToLastHeatPage))]
    private void GoToLastHeatPage() => HeatCurrentPage = HeatTotalPages - 1;

    private bool CanGoToLastHeatPage() => HeatTotalPages > 0 && HeatCurrentPage < HeatTotalPages - 1;

    [RelayCommand(CanExecute = nameof(CanCreateHeat))]
    private void CreateHeat() => ShowHeatAddEditDialog();

    [RelayCommand(CanExecute = nameof(CanEditHeat))]
    private void EditHeat()
    {
        if (SelectedHeatId is not int heatId)
            return;

        ShowHeatAddEditDialog(heatId);
    }

    public void ApplySelection(int heatId, HeatPositionView? position, bool wholeHeat)
    {
        SelectedHeatId = heatId;
        SelectedHeatPosition = position;
        IsWholeHeatSelected = wholeHeat;
    }

    [RelayCommand(CanExecute = nameof(CanDeleteSelected))]
    private async Task DeleteSelectedAsync()
    {
        try
        {
            var deleteConfirmation = App.Current.Services.GetRequiredService<IConfirmDialogService>();

            if (ShouldDeleteWholeHeat() && SelectedHeatId is int heatId)
            {
                if (!await deleteConfirmation.ConfirmDeleteIfOfficialResultsAffectedAsync<Heat>([heatId]))
                    return;

                await HeatService.DeleteHeatAsync(heatId);
                SelectedHeatId = null;
                SelectedHeatPosition = null;
                IsWholeHeatSelected = false;
            }
            else if (SelectedHeatPosition is not null)
            {
                if (!await deleteConfirmation.ConfirmDeleteIfOfficialResultsAffectedAsync<Entry>(
                        [SelectedHeatPosition.EntryId]))
                    return;

                await HeatService.DeleteHeatPositionAsync(
                    SelectedHeatPosition.HeatId,
                    SelectedHeatPosition.EntryId);
            }
            else
            {
                return;
            }

            await RefreshAsync();
        }
        catch (ValidationException)
        {
            // ignored
        }
    }

    private bool CanCreateHeat() => SelectedSwimEvent?.Id is not null;

    private bool CanEditHeat() => SelectedHeatId is not null;

    private bool CanDeleteSelected() =>
        ShouldDeleteWholeHeat() || SelectedHeatPosition is not null;

    private bool ShouldDeleteWholeHeat() =>
        IsWholeHeatSelected && SelectedHeatId is not null;

    protected virtual void ShowHeatAddEditDialog(int? heatId = null)
    {
        var context = SelectedSwimEvent?.Id is int eventId
            ? new AddEditContext { EventId = eventId }
            : null;
        var result = _windowFactory.CreateAndShow<HeatAddEditWindow>(heatId, context);
        if (result == true)
            _ = RefreshAsync();
    }

    [RelayCommand]
    private void GoToNextEvent()
    {
        if (Items.Count == 0)
            return;

        var idx = SelectedSwimEvent is null ? -1 : Items.IndexOf(SelectedSwimEvent);
        var nextIdx = (idx + 1) % Items.Count;
        SelectedSwimEvent = Items[nextIdx];
    }

    [RelayCommand]
    private void GoToPrevEvent()
    {
        if (Items.Count == 0)
            return;

        var idx = SelectedSwimEvent is null ? 1 : Items.IndexOf(SelectedSwimEvent);
        var prevIdx = idx - 1;
        if (prevIdx < 0)
            prevIdx += Items.Count;
        SelectedSwimEvent = Items[prevIdx];
    }

    partial void OnSelectedHeatPositionChanged(HeatPositionView? value)
    {
        if (value is not null)
            SelectedHeatId = value.HeatId;

        OpenAthleteDetailsCommand.NotifyCanExecuteChanged();
        EditHeatCommand.NotifyCanExecuteChanged();
        DeleteSelectedCommand.NotifyCanExecuteChanged();
    }

    partial void OnSelectedHeatIdChanged(int? value)
    {
        EditHeatCommand.NotifyCanExecuteChanged();
        DeleteSelectedCommand.NotifyCanExecuteChanged();
    }

    partial void OnIsWholeHeatSelectedChanged(bool value) =>
        DeleteSelectedCommand.NotifyCanExecuteChanged();

    private IEnumerable<HeatPositionView> FilterHeatPositions(IEnumerable<HeatPositionView> positions)
    {
        if (string.IsNullOrWhiteSpace(SearchText))
            return positions;

        var terms = SearchText.Trim()
            .Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (terms.Length == 0)
            return positions;

        return positions.Where(position => terms.All(term =>
            MatchesTerm(position, term)));
    }

    private static bool MatchesTerm(HeatPositionView position, string term)
    {
        if (Contains(position.Participant, term) || Contains(position.Club, term))
            return true;

        var athlete = position.Entry.Athlete;
        if (athlete is null)
            return false;

        return Contains(athlete.FirstName, term) ||
               Contains(athlete.LastName, term) ||
               Contains($"{athlete.FirstName} {athlete.LastName}", term) ||
               Contains($"{athlete.LastName} {athlete.FirstName}", term);
    }

    private static bool Contains(string? value, string term) =>
        !string.IsNullOrEmpty(value) &&
        value.Contains(term, StringComparison.CurrentCultureIgnoreCase);

    private bool CanOpenAthleteDetails() =>
        EntryAthleteNavigationHelper.TryGetAthleteId(SelectedHeatPosition?.Entry, out _);

    [RelayCommand(CanExecute = nameof(CanOpenAthleteDetails))]
    private void OpenAthleteDetails()
    {
        if (!EntryAthleteNavigationHelper.TryGetAthleteId(SelectedHeatPosition?.Entry, out var athleteId))
            return;

        navigationService.NavigateTo<AthleteDetailsViewModel>(athleteId);
    }
}

public sealed class HeatPositionView(
    HeatPosition heatPosition,
    SwimEvent? swimEvent,
    int heatNumber,
    int heatsInEvent,
    int heatOrder,
    int heatsTotal,
    HeatStatus heatStatus,
    string heatDayTime)
{
    private HeatPosition HeatPosition { get; set; } = heatPosition;

    public Entry Entry => HeatPosition.Entry;

    public int HeatId => HeatPosition.HeatId;
    public int EntryId => HeatPosition.EntryId;
    public HeatStatus HeatStatus { get; } = heatStatus;
    public string HeatGroupHeader => string.Format(
        Strings.Heats_GroupHeader_Format,
        heatNumber,
        heatsInEvent,
        heatOrder,
        heatsTotal,
        heatDayTime,
        heatStatus);
    public int Lane => HeatPosition.Lane;

    public string DisplayLane => swimEvent is not null
        ? SwimEventLaneNames.GetLaneDisplay(swimEvent, HeatPosition.Lane)
        : HeatPosition.Lane.ToString();
    public string Participant => HeatPosition.Entry.DisplayParticipantName;
    public int? YearOfBirth => HeatPosition.Entry.Athlete?.YearOfBirth;
    public string Club => HeatPosition.Entry.DisplayParticipantClubName;
    public string EntryTime => HeatPosition.Entry.DisplayEntryTime;
    public string FinishTime => HeatPosition.Entry.DisplayFinishTime;
}
