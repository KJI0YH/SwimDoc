using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using DataLayer.EfClasses;
using ServiceLayer.ClubService;
using UI.Views.Controls.SearchableComboBox;

namespace UI.ViewModels.Windows.AddEdit;

public partial class ClubAddViewModel(int? id, IClubService crudService)
    : AddEditViewModel<Club, int?>(id, crudService)
{
    [ObservableProperty] private ObservableCollection<SearchableItem> _availableAthletes = new();

    [ObservableProperty] private ObservableCollection<SearchableItem> _selectedAthletes = new();

    [ObservableProperty] private SearchableItem? _selectedAvailableAthlete;

    public override string WindowTitle => IsAdd ? "Создание клуба" : "Редактирование клуба";

    public string Name
    {
        get => Entity.Name;
        set
        {
            Entity.Name = value;
            OnPropertyChanged();
        }
    }
}