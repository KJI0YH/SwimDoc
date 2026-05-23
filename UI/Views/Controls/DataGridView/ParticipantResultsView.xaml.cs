using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using UI.ViewModels.Pages;
using UI.ViewModels.Pages.Data;

namespace UI.Views.Controls.DataGridView;

public partial class ParticipantResultsView : UserControl
{
    public ParticipantResultsView()
    {
        InitializeComponent();
    }

    private void DataGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (sender is not DataGrid dataGrid || dataGrid.DataContext is not IParticipantResultsViewModel viewModel)
            return;

        if (e.OriginalSource is DependencyObject source)
        {
            for (var current = source; current is not null && current != dataGrid; current = VisualTreeHelper.GetParent(current))
            {
                if (current is DataGridRow { Item: ParticipantResultEntryView result })
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
