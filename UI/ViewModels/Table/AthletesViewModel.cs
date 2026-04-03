using System.ComponentModel;
using System.Globalization;
using BizLogic.Helpers;
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

        ColumnConfigurations.Add(new ColumnConfiguration<Athlete>("FirstName", "Имя", 200));
        ColumnConfigurations.Add(new ColumnConfiguration<Athlete>("LastName", "Фамилия", 200));
        ColumnConfigurations.Add(new ColumnConfiguration<Athlete>("Gender", "Пол", 85));
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
        
        if (int.TryParse(SearchText, out _))
        {
            return query.Where(a => 
                EF.Functions.Like(a .YearOfBirth.ToString(), $"%{SearchText}%"));
        }

        if (EnumHelper.TryGetEnumByDescriptionContains<Category>(SearchText, out var category))
        {
            return query.Where(a => a.Category == category);
        }

        if (EnumHelper.TryGetEnumByDescriptionContains<Gender>(SearchText, out var gender))
        {
            return query.Where(a => a.Gender == gender);
        }
        
        return query.Where(a =>
            EF.Functions.Like(a.FirstName, $"%{SearchText}%") ||
            EF.Functions.Like(a.LastName, $"%{SearchText}%") ||
            EF.Functions.Like(a.Club.Name, $"%{SearchText}%"));
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