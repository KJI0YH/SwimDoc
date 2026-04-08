using System.Windows;
using System.Windows.Controls;
using DataLayer.EfClasses;
using UI.ViewModels.Pages;

namespace UI.Views.Controls.TreeView;

public partial class HeatsTreeView : UserControl
{
    public HeatsTreeView()
    {
        InitializeComponent();
    }

    private void TreeView_OnSelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
    {
        if (DataContext is not HeatsViewModel viewModel)
            return;

        switch (e.NewValue)
        {
            case SwimEvent swimEvent:
                viewModel.SyncSelectedItemsFromGrid(new[] {swimEvent});
                viewModel.SelectedSwimEvent = swimEvent;
                viewModel.SelectedHeat = null;
                viewModel.SelectedHeatPosition = null;
                break;
            case Heat heat:
                viewModel.SyncSelectedItemsFromGrid(new[] { heat });
                viewModel.SelectedHeat = heat;
                viewModel.SelectedHeatPosition = null;
                break;
            case HeatPosition heatPosition:
                viewModel.SyncSelectedItemsFromGrid(new[] { heatPosition.Heat });
                viewModel.SelectedHeat = heatPosition.Heat;
                viewModel.SelectedHeatPosition = heatPosition;
                break;
            default:
                viewModel.SyncSelectedItemsFromGrid(null);
                viewModel.SelectedHeat = null;
                viewModel.SelectedHeatPosition = null;
                break;
        }
    }
}