using System.Windows.Controls;
using UI.Services;
using UI.ViewModels.Details;

namespace UI.Views.Pages;

public partial class SwimStyleDetailsPage : Page
{
    public SwimStyleDetailsPage(SwimStyleDetailsViewModel viewModel, INavigationService navigationService)
    {
        InitializeComponent();
        viewModel.OnNavigatedTo(navigationService.GetNavigationParameter<SwimStyleDetailsViewModel>());
        DataContext = viewModel;
    }
}

