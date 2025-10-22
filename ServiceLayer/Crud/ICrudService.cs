using System.Collections.Immutable;
using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace ServiceLayer.Crud;

public interface ICrudService<TEntity, TKey>
    where TEntity : class
{
    // Read
    IQueryable<TEntity> Query(bool asNoTracking = true);
    Task<TEntity?> FindAsync(TKey id, bool asNoTracking = true, CancellationToken cancellationToken = default);

    // Create
    Task<(TEntity entity, IImmutableList<ValidationResult> errors)> CreateAsync(
        TEntity entity,
        CancellationToken cancellationToken = default);

    // Update
    Task<IImmutableList<ValidationResult>> UpdateAsync(
        TEntity entity,
        CancellationToken cancellationToken = default);

    // Delete
    Task<IImmutableList<ValidationResult>> DeleteAsync(
        TKey id,
        CancellationToken cancellationToken = default);
}
