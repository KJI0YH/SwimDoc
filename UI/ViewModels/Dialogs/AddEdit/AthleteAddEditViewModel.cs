using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DataLayer.EfClasses;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ServiceLayer.AthleteService;
using ServiceLayer.ClubService;
using UI.Resources;
using UI.Models;
using UI.Views.Dialogs.Markers.AddEdit;

namespace UI.ViewModels.Dialogs.AddEdit;

public partial class AthleteAddViewModel(int? id, IAthleteService athleteService, IClubService clubService)
    : AddEditViewModel<Athlete, int?>(id, athleteService), INavigationContextAware
{
    [ObservableProperty] private ObservableCollection<SearchableItem> _clubs = new();
    private int? _contextClubId;
    [ObservableProperty] private SearchableItem? _selectedClub;
    public override string WindowTitle => IsAdd ? Strings.WindowTitle_CreateAthlete : Strings.WindowTitle_EditAthlete;
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

    public IEnumerable<EnumOption<Gender>> GenderOptions =>
        Enum.GetValues<Gender>().Where(g => g != Gender.Mixed).Select(g => new EnumOption<Gender>(g));
    public IEnumerable<EnumOption<Category>> CategoryOptions =>
        Enum.GetValues<Category>().Select(c => new EnumOption<Category>(c));

    public void ApplyContext(NavigationContext context)
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
            Clubs.Add(new SearchableItem { Value = null, DisplayText = Strings.Common_PersonalParen });
        foreach (var club in clubs)
            Clubs.Add(new SearchableItem
            {
                Value = club,
                DisplayText = club.Name
            });
    }

    [RelayCommand]
    private void CreateNewClub()
    {
        var factory = App.Current.Services.GetRequiredService<IAddEditWindowFactory>();
        var dialog = factory.CreateAndShowAndReturn<ClubAddEditWindow>();
        if (dialog.DialogResult == true && dialog.DataContext is IWindowResult result &&
            result.Result is Club newClub)
        {
            LoadClubs();
            SelectedClub = Enumerable.FirstOrDefault<SearchableItem>(Clubs, item => item.Value is Club c && c.Id == newClub.Id);
        }
    }
}
