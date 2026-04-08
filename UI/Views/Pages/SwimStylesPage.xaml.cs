using System.Windows.Controls;
using UI.ViewModels.Pages;

namespace UI.Views.Pages;

public partial class SwimStylesPage : Page
{
    public SwimStylesPage(SwimStylesViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}