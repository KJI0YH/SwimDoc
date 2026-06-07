using System.ComponentModel;
using System.Linq.Expressions;
using UI.ViewModels.Generic;

namespace UI.Models.Grid;

public static class ColumnSort
{
    public static ColumnSortQuery<TEntity> By<TEntity, TKey>(Expression<Func<TEntity, TKey>> keySelector)
        where TEntity : class =>
        (query, direction) => direction == ListSortDirection.Ascending
            ? query.OrderBy(keySelector)
            : query.OrderByDescending(keySelector);

    public static ColumnSortQuery<TEntity> By<TEntity, TKey1, TKey2>(
        Expression<Func<TEntity, TKey1>> primary,
        Expression<Func<TEntity, TKey2>> secondary)
        where TEntity : class =>
        (query, direction) => direction == ListSortDirection.Ascending
            ? query.OrderBy(primary).ThenBy(secondary)
            : query.OrderByDescending(primary).ThenByDescending(secondary);

    public static ColumnSortQuery<TEntity> By<TEntity, TKey1, TKey2, TKey3>(
        Expression<Func<TEntity, TKey1>> primary,
        Expression<Func<TEntity, TKey2>> secondary,
        Expression<Func<TEntity, TKey3>> tertiary)
        where TEntity : class =>
        (query, direction) => direction == ListSortDirection.Ascending
            ? query.OrderBy(primary).ThenBy(secondary).ThenBy(tertiary)
            : query.OrderByDescending(primary).ThenByDescending(secondary).ThenByDescending(tertiary);

    public static ColumnSortQuery<TEntity> ByDirection<TEntity>(
        Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>> ascending,
        Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>> descending)
        where TEntity : class =>
        (query, direction) => QueryableSortByDirection.Sort(query, direction, ascending, descending);
}
