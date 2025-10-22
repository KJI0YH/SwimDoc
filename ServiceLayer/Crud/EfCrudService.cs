using System.Collections.Immutable;
using System.ComponentModel.DataAnnotations;
using DataLayer.EfCore;
using Microsoft.EntityFrameworkCore;

namespace ServiceLayer.Crud;

public class EfCrudService<TEntity, TKey> : ICrudService<TEntity, TKey>
    where TEntity : class
{
    private readonly EfCoreContext _dbContext;

    public EfCrudService(EfCoreContext dbContext)
    {
        _dbContext = dbContext;
    }

    public IQueryable<TEntity> Query(bool asNoTracking = true)
    {
        var set = _dbContext.Set<TEntity>();
        return asNoTracking ? set.AsNoTracking() : set;
    }

    public async Task<TEntity?> FindAsync(
        TKey id,
        bool asNoTracking = true,
        CancellationToken cancellationToken = default)
    {
        var entity = await _dbContext.FindAsync<TEntity>([id], cancellationToken);
        if (entity == null) return null;

        if (asNoTracking)
        {
            // Detach entity for no-tracking mode
            var entry = _dbContext.Entry(entity);
            if (entry.State != EntityState.Detached)
            {
                entry.State = EntityState.Detached;
            }
        }

        return entity;
    }

    public async Task<(TEntity entity, IImmutableList<ValidationResult> errors)> CreateAsync(
        TEntity entity,
        CancellationToken cancellationToken = default)
    {
        await _dbContext.Set<TEntity>().AddAsync(entity, cancellationToken);
        var errors = await _dbContext.SaveChangesWithValidationAsync();
        return (entity, errors);
    }

    public async Task<IImmutableList<ValidationResult>> UpdateAsync(
        TEntity entity,
        CancellationToken cancellationToken = default)
    {
        _dbContext.Set<TEntity>().Attach(entity);
        _dbContext.Entry(entity).State = EntityState.Modified;
        var errors = await _dbContext.SaveChangesWithValidationAsync();
        return errors;
    }

    public async Task<IImmutableList<ValidationResult>> DeleteAsync(
        TKey id,
        CancellationToken cancellationToken = default)
    {
        var entity = await _dbContext.FindAsync<TEntity>([id], cancellationToken);
        if (entity == null)
        {
            return ImmutableArray<ValidationResult>.Empty;
        }

        _dbContext.Set<TEntity>().Remove(entity);
        var errors = await _dbContext.SaveChangesWithValidationAsync();
        return errors;
    }
}
