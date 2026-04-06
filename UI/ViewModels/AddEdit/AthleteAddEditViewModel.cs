using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DataLayer.EfClasses;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ServiceLayer.AthleteService;
using ServiceLayer.ClubService;
using UI.Services;
using UI.ViewModels.Generic;
using UI.Views.AddEdit;
using UI.Views.Controls;

namespace UI.ViewModels.AddEdit;

public partial class AthleteAddAddEditViewModel(int? id, IAthleteService athleteService, IClubService clubService)
    : GenericAddEditViewModel<Athlete, int?>(id, athleteService), IAddEditContextAware
{
    private int? _contextClubId;

    [ObservableProperty] private ObservableCollection<SearchableItem> _clubs = new();

    [ObservableProperty] private SearchableItem? _selectedClub;

    public override string WindowTitle => IsAdd ? "Создание спортсмена" : "Редактирование спортсмена";

    public string FirstName
    {
        get => Entity.FirstName;
        set
        {
            Entity.FirstName = value;
            OnPropertyChanged();
        }
    }

    public string LastName
    {
        get => Entity.LastName;
        set
        {
            Entity.LastName = value;
            OnPropertyChanged();
        }
    }

    public Gender Gender
    {
        get => Entity.Gender;
        set
        {
            Entity.Gender = value;
            OnPropertyChanged();
        }
    }

    public int YearOfBirth
    {
        get => Entity.YearOfBirth;
        set
        {
            Entity.YearOfBirth = value;
            OnPropertyChanged();
        }
    }

    public Category Category
    {
        get => Entity.Category;
        set
        {
            Entity.Category = value;
            OnPropertyChanged();
        }
    }

    public Array GenderValues => Enum.GetValues<Gender>().Cast<Gender>().Where(g => g != Gender.Mixed).ToArray();
    public Array CategoryValues => Enum.GetValues<Category>();

    protected override async Task<Athlete?> LoadEntityAsync(int? id)
    {
        return await CrudService.Query()
            .Include(a => a.Club)
            .FirstOrDefaultAsync(a => a.Id == id);
    }

    public override async Task InitializeAsync()
    {
        await base.InitializeAsync();
        LoadClubs();

        if (IsEdit)
        {
            SelectedClub = Clubs.FirstOrDefault(item => item.Value is Club club && club.Id == Entity.ClubId.Value);
        }
        else
        {
            YearOfBirth = DateTime.Now.Year;
            if (_contextClubId.HasValue)
            {
                SelectedClub = Clubs.FirstOrDefault(item => item.Value is Club c && c.Id == _contextClubId.Value);
            }
            else
            {
                SelectedClub = Clubs.FirstOrDefault(item => item.Value == null);
            }
        }
    }

    public void ApplyContext(AddEditContext context)
    {
        _contextClubId = context.ClubId;
    }

    partial void OnSelectedClubChanged(SearchableItem? item)
    {
        if (item?.Value is null)
        {
            Entity.ClubId = null;
            return;
        }

        if (item?.Value is not Club club) return;
        Entity.ClubId = club.Id;
    }

    private void LoadClubs()
    {
        var clubsQuery = clubService.Query();
        if (_contextClubId.HasValue)
            clubsQuery = clubsQuery.Where(c => c.Id == _contextClubId.Value);

        var clubs = clubsQuery.ToList();
        Clubs.Clear();

        if (!_contextClubId.HasValue)
            Clubs.Add(new SearchableItem { Value = null, DisplayText = "(Лично)" });

        foreach (var club in clubs)
            Clubs.Add(new SearchableItem
            {
                Value = club,
                DisplayText = $"{club.Name} | {club.ShortName ?? string.Empty}"
            });
    }

    [RelayCommand]
    private void CreateNewClub()
    {
        var factory = App.Current.Services.GetRequiredService<IAddEditWindowFactory>();
        var window = factory.CreateAndShowAndReturn<ClubAddEditWindow>();
        if (window.DialogResult == true && window.DataContext is IAddEditWindowResult result &&
            result.SavedEntity is Club newClub)
        {
            LoadClubs();
            SelectedClub = Clubs.FirstOrDefault(item => item.Value is Club c && c.Id == newClub.Id);
        }
    }
}