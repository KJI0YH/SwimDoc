using System.Windows.Controls;
using EventDetailsViewModel = UI.ViewModels.Pages.EventDetailsViewModel;

namespace UI.Views.Pages;

public partial class EventDetailsPage : Page
{
    public EventDetailsPage(EventDetailsViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}
