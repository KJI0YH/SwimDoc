using System.Collections.Immutable;
using System.ComponentModel.DataAnnotations;
using DataLayer.EfCore;
using Microsoft.EntityFrameworkCore;

namespace ServiceLayer.Crud;

public class CrudService<TEntity, TKey>(EfCoreContext dbContext) : ICrudService<TEntity, TKey>
    where TEntity : class
{
    public virtual IQueryable<TEntity> Query(bool asNoTracking = true)
    {
        var set = dbContext.Set<TEntity>();
        return asNoTracking ? set.AsNoTracking() : set;
    }

    public virtual async Task<TEntity?> FindAsync(TKey id, CancellationToken cancellationToken = default)
    {
        if (id == null) return null;
        var entity = await dbContext.FindAsync<TEntity>([id], cancellationToken);
        return entity;
    }

    public virtual async Task<(TEntity? entity, ImmutableList<ValidationResult> errors)> CreateAsync(TEntity entity, CancellationToken cancellationToken = default)
    {
        await dbContext.Set<TEntity>().AddAsync(entity, cancellationToken);
        var errors = await dbContext.SaveChangesWithValidationAsync();
        if (errors.Count > 0)
            dbContext.Entry(entity).State = EntityState.Detached;
        return (entity, errors);
    }

    public virtual async Task<(TEntity? entity, ImmutableList<ValidationResult> errors)> UpdateAsync(TEntity entity, CancellationToken cancellationToken = default)
    {
        var entry = dbContext.Entry(entity);

        var idProperty = typeof(TEntity).GetProperty("Id");
        if (idProperty == null)
        {
            throw new InvalidOperationException($"Entity type {typeof(TEntity).Name} does not have an Id property.");
        }

        var id = idProperty.GetValue(entity);
        if (id == null)
        {
            throw new InvalidOperationException("Cannot update entity with null Id.");
        }

        var trackedEntity = await dbContext.FindAsync<TEntity>([id], cancellationToken);

        if (trackedEntity == null)
        {
            throw new InvalidOperationException($"Entity with Id {id} was not found in the database.");
        }

        dbContext.Entry(trackedEntity).CurrentValues.SetValues(entity);

        dbContext.Entry(trackedEntity).State = EntityState.Modified;

        var errors = await dbContext.SaveChangesWithValidationAsync();
        if (errors.Count > 0)
            await dbContext.Entry(trackedEntity).ReloadAsync(cancellationToken).ConfigureAwait(false);
        return (trackedEntity, errors);
    }

    public virtual async Task DeleteAsync(TKey id, CancellationToken cancellationToken = default)
    {
        var entity = await FindAsync(id, cancellationToken);
        if (entity == null) return;
        dbContext.Set<TEntity>().Remove(entity);
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
