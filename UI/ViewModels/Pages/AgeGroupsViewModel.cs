using System.ComponentModel;
using DataLayer.EfClasses;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ServiceLayer.AgeGroupService;
using UI.Helpers;
using UI.Resources;
using UI.Services;
using UI.ViewModels.Pages.Data;
using UI.Views.Windows.AddEdit;

namespace UI.ViewModels.Pages;

public class AgeGroupsViewModel : DataViewModel<AgeGroup, int?>
{
    protected override PagingPage PagingSettingsPage => PagingPage.AgeGroups;

    private readonly IAddEditWindowFactory _windowFactory;

    public AgeGroupsViewModel(IAgeGroupService ageGroupService) : base(ageGroupService)
    {
        _windowFactory = App.Current.Services.GetRequiredService<IAddEditWindowFactory>();
    }

    protected override void InitializeColumns()
    {
        AutoGenerateColumns = false;
        ColumnConfigurations.Clear();

        ColumnConfigurations.Add(new ColumnConfiguration<AgeGroup>(".", Strings.AgeGroups_Col_Name, 400,
            (query, direction) =>
            {
                return direction == ListSortDirection.Ascending
                    ? query.OrderBy(ag => ag.Name)
                    : query.OrderByDescending(ag => ag.Name);
            })
        {
            Converter = EntityDisplayConverter.Instance,
            ConverterParameter = EntityDisplayConverter.AgeGroupKind
        });
        ColumnConfigurations.Add(new ColumnConfiguration<AgeGroup>("Gender", Strings.AgeGroups_Col_Gender, 100));
        ColumnConfigurations.Add(new ColumnConfiguration<AgeGroup>("BirthYearMin", Strings.AgeGroups_Col_BirthYearFrom, 150,
            (query, direction) =>
            {
                return direction == ListSortDirection.Ascending
                    ? query.OrderBy(ag => ag.BirthYearMin)
                    : query.OrderByDescending(ag => ag.BirthYearMin);
            })
        {
            Converter = new BirthYearBoundConverter(),
            ConverterParameter = BirthYearBoundConverter.Min
        });
        ColumnConfigurations.Add(new ColumnConfiguration<AgeGroup>("BirthYearMax", Strings.AgeGroups_Col_BirthYearTo, 150,
            (query, direction) =>
            {
                return direction == ListSortDirection.Ascending
                    ? query.OrderBy(ag => ag.BirthYearMax)
                    : query.OrderByDescending(ag => ag.BirthYearMax);
            })
        {
            Converter = new BirthYearBoundConverter(),
            ConverterParameter = BirthYearBoundConverter.Max
        });
    }

    protected override IQueryable<AgeGroup> ApplySearch(IQueryable<AgeGroup> query)
    {
        if (string.IsNullOrWhiteSpace(SearchText))
            return query;
        if (Strings.TryFindEnumByDisplayContains(SearchText, out Gender gender))
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