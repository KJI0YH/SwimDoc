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
using UI.ViewModels;
using UI.ViewModels.Pages;

namespace UI.ViewModels.Pages.Data;

public class HeatByEntryIdViewModel : HeatsViewModel
{
    private int? _entryId;
    protected override bool UsesHeatPaging => false;
    public HeatByEntryIdViewModel(IEventService eventService, IHeatService heatService,
        INavigationService navigationService) : base(eventService, heatService, navigationService)
    {
    }

    public void SetEntryId(int? entryId)
    {
        _entryId = entryId;
        LoadDataCommand.Execute(null);
    }

    protected override async Task LoadHeatPositionsAsync()
    {
        if (SelectedSwimEvent?.Id is not int eventId || !_entryId.HasValue)
        {
            HeatPositions = [];
            UpdateHeatPaging(0);
            return;
        }
        IsLoading = true;
        try
        {
            var heats = await HeatService.GetHeatsByEventIdAsync(eventId);
            var heatsInEvent = heats.Count;
            var heatsForEntry = heats
                .Where(heat => heat.Positions.Any(hp => hp.EntryId == _entryId.Value))
                .ToList();
            var heatsTotal = HeatService.GetTotalHeats();
            var swimEvent = SelectedSwimEvent;
            var heatPositionViews = heatsForEntry.SelectMany(h =>
                h.Positions.Select(p =>
                    new HeatPositionView(p, swimEvent, h.Number, heatsInEvent, h.Order, heatsTotal, h.Status, EntityDisplayFormatter.FormatHeatDayTime(h))));
            HeatPositions = new ObservableCollection<HeatPositionView>(heatPositionViews);
            SelectedHeatPosition = heatPositionViews.FirstOrDefault(p => p.Entry.Id == _entryId.Value);
            UpdateHeatPaging(heatsForEntry.Count, resetPage: false);
        }
        finally
        {
            IsLoading = false;
        }
    }

    protected override IQueryable<SwimEvent> ApplyQuery(IQueryable<SwimEvent> query)
    {
        query = base.ApplyQuery(query);
        return _entryId.HasValue
            ? query.Select(se => new SwimEvent
            {
                Id = se.Id,
                Date = se.Date,
                Time = se.Time,
                Order = se.Order,
                AgeGroup = se.AgeGroup,
                SwimStyle = se.SwimStyle,
                LaneMin = se.LaneMin,
                LaneMax = se.LaneMax,
                Heats = se.Heats
                        .Where(heat => heat.Positions
                            .Any(hp => hp.EntryId == _entryId.Value)
                        )
                        .ToList()
            })
                .Where(se => se.Heats.Any())
            : query.Where(_ => false);
    }
}
