using System.Windows.Controls;
using UI.Services;
using UI.ViewModels.Pages;

namespace UI.Views.Pages;

public partial class EntryDetailsPage : Page
{
    public EntryDetailsPage(EntryDetailsViewModel viewModel, INavigationService navigationService)
    {
        InitializeComponent();
        viewModel.OnNavigatedTo(navigationService.GetNavigationParameter<EntryDetailsViewModel>());
        DataContext = viewModel;
    }
}