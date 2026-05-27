using System.ComponentModel;
using DataLayer.EfClasses;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ServiceLayer.AthleteService;
using ServiceLayer.ClubService;
using UI.Services;
using UI.ViewModels.Pages.Data;
using UI.Views.Windows.AddEdit;

namespace UI.ViewModels.Pages;

public class ClubsViewModel : DataViewModel<Club, int?>
{
    private readonly IAddEditWindowFactory _windowFactory;

    public ClubsViewModel(IClubService clubService, IAthleteService athleteService) : base(clubService)
    {
        _windowFactory = App.Current.Services.GetRequiredService<IAddEditWindowFactory>();
    }

    protected override void InitializeColumns()
    {
        AutoGenerateColumns = false;
        ColumnConfigurations.Clear();

        ColumnConfigurations.Add(new ColumnConfiguration<Club>("Name", "Название", 300));
        ColumnConfigurations.Add(new ColumnConfiguration<Club>("DisplayAthleteCount", "Спортсмены", 150,
            (query, direction) =>
            {
                return direction == ListSortDirection.Ascending
                    ? query.OrderBy(club => club.Athletes.Count)
                    : query.OrderByDescending(club => club.Athletes.Count);
            }));

        ColumnConfigurations.Add(new ColumnConfiguration<Club>("DisplayEntryCount", "Заявки", 150,
            (query, direction) =>
            {
                return direction == ListSortDirection.Ascending
                    ? query.OrderBy(club => club.Athletes.Sum(a => a.Entries.Count))
                    : query.OrderByDescending(club => club.Athletes.Sum(a => a.Entries.Count));
            }));
        ColumnConfigurations.Add(new ColumnConfiguration<Club>("DisplayRelayCount", "Эстафеты", 150,
            (query, direction) =>
            {
                return direction == ListSortDirection.Ascending
                    ? query.OrderBy(club => club.Relays.Count)
                    : query.OrderByDescending(club => club.Relays.Count);
            }));
        ColumnConfigurations.Add(new ColumnConfiguration<Club>("DisplayPointCount", "Очки", 150,
            (query, direction) =>
            {
                return direction == ListSortDirection.Ascending
                    ? query.OrderBy(club => club.Athletes.Sum(a => a.Entries.Where(e => e.Scoring).Sum(e => e.Points)))
                    : query.OrderByDescending(club =>
                        club.Athletes.Sum(a => a.Entries.Where(e => e.Scoring).Sum(e => e.Points)));
            }));
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

        return Queryable.Where(query, club =>
            EF.Functions.Like(club.Name, $"%{SearchText}%"));
    }

    protected override void ShowAddEditDialog(int? id = default)
    {
        var result = _windowFactory.CreateAndShow<ClubAddEditWindow>(id);
        if (result == true) _ = LoadDataAsync();
    }
}