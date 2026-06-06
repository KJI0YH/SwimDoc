using DataLayer.EfClasses;
using ServiceLayer.AgeGroupService;
using UI.Helpers;
using UI.Resources;

namespace UI.ViewModels.Dialogs.AddEdit;

public class AgeGroupAddViewModel(int? id, IAgeGroupService crudService)
    : AddEditViewModel<AgeGroup, int?>(id, crudService)
{
    public override string WindowTitle => IsAdd ? Strings.WindowTitle_CreateAgeGroup : Strings.WindowTitle_EditAgeGroup;

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

    public IEnumerable<EnumOption<Gender>> GenderOptions =>
        Enum.GetValues<Gender>().Select(g => new EnumOption<Gender>(g));
}
