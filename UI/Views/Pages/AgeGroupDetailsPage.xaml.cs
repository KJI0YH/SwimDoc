using System.Windows.Controls;
using AgeGroupDetailsViewModel = UI.ViewModels.Pages.AgeGroupDetailsViewModel;

namespace UI.Views.Pages;

public partial class AgeGroupDetailsPage : Page
{
    public AgeGroupDetailsPage(AgeGroupDetailsViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}
