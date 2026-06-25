using System.Collections.Immutable;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using BizLogic.Helpers;
using DataLayer.Display;
using DataLayer.EfClasses;
using DataLayer.EfCore;
using Microsoft.EntityFrameworkCore;
using ServiceLayer.Crud;
using ServiceLayer.Logging;
using ServiceLayer.Resources;

namespace ServiceLayer.EntryService;

public class EntryService(EfCoreContext dbContext, IAppLog log) : CrudService<Entry, int?>(dbContext, log), IEntryService
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
        dbContext.Entry(tracked).CurrentValues.SetValues(entity);
        ApplyHeatEntryUpdateRules(tracked, preservedStatus, heatPosition, swimEventIdBeforeUpdate);
        if (tracked.Relay != null)
        {
            if (entity.Relay != null)
            {
                tracked.Relay.ClubId = entity.Relay.ClubId;
                tracked.Relay.Number = entity.Relay.Number;
            }
            var incomingPositions = entity.Relay?.Positions?.ToList() ?? [];
            var relayId = tracked.Relay.Id;
            var existingPositions = tracked.Relay.Positions.ToList();
            if (existingPositions.Count > 0)
                dbContext.RelayPositions.RemoveRange(existingPositions);
            tracked.Relay.Positions.Clear();
            foreach (var p in incomingPositions.OrderBy(p => p.Order))
            {
                tracked.Relay.Positions.Add(new RelayPosition
                {
                    RelayId = relayId,
                    AthleteId = p.AthleteId,
                    Order = p.Order,
                    EntryTime = p.EntryTime
                });
            }
        }
        var errors = await dbContext.SaveChangesWithValidationAsync();
        if (errors.Count > 0)
            await dbContext.Entry(tracked).ReloadAsync(cancellationToken).ConfigureAwait(false);
        else
            log.Info(EntityLogFormatter.FormatOperation("Update", entity));
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
        else
            log.Info(EntityLogFormatter.FormatOperation("Update", entity));
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

    public async Task<List<Entry>> GetEntriesByEventIdOrderByFinishTimeAsync(int eventId)
    {
        var entries = await dbContext.Entries
            .AsNoTracking()
            .Where(e => e.SwimEventId == eventId)
            .Include(e => e.SwimEvent!)
            .ThenInclude(se => se.AgeGroup)
            .Include(e => e.SwimEvent!)
            .ThenInclude(se => se.SwimStyle)
            .Include(e => e.Athlete!)
            .ThenInclude(a => a.Club)
            .Include(e => e.Relay!)
            .ThenInclude(r => r.Positions)
            .ThenInclude(p => p.Athlete)
            .ThenInclude(a => a.Club)
            .Include(e => e.Relay!)
            .ThenInclude(r => r.Club)
            .ToListAsync();
        return EntryPlaceAssignment.OrderForResults(entries).ToList();
    }

    public async Task<CombinedResultsData> GetCombinedResultsByAgeGroupAsync(int ageGroupId)
    {
        var events = await dbContext.SwimEvents
            .AsNoTracking()
            .Where(se => se.AgeGroupId == ageGroupId && se.SwimStyle.RelayCount == 0)
            .Include(se => se.SwimStyle)
            .OrderBy(se => se.Order)
            .ToListAsync();
        if (events.Count == 0)
            return new CombinedResultsData([], []);
        var eventIds = events.Select(se => se.Id).ToList();
        var entries = await dbContext.Entries
            .AsNoTracking()
            .Where(e => e.SwimEventId != null && eventIds.Contains(e.SwimEventId.Value))
            .Where(e => e.AthleteId != null)
            .Include(e => e.Athlete!)
            .ThenInclude(a => a.Club)
            .ToListAsync();
        var eventGroups = CombinedResultsCalculator.GroupEventsBySwimStyle(events);
        var eventColumns = eventGroups
            .Select(group =>
            {
                var swimStyle = group.First().SwimStyle;
                return new CombinedResultsEventColumn(
                    group.Key,
                    LocalizedEntityDisplayFormatter.FormatSwimStyle(swimStyle),
                    entries.Any(entry =>
                        entry.Scoring &&
                        group.Any(swimEvent => swimEvent.Id == entry.SwimEventId)));
            })
            .ToList();
        var athletes = entries
            .GroupBy(e => e.AthleteId!.Value)
            .Select(group =>
            {
                var athlete = group.First().Athlete!;
                var entriesByEventId = group
                    .Where(entry => entry.SwimEventId.HasValue)
                    .ToDictionary(entry => entry.SwimEventId!.Value);
                var pointsBySwimStyleId = new Dictionary<int, string>();
                var scoringBySwimStyleId = new Dictionary<int, bool>();
                var totalPoints = 0;
                foreach (var eventGroup in eventGroups)
                {
                    var groupEvents = eventGroup.ToList();
                    var groupEntriesByEventId = groupEvents
                        .Where(swimEvent => entriesByEventId.ContainsKey(swimEvent.Id))
                        .ToDictionary(swimEvent => swimEvent.Id, swimEvent => entriesByEventId[swimEvent.Id]);
                    if (groupEntriesByEventId.Count == 0)
                        continue;
                    var highestRoundEntry =
                        CombinedResultsCalculator.GetHighestRoundEntry(groupEvents, groupEntriesByEventId);
                    pointsBySwimStyleId[eventGroup.Key] =
                        CombinedResultsCalculator.FormatPoints(highestRoundEntry);
                    if (highestRoundEntry is not null)
                        scoringBySwimStyleId[eventGroup.Key] = highestRoundEntry.Scoring;
                    totalPoints += CombinedResultsCalculator.GetTotalContribution(highestRoundEntry);
                }
                var isInOfficialStandings = group.Any(e => e.Scoring);
                return new CombinedResultsAthleteRow(
                    athlete,
                    pointsBySwimStyleId,
                    scoringBySwimStyleId,
                    totalPoints,
                    isInOfficialStandings);
            })
            .OrderByDescending(row => row.IsInOfficialStandings)
            .ThenByDescending(row => row.TotalPoints)
            .ThenBy(row => row.Athlete.LastName)
            .ThenBy(row => row.Athlete.FirstName)
            .ToList();
        return new CombinedResultsData(eventColumns, athletes);
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
            throw new InvalidOperationException(string.Format(
                CultureInfo.CurrentUICulture,
                ServiceErrorStrings.Entry_Copy_EventNotFound_Format,
                targetEventId));
        if (targetEvent.RoundParticipantsCount is null or <= 0)
            throw new InvalidOperationException(ServiceErrorStrings.Entry_Copy_RoundParticipantsCountMissing);
        var previousEvent = await dbContext.SwimEvents
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == previousEventId, cancellationToken);
        if (previousEvent is null)
            throw new InvalidOperationException(string.Format(
                CultureInfo.CurrentUICulture,
                ServiceErrorStrings.Entry_Copy_EventNotFound_Format,
                previousEventId));
        if (previousEvent.Status != SwimEventStatus.OFFICIAL)
            throw new InvalidOperationException(ServiceErrorStrings.Entry_Copy_PreviousEventMustBeOfficial);
        var orderedEntries = await GetEntriesByEventIdOrderByFinishTimeAsync(previousEventId);
        var finishers = orderedEntries
            .Where(e => e.Status == EntryStatus.FINISH && e.FinishTime.HasValue)
            .ToList();
        var selectedSources = SelectQualifiersByFinishTime(finishers, targetEvent.RoundParticipantsCount.Value);
        if (selectedSources.Count == 0)
            throw new InvalidOperationException(ServiceErrorStrings.Entry_Copy_NoFinishersWithTime);
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
        log.Info(
            $"Copy entries from SwimEventId={previousEventId} to SwimEventId={targetEventId}: created {created.Count}, errors {errors.Count}");
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
        var scoring = source.Scoring;
        var entry = new Entry
        {
            SwimEventId = targetEvent.Id,
            SwimStyleId = targetEvent.SwimStyleId,
            Scoring = scoring,
            EntryTime = source.FinishTime,
            Comment = source.Comment
        };
        if (source.AthleteId.HasValue)
        {
            entry.AthleteId = source.AthleteId;
            entry = dbContext.NormalizeEntry(entry);
            entry.Scoring = scoring;
            return entry;
        }
        if (source.Relay is null)
        {
            entry = dbContext.NormalizeEntry(entry);
            entry.Scoring = scoring;
            return entry;
        }
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
        entry = dbContext.NormalizeEntry(entry);
        entry.Scoring = scoring;
        return entry;
    }

    public override async Task DeleteAsync(int? id, CancellationToken cancellationToken = default)
    {
        if (id == null) return;
        var entry = await dbContext.Entries
            .Include(e => e.Relay)
            .ThenInclude(r => r!.Positions)
            .FirstOrDefaultAsync(e => e.Id == id.Value, cancellationToken);
        if (entry == null) return;
        log.Info(EntityLogFormatter.FormatOperation("Delete", entry));
        var relay = entry.Relay;
        dbContext.Entries.Remove(entry);
        if (relay != null)
            dbContext.Relays.Remove(relay);
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
