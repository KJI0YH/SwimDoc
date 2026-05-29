using System.Collections.ObjectModel;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Windows.Data;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
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

    protected IHeatService HeatService { get; } = heatService;

    [ObservableProperty] ObservableCollection<HeatPositionView> _heatPositions = new();

    private bool _searchHandlerAttached;

    private void EnsureSearchHandlerAttached()
    {
        if (_searchHandlerAttached)
            return;

        _searchHandlerAttached = true;
        PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(SearchText))
                _ = LoadHeatPositionsAsync();
        };
    }

    [ObservableProperty] private SwimEvent? _selectedSwimEvent;
    [ObservableProperty] private ObservableCollection<SearchableItem> _swimEventOptions = new();

    [ObservableProperty] private HeatPositionView? _selectedHeatPosition;

    [ObservableProperty] private int? _selectedHeatId;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(DeleteToolTip))]
    private bool _isWholeHeatSelected;

    private ObservableCollection<HeatPositionView>? _heatPositionsGroupedSource;

    public string DeleteToolTip => IsWholeHeatSelected ? Strings.Heats_DeleteTooltip_Heat : Strings.Heats_DeleteTooltip_Position;
    private ListCollectionView? _heatPositionsGroupedView;

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

        if (items.Count == 0)
        {
            SelectedSwimEvent = null;
            SwimEventOptions = [];
            HeatPositions = [];
            return;
        }

        SelectedSwimEvent ??= items.OrderBy(e => e.Order).FirstOrDefault();

        SwimEventOptions = new ObservableCollection<SearchableItem>(
            items.Select(e => new SearchableItem
            {
                Value = e,
                DisplayText = EntityDisplayFormatter.FormatSwimEvent(e)
            }));
    }

    public Task RefreshAsync() => LoadHeatPositionsAsync();

    [RelayCommand]
    private async Task RefreshHeatsAsync()
    {
        await LoadDataAsync();
        await RefreshAsync();
    }

    partial void OnSelectedSwimEventChanged(SwimEvent? value)
    {
        CreateHeatCommand.NotifyCanExecuteChanged();
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

    protected virtual async Task LoadHeatPositionsAsync()
    {
        if (SelectedSwimEvent?.Id is not int eventId)
        {
            HeatPositions = [];
            return;
        }

        IsLoading = true;
        try
        {
            var heats = await HeatService.GetHeatsByEventIdAsync(eventId);
            var heatsInEvent = heats.Count;
            var heatsTotal = HeatService.GetTotalHeats();
            var heatPositionViews = heats.SelectMany(h =>
                h.Positions.Select(p => new HeatPositionView(
                    p,
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
        if (Items.Count == 0) return;
        var idx = SelectedSwimEvent is null ? -1 : Items.IndexOf(SelectedSwimEvent);
        var nextIdx = (idx + 1) % Items.Count;
        SelectedSwimEvent = Items[nextIdx];
    }

    [RelayCommand]
    private void GoToPrevEvent()
    {
        if (Items.Count == 0) return;
        var idx = SelectedSwimEvent is null ? 1 : Items.IndexOf(SelectedSwimEvent);
        var prevIdx = idx - 1;
        if (prevIdx < 0) prevIdx += Items.Count;
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
        value.Contains(term, StringComparison.OrdinalIgnoreCase);

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
    public string Participant => HeatPosition.Entry.DisplayParticipantName;
    public int? YearOfBirth => HeatPosition.Entry.Athlete?.YearOfBirth;
    public string Club => HeatPosition.Entry.DisplayParticipantClubName;
    public string EntryTime => HeatPosition.Entry.DisplayEntryTime;
    public string FinishTime => HeatPosition.Entry.DisplayFinishTime;
}
