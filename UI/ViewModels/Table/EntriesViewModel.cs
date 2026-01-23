using DataLayer.EfClasses;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ServiceLayer.EntryService;
using UI.Services;
using UI.ViewModels.Generic;
using UI.Views.AddEdit;

namespace UI.ViewModels.Table;

public class EntriesViewModel : GenericTableViewModel<Entry, int?>
{
    private readonly IAddEditWindowFactory _windowFactory;

    public EntriesViewModel(IEntryService entryService) : base(entryService)
    {
        _windowFactory = App.Current.Services.GetRequiredService<IAddEditWindowFactory>();
    }

    public string Title => "Заявки";

    protected override void InitializeColumns()
    {
        AutoGenerateColumns = false;
        ColumnConfigurations.Clear();
        
        ColumnConfigurations.Add(ColumnConfiguration.Create("SwimEvent.DisplayName", "Дистанция", 500));
        ColumnConfigurations.Add(ColumnConfiguration.Create("Athlete.DisplayName", "Участник", 300));
        ColumnConfigurations.Add(ColumnConfiguration.Create("Status", "Статус", 100));
        ColumnConfigurations.Add(ColumnConfiguration.Create("Scoring", "В зачёт", 60));
        ColumnConfigurations.Add(ColumnConfiguration.Create("DisplayEntryTime", "Заявочное время", 130));
        ColumnConfigurations.Add(ColumnConfiguration.Create("DisplayFinishTime", "Финишное время", 130));
        ColumnConfigurations.Add(ColumnConfiguration.Create("Points", "Очки", 100));
        ColumnConfigurations.Add(ColumnConfiguration.Create("Comment", "Примечание", 100));
    }

    protected override IQueryable<Entry> ApplyQuery(IQueryable<Entry> query)
    {
        return query.Include(entry => entry.Athlete)
            .Include(entry => entry.SwimEvent)
            .ThenInclude(se => se.SwimStyle)
            .Include(entry => entry.SwimEvent)
            .ThenInclude(se => se.AgeGroup)
            .Include(entry => entry.HeatPosition)
            .ThenInclude(heatPosition => heatPosition.Heat);
    }

    protected override void ShowAddEditDialog(int? id = default)
    {
        var result = _windowFactory.CreateAndShow<EntryAddEditWindow>(id);
        if (result == true)
        {
            _ = LoadDataAsync();
        }
    }
}

