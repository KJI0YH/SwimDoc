using System.Windows.Controls;
using UI.ViewModels;
using CompetitionSelectionViewModel = UI.ViewModels.Pages.CompetitionSelectionViewModel;

namespace UI.Views.Pages;

public partial class CompetitionSelectionPage : Page
{
    public CompetitionSelectionPage(CompetitionSelectionViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}