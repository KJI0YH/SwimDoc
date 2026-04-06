using System.Windows.Controls;
using UI.Services;
using UI.ViewModels.Details;

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

