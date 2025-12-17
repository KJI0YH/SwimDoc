using DataLayer.EfClasses;
using ServiceLayer.ClubService;

namespace UI.ViewModels;

public class ClubsViewModel : GenericTableViewModel<Club, int>
{
    public ClubsViewModel(IClubService clubService) : base(clubService)
    {
    }

    public string Title => "Клубы";
}

