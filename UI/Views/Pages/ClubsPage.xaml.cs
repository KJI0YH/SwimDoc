using System.Windows.Controls;
using UI.Helpers.Navigation;
using UI.ViewModels.Pages;

namespace UI.Views.Pages;

public partial class ClubsPage : Page
{
    public ClubsPage(ClubsViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
        DataPageNavigation.WireLoaded(this);
    }
}
