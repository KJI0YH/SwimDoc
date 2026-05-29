using System.ComponentModel;
using DataLayer.EfClasses;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ServiceLayer.AthleteService;
using UI.Resources;
using UI.Services;
using UI.ViewModels.Pages.Data;
using UI.Views.Windows.AddEdit;

namespace UI.ViewModels.Pages;

public class AthletesViewModel : DataViewModel<Athlete, int?>
{
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
        ColumnConfigurations.Add(new ColumnConfiguration<Athlete>("DisplayClubName", Strings.Athletes_Col_Team, 300,
            (query, direction) =>
            {
                return direction == ListSortDirection.Ascending
                    ? query.OrderBy(e => e.Club.Name)
                    : query.OrderByDescending(e => e.Club.Name);
            }));
    }

    protected override IQueryable<Athlete> ApplyQuery(IQueryable<Athlete> query)
    {
        return query.Include(athlete => athlete.Club);
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

            var termPattern = $"%{term}%";
            query = query.Where(a =>
                EF.Functions.Like(a.FirstName, termPattern) ||
                EF.Functions.Like(a.LastName, termPattern) ||
                EF.Functions.Like(a.FirstName + " " + a.LastName, termPattern) ||
                EF.Functions.Like(a.LastName + " " + a.FirstName, termPattern) ||
                (a.Club != null && EF.Functions.Like(a.Club.Name, termPattern)));
        }

        return query;
    }

    protected override void ShowAddEditDialog(int? id = default)
    {
        var result = _windowFactory.CreateAndShow<AthleteAddEditWindow>(id);
        if (result == true) _ = LoadDataAsync();
    }
}