using System.Collections.Immutable;
using System.ComponentModel.DataAnnotations;
using DataLayer.EfClasses;
using DataLayer.EfCore;
using ServiceLayer.Crud;

namespace ServiceLayer.EventService;

public class EventService(EfCoreContext dbContext) : CrudService<SwimEvent, int?>(dbContext), IEventService
{
    public int GetNextOrderNumber()
    {
        var total = dbContext.SwimEvents.Count();
        var max = total > 0 ? dbContext.SwimEvents.Max(se => se.Order) : 0;
        return Math.Max(total, max) + 1;
    }

    public (int min, int max) GetPreviousLanes()
    {
        var swimEvent = dbContext.SwimEvents.OrderByDescending(se => se.Order).FirstOrDefault();
        return swimEvent is null ? (0, 0) : (swimEvent.LaneMin, swimEvent.LaneMax);
    }

    public override async Task<(SwimEvent? entity, ImmutableList<ValidationResult> errors)> CreateAsync(
        SwimEvent entity, CancellationToken cancellationToken = default)
    {
        var result = await base.CreateAsync(entity, cancellationToken);
        if (result.errors.IsEmpty)
            UpdateRelatedEntries(entity);
        return result;
    }

    public override async Task<(SwimEvent? entity, ImmutableList<ValidationResult> errors)> UpdateAsync(
        SwimEvent entity, CancellationToken cancellationToken = default)
    {
        var result = await base.UpdateAsync(entity, cancellationToken);
        if (result.errors.IsEmpty)
            UpdateRelatedEntries(entity);
        return result;
    }

    private void UpdateRelatedEntries(SwimEvent swimEvent)
    {
        var ageGroup = dbContext.AgeGroups.First(ag => ag.Id == swimEvent.AgeGroupId);
        var eventEntries = dbContext.Entries.Where(e =>
            e.SwimEventId == swimEvent.Id &&
            e.Status == EntryStatus.EVENT &&
            (e.SwimStyleId != swimEvent.SwimStyleId ||
             e.Athlete.Gender != ageGroup.Gender ||
             e.Athlete.YearOfBirth < (ageGroup.BirthYearMin ?? 0) ||
             e.Athlete.YearOfBirth > (ageGroup.BirthYearMax ?? int.MaxValue)));
        foreach (var entry in eventEntries)
        {
            entry.Status = EntryStatus.ENTRY;
            entry.SwimEventId = null;
        }

        var entries = dbContext.Entries.Where(e =>
            e.Status == EntryStatus.ENTRY &&
            e.SwimStyleId == swimEvent.SwimStyleId &&
            e.Athlete.Gender == ageGroup.Gender &&
            e.Athlete.YearOfBirth >= (ageGroup.BirthYearMin ?? 0) &&
            e.Athlete.YearOfBirth <= (ageGroup.BirthYearMax ?? int.MaxValue));
        foreach (var entry in entries)
        {
            entry.SwimEventId = swimEvent.Id;
            entry.Status = EntryStatus.EVENT;
        }

        dbContext.SaveChanges();
    }
}