using System.Windows.Controls;
using Microsoft.Extensions.DependencyInjection;
using UI.ViewModels;
using UI.ViewModels.Pages;
using UI.Views.Pages;
using AgeGroupDetailsViewModel = UI.ViewModels.Pages.AgeGroupDetailsViewModel;
using AthleteDetailsViewModel = UI.ViewModels.Pages.AthleteDetailsViewModel;
using ClubDetailsViewModel = UI.ViewModels.Pages.ClubDetailsViewModel;
using EntriesViewModel = UI.ViewModels.Pages.EntriesViewModel;
using EventDetailsViewModel = UI.ViewModels.Pages.EventDetailsViewModel;
using EventsViewModel = UI.ViewModels.Pages.EventsViewModel;
using HeatsViewModel = UI.ViewModels.Pages.HeatsViewModel;
using ResultsViewModel = UI.ViewModels.Pages.ResultsViewModel;
using SwimStyleDetailsViewModel = UI.ViewModels.Pages.SwimStyleDetailsViewModel;

namespace UI.Services;

public class NavigationService(IServiceProvider serviceProvider) : INavigationService
{
    private readonly Stack<ViewModelBase> _navigationHistory = new();
    private readonly Dictionary<Type, object?> _navigationParameters = new();

    private readonly Dictionary<Type, Type> _viewModelToPageMapping = new()
    {
        [typeof(EventsViewModel)] = typeof(EventsPage),
        [typeof(HeatsViewModel)] = typeof(HeatsPage),
        [typeof(FixationViewModel)] = typeof(FixationPage),
        [typeof(ResultsViewModel)] = typeof(ResultsPage),
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

    private readonly Dictionary<Type, Type> _viewModelToViewMapping = new();
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

        if (_currentViewModel is INavigationAware leaving)
            leaving.OnNavigatedFrom();

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

        if (_currentViewModel is INavigationAware leaving)
            leaving.OnNavigatedFrom();

        var previousViewModel = _navigationHistory.Pop();
        CurrentViewModel = previousViewModel;
        if (previousViewModel != null &&
            _viewModelToPageMapping.TryGetValue(previousViewModel.GetType(), out var pageType))
            PageNavigationRequested?.Invoke(pageType);
    }

    public void RegisterMapping<TViewModel, TView>()
        where TViewModel : ViewModelBase
        where TView : UserControl
    {
        _viewModelToViewMapping[typeof(TViewModel)] = typeof(TView);
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