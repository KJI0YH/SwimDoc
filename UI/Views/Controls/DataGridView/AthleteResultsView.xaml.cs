using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using UI.ViewModels.Pages;
using UI.ViewModels.Pages.Data;

namespace UI.Views.Controls.DataGridView;

public partial class AthleteResultsView : UserControl
{
    public AthleteResultsView()
    {
        InitializeComponent();
    }

    private void DataGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (sender is not DataGrid { DataContext: ResultsByAthleteViewModel viewModel } dataGrid)
            return;

        if (e.OriginalSource is DependencyObject source)
        {
            for (var current = source; current is not null && current != dataGrid; current = VisualTreeHelper.GetParent(current))
            {
                if (current is DataGridRow { Item: AthleteResultEntryView result })
                {
                    viewModel.SelectedResult = result;
                    break;
                }
            }
        }

        if (!viewModel.OpenEventResultsCommand.CanExecute(null))
            return;

        viewModel.OpenEventResultsCommand.Execute(null);
        e.Handled = true;
    }
}
