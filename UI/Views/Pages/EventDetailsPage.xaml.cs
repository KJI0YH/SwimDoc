using System.Windows.Controls;
using UI.Services;
using EventDetailsViewModel = UI.ViewModels.Pages.EventDetailsViewModel;

namespace UI.Views.Pages;

public partial class EventDetailsPage : Page
{
    public EventDetailsPage(EventDetailsViewModel viewModel, INavigationService navigationService)
    {
        InitializeComponent();
        viewModel.OnNavigatedTo(navigationService.GetNavigationParameter<EventDetailsViewModel>());
        DataContext = viewModel;
    }
}