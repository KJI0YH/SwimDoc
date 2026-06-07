using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace UI.Helpers.Controls;

public static class DataGridRowSelectionHelper
{
    public static bool TryGetRowItem<TItem>(DataGrid dataGrid, MouseButtonEventArgs e, out TItem? item)
        where TItem : class
    {
        item = null;
        if (e.OriginalSource is not DependencyObject source)
            return false;
        for (var current = source; current is not null && current != dataGrid; current = VisualTreeHelper.GetParent(current))
        {
            if (current is DataGridRow { Item: TItem rowItem })
            {
                item = rowItem;
                return true;
            }
        }
        return false;
    }
}
