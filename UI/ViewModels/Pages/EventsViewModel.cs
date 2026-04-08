using System.ComponentModel;
using BizLogic.HeatLogic;
using BizLogic.Helpers;
using CommunityToolkit.Mvvm.Input;
using DataLayer.EfClasses;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ServiceLayer.EventService;
using ServiceLayer.HeatService;
using UI.Services;
using UI.ViewModels.Pages.Data;
using UI.ViewModels.Windows.HeatAllocationParameters;
using UI.Views.Windows.AddEdit;
using HeatAllocationParametersWindow = UI.Views.Windows.HeatAllocationParameters.HeatAllocationParametersWindow;

namespace UI.ViewModels.Pages;

public partial class EventsViewModel : DataViewModel<SwimEvent, int?>
{
    private readonly IHeatService _heatService;
    private readonly IAddEditWindowFactory _windowFactory;

    public EventsViewModel(IEventService eventService)
        : this(eventService, App.Current.Services.GetRequiredService<IHeatService>())
    {
    }

    public EventsViewModel(IEventService eventService, IHeatService heatService) : base(eventService)
    {
        _windowFactory = App.Current.Services.GetRequiredService<IAddEditWindowFactory>();
        _heatService = heatService;
        PropertyChanged += OnViewModelPropertyChanged;
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(SelectedItems))
            HeatAllocationCommand.NotifyCanExecuteChanged();
    }

    protected override void InitializeColumns()
    {
        AutoGenerateColumns = false;
        ColumnConfigurations.Clear();

        ColumnConfigurations.Add(new ColumnConfiguration<SwimEvent>("Order", "Порядок", 80));
        ColumnConfigurations.Add(new ColumnConfiguration<SwimEvent>("DisplayDate", "Дата", 100,
            (query, direction) =>
            {
                return direction == ListSortDirection.Ascending
                    ? query.OrderBy(e => e.Date)
                    : query.OrderByDescending(e => e.Date);
            }));
        ColumnConfigurations.Add(new ColumnConfiguration<SwimEvent>("DisplayTime", "Время", 120));
        ColumnConfigurations.Add(new ColumnConfiguration<SwimEvent>("Round", "Раунд", 150));
        ColumnConfigurations.Add(new ColumnConfiguration<SwimEvent>("AgeGroup.DisplayName", "Возрастная группа", 300,
            (query, direction) =>
            {
                return direction == ListSortDirection.Ascending
                    ? query.OrderBy(e => e.AgeGroup.Name)
                    : query.OrderByDescending(e => e.AgeGroup.Name);
            }));
        ColumnConfigurations.Add(new ColumnConfiguration<SwimEvent>("SwimStyle.DisplayName", "Дистанция", 300,
            (query, direction) =>
            {
                return direction == ListSortDirection.Ascending
                    ? query.OrderBy(e => e.SwimStyle.Distance).ThenBy(e => e.SwimStyle.Stroke)
                        .ThenBy(e => e.SwimStyle.RelayCount)
                    : query.OrderByDescending(e => e.SwimStyle.Distance).ThenByDescending(e => e.SwimStyle.Stroke)
                        .ThenBy(e => e.SwimStyle.RelayCount);
            }));
        ColumnConfigurations.Add(new ColumnConfiguration<SwimEvent>("DisplayLanes", "Дорожки", 100,
            (query, direction) =>
            {
                return direction == ListSortDirection.Ascending
                    ? query.OrderBy(e => e.LaneMin).ThenBy(e => e.LaneMax)
                    : query.OrderByDescending(e => e.LaneMin).ThenByDescending(e => e.LaneMax);
            }));
        ColumnConfigurations.Add(new ColumnConfiguration<SwimEvent>("DisplayStatus", "Статус", 120));
    }

    protected override IQueryable<SwimEvent> ApplyQuery(IQueryable<SwimEvent> query)
    {
        return query
            .OrderBy(se => se.Order)
            .Include(swimEvent => swimEvent.AgeGroup)
            .Include(swimEvent => swimEvent.SwimStyle)
            .Include(swimEvent => swimEvent.Heats);
    }

    protected override IQueryable<SwimEvent> ApplySorting(IQueryable<SwimEvent> query)
    {
        if (string.IsNullOrWhiteSpace(SearchText))
            return query;
        if (EnumHelper.TryGetEnumByDescriptionContains<EventRound>(SearchText, out var round))
            return query.Where(e => e.Round == round);

        if (EnumHelper.TryGetEnumByDescriptionContains<Stroke>(SearchText, out var stroke))
            return query.Where(e => e.SwimStyle.Stroke == stroke);

        if (EnumHelper.TryGetEnumByDescriptionContains<Gender>(SearchText, out var gender))
            return query.Where(e => e.AgeGroup.Gender == gender);

        return Queryable.Where(query, e =>
            EF.Functions.Like(e.AgeGroup.Name, $"%{SearchText}%"));
    }

    protected override void ShowAddEditDialog(int? id = default)
    {
        var result = _windowFactory.CreateAndShow<EventAddEditWindow>(id);
        if (result == true) _ = LoadDataAsync();
    }

    [RelayCommand(CanExecute = nameof(CanAllocateHeats))]
    private void HeatAllocation()
    {
        if (SelectedItems.Count == 0)
            return;

        var window = _windowFactory.CreateAndShowAndReturn<HeatAllocationParametersWindow>();
        if (window.DataContext is not IWindowResult { Result: HeatAllocationParametersResult result })
            return;

        foreach (var swimEvent in SelectedItems)
        {
            var parameters = new HeatAllocationParameters(swimEvent.Id, result.HeatOrder, result.MinHeatSize);
            _heatService.AllocateEntriesToHeats(parameters);
        }

        _ = LoadDataAsync();
    }

    private bool CanAllocateHeats()
    {
        return SelectedItems.Count > 0;
    }
}