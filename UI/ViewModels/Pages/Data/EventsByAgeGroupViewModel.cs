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
using UI.Services;
using UI.ViewModels;
using UI.ViewModels.Pages;
using UI.Views.Dialogs.Markers.AddEdit;

namespace UI.ViewModels.Pages.Data;

public class EventsByAgeGroupViewModel : EventsViewModel
{
    private readonly IAddEditWindowFactory _windowFactory;
    private int? _ageGroupId;

    public EventsByAgeGroupViewModel(IEventService eventService) : base(eventService)
    {
        _windowFactory = App.Current.Services.GetRequiredService<IAddEditWindowFactory>();
    }

    public void SetAgeGroupId(int? ageGroupId)
    {
        _ageGroupId = ageGroupId;
        LoadDataCommand.Execute(null);
    }

    protected override IQueryable<SwimEvent> ApplyQuery(IQueryable<SwimEvent> query)
    {
        query = base.ApplyQuery(query);
        return _ageGroupId.HasValue ? query.Where(e => e.AgeGroupId == _ageGroupId.Value) : query.Where(_ => false);
    }

    protected override void ShowAddEditDialog(int? id = default)
    {
        var context = _ageGroupId.HasValue ? new NavigationContext { AgeGroupId = _ageGroupId.Value } : null;
        var result = _windowFactory.CreateAndShow<EventAddEditWindow>(id, context);
        if (result == true)
            _ = LoadDataAsync();
    }
}
