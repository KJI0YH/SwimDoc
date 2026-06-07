using System.ComponentModel;
using DataLayer.EfClasses;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ServiceLayer.SwimStyleService;
using UI.Resources;
using UI.ViewModels.Pages.Data;
using UI.Models.Rows;
using UI.Models.Rows.Projections;
using UI.Views.Dialogs.Markers.AddEdit;

namespace UI.ViewModels.Pages;

public class SwimStylesViewModel : DataViewModel<SwimStyle, SwimStyleRowView, int?>
{
    protected override PagingPage PagingSettingsPage => PagingPage.SwimStyles;
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
        ColumnConfigurations.Add(new ColumnConfiguration<SwimStyle>("DisplayName", Strings.SwimStyles_Col_Name, 300,
            ColumnConfiguration<SwimStyle>.SortBy(e => e.Distance, e => e.Stroke, e => e.RelayCount)));
        ColumnConfigurations.Add(new ColumnConfiguration<SwimStyle>("Distance", Strings.SwimStyles_Col_Distance, 150,
            ColumnConfiguration<SwimStyle>.SortBy(e => e.Distance)));
        ColumnConfigurations.Add(new ColumnConfiguration<SwimStyle>("Stroke", Strings.SwimStyles_Col_Stroke, 200,
            ColumnConfiguration<SwimStyle>.SortBy(e => e.Stroke)));
    }

    protected override async Task<List<SwimStyleRowView>> LoadPageRowsAsync(IQueryable<SwimStyle> query)
    {
        var projections = await RowProjectionQueries.SelectSwimStyle(query).ToListAsync();
        return projections.Select(SwimStyleRowView.FromProjection).ToList();
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
