using ServiceLayer.AthleteService;

namespace UI.ViewModels;

public class AthletesViewModel : ViewModelBase
{
    private readonly IAthleteService _athleteService;

    public AthletesViewModel(IAthleteService athleteService)
    {
        _athleteService = athleteService;
    }

    public string Title => "Athletes Page";
}

