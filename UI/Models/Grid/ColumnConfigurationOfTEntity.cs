using System.ComponentModel;
using System.Linq.Expressions;
using System.Reflection;
using Expression = System.Linq.Expressions.Expression;

namespace UI.Models.Grid;

public class ColumnConfiguration<TEntity> : ColumnConfiguration where TEntity : class
{
    public ColumnConfiguration(string propertyPath, string? header, double? width, string? sortMemberPath = null)
    {
        PropertyPath = propertyPath;
        SortMemberPath = sortMemberPath;
        Header = header;
        Width = width;

        var pathForSort = sortMemberPath ?? propertyPath;

        SortQuery = (query, direction) =>
        {
            var parameter = Expression.Parameter(typeof(TEntity), "x");
            var body = BuildPropertyAccess(parameter, pathForSort);
            var lambda = Expression.Lambda(body, parameter);
            var methodName = direction == ListSortDirection.Ascending ? "OrderBy" : "OrderByDescending";
            var resultExpression = Expression.Call(
                typeof(Queryable),
                methodName,
                [typeof(TEntity), body.Type],
                query.Expression,
                Expression.Quote(lambda));
            return query.Provider.CreateQuery<TEntity>(resultExpression);
        };
    }

    public ColumnConfiguration(string propertyPath, string? header, double width, ColumnSortQuery<TEntity> sortQuery,
        string? sortMemberPath = null)
    {
        PropertyPath = propertyPath;
        SortMemberPath = sortMemberPath;
        Header = header;
        Width = width;
        SortQuery = sortQuery;
    }

    public ColumnSortQuery<TEntity> SortQuery { get; set; }

    public static IQueryable<TEntity> SortQueryableByPropertyPath(IQueryable<TEntity> query, string propertyPath,
        ListSortDirection direction)
    {
        var parameter = Expression.Parameter(typeof(TEntity), "x");
        var body = BuildPropertyAccess(parameter, propertyPath);
        var lambda = Expression.Lambda(body, parameter);
        var methodName = direction == ListSortDirection.Ascending ? "OrderBy" : "OrderByDescending";
        var resultExpression = Expression.Call(
            typeof(Queryable),
            methodName,
            [typeof(TEntity), body.Type],
            query.Expression,
            Expression.Quote(lambda));
        return query.Provider.CreateQuery<TEntity>(resultExpression);
    }

    private static Expression BuildPropertyAccess(Expression parameter, string propertyPath)
    {
        var expr = parameter;
        foreach (var part in propertyPath.Split('.', StringSplitOptions.RemoveEmptyEntries))
        {
            var prop = expr.Type.GetProperty(part.Trim(),
                BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
            if (prop == null)
                throw new InvalidOperationException($"Property '{part}' not found on type '{expr.Type.Name}'.");

            expr = Expression.Property(expr, prop);
        }

        return expr;
    }
}
