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

public class EntriesBySwimStyleViewModel : EntriesViewModel
{
    private readonly IAddEditWindowFactory _windowFactory;
    private int? _swimStyleId;

    public EntriesBySwimStyleViewModel(IEntryService entryService,
        IEntryDocumentReaderService entryDocumentReaderService)
        : base(entryService, entryDocumentReaderService)
    {
        _windowFactory = App.Current.Services.GetRequiredService<IAddEditWindowFactory>();
    }

    public void SetSwimStyleId(int? swimStyleId)
    {
        _swimStyleId = swimStyleId;
        LoadDataCommand.Execute(null);
    }

    protected override IQueryable<Entry> ApplyQuery(IQueryable<Entry> query)
    {
        query = base.ApplyQuery(query);
        return _swimStyleId.HasValue ? query.Where(e => e.SwimStyleId == _swimStyleId.Value) : query.Where(_ => false);
    }

    protected override void ShowAddEditDialog(int? id = default)
    {
        var context = _swimStyleId.HasValue ? new NavigationContext { SwimStyleId = _swimStyleId.Value } : null;
        var result = _windowFactory.CreateAndShow<EntryAddEditWindow>(id, context);
        if (result == true)
            _ = LoadDataAsync();
    }
}
