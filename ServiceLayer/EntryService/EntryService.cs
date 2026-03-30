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
        entity = Normalize(entity);
        return base.CreateAsync(entity, cancellationToken);
    }

    public override Task<(Entry? entity, ImmutableList<ValidationResult> errors)> UpdateAsync(Entry entity,
        CancellationToken cancellationToken = default)
    {
        entity = Normalize(entity);
        return base.UpdateAsync(entity, cancellationToken);
    }

    private Entry Normalize(Entry entry)
    {
        var swimEvent = dbContext.SwimEvents.AsNoTracking().FirstOrDefault(se => se.Id == entry.SwimEventId);
        if (swimEvent != null)
            entry.SwimStyleId = swimEvent.SwimStyleId;

        if (IsAdded(entry))
        {
            entry.Status = swimEvent is null ? EntryStatus.INS : EntryStatus.EVENT;
        }

        return entry;
    }

    private bool IsAdded(Entry entry)
    {
        var entryState = dbContext.Entry(entry).State;
        return entryState is EntityState.Added or EntityState.Detached;
    }
}