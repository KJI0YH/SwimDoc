using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Data;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DataLayer.EfClasses;
using Microsoft.EntityFrameworkCore;
using ServiceLayer.EventService;
using ServiceLayer.HeatService;
using UI.ViewModels.Pages.Data;

namespace UI.ViewModels.Pages;

public partial class HeatsViewModel(IEventService eventService, IHeatService heatService)
    : DataViewModel<SwimEvent, int?>(eventService)
{
    [ObservableProperty] ObservableCollection<HeatPositionView> _heatPositions = new();

    [ObservableProperty] private SwimEvent? _selectedSwimEvent;

    private ObservableCollection<HeatPositionView>? _heatPositionsGroupedSource;
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
        if (items.Count == 0)
        {
            SelectedSwimEvent = null;
            HeatPositions = [];
            return;
        }

        SelectedSwimEvent ??= items.OrderBy(e => e.Order).FirstOrDefault();
    }

    partial void OnSelectedSwimEventChanged(SwimEvent? value)
    {
        _ = LoadHeatPositionsAsync();
    }

    partial void OnHeatPositionsChanged(ObservableCollection<HeatPositionView> value)
    {
        OnPropertyChanged(nameof(HeatPositionsView));
    }

    private async Task LoadHeatPositionsAsync()
    {
        if (SelectedSwimEvent?.Id is not int eventId)
        {
            HeatPositions = [];
            return;
        }

        IsLoading = true;
        try
        {
            var heats = await heatService.GetHeatsByEventIdAsync(eventId);
            var heatsInEvent = heats.Count;
            var heatsTotal = heatService.GetTotalHeats();
            var heatPositionViews = heats.SelectMany(h =>
                h.Positions.Select(p => new HeatPositionView(p, h.Number, heatsInEvent, h.Order, heatsTotal)));
            HeatPositions = new ObservableCollection<HeatPositionView>(heatPositionViews);
        }
        finally
        {
            IsLoading = false;
        }
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
}

public sealed class HeatPositionView(
    HeatPosition heatPosition,
    int heatNumber,
    int heatsInEvent,
    int heatOrder,
    int heatsTotal)
{
    private HeatPosition HeatPosition { get; set; } = heatPosition;

    public int HeatId => HeatPosition.HeatId;
    public string HeatGroupHeader => $"Заплыв {heatNumber} из {heatsInEvent} ({heatOrder} из {heatsTotal})";
    public int Lane => HeatPosition.Lane;
    public string Participant => HeatPosition.Entry.Athlete?.DisplayName ?? string.Empty;
    public int? YearOfBirth => HeatPosition.Entry.Athlete?.YearOfBirth;
    public string Club => HeatPosition.Entry.Athlete?.Club?.Name ?? string.Empty;
    public string EntryTime => HeatPosition.Entry.DisplayEntryTime;
    public string FinishTime => HeatPosition.Entry.DisplayFinishTime;
}