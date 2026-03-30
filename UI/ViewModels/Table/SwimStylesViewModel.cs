using DataLayer.EfClasses;
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

        ColumnConfigurations.Add(ColumnConfiguration.Create("DisplayName", "Название", 300));
        ColumnConfigurations.Add(ColumnConfiguration.Create("Distance", "Дистанция", 150));
        ColumnConfigurations.Add(ColumnConfiguration.Create("Stroke", "Стиль", 200));
        ColumnConfigurations.Add(ColumnConfiguration.Create("RelayCount", "Участников эстафеты", 150));
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