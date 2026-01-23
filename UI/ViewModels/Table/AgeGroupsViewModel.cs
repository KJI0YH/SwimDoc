using DataLayer.EfClasses;
using Microsoft.Extensions.DependencyInjection;
using ServiceLayer.AgeGroupService;
using UI.Services;
using UI.ViewModels.Generic;
using UI.Views.AddEdit;

namespace UI.ViewModels.Table;

public class AgeGroupsViewModel : GenericTableViewModel<AgeGroup, int?>
{
    private readonly IAddEditWindowFactory _windowFactory;

    public AgeGroupsViewModel(IAgeGroupService ageGroupService) : base(ageGroupService)
    {
        _windowFactory = App.Current.Services.GetRequiredService<IAddEditWindowFactory>();
    }

    public string Title => "Возрастные группы";

    protected override void InitializeColumns()
    {
        AutoGenerateColumns = false;
        ColumnConfigurations.Clear();

        ColumnConfigurations.Add(ColumnConfiguration.Create("DisplayName", "Название", 400));
        ColumnConfigurations.Add(ColumnConfiguration.Create("Gender", "Пол", 100));
        ColumnConfigurations.Add(ColumnConfiguration.Create("DisplayBirthYearMax", "Год рождения от", 150));
        ColumnConfigurations.Add(ColumnConfiguration.Create("DisplayBirthYearMin", "Год рождения до", 150));
    }

    protected override void ShowAddEditDialog(int? id = default)
    {
        var result = _windowFactory.CreateAndShow<AgeGroupAddEditWindow>(id);
        if (result == true)
        {
            _ = LoadDataAsync();
        }
    }
}