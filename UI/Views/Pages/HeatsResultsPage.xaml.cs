using System.Windows.Controls;
using UI.ViewModels.Pages;

namespace UI.Views.Pages;

public partial class HeatsResultsPage : Page
{
    public HeatsResultsPage(HeatsResultsViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}

