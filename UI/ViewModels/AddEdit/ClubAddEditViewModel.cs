using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using DataLayer.EfClasses;
using ServiceLayer.ClubService;
using ServiceLayer.Crud;
using UI.ViewModels.Generic;
using UI.Views.Controls;

namespace UI.ViewModels.AddEdit;

public partial class ClubAddAddEditViewModel(int? id, IClubService crudService)
    : GenericAddEditViewModel<Club, int?>(id, crudService)
{
    [ObservableProperty]
    private ObservableCollection<SearchableItem> _availableAthletes = new();

    [ObservableProperty]
    private ObservableCollection<SearchableItem> _selectedAthletes = new();

    [ObservableProperty]
    private SearchableItem? _selectedAvailableAthlete;

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

    public string? ShortName
    {
        get => Entity.ShortName;
        set
        {
            Entity.ShortName = value;
            OnPropertyChanged();
        }
    }

    public ClubType Type
    {
        get => Entity.Type;
        set
        {
            Entity.Type = value;
            OnPropertyChanged();
        }
    }

    public Array ClubTypeValues => Enum.GetValues<ClubType>();
}
