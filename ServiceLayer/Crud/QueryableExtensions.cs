using DataLayer.QueryObjects;

namespace ServiceLayer.Crud;

public static class QueryableExtensions
{
    public static Task<PagedResult<T>> ToPagedResultAsync<T>(
        this IQueryable<T> query,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var total = query.Count();
        var items = query.Page(page, pageSize).ToList();
        // If async provider exists, developers can adjust to EF async ops.
        var result = new PagedResult<T>
        {
            Items = items,
            TotalCount = total,
            Page = page,
            PageSize = pageSize
        };
        return Task.FromResult(result);
    }
}
