using System.Windows.Controls;
using UI.ViewModels.Pages;

namespace UI.Views.Pages;

public partial class AgeGroupsPage : Page
{
    public AgeGroupsPage(AgeGroupsViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}
