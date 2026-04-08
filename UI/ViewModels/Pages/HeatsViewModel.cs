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
    protected bool SuppressSwimEventHeatAutoPick;

    [ObservableProperty] private int _selectedTabIndex;

    [ObservableProperty] private SwimEvent? _selectedSwimEvent;
    [ObservableProperty] private Heat? _selectedHeat;
    [ObservableProperty] private HeatPosition? _selectedHeatPosition;

    public int TotalHeats => heatService.GetTotalHeats();

    protected override IQueryable<SwimEvent> ApplyQuery(IQueryable<SwimEvent> query)
    {
        return query
            .OrderBy(se => se.Order)
            .Include(e => e.AgeGroup)
            .Include(e => e.SwimStyle)
            .Include(e => e.Heats.OrderBy(h => h.Number))
            .ThenInclude(h => h.Positions.OrderBy(p => p.Lane))
            .ThenInclude(hp => hp.Entry)
            .ThenInclude(ent => ent.Athlete!)
            .ThenInclude(a => a.Club);
    }

    protected override void OnItemsLoaded(IReadOnlyList<SwimEvent> items)
    {
        foreach (var swimEvent in items)
        {
            var totalHeatsInEvent = heatService.GetTotalHeatsInEvent(swimEvent.Id);
            foreach (var heat in swimEvent.Heats)
                heat.TotalHeatsInEvent = totalHeatsInEvent;
        }

        if (AfterItemsLoaded(items))
            return;

        EnsureDefaultSelection();
    }

    protected virtual bool AfterItemsLoaded(IReadOnlyList<SwimEvent> items) => false;

    private void EnsureDefaultSelection()
    {
        if (Items.Count == 0)
        {
            SelectedSwimEvent = null;
            SelectedHeat = null;
            return;
        }

        var ordered = Items
            .OrderBy(se => se.Order)
            .SelectMany(se => se.Heats.OrderBy(h => h.Number).Select(h => (eventRef: se, heatRef: h)))
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
            return;

        if (value is null)
        {
            SelectedHeat = null;
            return;
        }

        // When selecting event (distance) from UI, default to earliest NOT_STARTED heat.
        var pick = value.Heats
            .OrderBy(h => h.Number)
            .FirstOrDefault(h => h.Status == HeatStatus.NOT_STARTED)
            ?? value.Heats.OrderBy(h => h.Number).FirstOrDefault();

        SelectedHeat = pick;
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

    [RelayCommand]
    private void GoToNextHeat()
    {
        if (Items.Count == 0)
            return;

        var ordered = Items
            .OrderBy(se => se.Order)
            .SelectMany(se => se.Heats.OrderBy(h => h.Number).Select(h => (eventRef: se, heatRef: h)))
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
            .SelectMany(se => se.Heats.OrderBy(h => h.Number).Select(h => (eventRef: se, heatRef: h)))
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
            SelectedHeat = nextEvent.Heats
                .OrderBy(h => h.Number)
                .FirstOrDefault(h => h.Status == HeatStatus.NOT_STARTED)
                ?? nextEvent.Heats.OrderBy(h => h.Number).FirstOrDefault();
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
            SelectedHeat = prevEvent.Heats
                .OrderBy(h => h.Number)
                .FirstOrDefault(h => h.Status == HeatStatus.NOT_STARTED)
                ?? prevEvent.Heats.OrderBy(h => h.Number).FirstOrDefault();
        }
        finally
        {
            SuppressSwimEventHeatAutoPick = false;
        }
    }
}