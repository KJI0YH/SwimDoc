using System.ComponentModel;
using BizLogic.Helpers;
using DataLayer.EfClasses;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ServiceLayer.SwimStyleService;
using UI.Services;
using UI.ViewModels.Generic;
using UI.Views.AddEdit;

namespace UI.ViewModels.Table;

public class SwimStylesViewModel : GenericTableViewModel<SwimStyle, int?>
{
    private readonly IAddEditWindowFactory _windowFactory;

    public SwimStylesViewModel(ISwimStyleService swimStyleService) : base(swimStyleService)
    {
        _windowFactory = App.Current.Services.GetRequiredService<IAddEditWindowFactory>();
    }

    protected override void InitializeColumns()
    {
        base.InitializeColumns();
        AutoGenerateColumns = false;
        ColumnConfigurations.Clear();

        ColumnConfigurations.Add(new ColumnConfiguration<SwimStyle>("DisplayName", "Название", 300,
            (query, direction) =>
            {
                return direction == ListSortDirection.Ascending
                    ? query.OrderBy(e => e.Distance).ThenBy(e => e.Stroke).ThenBy(e => e.RelayCount)
                    : query.OrderByDescending(e => e.Distance).ThenByDescending(e => e.Stroke)
                        .ThenBy(e => e.RelayCount);
            }));
        ColumnConfigurations.Add(new ColumnConfiguration<SwimStyle>("Distance", "Дистанция", 150));
        ColumnConfigurations.Add(new ColumnConfiguration<SwimStyle>("Stroke", "Стиль", 200));
        ColumnConfigurations.Add(new ColumnConfiguration<SwimStyle>("RelayCount", "Участников эстафеты", 150));
    }

    protected override IQueryable<SwimStyle> ApplySearch(IQueryable<SwimStyle> query)
    {
        if (string.IsNullOrWhiteSpace(SearchText))
            return query;

        if (EnumHelper.TryGetEnumByDescriptionContains<Stroke>(SearchText, out var stroke))
        {
            return query.Where(ss => ss.Stroke == stroke);
        }

        if (int.TryParse(SearchText, out var distance))
        {
            return query.Where(ss =>
                EF.Functions.Like(ss.Distance.ToString(), $"%{SearchText}%"));
        }

        return query;
    }

    protected override void ShowAddEditDialog(int? id = default)
    {
        var result = _windowFactory.CreateAndShow<SwimStyleAddEditWindow>(id);
        if (result == true)
        {
            _ = LoadDataAsync();
        }
    }
}