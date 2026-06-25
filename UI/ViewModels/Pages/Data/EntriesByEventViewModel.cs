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

public class EntriesByEventViewModel : EntriesViewModel
{
    private readonly IAddEditWindowFactory _windowFactory;
    private int? _eventId;
    public EntriesByEventViewModel(IEntryService entryService, IEntryDocumentReaderService entryDocumentReaderService)
        : base(entryService, entryDocumentReaderService)
    {
        _windowFactory = App.Current.Services.GetRequiredService<IAddEditWindowFactory>();
    }

    public void SetEventId(int? eventId)
    {
        _eventId = eventId;
        RequestReload();
        EnsureDataLoaded();
    }

    protected override IQueryable<Entry> ApplyQuery(IQueryable<Entry> query)
    {
        query = base.ApplyQuery(query);
        return _eventId.HasValue ? query.Where(e => e.SwimEventId == _eventId.Value) : query.Where(_ => false);
    }

    protected override void ShowAddEditDialog(int? id = default)
    {
        var context = _eventId.HasValue ? new NavigationContext { EventId = _eventId.Value } : null;
        var result = _windowFactory.CreateAndShow<EntryAddEditWindow>(id, context);
        if (result == true)
            ReloadAfterMutation();
    }

    protected override NavigationContext? GetLoadEntriesFromPreviousEventContext() =>
        _eventId.HasValue ? new NavigationContext { EventId = _eventId.Value } : null;
}
