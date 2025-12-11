using ServiceLayer.HeatService;

namespace UI.ViewModels;

public class HeatsViewModel : ViewModelBase
{
    private readonly IHeatService _heatService;

    public HeatsViewModel(IHeatService heatService)
    {
        _heatService = heatService;
    }

    public string Title => "Heats Page";
}

