using System.Windows.Controls;
using UI.ViewModels.Table;

namespace UI.Views.Pages;

public partial class EventsPage : Page
{
    public EventsPage(EventsViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}
