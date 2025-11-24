using System.Collections.Immutable;
using System.ComponentModel.DataAnnotations;
using DataLayer.EfCore;
using Microsoft.EntityFrameworkCore;

namespace ServiceLayer.Crud;

public class CrudService<TEntity, TKey> : ICrudService<TEntity, TKey> where TEntity : class
{
    private readonly EfCoreContext _dbContext;

    public CrudService(EfCoreContext dbContext)
    {
        _dbContext = dbContext;
    }

    public IQueryable<TEntity> Query(bool asNoTracking = true)
    {
        var set = _dbContext.Set<TEntity>();
        return asNoTracking ? set.AsNoTracking() : set;
    }

    public async Task<TEntity?> FindAsync(TKey id, CancellationToken cancellationToken = default)
    {
        var entity = await _dbContext.FindAsync<TEntity>([id], cancellationToken);
        return entity;
    }

    public async Task<(TEntity? entity, ImmutableList<ValidationResult> errors)> CreateAsync(TEntity entity, CancellationToken cancellationToken = default)
    {
        await _dbContext.Set<TEntity>().AddAsync(entity, cancellationToken);
        var errors = await _dbContext.SaveChangesWithValidationAsync();
        return (entity, errors);
    }

    public async Task<(TEntity? entity, ImmutableList<ValidationResult> errors)> UpdateAsync(TEntity entity, CancellationToken cancellationToken = default)
    {
        _dbContext.Set<TEntity>().Attach(entity);
        _dbContext.Entry(entity).State = EntityState.Modified;
        var errors = await _dbContext.SaveChangesWithValidationAsync();
        return (entity, errors);
    }

    public async Task DeleteAsync(TKey id, CancellationToken cancellationToken = default)
    {
        var entity = await FindAsync(id, cancellationToken);
        if (entity == null) return;
        _dbContext.Set<TEntity>().Remove(entity);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}