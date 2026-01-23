using System.Windows.Controls;
using UI.ViewModels;

namespace UI.Views.Pages;

public partial class CompetitionSelectionPage : Page
{
    public CompetitionSelectionPage(CompetitionSelectionViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}
