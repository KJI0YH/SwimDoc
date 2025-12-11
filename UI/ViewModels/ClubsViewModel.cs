using ServiceLayer.ClubService;

namespace UI.ViewModels;

public class ClubsViewModel : ViewModelBase
{
    private readonly IClubService _clubService;

    public ClubsViewModel(IClubService clubService)
    {
        _clubService = clubService;
    }

    public string Title => "Clubs Page";
}

