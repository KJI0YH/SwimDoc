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

public class AthletesByClubViewModel : AthletesViewModel
{
    private readonly IAddEditWindowFactory _windowFactory;
    private int? _clubId;
    public AthletesByClubViewModel(IAthleteService athleteService) : base(athleteService)
    {
        _windowFactory = App.Current.Services.GetRequiredService<IAddEditWindowFactory>();
    }

    public void SetClubId(int? clubId)
    {
        _clubId = clubId;
        LoadDataCommand.Execute(null);
    }

    protected override IQueryable<Athlete> ApplyQuery(IQueryable<Athlete> query)
    {
        query = base.ApplyQuery(query);
        return _clubId.HasValue ? query.Where(a => a.ClubId == _clubId.Value) : query.Where(_ => false);
    }

    protected override void ShowAddEditDialog(int? id = default)
    {
        var context = _clubId.HasValue ? new NavigationContext { ClubId = _clubId.Value } : null;
        var result = _windowFactory.CreateAndShow<AthleteAddEditWindow>(id, context);
        if (result == true)
            ReloadAfterMutation();
    }
}
