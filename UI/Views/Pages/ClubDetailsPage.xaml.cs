using System.Windows.Controls;
using UI.Services;
using ClubDetailsViewModel = UI.ViewModels.Pages.ClubDetailsViewModel;

namespace UI.Views.Pages;

public partial class ClubDetailsPage : Page
{
    public ClubDetailsPage(ClubDetailsViewModel viewModel, INavigationService navigationService)
    {
        InitializeComponent();
        viewModel.OnNavigatedTo(navigationService.GetNavigationParameter<ClubDetailsViewModel>());
        DataContext = viewModel;
    }
}