using System.Windows.Controls;
using System.Windows.Input;
using UI.Helpers;
using UI.ViewModels.Pages;

namespace UI.Views.Controls.DataGridView;

public partial class HeatsView
{
    public HeatsView()
    {
        InitializeComponent();
    }

    private void HeatPositionsGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (sender is not DataGrid dataGrid || DataContext is not HeatsViewModel viewModel)
            return;

        if (DataGridRowSelectionHelper.TryGetRowItem(dataGrid, e, out HeatPositionView? position))
            viewModel.SelectedHeatPosition = position;

        if (!viewModel.OpenAthleteDetailsCommand.CanExecute(null))
            return;

        viewModel.OpenAthleteDetailsCommand.Execute(null);
        e.Handled = true;
    }
}
