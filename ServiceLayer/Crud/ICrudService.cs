using System.Collections.Immutable;
using System.ComponentModel.DataAnnotations;
using BizLogic.HeatLogic;

namespace ServiceLayer.Crud;

public interface ICrudService<TEntity, TKey> where TEntity : class
{
    IQueryable<TEntity> Query(bool asNoTracking = true);
    Task<TEntity?> FindAsync(TKey id, CancellationToken cancellationToken = default);
    Task<(TEntity? entity, ImmutableList<ValidationResult> errors)> CreateAsync(TEntity entity, CancellationToken cancellationToken = default);
    Task<(TEntity? entity, ImmutableList<ValidationResult> errors)> UpdateAsync(TEntity entity, CancellationToken cancellationToken = default);
    Task DeleteAsync(TKey id, CancellationToken cancellationToken = default);
}