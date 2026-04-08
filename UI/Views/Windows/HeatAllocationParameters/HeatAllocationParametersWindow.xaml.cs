using System.Windows;
using HeatAllocationParametersViewModel = UI.ViewModels.Windows.HeatAllocationParameters.HeatAllocationParametersViewModel;

namespace UI.Views.Windows.HeatAllocationParameters;

public partial class HeatAllocationParametersWindow : Window
{
    public HeatAllocationParametersWindow(int? _ = null)
    {
        InitializeComponent();
        var viewModel = new HeatAllocationParametersViewModel();
        viewModel.CloseRequested += (_, _) =>
        {
            DialogResult = viewModel.Result is not null;
            Close();
        };

        DataContext = viewModel;
    }
}