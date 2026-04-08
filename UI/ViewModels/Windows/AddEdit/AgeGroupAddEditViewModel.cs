using DataLayer.EfClasses;
using ServiceLayer.AgeGroupService;

namespace UI.ViewModels.Windows.AddEdit;

public class AgeGroupAddViewModel(int? id, IAgeGroupService crudService)
    : AddEditViewModel<AgeGroup, int?>(id, crudService)
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