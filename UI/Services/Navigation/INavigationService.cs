using UI.ViewModels;

namespace UI.Services.Navigation;

public interface INavigationService
{
    ViewModelBase? CurrentViewModel { get; }
    bool CanGoBack { get; }
    event Action<ViewModelBase?>? CurrentViewModelChanged;
    event Action<Type>? PageNavigationRequested;
    void NavigateTo<TViewModel>() where TViewModel : ViewModelBase;
    void NavigateTo<TViewModel>(object? parameter) where TViewModel : ViewModelBase;
    void NavigateTo<TViewModel>(NavigationContext context) where TViewModel : ViewModelBase;
    NavigationContext? GetNavigationContext<TViewModel>() where TViewModel : ViewModelBase;
    object? GetNavigationParameter<TViewModel>() where TViewModel : ViewModelBase;
    void GoBack();
    void ResetForNewCompetition();
}
