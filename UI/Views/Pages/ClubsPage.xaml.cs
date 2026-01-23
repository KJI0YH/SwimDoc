using System.Windows.Controls;
using UI.ViewModels.Table;

namespace UI.Views.Pages;

public partial class ClubsPage : Page
{
    public ClubsPage(ClubsViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}
