namespace DataLayer.QueryObjects;

public static class GenericPaging
{
    public static IQueryable<T> Page<T>(this IQueryable<T> query, int page, int pageSize)
    {
        if (pageSize == 0)
            throw new ArgumentOutOfRangeException(nameof(pageSize), "page size cannot be zero.");
        if (page != 0)
            query = query.Skip(page * pageSize);
        return query.Take(pageSize);
    }
}