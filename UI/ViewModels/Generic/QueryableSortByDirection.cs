using System.ComponentModel;

namespace UI.ViewModels.Generic;

public static class QueryableSortByDirection
{
    public static IQueryable<T> Sort<T>(
        this IQueryable<T> query,
        ListSortDirection direction,
        Func<IQueryable<T>, IOrderedQueryable<T>> ascending,
        Func<IQueryable<T>, IOrderedQueryable<T>> descending)
        where T : class
    {
        return direction == ListSortDirection.Ascending
            ? ascending(query)
            : descending(query);
    }
}
