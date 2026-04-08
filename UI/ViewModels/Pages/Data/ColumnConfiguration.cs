using System.ComponentModel;
using System.Reflection;
using System.Windows.Controls;
using Expression = System.Linq.Expressions.Expression;

namespace UI.ViewModels.Pages.Data;

public delegate IQueryable<TEntity> ColumnSortQuery<TEntity>(IQueryable<TEntity> query, ListSortDirection direction)
    where TEntity : class;

public class ColumnConfiguration
{
    public string PropertyPath { get; set; } = string.Empty;
    public string? SortMemberPath { get; set; }
    public string? Header { get; set; }
    public double? Width { get; set; }
    public DataGridLengthUnitType? WidthUnitType { get; set; }
    public bool IsReadOnly { get; set; } = true;
    public object? Converter { get; set; }
    public string? ConverterParameter { get; set; }
    public string? TrueSymbolIcon { get; set; }
    public string? FalseSymbolIcon { get; set; }

    internal string GetSortKey()
    {
        return SortMemberPath ?? PropertyPath;
    }
}

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