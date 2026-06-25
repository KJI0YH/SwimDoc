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
using UI.Models.Rows.Projections;
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
        ColumnConfigurations.Add(new ColumnConfiguration<Club>("Name", Strings.Clubs_Col_Name, 300,
            ColumnConfiguration<Club>.SortBy(club => club.Name)));
        ColumnConfigurations.Add(new ColumnConfiguration<Club>("AthleteCount", Strings.Clubs_Col_Athletes, 150,
            ColumnConfiguration<Club>.SortBy(club => club.Athletes.Count)));
        ColumnConfigurations.Add(new ColumnConfiguration<Club>("EntryCount", Strings.Clubs_Col_Entries, 170,
            ColumnConfiguration<Club>.SortBy(club =>
                club.Athletes.Sum(athlete => athlete.Entries.Count(e => e.Scoring)),
                club => club.Athletes.Sum(athlete => athlete.Entries.Count(e => !e.Scoring)))));
        ColumnConfigurations.Add(new ColumnConfiguration<Club>("RelayCount", Strings.Clubs_Col_Relays, 170,
            ColumnConfiguration<Club>.SortBy(club =>
                club.Relays.Count(r => r.Entry != null && r.Entry.Scoring),
                club => club.Relays.Count(r => r.Entry != null && !r.Entry.Scoring))));
        ColumnConfigurations.Add(new ColumnConfiguration<Club>("PointCount", Strings.Clubs_Col_Points, 150,
            ColumnConfiguration<Club>.SortBy(club => club.Athletes.Sum(athlete =>
                athlete.Entries.Where(entry => entry.Scoring).Sum(entry => entry.Points ?? 0)))));
    }

    protected override async Task<List<ClubRowView>> LoadPageRowsAsync(
        IQueryable<Club> query,
        IServiceProvider serviceProvider)
    {
        var projections = await RowProjectionQueries.SelectClub(query).ToListAsync().ConfigureAwait(false);
        return projections.Select(ClubRowView.FromProjection).ToList();
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
        if (result == true) ReloadAfterMutation();
    }
}
