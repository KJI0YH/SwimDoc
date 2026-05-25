using System.ComponentModel;
using DataLayer.EfClasses;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ServiceLayer.AthleteService;
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

        ColumnConfigurations.Add(new ColumnConfiguration<Athlete>("FirstName", "Имя", 200));
        ColumnConfigurations.Add(new ColumnConfiguration<Athlete>("LastName", "Фамилия", 200));
        ColumnConfigurations.Add(new ColumnConfiguration<Athlete>("Gender", "Пол", 90));
        ColumnConfigurations.Add(new ColumnConfiguration<Athlete>("YearOfBirth", "Год рождения", 120));
        ColumnConfigurations.Add(new ColumnConfiguration<Athlete>("Category", "Разряд", 100));
        ColumnConfigurations.Add(new ColumnConfiguration<Athlete>("DisplayClubName", "Команда", 300,
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