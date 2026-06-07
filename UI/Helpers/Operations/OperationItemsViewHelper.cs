using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Data;
using UI.Models.Operations;

namespace UI.Helpers.Operations;

public static class OperationItemsViewHelper
{
    public static ListCollectionView CreateView(ObservableCollection<OperationItem> source)
    {
        var view = new ListCollectionView(source);
        ApplySort(view, null, ListSortDirection.Ascending);
        return view;
    }

    public static void ApplySort(ListCollectionView view, string? sortProperty, ListSortDirection direction)
    {
        view.CustomSort = new OperationItemsComparer(sortProperty, direction);
        view.Refresh();
    }
}
