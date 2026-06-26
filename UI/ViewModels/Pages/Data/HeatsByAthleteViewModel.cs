using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DataLayer.EfClasses;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ServiceLayer.AgeGroupService;
using ServiceLayer.AthleteService;
using ServiceLayer.ClubService;
using ServiceLayer.EntryDocumentReaderService;
using ServiceLayer.EntryService;
using ServiceLayer.EventService;
using ServiceLayer.HeatService;
using ServiceLayer.PointScoreProvider;
using ServiceLayer.SwimStyleService;
using UI.Helpers.Threading;
using UI.ViewModels;
using UI.ViewModels.Pages;

namespace UI.ViewModels.Pages.Data;

public class HeatsByAthleteViewModel : HeatsViewModel
{
    private int? _athleteId;
    private int? _focusEntryId;
    private int? _focusSwimEventId;
    protected override bool UsesHeatPaging => false;
    public HeatsByAthleteViewModel(IEventService eventService, IHeatService heatService,
        INavigationService navigationService) : base(eventService, heatService, navigationService)
    {
    }

    public void SetAthleteId(int? athleteId, int? focusEntryId = null, int? focusSwimEventId = null)
    {
        _athleteId = athleteId;
        _focusEntryId = focusEntryId;
        _focusSwimEventId = focusSwimEventId;
        LoadDataCommand.Execute(null);
    }

    protected override void OnItemsLoaded(IReadOnlyList<SwimEvent> items)
    {
        base.OnItemsLoaded(items);
        if (!_focusSwimEventId.HasValue)
            return;
        var focusEvent = items.FirstOrDefault(e => e.Id == _focusSwimEventId.Value);
        if (focusEvent is not null)
            SelectedSwimEvent = focusEvent;
    }

    protected override async Task LoadHeatPositionsAsync()
    {
        if (SelectedSwimEvent?.Id is not int eventId || !_athleteId.HasValue)
        {
            HeatPositions = [];
            return;
        }
        var athleteId = _athleteId.Value;
        var focusEntryId = _focusEntryId;
        var focusSwimEventId = _focusSwimEventId;
        var swimEvent = SelectedSwimEvent;
        await YieldToBackgroundAsync();

        var heats = await HeatService.GetHeatsByEventIdAsync(eventId).ConfigureAwait(false);
            var heatsInEvent = heats.Count;
            var heatsForAthlete = heats
                .Where(heat => heat.Positions.Any(hp =>
                    hp.Entry.AthleteId == athleteId ||
                    (hp.Entry.Relay != null && hp.Entry.Relay.Positions.Any(p => p.AthleteId == athleteId))))
                .ToList();
            var heatsTotal = await HeatService.GetTotalHeatsAsync().ConfigureAwait(false);
            var heatPositionViews = heatsForAthlete.SelectMany(h =>
                h.Positions.Select(p =>
                    new HeatPositionView(p, swimEvent, h.Number, heatsInEvent, h.Order, heatsTotal, h.Status, EntityDisplayFormatter.FormatHeatDayTime(h))))
                .ToList();

        await DispatcherUiHelper.InvokeOnUiAsync(() =>
        {
            HeatPositions = new ObservableCollection<HeatPositionView>(heatPositionViews);
            UpdateHeatPaging(heatsForAthlete.Count, resetPage: false);
            if (focusEntryId.HasValue)
            {
                SelectedHeatPosition = heatPositionViews.FirstOrDefault(p => p.EntryId == focusEntryId.Value);
                _focusEntryId = null;
                _focusSwimEventId = null;
            }
        });
    }

    protected override IQueryable<SwimEvent> ApplyQuery(IQueryable<SwimEvent> query)
    {
        query = base.ApplyQuery(query);
        if (!_athleteId.HasValue)
            return query.Where(_ => false);
        return query.Where(se => se.Heats.Any(heat =>
            heat.Positions.Any(hp =>
                hp.Entry.AthleteId == _athleteId.Value ||
                (hp.Entry.Relay != null &&
                 hp.Entry.Relay.Positions.Any(p => p.AthleteId == _athleteId.Value)))));
    }
}
