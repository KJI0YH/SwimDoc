using System.Windows;
using UI.ViewModels.Windows.ReportGeneration;

namespace UI.Views.Windows.ReportGeneration;

public partial class ReportGenerationWindow : Window
{
    public ReportGenerationWindow(int? _ = null)
    {
        InitializeComponent();
        var viewModel = new ReportGenerationViewModel();
        viewModel.CloseRequested += (_, _) =>
        {
            DialogResult = viewModel.Result is not null;
            Close();
        };

        DataContext = viewModel;
    }
}

