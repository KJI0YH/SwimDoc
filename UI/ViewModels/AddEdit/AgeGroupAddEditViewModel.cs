using CommunityToolkit.Mvvm.ComponentModel;
using DataLayer.EfClasses;
using ServiceLayer.AgeGroupService;
using ServiceLayer.Crud;
using UI.ViewModels.Generic;

namespace UI.ViewModels.AddEdit;

public class AgeGroupAddAddEditViewModel(int? id, IAgeGroupService crudService)
    : GenericAddEditViewModel<AgeGroup, int?>(id, crudService)
{
    public override string WindowTitle => IsAdd ? "Создание возрастной группы" : "Редактирование возрастной группы";

    public string? Name
    {
        get => Entity.Name;
        set
        {
            Entity.Name = value;
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

    public int? BirthYearMin
    {
        get => Entity.BirthYearMin;
        set
        {
            Entity.BirthYearMin = value;
            OnPropertyChanged();
        }
    }

    public int? BirthYearMax
    {
        get => Entity.BirthYearMax;
        set
        {
            Entity.BirthYearMax = value;
            OnPropertyChanged();
        }
    }

    public Array GenderValues => Enum.GetValues<Gender>();
}
