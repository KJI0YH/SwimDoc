using System.Windows.Controls;
using AthleteDetailsViewModel = UI.ViewModels.Pages.AthleteDetailsViewModel;

namespace UI.Views.Pages;

public partial class AthleteDetailsPage : Page
{
    public AthleteDetailsPage(AthleteDetailsViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}
