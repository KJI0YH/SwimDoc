using System.ComponentModel;
using BizLogic.Helpers;
using DataLayer.EfClasses;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ServiceLayer.EventService;
using UI.Services;
using UI.ViewModels.Generic;
using UI.Views.AddEdit;

namespace UI.ViewModels.Table;

public class EventsViewModel : GenericTableViewModel<SwimEvent, int?>
{
    private readonly IAddEditWindowFactory _windowFactory;

    public EventsViewModel(IEventService eventService) : base(eventService)
    {
        _windowFactory = App.Current.Services.GetRequiredService<IAddEditWindowFactory>();
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
        ColumnConfigurations.Add(new ColumnConfiguration<SwimEvent>("Time", "Время", 100));
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
    }

    protected override IQueryable<SwimEvent> ApplyQuery(IQueryable<SwimEvent> query)
    {
        return query.Include(swimEvent => swimEvent.AgeGroup)
            .Include(swimEvent => swimEvent.SwimStyle);
    }

    protected override IQueryable<SwimEvent> ApplySorting(IQueryable<SwimEvent> query)
    {
        if (string.IsNullOrWhiteSpace(SearchText))
            return query;
        if (EnumHelper.TryGetEnumByDescriptionContains<EventRound>(SearchText, out var round))
        {
            return query.Where(e => e.Round == round);
        }

        if (EnumHelper.TryGetEnumByDescriptionContains<Stroke>(SearchText, out var stroke))
        {
            return query.Where(e => e.SwimStyle.Stroke == stroke);
        }

        if (EnumHelper.TryGetEnumByDescriptionContains<Gender>(SearchText, out var gender))
        {
            return query.Where(e => e.AgeGroup.Gender == gender);
        }

        return query.Where(e =>
            EF.Functions.Like(e.AgeGroup.Name, $"%{SearchText}%"));
    }

    protected override void ShowAddEditDialog(int? id = default)
    {
        var result = _windowFactory.CreateAndShow<EventAddEditWindow>(id);
        if (result == true)
        {
            _ = LoadDataAsync();
        }
    }
}