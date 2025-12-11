using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;
using UI.ViewModels;

namespace UI.Services;

public class NavigationService(IServiceProvider serviceProvider) : INavigationService
{
    private readonly Dictionary<Type, Type> _viewModelToViewMapping = new();
    private readonly Stack<ViewModelBase> _navigationHistory = new();
    private ViewModelBase? _currentViewModel;

    public ViewModelBase? CurrentViewModel
    {
        get => _currentViewModel;
        private set
        {
            if (_currentViewModel == value) return;
            _currentViewModel = value;
            CurrentViewModelChanged?.Invoke(_currentViewModel);
        }
    }

    public event Action<ViewModelBase?>? CurrentViewModelChanged;

    public bool CanGoBack => _navigationHistory.Count > 0;

    public void RegisterMapping<TViewModel, TView>() 
        where TViewModel : ViewModelBase
        where TView : System.Windows.Controls.UserControl
    {
        _viewModelToViewMapping[typeof(TViewModel)] = typeof(TView);
    }

    public void NavigateTo<TViewModel>() where TViewModel : ViewModelBase
    {
        NavigateTo<TViewModel>(null);
    }

    public void NavigateTo<TViewModel>(object? parameter) where TViewModel : ViewModelBase
    {
        var viewModel = serviceProvider.GetRequiredService<TViewModel>();

        if (_currentViewModel != null)
        {
            _navigationHistory.Push(_currentViewModel);
        }

        CurrentViewModel = viewModel;
    }

    public void GoBack()
    {
        if (!CanGoBack)
            return;

        var previousViewModel = _navigationHistory.Pop();
        CurrentViewModel = previousViewModel;
    }

    public Type? GetViewType<TViewModel>() where TViewModel : ViewModelBase
    {
        var viewModelType = typeof(TViewModel);
        return _viewModelToViewMapping.TryGetValue(viewModelType, out var viewType) 
            ? viewType 
            : null;
    }
}

