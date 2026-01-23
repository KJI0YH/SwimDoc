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

    public string Title => "События";

    protected override void InitializeColumns()
    {
        AutoGenerateColumns = false;
        ColumnConfigurations.Clear();

        ColumnConfigurations.Add(ColumnConfiguration.Create("Order", "Порядок", 80));
        ColumnConfigurations.Add(ColumnConfiguration.Create("DisplayDate", "Дата", 100));
        ColumnConfigurations.Add(ColumnConfiguration.Create("Time", "Время", 100));
        ColumnConfigurations.Add(ColumnConfiguration.Create("Round", "Раунд", 150));
        ColumnConfigurations.Add(ColumnConfiguration.Create("AgeGroup.DisplayName", "Возрастная группа", 300));
        ColumnConfigurations.Add(ColumnConfiguration.Create("SwimStyle.DisplayName", "Дистанция", 300));
        ColumnConfigurations.Add(ColumnConfiguration.Create("DisplayLanes", "Дорожки", 100));
    }

    protected override IQueryable<SwimEvent> ApplyQuery(IQueryable<SwimEvent> query)
    {
        return query.Include(swimEvent => swimEvent.AgeGroup)
            .Include(swimEvent => swimEvent.SwimStyle);
    }

    protected override void ShowAddEditDialog(int? id = default)    {
        var result = _windowFactory.CreateAndShow<EventAddEditWindow>(id);
        if (result == true)
        {
            _ = LoadDataAsync();
        }
    }
}

