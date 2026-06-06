using System.Windows.Controls;
using EventsViewModel = UI.ViewModels.Pages.EventsViewModel;

namespace UI.Views.Pages;

public partial class EventsPage : Page
{
    public EventsPage(EventsViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}
