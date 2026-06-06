using DataLayer.EfClasses;
using Microsoft.EntityFrameworkCore;
using ServiceLayer.EventService;
using ServiceLayer.HeatService;
using ServiceLayer.PointScoreProvider;
using UI.Services;
using UI.ViewModels.Pages;

namespace UI.ViewModels.Pages.Data;

public class FixationByEventViewModel(
    IEventService eventService,
    IHeatService heatService,
    IPointScoreProvider pointScoreProvider,
    INavigationService navigationService)
    : FixationViewModel(eventService, heatService, pointScoreProvider, navigationService)
{
    private int? _eventId;

    public void SetEventId(int? eventId)
    {
        _eventId = eventId;
        LoadDataCommand.Execute(null);
    }

    protected override IQueryable<SwimEvent> ApplyQuery(IQueryable<SwimEvent> query)
    {
        query = base.ApplyQuery(query);
        return _eventId.HasValue
            ? query.Where(se => se.Id == _eventId.Value)
            : query.Where(_ => false);
    }
}
