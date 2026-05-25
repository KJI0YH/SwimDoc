using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using UI.Helpers;
using UI.ViewModels.Pages;

namespace UI.Views.Controls.DataGridView;

public partial class HeatsView
{
    private bool _isSelectingFromHeader;

    public HeatsView()
    {
        InitializeComponent();
    }

    private void HeatGroupHeader_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (sender is not FrameworkElement { DataContext: CollectionViewGroup group } ||
            DataContext is not HeatsViewModel viewModel)
            return;

        var heatPositions = group.Items.Cast<HeatPositionView>().ToList();
        if (heatPositions.Count == 0)
            return;

        if (FindParentDataGrid(sender as DependencyObject) is not { } dataGrid)
            return;

        var heatId = heatPositions[0].HeatId;
        var primaryPosition = heatPositions[0];

        _isSelectingFromHeader = true;
        try
        {
            dataGrid.SelectedItems.Clear();
            foreach (var position in heatPositions)
                dataGrid.SelectedItems.Add(position);

            dataGrid.SelectedItem = primaryPosition;
        }
        finally
        {
            _isSelectingFromHeader = false;
        }

        Dispatcher.BeginInvoke(() =>
            viewModel.ApplySelection(heatId, primaryPosition, wholeHeat: true), DispatcherPriority.Loaded);

        e.Handled = true;
    }

    private void HeatPositionsGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_isSelectingFromHeader ||
            sender is not DataGrid dataGrid ||
            DataContext is not HeatsViewModel viewModel)
            return;

        var selected = dataGrid.SelectedItems.Cast<HeatPositionView>().ToList();
        if (selected.Count == 0)
        {
            viewModel.IsWholeHeatSelected = false;
            return;
        }

        var heatId = selected[0].HeatId;
        var isWholeHeat = selected.Count > 1 && selected.All(position => position.HeatId == heatId);
        viewModel.ApplySelection(heatId, selected[0], isWholeHeat);
    }

    private static DataGrid? FindParentDataGrid(DependencyObject? source)
    {
        for (var current = source; current is not null; current = VisualTreeHelper.GetParent(current))
        {
            if (current is DataGrid dataGrid)
                return dataGrid;
        }

        return null;
    }

    private void HeatPositionsGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (sender is not DataGrid dataGrid || DataContext is not HeatsViewModel viewModel)
            return;

        if (DataGridRowSelectionHelper.TryGetRowItem(dataGrid, e, out HeatPositionView? position))
            viewModel.ApplySelection(position.HeatId, position, wholeHeat: false);

        if (!viewModel.OpenAthleteDetailsCommand.CanExecute(null))
            return;

        viewModel.OpenAthleteDetailsCommand.Execute(null);
        e.Handled = true;
    }
}
