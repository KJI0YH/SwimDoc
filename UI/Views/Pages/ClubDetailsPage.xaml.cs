using System.Windows.Controls;
using ClubDetailsViewModel = UI.ViewModels.Pages.ClubDetailsViewModel;

namespace UI.Views.Pages;

public partial class ClubDetailsPage : Page
{
    public ClubDetailsPage(ClubDetailsViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}
