using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;
using UI.ViewModels;
using UI.ViewModels.Details;
using UI.ViewModels.Generic;
using UI.ViewModels.Table;
using UI.Views.Pages;

namespace UI.Services;

public class NavigationService(IServiceProvider serviceProvider) : INavigationService
{
    private readonly Dictionary<Type, Type> _viewModelToViewMapping = new();
    private readonly Dictionary<Type, Type> _viewModelToPageMapping = new()
    {
        [typeof(EventsViewModel)] = typeof(EventsPage),
        [typeof(HeatsViewModel)] = typeof(HeatsPage),
        [typeof(EntriesViewModel)] = typeof(EntriesPage),
        [typeof(AthletesViewModel)] = typeof(AthletesPage),
        [typeof(ClubsViewModel)] = typeof(ClubsPage),
        [typeof(AgeGroupsViewModel)] = typeof(AgeGroupsPage),
        [typeof(SwimStylesViewModel)] = typeof(SwimStylesPage),
        [typeof(AthleteDetailsViewModel)] = typeof(AthleteDetailsPage),
        [typeof(ClubDetailsViewModel)] = typeof(ClubDetailsPage),
        [typeof(EntryDetailsViewModel)] = typeof(EntryDetailsPage),
        [typeof(EventDetailsViewModel)] = typeof(EventDetailsPage),
        [typeof(AgeGroupDetailsViewModel)] = typeof(AgeGroupDetailsPage),
        [typeof(SwimStyleDetailsViewModel)] = typeof(SwimStyleDetailsPage)
    };

    private readonly Stack<ViewModelBase> _navigationHistory = new();
    private readonly Dictionary<Type, object?> _navigationParameters = new();
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
    public event Action<Type>? PageNavigationRequested;

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
        _navigationParameters[typeof(TViewModel)] = parameter;

        if (viewModel is INavigationAware navigationAware)
            navigationAware.OnNavigatedTo(parameter);

        if (ReferenceEquals(_currentViewModel, viewModel))
        {
            RequestPageForViewModel<TViewModel>();
            return;
        }

        if (_currentViewModel != null)
            _navigationHistory.Push(_currentViewModel);

        CurrentViewModel = viewModel;
        RequestPageForViewModel<TViewModel>();
    }

    public object? GetNavigationParameter<TViewModel>() where TViewModel : ViewModelBase
    {
        return _navigationParameters.TryGetValue(typeof(TViewModel), out var parameter) ? parameter : null;
    }

    public void GoBack()
    {
        if (!CanGoBack)
            return;

        var previousViewModel = _navigationHistory.Pop();
        CurrentViewModel = previousViewModel;
        if (previousViewModel != null &&
            _viewModelToPageMapping.TryGetValue(previousViewModel.GetType(), out var pageType))
            PageNavigationRequested?.Invoke(pageType);
    }

    public Type? GetViewType<TViewModel>() where TViewModel : ViewModelBase
    {
        var viewModelType = typeof(TViewModel);
        return _viewModelToViewMapping.TryGetValue(viewModelType, out var viewType) 
            ? viewType 
            : null;
    }

    private void RequestPageForViewModel<TViewModel>() where TViewModel : ViewModelBase
    {
        if (_viewModelToPageMapping.TryGetValue(typeof(TViewModel), out var pageType))
            PageNavigationRequested?.Invoke(pageType);
    }
}

