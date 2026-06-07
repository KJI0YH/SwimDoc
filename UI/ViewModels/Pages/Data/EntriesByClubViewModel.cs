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

public class EntriesByClubViewModel : EntriesViewModel
{
    private readonly IAddEditWindowFactory _windowFactory;
    private int? _clubId;
    public EntriesByClubViewModel(IEntryService entryService, IEntryDocumentReaderService entryDocumentReaderService)
        : base(entryService, entryDocumentReaderService)
    {
        _windowFactory = App.Current.Services.GetRequiredService<IAddEditWindowFactory>();
    }

    protected override void InitializeColumns()
    {
        InitializeEntriesColumns();
    }

    public void SetClubId(int? clubId)
    {
        _clubId = clubId;
        ResetFilterOptions();
        RequestReload();
        EnsureDataLoaded();
    }

    protected override IQueryable<Entry> ApplyQuery(IQueryable<Entry> query) => ApplyClubScope(query);

    private IQueryable<Entry> ApplyClubScope(IQueryable<Entry> query) =>
        _clubId.HasValue
            ? query.Where(e =>
                (e.Athlete != null && e.Athlete.ClubId == _clubId.Value) ||
                (e.Relay != null && e.Relay.ClubId == _clubId.Value))
            : query.Where(_ => false);

    protected override void ShowAddEditDialog(int? id = default)
    {
        var context = _clubId.HasValue ? new NavigationContext { ClubId = _clubId.Value } : null;
        var result = _windowFactory.CreateAndShow<EntryAddEditWindow>(id, context);
        if (result == true)
            _ = LoadDataAsync();
    }
}
