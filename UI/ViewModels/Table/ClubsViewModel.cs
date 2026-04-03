using DataLayer.EfClasses;
using Microsoft.EntityFrameworkCore;
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

    protected override void InitializeColumns()
    {
        AutoGenerateColumns = false;
        ColumnConfigurations.Clear();

        ColumnConfigurations.Add(new ColumnConfiguration<Club>("Name", "Полное название", 300));
        ColumnConfigurations.Add(new ColumnConfiguration<Club>("ShortName", "Короткое название", 150));
        ColumnConfigurations.Add(new ColumnConfiguration<Club>("Type", "Тип", 100));
    }

    protected override IQueryable<Club> ApplySearch(IQueryable<Club> query)
    {
        if (string.IsNullOrWhiteSpace(SearchText)) 
            return query;
        
        return query.Where(club => 
            EF.Functions.Like(club.Name, $"%{SearchText}%") ||
            EF.Functions.Like(club.ShortName, $"%{SearchText}%"));
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

