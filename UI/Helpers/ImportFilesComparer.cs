using System.Collections;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Data;
using UI.ViewModels.Pages;

namespace UI.Helpers;

internal sealed class ImportFilesComparer(string? sortProperty, ListSortDirection direction) : IComparer
    {
        public int Compare(object? x, object? y)
        {
            var a = (EntriesFile)x!;
            var b = (EntriesFile)y!;

            if (a.IsSummaryRow != b.IsSummaryRow)
                return a.IsSummaryRow ? 1 : -1;

            if (string.IsNullOrEmpty(sortProperty))
                return 0;

            var cmp = CompareByProperty(a, b, sortProperty);
            return direction == ListSortDirection.Descending ? -cmp : cmp;
        }

        private static int CompareByProperty(
            EntriesFile a,
            EntriesFile b,
            string propertyName)
        {
            var prop = typeof(EntriesFile).GetProperty(propertyName);
            if (prop is null)
                return 0;

            var av = prop.GetValue(a);
            var bv = prop.GetValue(b);

            if (av is IComparable ac && bv is IComparable bc)
                return ac.CompareTo(bc);

            return string.Compare(av?.ToString(), bv?.ToString(), StringComparison.CurrentCultureIgnoreCase);
        }
    }
