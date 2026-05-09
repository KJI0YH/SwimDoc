using System.Collections.Immutable;
using System.ComponentModel.DataAnnotations;
using DataLayer.EfClasses;
using DataLayer.EfCore;
using Microsoft.EntityFrameworkCore;
using ServiceLayer.Crud;

namespace ServiceLayer.EntryService;

public class EntryService(EfCoreContext dbContext) : CrudService<Entry, int?>(dbContext), IEntryService
{
    public override Task<(Entry? entity, ImmutableList<ValidationResult> errors)> CreateAsync(Entry entity,
        CancellationToken cancellationToken = default)
    {
        entity = dbContext.NormalizeEntry(entity);
        return base.CreateAsync(entity, cancellationToken);
    }

    public override Task<(Entry? entity, ImmutableList<ValidationResult> errors)> UpdateAsync(Entry entity,
        CancellationToken cancellationToken = default)
    {
        entity = dbContext.NormalizeEntry(entity);
        return base.UpdateAsync(entity, cancellationToken);
    }

    public Task<List<Entry>> GetEntriesByEventIdOrderByFinishTimeAsync(int eventId)
    {
        return dbContext.Entries
            .Where(e => e.SwimEventId == eventId)
            .Include(e => e.Athlete!)
            .ThenInclude(a => a.Club)
            .OrderBy(e => e.Status > EntryStatus.FINISH ? 1 : 0)
            .ThenBy(e => e.Status == EntryStatus.FINISH ? (e.FinishTime ?? int.MaxValue) : int.MaxValue)
            .ToListAsync();
    }
}