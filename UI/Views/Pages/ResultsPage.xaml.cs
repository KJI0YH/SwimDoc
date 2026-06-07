using System.Windows.Controls;
using Microsoft.Extensions.DependencyInjection;
using UI.Helpers.Navigation;
using UI.ViewModels.Pages;

namespace UI.Views.Pages;

public partial class ResultsPage : Page
{
    private readonly INavigationService _navigationService;
    public ResultsPage(INavigationService navigationService)
    {
        _navigationService = navigationService;
        InitializeComponent();
        _navigationService.PageNavigationRequested += OnPageNavigationRequested;
        Unloaded += (_, _) => _navigationService.PageNavigationRequested -= OnPageNavigationRequested;
        DataPageNavigation.WireLoaded(this);
        ApplyViewModel();
    }

    private void OnPageNavigationRequested(Type pageType)
    {
        if (pageType == typeof(ResultsPage))
            ApplyViewModel();
    }

    private void ApplyViewModel()
    {
        var (viewModel, parameter) = ResolveViewModel();
        viewModel.OnNavigatedTo(parameter);
        DataContext = viewModel;
    }

    private (ResultsViewModel ViewModel, object? Parameter) ResolveViewModel()
    {
        if (_navigationService.CurrentViewModel is ResultsViewModel resultsVm)
            return (resultsVm, _navigationService.GetNavigationParameter<ResultsViewModel>());
        return (App.Current.Services.GetRequiredService<ResultsViewModel>(), null);
    }
}
