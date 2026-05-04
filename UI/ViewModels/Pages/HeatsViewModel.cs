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

public sealed class HeatPositionGridRow
{
    public int HeatGroupOrder { get; init; }

    public string HeatGroupTitle { get; init; } = string.Empty;

    public int Lane { get; init; }
    public string Participant { get; init; } = string.Empty;
    public string YearOfBirth { get; init; } = string.Empty;
    public string Club { get; init; } = string.Empty;
    public string EntryTime { get; init; } = string.Empty;
    public string FinishTime { get; init; } = string.Empty;
}

public partial class HeatsViewModel(IEventService eventService, IHeatService heatService)
    : DataViewModel<SwimEvent, int?>(eventService)
{
    private readonly ObservableCollection<HeatPositionGridRow> _rows = new();
    private ICollectionView? _rowsView;
    private bool _rowsViewConfigured;

    protected bool SuppressSwimEventHeatAutoPick;

    [ObservableProperty] private int _selectedTabIndex;

    [ObservableProperty] private SwimEvent? _selectedSwimEvent;
    [ObservableProperty] private Heat? _selectedHeat;
    [ObservableProperty] private HeatPosition? _selectedHeatPosition;

    public int TotalHeats => heatService.GetTotalHeats();

    public ICollectionView RowsView => EnsureRowsView();

    private ICollectionView EnsureRowsView()
    {
        if (_rowsView is not null)
            return _rowsView;

        _rowsView = CollectionViewSource.GetDefaultView(_rows);
        if (!_rowsViewConfigured)
        {
            _rowsView.GroupDescriptions.Add(new PropertyGroupDescription(nameof(HeatPositionGridRow.HeatGroupOrder)));
            _rowsView.SortDescriptions.Add(new SortDescription(nameof(HeatPositionGridRow.HeatGroupOrder),
                ListSortDirection.Ascending));
            _rowsView.SortDescriptions.Add(new SortDescription(nameof(HeatPositionGridRow.Lane),
                ListSortDirection.Ascending));
            _rowsViewConfigured = true;
        }

        return _rowsView;
    }

    protected override IQueryable<SwimEvent> ApplyQuery(IQueryable<SwimEvent> query)
    {
        return query
            .OrderBy(se => se.Order)
            .Include(e => e.AgeGroup)
            .Include(e => e.SwimStyle)
            .Include(e => e.Heats.OrderBy(h => h.Order).ThenBy(h => h.Number))
            .ThenInclude(h => h.Positions.OrderBy(p => p.Lane))
            .ThenInclude(hp => hp.Entry)
            .ThenInclude(ent => ent.Athlete!)
            .ThenInclude(a => a.Club)
            .Include(e => e.Heats.OrderBy(h => h.Order).ThenBy(h => h.Number))
            .ThenInclude(h => h.Positions.OrderBy(p => p.Lane))
            .ThenInclude(hp => hp.Entry)
            .ThenInclude(ent => ent.Relay!)
            .ThenInclude(r => r.Club);
    }

    protected override void OnItemsLoaded(IReadOnlyList<SwimEvent> items)
    {
        EnsureRowsView();

        foreach (var swimEvent in items)
        {
            var totalHeatsInEvent = heatService.GetTotalHeatsInEvent(swimEvent.Id);
            foreach (var heat in swimEvent.Heats)
                heat.TotalHeatsInEvent = totalHeatsInEvent;
        }

        if (AfterItemsLoaded(items))
        {
            RebuildRows();
            return;
        }

        EnsureDefaultSelection();
        RebuildRows();
    }

    protected virtual bool AfterItemsLoaded(IReadOnlyList<SwimEvent> items) => false;

    protected virtual bool UseFixationStyleDefaultSwimEventSelection => false;

    protected static IOrderedEnumerable<Heat> HeatsInProgramOrder(IEnumerable<Heat> heats) =>
        heats.OrderBy(h => h.Order).ThenBy(h => h.Number);

    private void EnsureDefaultSelection()
    {
        if (UseFixationStyleDefaultSwimEventSelection)
            EnsureFixationStyleDefaultSelection();
        else
            EnsureHeatsPageProgramDefaultSelection();
    }

    private void EnsureHeatsPageProgramDefaultSelection()
    {
        if (Items.Count == 0)
        {
            SelectedSwimEvent = null;
            SelectedHeat = null;
            return;
        }

        if (SelectedHeat is not null)
        {
            var matchEvent = Items.FirstOrDefault(se => se.Heats.Any(h => h.Id == SelectedHeat.Id));
            if (matchEvent is not null)
            {
                SuppressSwimEventHeatAutoPick = true;
                try
                {
                    SelectedSwimEvent = matchEvent;
                    SelectedHeat = HeatsInProgramOrder(matchEvent.Heats).FirstOrDefault(h => h.Id == SelectedHeat.Id);
                }
                finally
                {
                    SuppressSwimEventHeatAutoPick = false;
                }

                return;
            }
        }

        var firstEvent = Items.OrderBy(se => se.Order).FirstOrDefault();
        SuppressSwimEventHeatAutoPick = true;
        try
        {
            SelectedSwimEvent = firstEvent;
            SelectedHeat = firstEvent is null ? null : PickDefaultHeatForSelectedSwimEvent(firstEvent);
        }
        finally
        {
            SuppressSwimEventHeatAutoPick = false;
        }
    }

    private void EnsureFixationStyleDefaultSelection()
    {
        if (Items.Count == 0)
        {
            SelectedSwimEvent = null;
            SelectedHeat = null;
            return;
        }

        var ordered = Items
            .OrderBy(se => se.Order)
            .SelectMany(se => HeatsInProgramOrder(se.Heats).Select(h => (eventRef: se, heatRef: h)))
            .ToList();

        if (ordered.Count == 0)
        {
            SelectedSwimEvent = Items.OrderBy(se => se.Order).FirstOrDefault();
            SelectedHeat = null;
            return;
        }

        if (SelectedHeat is not null)
        {
            var match = ordered.FirstOrDefault(x => x.heatRef.Id == SelectedHeat.Id);
            if (match.heatRef is not null)
            {
                SuppressSwimEventHeatAutoPick = true;
                try
                {
                    SelectedSwimEvent = match.eventRef;
                    SelectedHeat = match.heatRef;
                }
                finally
                {
                    SuppressSwimEventHeatAutoPick = false;
                }

                return;
            }
        }

        var firstNotStarted = ordered.FirstOrDefault(x => x.heatRef.Status == HeatStatus.NOT_STARTED);
        var pick = firstNotStarted.heatRef is not null ? firstNotStarted : ordered[0];

        SuppressSwimEventHeatAutoPick = true;
        try
        {
            SelectedSwimEvent = pick.eventRef;
            SelectedHeat = pick.heatRef;
        }
        finally
        {
            SuppressSwimEventHeatAutoPick = false;
        }
    }

    protected virtual Heat? PickDefaultHeatForSelectedSwimEvent(SwimEvent swimEvent) =>
        HeatsInProgramOrder(swimEvent.Heats).FirstOrDefault();

    protected override bool CanDelete()
    {
        return SelectedSwimEvent is not null ||
               SelectedHeat is not null ||
               SelectedHeatPosition is not null;
    }

    protected override Task DeleteItem()
    {
        if (SelectedHeat?.Status == HeatStatus.OFFICIAL)
            return Task.CompletedTask;

        if (SelectedHeatPosition is not null)
            return DeleteSelectedHeatPositionAsync();
        if (SelectedHeat is not null)
            return DeleteSelectedHeatAsync();
        if (SelectedSwimEvent is not null)
            return DeleteSwimEventHeatsAsync();
        return Task.CompletedTask;
    }

    private async Task DeleteSelectedHeatAsync()
    {
        if (SelectedHeat is null)
            return;

        await heatService.DeleteAsync(SelectedHeat.Id);
        SelectedHeat = null;
        SelectedHeatPosition = null;
        await LoadDataAsync();
    }

    private async Task DeleteSelectedHeatPositionAsync()
    {
        if (SelectedHeatPosition is null)
            return;

        await heatService.DeleteHeatPositionAsync(SelectedHeatPosition.HeatId, SelectedHeatPosition.EntryId);
        SelectedHeatPosition = null;
        await LoadDataAsync();
    }

    private async Task DeleteSwimEventHeatsAsync()
    {
        if (SelectedSwimEvent is null) return;
        await heatService.DeleteSwimEventHeatsAsync(SelectedSwimEvent.Id);
        SelectedItem = null;
        await LoadDataAsync();
    }

    partial void OnSelectedSwimEventChanged(SwimEvent? value)
    {
        DeleteItemCommand.NotifyCanExecuteChanged();

        if (SuppressSwimEventHeatAutoPick)
        {
            RebuildRows();
            return;
        }

        if (value is null)
        {
            SelectedHeat = null;
            RebuildRows();
            return;
        }

        SelectedHeat = PickDefaultHeatForSelectedSwimEvent(value);
        RebuildRows();
    }

    partial void OnSelectedHeatChanged(Heat? value)
    {
        DeleteItemCommand.NotifyCanExecuteChanged();
        OnSelectedHeatChangedCore(value);
    }

    partial void OnSelectedHeatPositionChanged(HeatPosition? value)
    {
        DeleteItemCommand.NotifyCanExecuteChanged();
        OnSelectedHeatPositionChangedCore(value);
    }

    protected virtual void OnSelectedHeatChangedCore(Heat? value) { }
    protected virtual void OnSelectedHeatPositionChangedCore(HeatPosition? value) { }

    private void RebuildRows()
    {
        EnsureRowsView();
        _rows.Clear();

        if (SelectedSwimEvent is null)
        {
            _rowsView!.Refresh();
            return;
        }

        var groupIndex = 0;
        foreach (var heat in HeatsInProgramOrder(SelectedSwimEvent.Heats))
        {
            groupIndex++;
            var totalInEvent = heat.TotalHeatsInEvent > 0
                ? heat.TotalHeatsInEvent
                : SelectedSwimEvent.Heats.Count;

            var heatTitle =
                $"Заплыв {heat.Number} из {totalInEvent} ({heat.Order} из {TotalHeats}) | {heat.Status}";

            foreach (var pos in heat.Positions.OrderBy(p => p.Lane))
            {
                var entry = pos.Entry;

                var athlete = entry.Athlete;
                var relay = entry.Relay;
                var participant = relay is not null
                    ? $"{relay.Club?.Name} {(relay.Number.HasValue ? relay.Number.Value : string.Empty)}"
                    : athlete?.DisplayName ?? string.Empty;
                var yob = athlete?.YearOfBirth.ToString() ?? string.Empty;
                var club = athlete?.DisplayClubName ?? relay?.Club?.Name ?? string.Empty;

                _rows.Add(new HeatPositionGridRow
                {
                    HeatGroupOrder = groupIndex,
                    HeatGroupTitle = heatTitle,
                    Lane = pos.Lane,
                    Participant = participant,
                    YearOfBirth = yob,
                    Club = club,
                    EntryTime = entry.DisplayEntryTime,
                    FinishTime = entry.DisplayFinishTime
                });
            }
        }

        _rowsView!.Refresh();
    }

    [RelayCommand]
    private void GoToNextHeat()
    {
        if (Items.Count == 0)
            return;

        var ordered = Items
            .OrderBy(se => se.Order)
            .SelectMany(se => HeatsInProgramOrder(se.Heats).Select(h => (eventRef: se, heatRef: h)))
            .ToList();

        if (ordered.Count == 0)
            return;

        var idx = SelectedHeat is null ? -1 : ordered.FindIndex(x => x.heatRef.Id == SelectedHeat.Id);
        var nextIdx = Math.Min(idx + 1, ordered.Count - 1);

        var next = ordered[nextIdx];
        SuppressSwimEventHeatAutoPick = true;
        try
        {
            SelectedSwimEvent = next.eventRef;
            SelectedHeat = next.heatRef;
        }
        finally
        {
            SuppressSwimEventHeatAutoPick = false;
        }
    }

    [RelayCommand]
    private void GoToPrevHeat()
    {
        if (Items.Count == 0)
            return;

        var ordered = Items
            .OrderBy(se => se.Order)
            .SelectMany(se => HeatsInProgramOrder(se.Heats).Select(h => (eventRef: se, heatRef: h)))
            .ToList();

        if (ordered.Count == 0)
            return;

        var idx = SelectedHeat is null ? 0 : ordered.FindIndex(x => x.heatRef.Id == SelectedHeat.Id);
        if (idx < 0) idx = 0;
        var prevIdx = Math.Max(idx - 1, 0);

        var prev = ordered[prevIdx];
        SuppressSwimEventHeatAutoPick = true;
        try
        {
            SelectedSwimEvent = prev.eventRef;
            SelectedHeat = prev.heatRef;
        }
        finally
        {
            SuppressSwimEventHeatAutoPick = false;
        }
    }

    [RelayCommand]
    private void GoToNextEvent()
    {
        if (Items.Count == 0)
            return;

        var orderedEvents = Items.OrderBy(se => se.Order).ToList();
        var idx = SelectedSwimEvent is null ? -1 : orderedEvents.FindIndex(se => se.Id == SelectedSwimEvent.Id);
        var nextIdx = Math.Min(idx + 1, orderedEvents.Count - 1);
        var nextEvent = orderedEvents[nextIdx];

        SuppressSwimEventHeatAutoPick = true;
        try
        {
            SelectedSwimEvent = nextEvent;
            SelectedHeat = PickDefaultHeatForSelectedSwimEvent(nextEvent);
        }
        finally
        {
            SuppressSwimEventHeatAutoPick = false;
        }
    }

    [RelayCommand]
    private void GoToPrevEvent()
    {
        if (Items.Count == 0)
            return;

        var orderedEvents = Items.OrderBy(se => se.Order).ToList();
        var idx = SelectedSwimEvent is null ? 0 : orderedEvents.FindIndex(se => se.Id == SelectedSwimEvent.Id);
        if (idx < 0) idx = 0;
        var prevIdx = Math.Max(idx - 1, 0);
        var prevEvent = orderedEvents[prevIdx];

        SuppressSwimEventHeatAutoPick = true;
        try
        {
            SelectedSwimEvent = prevEvent;
            SelectedHeat = PickDefaultHeatForSelectedSwimEvent(prevEvent);
        }
        finally
        {
            SuppressSwimEventHeatAutoPick = false;
        }
    }
}
