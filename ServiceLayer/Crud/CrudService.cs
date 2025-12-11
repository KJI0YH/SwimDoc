using System.Collections.Immutable;
using System.ComponentModel.DataAnnotations;
using DataLayer.EfCore;
using Microsoft.EntityFrameworkCore;

namespace ServiceLayer.Crud;

public class CrudService<TEntity, TKey>(EfCoreContext dbContext) : ICrudService<TEntity, TKey>
    where TEntity : class
{
    public IQueryable<TEntity> Query(bool asNoTracking = true)
    {
        var set = dbContext.Set<TEntity>();
        return asNoTracking ? set.AsNoTracking() : set;
    }

    public async Task<TEntity?> FindAsync(TKey id, CancellationToken cancellationToken = default)
    {
        var entity = await dbContext.FindAsync<TEntity>([id], cancellationToken);
        return entity;
    }

    public async Task<(TEntity? entity, ImmutableList<ValidationResult> errors)> CreateAsync(TEntity entity, CancellationToken cancellationToken = default)
    {
        await dbContext.Set<TEntity>().AddAsync(entity, cancellationToken);
        var errors = await dbContext.SaveChangesWithValidationAsync();
        return (entity, errors);
    }

    public async Task<(TEntity? entity, ImmutableList<ValidationResult> errors)> UpdateAsync(TEntity entity, CancellationToken cancellationToken = default)
    {
        dbContext.Set<TEntity>().Attach(entity);
        dbContext.Entry(entity).State = EntityState.Modified;
        var errors = await dbContext.SaveChangesWithValidationAsync();
        return (entity, errors);
    }

    public async Task DeleteAsync(TKey id, CancellationToken cancellationToken = default)
    {
        var entity = await FindAsync(id, cancellationToken);
        if (entity == null) return;
        dbContext.Set<TEntity>().Remove(entity);
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}