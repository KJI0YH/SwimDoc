using System.Collections;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Data;
using UI.ViewModels.Pages;

namespace UI.Helpers.Import;

public static class ImportFilesViewHelper
{
    public static ListCollectionView CreateView(ObservableCollection<EntriesFile> source)
    {
        var view = new ListCollectionView(source);
        ApplySort(view, null, ListSortDirection.Ascending);
        return view;
    }

    public static void ApplySort(ListCollectionView view, string? sortProperty, ListSortDirection direction)
    {
        view.CustomSort = new ImportFilesComparer(sortProperty, direction);
        view.Refresh();
    }
}
