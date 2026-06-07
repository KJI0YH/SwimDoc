using UI.ViewModels;

namespace UI.Services.Navigation;

public interface INavigationService
{
    ViewModelBase? CurrentViewModel { get; }
    bool CanGoBack { get; }
    IReadOnlyList<NavigationFrame> BackStackFrames { get; }
    event Action<ViewModelBase?>? CurrentViewModelChanged;
    event Action<NavigationState>? NavigationStateChanged;
    void NavigateTo<TViewModel>() where TViewModel : ViewModelBase;
    void NavigateTo<TViewModel>(object? parameter) where TViewModel : ViewModelBase;
    void NavigateTo<TViewModel>(NavigationContext context) where TViewModel : ViewModelBase;
    void NavigateToRoot<TViewModel>() where TViewModel : ViewModelBase;
    void NavigateToCompetitionSelection();
    void NavigateSidebar(string tag);
    NavigationContext? GetNavigationContext<TViewModel>() where TViewModel : ViewModelBase;
    object? GetNavigationParameter<TViewModel>() where TViewModel : ViewModelBase;
    void GoBack();
    void ResetForNewCompetition();
}
