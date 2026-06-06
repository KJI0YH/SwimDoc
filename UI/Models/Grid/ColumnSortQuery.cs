using System.ComponentModel;

namespace UI.Models.Grid;

public delegate IQueryable<TEntity> ColumnSortQuery<TEntity>(IQueryable<TEntity> query, ListSortDirection direction)
    where TEntity : class;
