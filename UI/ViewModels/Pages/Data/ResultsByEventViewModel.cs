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

public class ResultsByEventViewModel(
    IEventService eventService,
    IEntryService entryService,
    IAgeGroupService ageGroupService,
    INavigationService navigationService)
    : ResultsViewModel(eventService, entryService, ageGroupService, navigationService)
{
    private int? _eventId;
    public Task RefreshForEventAsync(int eventId) => LoadEntriesForEventIdAsync(eventId);
    public void SetEventId(int? eventId)
    {
        _eventId = eventId;
        SelectedSwimEvent = null;
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
