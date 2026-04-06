using System.Windows.Controls;
using UI.Services;
using UI.ViewModels.Details;

namespace UI.Views.Pages;

public partial class AthleteDetailsPage : Page
{
    public AthleteDetailsPage(AthleteDetailsViewModel viewModel, INavigationService navigationService)
    {
        InitializeComponent();
        viewModel.OnNavigatedTo(navigationService.GetNavigationParameter<AthleteDetailsViewModel>());
        DataContext = viewModel;
    }
}

