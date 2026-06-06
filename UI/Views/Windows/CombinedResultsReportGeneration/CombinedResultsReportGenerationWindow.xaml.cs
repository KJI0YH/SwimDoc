using System.Windows;
using UI.ViewModels.Windows.CombinedResultsReportGeneration;

namespace UI.Views.Windows.CombinedResultsReportGeneration;

public partial class CombinedResultsReportGenerationWindow : Window
{
    public CombinedResultsReportGenerationWindow(int? _ = null)
    {
        InitializeComponent();
        var viewModel = new CombinedResultsReportGenerationViewModel();
        viewModel.CloseRequested += (_, _) =>
        {
            DialogResult = viewModel.Result is not null;
            Close();
        };

        DataContext = viewModel;
    }
}
