using ServiceLayer.HeatService;

namespace UI.ViewModels.Table;

public class HeatsViewModel : ViewModelBase
{
    private readonly IHeatService _heatService;

    public HeatsViewModel(IHeatService heatService)
    {
        _heatService = heatService;
    }
}

