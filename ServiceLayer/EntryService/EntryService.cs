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

    public override async Task<(Entry? entity, ImmutableList<ValidationResult> errors)> UpdateAsync(Entry entity,
        CancellationToken cancellationToken = default)
    {
        entity = dbContext.NormalizeEntry(entity);

        if (entity.Relay == null && entity.RelayId == null)
            return await UpdateIndividualEntryAsync(entity, cancellationToken);

        var id = entity.Id;
        var tracked = await dbContext.Entries
            .Include(e => e.Relay)
            .ThenInclude(r => r.Positions)
            .FirstOrDefaultAsync(e => e.Id == id, cancellationToken);

        if (tracked == null)
            throw new InvalidOperationException($"Entity with Id {id} was not found in the database.");

        var preservedStatus = tracked.Status;
        var heatPosition = await dbContext.HeatPositions
            .FirstOrDefaultAsync(hp => hp.EntryId == id, cancellationToken);
        var swimEventIdBeforeUpdate = tracked.SwimEventId;

        // Update entry scalar fields
        dbContext.Entry(tracked).CurrentValues.SetValues(entity);
        ApplyHeatEntryUpdateRules(tracked, preservedStatus, heatPosition, swimEventIdBeforeUpdate);

        // Sync relay fields + positions
        if (tracked.Relay != null)
        {
            if (entity.Relay != null)
            {
                tracked.Relay.ClubId = entity.Relay.ClubId;
                tracked.Relay.Number = entity.Relay.Number;
            }

            var incomingPositions = entity.Relay?.Positions?.ToList() ?? [];

            tracked.Relay.Positions ??= new List<RelayPosition>();
            tracked.Relay.Positions.Clear();

            foreach (var p in incomingPositions.OrderBy(p => p.Order))
            {
                tracked.Relay.Positions.Add(new RelayPosition
                {
                    RelayId = tracked.Relay.Id,
                    AthleteId = p.AthleteId,
                    Order = p.Order,
                    EntryTime = p.EntryTime
                });
            }
        }

        var errors = await dbContext.SaveChangesWithValidationAsync();
        if (errors.Count > 0)
            await dbContext.Entry(tracked).ReloadAsync(cancellationToken).ConfigureAwait(false);

        return (tracked, errors);
    }

    private async Task<(Entry? entity, ImmutableList<ValidationResult> errors)> UpdateIndividualEntryAsync(
        Entry entity,
        CancellationToken cancellationToken)
    {
        var id = entity.Id;
        var tracked = await dbContext.Entries.FirstOrDefaultAsync(e => e.Id == id, cancellationToken);
        if (tracked == null)
            throw new InvalidOperationException($"Entity with Id {id} was not found in the database.");

        var preservedStatus = tracked.Status;
        var heatPosition = await dbContext.HeatPositions
            .FirstOrDefaultAsync(hp => hp.EntryId == id, cancellationToken);
        var swimEventIdBeforeUpdate = tracked.SwimEventId;

        dbContext.Entry(tracked).CurrentValues.SetValues(entity);
        ApplyHeatEntryUpdateRules(tracked, preservedStatus, heatPosition, swimEventIdBeforeUpdate);

        var errors = await dbContext.SaveChangesWithValidationAsync();
        if (errors.Count > 0)
            await dbContext.Entry(tracked).ReloadAsync(cancellationToken).ConfigureAwait(false);

        return (tracked, errors);
    }

    private void ApplyHeatEntryUpdateRules(
        Entry tracked,
        EntryStatus preservedStatus,
        HeatPosition? heatPosition,
        int? swimEventIdBeforeUpdate)
    {
        if (heatPosition is null)
            return;

        if (tracked.SwimEventId != swimEventIdBeforeUpdate)
            dbContext.HeatPositions.Remove(heatPosition);
        else
            tracked.Status = preservedStatus;
    }

    public Task<List<Entry>> GetEntriesByEventIdOrderByFinishTimeAsync(int eventId)
    {
        return dbContext.Entries
            .AsNoTracking()
            .Where(e => e.SwimEventId == eventId)
            .Include(e => e.Athlete!)
            .ThenInclude(a => a.Club)
            .Include(e => e.Relay!)
            .ThenInclude(r => r.Positions)
            .ThenInclude(p => p.Athlete)
            .ThenInclude(a => a.Club)
            .Include(e => e.Relay!)
            .ThenInclude(r => r.Club)
            .OrderBy(e => e.Status > EntryStatus.FINISH ? 1 : 0)
            .ThenBy(e => e.Status == EntryStatus.FINISH ? (e.FinishTime ?? int.MaxValue) : int.MaxValue)
            .ToListAsync();
    }

    public override async Task DeleteAsync(int? id, CancellationToken cancellationToken = default)
    {
        if (id == null) return;

        var entry = await dbContext.Entries
            .Include(e => e.Relay)
            .FirstOrDefaultAsync(e => e.Id == id.Value, cancellationToken);

        if (entry == null) return;

        var relay = entry.Relay;
        dbContext.Entries.Remove(entry);

        if (relay != null)
            dbContext.Relays.Remove(relay);

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}