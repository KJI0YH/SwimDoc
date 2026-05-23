using System.Windows.Controls;
using System.Windows.Input;
using UI.Helpers;
using UI.ViewModels.Pages;

namespace UI.Views.Controls.DataGridView;

public partial class ResultsView : UserControl
{
    public ResultsView()
    {
        InitializeComponent();
    }

    private void ResultsGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (sender is not DataGrid dataGrid || DataContext is not ResultsViewModel viewModel)
            return;

        if (DataGridRowSelectionHelper.TryGetRowItem(dataGrid, e, out ResultEntryView? result))
            viewModel.SelectedResultEntry = result;

        if (!viewModel.OpenAthleteDetailsCommand.CanExecute(null))
            return;

        viewModel.OpenAthleteDetailsCommand.Execute(null);
        e.Handled = true;
    }
}
