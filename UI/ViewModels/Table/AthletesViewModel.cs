using DataLayer.EfClasses;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ServiceLayer.AthleteService;
using UI.Services;
using UI.ViewModels.Generic;
using UI.Views.AddEdit;

namespace UI.ViewModels.Table;

public class AthletesViewModel : GenericTableViewModel<Athlete, int?>
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

        ColumnConfigurations.Add(ColumnConfiguration.Create("FirstName", "Имя", 200));
        ColumnConfigurations.Add(ColumnConfiguration.Create("LastName", "Фамилия", 200));
        ColumnConfigurations.Add(ColumnConfiguration.Create("Gender", "Пол", 85));
        ColumnConfigurations.Add(ColumnConfiguration.Create("YearOfBirth", "Год рождения", 120));
        ColumnConfigurations.Add(ColumnConfiguration.Create("Category", "Разряд", 100));
        ColumnConfigurations.Add(ColumnConfiguration.Create("DisplayClubName", "Команда", 300));
    }

    protected override IQueryable<Athlete> ApplyQuery(IQueryable<Athlete> query)
    {
        return query.Include(athlete => athlete.Club);
    }

    protected override IQueryable<Athlete> ApplySearch(IQueryable<Athlete> query)
    {
        if (string.IsNullOrWhiteSpace(SearchText))
            return query;

        var searchLower = SearchText.ToLower();
        return query.Where(a =>
            a.FirstName.Contains(searchLower, StringComparison.CurrentCultureIgnoreCase) ||
            a.LastName.Contains(searchLower, StringComparison.CurrentCultureIgnoreCase));
    }

    protected override void ShowAddEditDialog(int? id = default)
    {
        var result = _windowFactory.CreateAndShow<AthleteAddEditWindow>(id);
        if (result == true)
        {
            _ = LoadDataAsync();
        }
    }
}