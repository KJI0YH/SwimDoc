using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DataLayer.EfClasses;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ServiceLayer.AthleteService;
using ServiceLayer.ClubService;
using UI.Services;
using UI.Views.Controls.SearchableComboBox;
using UI.Views.Windows.AddEdit;

namespace UI.ViewModels.Windows.AddEdit;

public partial class AthleteAddViewModel(int? id, IAthleteService athleteService, IClubService clubService)
    : AddEditViewModel<Athlete, int?>(id, athleteService), IAddEditContextAware
{
    [ObservableProperty] private ObservableCollection<SearchableItem> _clubs = new();
    private int? _contextClubId;

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

    public Array GenderValues => Enum.GetValues<Gender>().Where(g => g != Gender.Mixed).ToArray();
    public Array CategoryValues => Enum.GetValues<Category>();

    public void ApplyContext(AddEditContext context)
    {
        _contextClubId = context.ClubId;
    }

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
            SelectedClub = Enumerable.FirstOrDefault<SearchableItem>(Clubs, item => item.Value is Club club && club.Id == Entity.ClubId.Value);
        }
        else
        {
            YearOfBirth = DateTime.Now.Year;
            if (_contextClubId.HasValue)
                SelectedClub = Enumerable.FirstOrDefault<SearchableItem>(Clubs, item => item.Value is Club c && c.Id == _contextClubId.Value);
            else
                SelectedClub = Enumerable.FirstOrDefault<SearchableItem>(Clubs, item => item.Value == null);
        }
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
        if (window.DialogResult == true && window.DataContext is IWindowResult result &&
            result.Result is Club newClub)
        {
            LoadClubs();
            SelectedClub = Enumerable.FirstOrDefault<SearchableItem>(Clubs, item => item.Value is Club c && c.Id == newClub.Id);
        }
    }
}