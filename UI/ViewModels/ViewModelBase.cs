using CommunityToolkit.Mvvm.ComponentModel;
using UI.Helpers.Threading;

namespace UI.ViewModels;

public class ViewModelBase : ObservableObject
{
    public virtual double ContentWidth => 520;

    protected static Task YieldLoadingUiAsync() => DispatcherUiHelper.YieldForRenderAsync();

    protected static Task YieldToBackgroundAsync() => DispatcherUiHelper.YieldToBackgroundAsync();
}
