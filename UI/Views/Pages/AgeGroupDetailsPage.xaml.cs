using System.Windows.Controls;
using UI.Services;
using UI.ViewModels.Details;

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

