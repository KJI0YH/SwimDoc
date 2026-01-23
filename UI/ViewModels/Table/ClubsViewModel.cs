using DataLayer.EfClasses;
using Microsoft.Extensions.DependencyInjection;
using ServiceLayer.AthleteService;
using ServiceLayer.ClubService;
using UI.Services;
using UI.ViewModels.Generic;
using UI.Views.AddEdit;

namespace UI.ViewModels.Table;

public class ClubsViewModel : GenericTableViewModel<Club, int?>
{
    private readonly IAthleteService _athleteService;
    private readonly IAddEditWindowFactory _windowFactory;

    public ClubsViewModel(IClubService clubService, IAthleteService athleteService) : base(clubService)
    {
        _athleteService = athleteService;
        _windowFactory = App.Current.Services.GetRequiredService<IAddEditWindowFactory>();
    }

    public string Title => "Клубы";
    
    protected override void InitializeColumns()
    {
        AutoGenerateColumns = false;
        ColumnConfigurations.Clear();

        ColumnConfigurations.Add(ColumnConfiguration.Create("Name", "Полное название", 300));
        ColumnConfigurations.Add(ColumnConfiguration.Create("ShortName", "Короткое название", 150));
        ColumnConfigurations.Add(ColumnConfiguration.Create("Type", "Тип", 100));
    }

    protected override void ShowAddEditDialog(int? id = default)
    {
        var result = _windowFactory.CreateAndShow<ClubAddEditWindow>(id);
        if (result == true)
        {
            _ = LoadDataAsync();
        }
    }
}

