using Microsoft.EntityFrameworkCore;

namespace BizDbAccess;

public class GenericRepository<TEntity> where TEntity : class
{
    protected readonly DbContext Context;

    public GenericRepository(DbContext context)
    {
        Context = context;
    }

    public IQueryable<TEntity> GetEntities()
    {
        return Context.Set<TEntity>();
    }

    public async Task<TEntity> FindEntityAsync(params object?[]? keyValues)
    {
        var entity = await Context.FindAsync<TEntity>(keyValues);
        if (entity == null)
            throw new Exception("Entity not found");
        return entity;
    }

    public Task PersistDataAsync()
    {
        return Context.SaveChangesAsync();
    }
}
