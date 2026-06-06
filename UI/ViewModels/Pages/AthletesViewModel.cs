using System.ComponentModel;
using DataLayer.EfClasses;
using DataLayer.EfCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ServiceLayer.AthleteService;
using UI.Resources;
using UI.Services;
using UI.ViewModels.Pages.Data;
using UI.Models.Rows;
using UI.Views.Dialogs.Markers.AddEdit;

namespace UI.ViewModels.Pages;

public class AthletesViewModel : DataViewModel<Athlete, AthleteRowView, int?>
{
    protected override PagingPage PagingSettingsPage => PagingPage.Athletes;

    private readonly IAthleteService _athleteService;
    private readonly IAddEditWindowFactory _windowFactory;

    public AthletesViewModel(IAthleteService athleteService) : base(athleteService)
    {
        _athleteService = athleteService;
        _windowFactory = App.Current.Services.GetRequiredService<IAddEditWindowFactory>();
    }

    protected override void InitializeColumns()
    {
        AutoGenerateColumns = false;
        ColumnConfigurations.Clear();

        ColumnConfigurations.Add(new ColumnConfiguration<Athlete>("FirstName", Strings.Athletes_Col_FirstName, 200));
        ColumnConfigurations.Add(new ColumnConfiguration<Athlete>("LastName", Strings.Athletes_Col_LastName, 200));
        ColumnConfigurations.Add(new ColumnConfiguration<Athlete>("Gender", Strings.Athletes_Col_Gender, 90));
        ColumnConfigurations.Add(new ColumnConfiguration<Athlete>("YearOfBirth", Strings.Athletes_Col_BirthYear, 120));
        ColumnConfigurations.Add(new ColumnConfiguration<Athlete>("Category", Strings.Athletes_Col_Category, 100));
        ColumnConfigurations.Add(new ColumnConfiguration<Athlete>("ClubName", Strings.Athletes_Col_Team, 300,
            (query, direction) =>
            {
                return direction == ListSortDirection.Ascending
                    ? query.OrderBy(e => e.Club.Name)
                    : query.OrderByDescending(e => e.Club.Name);
            }));
        ColumnConfigurations.Add(new ColumnConfiguration<Athlete>("PointCount", Strings.Athletes_Col_Points, 80,
            (query, direction) =>
            {
                return direction == ListSortDirection.Ascending
                    ? query.OrderBy(a => a.Entries.Where(e => e.Scoring).Sum(e => e.Points ?? 0))
                    : query.OrderByDescending(a => a.Entries.Where(e => e.Scoring).Sum(e => e.Points ?? 0));
            }));
    }

    protected override IQueryable<Athlete> ApplyQuery(IQueryable<Athlete> query)
    {
        return query
            .Include(athlete => athlete.Club)
            .Include(athlete => athlete.Entries);
    }

    protected override IQueryable<Athlete> ApplySearch(IQueryable<Athlete> query)
    {
        if (string.IsNullOrWhiteSpace(SearchText))
            return query;

        var trimmed = SearchText.Trim();

        if (int.TryParse(trimmed, out _) && !trimmed.Contains(' '))
            return query.Where(a => EF.Functions.Like(a.YearOfBirth.ToString(), $"%{trimmed}%"));

        var terms = trimmed.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (terms.Length == 0)
            return query;

        foreach (var term in terms)
        {
            if (Strings.TryFindEnumByDisplayContains(term, out Gender gender))
            {
                query = query.Where(a => a.Gender == gender);
                continue;
            }

            query = query.Where(a =>
                SwimDocDbFunctions.ContainsIgnoreCase(a.FirstName, term) ||
                SwimDocDbFunctions.ContainsIgnoreCase(a.LastName, term) ||
                SwimDocDbFunctions.ContainsIgnoreCase(a.FirstName + " " + a.LastName, term) ||
                SwimDocDbFunctions.ContainsIgnoreCase(a.LastName + " " + a.FirstName, term) ||
                (a.Club != null && SwimDocDbFunctions.ContainsIgnoreCase(a.Club.Name, term)));
        }

        return query;
    }

    protected override void ShowAddEditDialog(int? id = default)
    {
        var result = _windowFactory.CreateAndShow<AthleteAddEditWindow>(id);
        if (result == true) _ = LoadDataAsync();
    }
}
