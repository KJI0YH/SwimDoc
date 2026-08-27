using System.Collections.ObjectModel;

namespace UI.Helpers.Collections;

public static class ObservableCollectionExtensions
{
    public static void ReplaceAll<T>(this ObservableCollection<T> collection, IEnumerable<T> items)
    {
        ArgumentNullException.ThrowIfNull(collection);
        ArgumentNullException.ThrowIfNull(items);
        if (collection is ResettableObservableCollection<T> resettable)
        {
            resettable.ReplaceAll(items);
            return;
        }

        collection.Clear();
        foreach (var item in items)
            collection.Add(item);
    }
}
