using System.ComponentModel;
using BizLogic.Helpers;
using DataLayer.EfClasses;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ServiceLayer.AgeGroupService;
using UI.Services;
using UI.ViewModels.Pages.Data;
using UI.Views.Windows.AddEdit;

namespace UI.ViewModels.Pages;

public class AgeGroupsViewModel : DataViewModel<AgeGroup, int?>
{
    private readonly IAddEditWindowFactory _windowFactory;

    public AgeGroupsViewModel(IAgeGroupService ageGroupService) : base(ageGroupService)
    {
        _windowFactory = App.Current.Services.GetRequiredService<IAddEditWindowFactory>();
    }

    protected override void InitializeColumns()
    {
        AutoGenerateColumns = false;
        ColumnConfigurations.Clear();

        ColumnConfigurations.Add(new ColumnConfiguration<AgeGroup>("DisplayName", "Название", 400,
            (query, direction) =>
            {
                return direction == ListSortDirection.Ascending
                    ? query.OrderBy(ag => ag.Name)
                    : query.OrderByDescending(ag => ag.Name);
            }));
        ColumnConfigurations.Add(new ColumnConfiguration<AgeGroup>("Gender", "Пол", 100));
        ColumnConfigurations.Add(new ColumnConfiguration<AgeGroup>("DisplayBirthYearMax", "Год рождения от", 150,
            (query, direction) =>
            {
                return direction == ListSortDirection.Ascending
                    ? query.OrderBy(ag => ag.BirthYearMax)
                    : query.OrderByDescending(ag => ag.BirthYearMax);
            }));
        ColumnConfigurations.Add(new ColumnConfiguration<AgeGroup>("DisplayBirthYearMin", "Год рождения до", 150,
            (query, direction) =>
            {
                return direction == ListSortDirection.Ascending
                    ? query.OrderBy(ag => ag.BirthYearMin)
                    : query.OrderByDescending(ag => ag.BirthYearMin);
            }));
    }

    protected override IQueryable<AgeGroup> ApplySearch(IQueryable<AgeGroup> query)
    {
        if (string.IsNullOrWhiteSpace(SearchText))
            return query;
        if (EnumHelper.TryGetEnumByDescriptionContains(SearchText, out Gender gender))
            return query.Where(ag => ag.Gender == gender);

        if (int.TryParse((string?)SearchText, out var year))
            return Queryable.Where(query, ag =>
                EF.Functions.Like(ag.BirthYearMin.ToString(), $"%{SearchText}%") ||
                EF.Functions.Like(ag.BirthYearMax.ToString(), $"%{SearchText}%"));

        return Queryable.Where(query, ag =>
            EF.Functions.Like(ag.Name, $"%{SearchText}%"));
    }

    protected override void ShowAddEditDialog(int? id = default)
    {
        var result = _windowFactory.CreateAndShow<AgeGroupAddEditWindow>(id);
        if (result == true) _ = LoadDataAsync();
    }
}