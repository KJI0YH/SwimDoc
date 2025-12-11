using ServiceLayer.AgeGroupService;

namespace UI.ViewModels;

public class AgeGroupsViewModel : ViewModelBase
{
    private readonly IAgeGroupService _ageGroupService;

    public AgeGroupsViewModel(IAgeGroupService ageGroupService)
    {
        _ageGroupService = ageGroupService;
    }

    public string Title => "Age Groups Page";
}

