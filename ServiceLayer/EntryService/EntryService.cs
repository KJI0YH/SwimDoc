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

    public async Task<(List<Entry> Created, IReadOnlyList<ValidationResult> Errors)> CopyEntriesFromPreviousEventAsync(
        int previousEventId,
        int targetEventId,
        CancellationToken cancellationToken = default)
    {
        var targetEvent = await dbContext.SwimEvents
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == targetEventId, cancellationToken);
        if (targetEvent is null)
            throw new InvalidOperationException($"Событие с Id {targetEventId} не найдено.");

        if (targetEvent.RoundParticipantsCount is null or <= 0)
            throw new InvalidOperationException("Для текущего события не задано количество участников раунда.");

        var previousEvent = await dbContext.SwimEvents
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == previousEventId, cancellationToken);
        if (previousEvent is null)
            throw new InvalidOperationException($"Событие с Id {previousEventId} не найдено.");

        if (previousEvent.Status != SwimEventStatus.OFFICIAL)
            throw new InvalidOperationException("Предыдущее событие должно быть в статусе OFFICIAL.");

        var orderedEntries = await GetEntriesByEventIdOrderByFinishTimeAsync(previousEventId);
        var finishers = orderedEntries
            .Where(e => e.Status == EntryStatus.FINISH && e.FinishTime.HasValue)
            .ToList();

        var selectedSources = SelectQualifiersByFinishTime(finishers, targetEvent.RoundParticipantsCount.Value);
        if (selectedSources.Count == 0)
            throw new InvalidOperationException("В предыдущем событии нет финишировавших участников с временем.");

        var created = new List<Entry>();
        var errors = new List<ValidationResult>();

        foreach (var source in selectedSources)
        {
            var entry = BuildEntryFromPreviousResult(source, targetEvent);
            var (entity, createErrors) = await CreateAsync(entry, cancellationToken);
            if (createErrors.Count > 0)
                errors.AddRange(createErrors);
            else if (entity is not null)
                created.Add(entity);
        }

        return (created, errors);
    }

    private static List<Entry> SelectQualifiersByFinishTime(List<Entry> finishers, int participantCount)
    {
        if (finishers.Count <= participantCount)
            return finishers;

        var cutoffFinishTime = finishers[participantCount - 1].FinishTime;
        return finishers
            .TakeWhile((entry, index) =>
                index < participantCount || entry.FinishTime == cutoffFinishTime)
            .ToList();
    }

    private Entry BuildEntryFromPreviousResult(Entry source, SwimEvent targetEvent)
    {
        var entry = new Entry
        {
            SwimEventId = targetEvent.Id,
            SwimStyleId = targetEvent.SwimStyleId,
            Scoring = source.Scoring,
            EntryTime = source.FinishTime,
            Comment = source.Comment
        };

        if (source.AthleteId.HasValue)
        {
            entry.AthleteId = source.AthleteId;
            return dbContext.NormalizeEntry(entry);
        }

        if (source.Relay is null)
            return dbContext.NormalizeEntry(entry);

        entry.Relay = new Relay
        {
            ClubId = source.Relay.ClubId,
            Number = source.Relay.Number,
            Positions = source.Relay.Positions
                .OrderBy(p => p.Order)
                .Select(p => new RelayPosition
                {
                    AthleteId = p.AthleteId,
                    Order = p.Order,
                    EntryTime = p.EntryTime
                })
                .ToList()
        };

        return dbContext.NormalizeEntry(entry);
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