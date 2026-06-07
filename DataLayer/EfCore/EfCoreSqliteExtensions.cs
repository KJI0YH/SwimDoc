using Microsoft.EntityFrameworkCore;

namespace DataLayer.EfCore;

public static class EfCoreSqliteExtensions
{
    public static DbContextOptionsBuilder UseSwimDocSqlite(this DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder
            .UseSqlite()
            .AddInterceptors(new SqliteFunctionsInterceptor());
        return optionsBuilder;
    }

    public static DbContextOptionsBuilder<TContext> UseSwimDocSqlite<TContext>(
        this DbContextOptionsBuilder<TContext> optionsBuilder)
        where TContext : DbContext
    {
        optionsBuilder
            .UseSqlite()
            .AddInterceptors(new SqliteFunctionsInterceptor());
        return optionsBuilder;
    }
}
