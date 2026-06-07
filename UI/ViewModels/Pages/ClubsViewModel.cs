using System.ComponentModel;
using DataLayer.EfClasses;
using DataLayer.EfCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ServiceLayer.AthleteService;
using ServiceLayer.ClubService;
using UI.Resources;
using UI.ViewModels.Pages.Data;
using UI.Models.Rows;
using UI.Views.Dialogs.Markers.AddEdit;

namespace UI.ViewModels.Pages;

public class ClubsViewModel : DataViewModel<Club, ClubRowView, int?>
{
    protected override PagingPage PagingSettingsPage => PagingPage.Clubs;
    private readonly IAddEditWindowFactory _windowFactory;
    public ClubsViewModel(IClubService clubService, IAthleteService athleteService) : base(clubService)
    {
        _windowFactory = App.Current.Services.GetRequiredService<IAddEditWindowFactory>();
    }

    protected override void InitializeColumns()
    {
        AutoGenerateColumns = false;
        ColumnConfigurations.Clear();
        ColumnConfigurations.Add(new ColumnConfiguration<Club>("Name", Strings.Clubs_Col_Name, 300));
        ColumnConfigurations.Add(new ColumnConfiguration<Club>("AthleteCount", Strings.Clubs_Col_Athletes, 150,
            ColumnConfiguration<Club>.SortBy(club => club.Athletes.Count)));
        ColumnConfigurations.Add(new ColumnConfiguration<Club>("EntryCount", Strings.Clubs_Col_Entries, 150,
            ColumnConfiguration<Club>.SortBy(club => club.Athletes.Sum(athlete => athlete.Entries.Count))));
        ColumnConfigurations.Add(new ColumnConfiguration<Club>("RelayCount", Strings.Clubs_Col_Relays, 150,
            ColumnConfiguration<Club>.SortBy(club => club.Relays.Count)));
        ColumnConfigurations.Add(new ColumnConfiguration<Club>("PointCount", Strings.Clubs_Col_Points, 150,
            ColumnConfiguration<Club>.SortBy(club => club.Athletes.Sum(athlete =>
                athlete.Entries.Where(entry => entry.Scoring).Sum(entry => entry.Points)))));
    }

    protected override IQueryable<Club> ApplyQuery(IQueryable<Club> query)
    {
        return base.ApplyQuery(query)
            .Include(club => club.Relays)
            .Include(club => club.Athletes)
            .ThenInclude(athlete => athlete.Entries);
    }

    protected override IQueryable<Club> ApplySearch(IQueryable<Club> query)
    {
        if (string.IsNullOrWhiteSpace(SearchText))
            return query;
        var term = SearchText.Trim();
        return Queryable.Where(query, club =>
            SwimDocDbFunctions.ContainsIgnoreCase(club.Name, term));
    }

    protected override void ShowAddEditDialog(int? id = default)
    {
        var result = _windowFactory.CreateAndShow<ClubAddEditWindow>(id);
        if (result == true) _ = LoadDataAsync();
    }
}
