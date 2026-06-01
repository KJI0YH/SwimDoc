using System.Collections;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Data;
using UI.ViewModels.Pages;

namespace UI.Helpers;

public static class ImportFilesViewHelper
{
    public static ListCollectionView CreateView(ObservableCollection<EntriesViewModel.EntriesFile> source)
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

    private sealed class ImportFilesComparer(string? sortProperty, ListSortDirection direction) : IComparer
    {
        public int Compare(object? x, object? y)
        {
            var a = (EntriesViewModel.EntriesFile)x!;
            var b = (EntriesViewModel.EntriesFile)y!;

            if (a.IsSummaryRow != b.IsSummaryRow)
                return a.IsSummaryRow ? 1 : -1;

            if (string.IsNullOrEmpty(sortProperty))
                return 0;

            var cmp = CompareByProperty(a, b, sortProperty);
            return direction == ListSortDirection.Descending ? -cmp : cmp;
        }

        private static int CompareByProperty(
            EntriesViewModel.EntriesFile a,
            EntriesViewModel.EntriesFile b,
            string propertyName)
        {
            var prop = typeof(EntriesViewModel.EntriesFile).GetProperty(propertyName);
            if (prop is null)
                return 0;

            var av = prop.GetValue(a);
            var bv = prop.GetValue(b);

            if (av is IComparable ac && bv is IComparable bc)
                return ac.CompareTo(bc);

            return string.Compare(av?.ToString(), bv?.ToString(), StringComparison.CurrentCultureIgnoreCase);
        }
    }
}
