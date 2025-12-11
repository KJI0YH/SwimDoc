using ServiceLayer.SwimStyleService;

namespace UI.ViewModels;

public class SwimStylesViewModel : ViewModelBase
{
    private readonly ISwimStyleService _swimStyleService;

    public SwimStylesViewModel(ISwimStyleService swimStyleService)
    {
        _swimStyleService = swimStyleService;
    }

    public string Title => "Swim Styles Page";
}

