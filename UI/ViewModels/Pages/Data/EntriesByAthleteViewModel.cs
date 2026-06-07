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

public class EntriesByAthleteViewModel : EntriesViewModel
{
    private readonly IAddEditWindowFactory _windowFactory;
    private int? _athleteId;
    public EntriesByAthleteViewModel(IEntryService entryService, IEntryDocumentReaderService entryDocumentReaderService)
        : base(entryService, entryDocumentReaderService)
    {
        _windowFactory = App.Current.Services.GetRequiredService<IAddEditWindowFactory>();
    }

    protected override void InitializeColumns()
    {
        InitializeEntriesColumns();
    }

    public void SetAthleteId(int? athleteId)
    {
        _athleteId = athleteId;
        ResetFilterOptions();
        RequestReload();
        EnsureDataLoaded();
    }

    protected override IQueryable<Entry> ApplyQuery(IQueryable<Entry> query) => ApplyAthleteScope(query);

    private IQueryable<Entry> ApplyAthleteScope(IQueryable<Entry> query) =>
        _athleteId.HasValue
            ? query.Where(e =>
                e.AthleteId == _athleteId.Value ||
                (e.Relay != null && e.Relay.Positions.Any(p => p.AthleteId == _athleteId.Value)))
            : query.Where(_ => false);

    protected override void ShowAddEditDialog(int? id = default)
    {
        var context = _athleteId.HasValue ? new NavigationContext { AthleteId = _athleteId.Value } : null;
        var result = _windowFactory.CreateAndShow<EntryAddEditWindow>(id, context);
        if (result == true)
            _ = LoadDataAsync();
    }
}
