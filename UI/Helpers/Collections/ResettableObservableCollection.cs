using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;

namespace UI.Helpers.Collections;

/// <summary>
/// ObservableCollection that can replace all items with a single Reset notification.
/// Avoids O(n) CollectionChanged events during bulk rebuilds (e.g. searchable dropdowns).
/// </summary>
public class ResettableObservableCollection<T> : ObservableCollection<T>
{
    public ResettableObservableCollection()
    {
    }

    public ResettableObservableCollection(IEnumerable<T> collection)
        : base(collection)
    {
    }

    public void ReplaceAll(IEnumerable<T> items)
    {
        ArgumentNullException.ThrowIfNull(items);
        var list = items as IList<T> ?? items.ToList();
        CheckReentrancy();
        Items.Clear();
        foreach (var item in list)
            Items.Add(item);
        OnPropertyChanged(new PropertyChangedEventArgs(nameof(Count)));
        OnPropertyChanged(new PropertyChangedEventArgs("Item[]"));
        OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
    }
}
