using DataLayer.EfClasses;
using ServiceLayer.AgeGroupService;

namespace UI.ViewModels;

public class AgeGroupsViewModel : GenericTableViewModel<AgeGroup, int>
{
    public AgeGroupsViewModel(IAgeGroupService ageGroupService) : base(ageGroupService)
    {
    }

    public string Title => "Возрастные группы";
}

