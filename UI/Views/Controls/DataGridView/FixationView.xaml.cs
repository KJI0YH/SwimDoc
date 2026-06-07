using System.Windows.Controls;
using System.Windows.Input;
using UI.ViewModels.Pages;

namespace UI.Views.Controls.DataGridView;

public partial class FixationView : UserControl
{
    public FixationView()
    {
        InitializeComponent();
    }

    private void FixationPositionsGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (sender is not DataGrid dataGrid || DataContext is not FixationViewModel viewModel)
            return;
        if (DataGridRowSelectionHelper.TryGetRowItem(dataGrid, e, out FixationHeatPositionView? position))
            viewModel.SelectedFixationPosition = position;
        if (!viewModel.OpenAthleteDetailsCommand.CanExecute(null))
            return;
        viewModel.OpenAthleteDetailsCommand.Execute(null);
        e.Handled = true;
    }
}
