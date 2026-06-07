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
using UI.Views.Dialogs.Markers.AddEdit;

namespace UI.ViewModels.Pages.Data;

public class HeatsByEventViewModel : HeatsViewModel
{
    private readonly IAddEditWindowFactory _windowFactory;
    private int? _eventId;
    public HeatsByEventViewModel(IEventService eventService, IHeatService heatService,
        INavigationService navigationService) : base(eventService, heatService, navigationService)
    {
        _windowFactory = App.Current.Services.GetRequiredService<IAddEditWindowFactory>();
    }

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

    protected override void ShowHeatAddEditDialog(int? heatId = null)
    {
        var context = _eventId.HasValue
            ? new NavigationContext { EventId = _eventId.Value }
            : SelectedSwimEvent?.Id is int eventId
                ? new NavigationContext { EventId = eventId }
                : null;
        var result = _windowFactory.CreateAndShow<HeatAddEditWindow>(heatId, context);
        if (result == true)
            _ = RefreshAsync();
    }
}
