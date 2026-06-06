using System.Windows.Controls;
using UI.Services;
using AgeGroupDetailsViewModel = UI.ViewModels.Pages.AgeGroupDetailsViewModel;

namespace UI.Views.Pages;

public partial class AgeGroupDetailsPage : Page
{
    public AgeGroupDetailsPage(AgeGroupDetailsViewModel viewModel, INavigationService navigationService)
    {
        InitializeComponent();
        viewModel.OnNavigatedTo(navigationService.GetNavigationParameter<AgeGroupDetailsViewModel>());
        DataContext = viewModel;
    }
}
