using System.Windows.Controls;
using UI.ViewModels.Pages;

namespace UI.Views.Pages;

public partial class ResultsPage : Page
{
    public ResultsPage(ResultsViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}