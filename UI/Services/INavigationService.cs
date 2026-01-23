using UI.ViewModels;

namespace UI.Services;

public interface INavigationService
{
    ViewModelBase? CurrentViewModel { get; }
    event Action<ViewModelBase?>? CurrentViewModelChanged;

    event Action<Type>? PageNavigationRequested;

    void NavigateTo<TViewModel>() where TViewModel : ViewModelBase;
    void NavigateTo<TViewModel>(object? parameter) where TViewModel : ViewModelBase;
    bool CanGoBack { get; }
    void GoBack();
}

