using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;

namespace BizDbAccess.Helpers;

public static class DbContextExtensions
{
    public static TEntity GetOrAdd<TEntity>(
        this DbContext context,
        Expression<Func<TEntity, bool>> predicate,
        Func<TEntity> createEntity) where TEntity : class
    {
        var localEntity = context.ChangeTracker.Entries<TEntity>()
            .Where(e => e.State != EntityState.Deleted)
            .Select(e => e.Entity)
            .FirstOrDefault(predicate.Compile());

        if (localEntity != null)
            return localEntity;

        var entityInDb = context.Set<TEntity>().FirstOrDefault(predicate);
        if (entityInDb != null)
            return entityInDb;

        var newEntity = createEntity();
        context.Set<TEntity>().Add(newEntity);
        return newEntity;
    }
}