using System.ComponentModel;
using DataLayer.EfClasses;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ServiceLayer.SwimStyleService;
using UI.Helpers;
using UI.Resources;
using UI.Services;
using UI.ViewModels.Pages.Data;
using UI.Views.Windows.AddEdit;

namespace UI.ViewModels.Pages;

public class SwimStylesViewModel : DataViewModel<SwimStyle, int?>
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

        ColumnConfigurations.Add(new ColumnConfiguration<SwimStyle>(".", Strings.SwimStyles_Col_Name, 300,
            (query, direction) =>
            {
                return direction == ListSortDirection.Ascending
                    ? query.OrderBy(e => e.Distance).ThenBy(e => e.Stroke).ThenBy(e => e.RelayCount)
                    : query.OrderByDescending(e => e.Distance).ThenByDescending(e => e.Stroke)
                        .ThenBy(e => e.RelayCount);
            })
        {
            Converter = EntityDisplayConverter.Instance,
            ConverterParameter = EntityDisplayConverter.SwimStyleKind
        });
        ColumnConfigurations.Add(new ColumnConfiguration<SwimStyle>("Distance", Strings.SwimStyles_Col_Distance, 150));
        ColumnConfigurations.Add(new ColumnConfiguration<SwimStyle>("Stroke", Strings.SwimStyles_Col_Stroke, 200));
    }

    protected override IQueryable<SwimStyle> ApplySearch(IQueryable<SwimStyle> query)
    {
        if (string.IsNullOrWhiteSpace(SearchText))
            return query;

        if (Strings.TryFindEnumByDisplayContains(SearchText, out Stroke stroke))
            return query.Where(ss => ss.Stroke == stroke);

        if (int.TryParse((string?)SearchText, out var distance))
            return query.Where(ss =>
                EF.Functions.Like(ss.Distance.ToString(), $"%{SearchText}%"));

        return query;
    }

    protected override void ShowAddEditDialog(int? id = default)
    {
        var result = _windowFactory.CreateAndShow<SwimStyleAddEditWindow>(id);
        if (result == true) _ = LoadDataAsync();
    }
}