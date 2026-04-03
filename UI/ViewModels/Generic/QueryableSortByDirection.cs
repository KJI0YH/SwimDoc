using System.ComponentModel;
using System.Linq.Expressions;

namespace UI.ViewModels.Generic;

/// <summary>
/// Упрощает запись сортировки с <see cref="ListSortDirection"/> и цепочками <c>ThenBy</c> / <c>ThenByDescending</c>.
/// </summary>
public static class QueryableSortByDirection
{
    /// <summary>
    /// Выбирает одну из двух веток (asc / desc) для произвольной цепочки ключей.
    /// </summary>
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
