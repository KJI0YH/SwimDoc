using DataLayer.EfClasses;
using DataLayer.EfCore;
using Microsoft.EntityFrameworkCore;

namespace BizDbAccess;

public interface IReportGeneratorDbAccess
{
    ICollection<SwimEvent> GetSwimEventsWithEntries(List<int> ids);

    ICollection<SwimEvent> GetSwimEventsWithHeats(List<int> ids);

    ICollection<SwimEvent> GetSwimEventsWithResults(List<int> ids);
}

public class ReportGeneratorDbAccess(EfCoreContext context) : IReportGeneratorDbAccess
{
    public ICollection<SwimEvent> GetSwimEventsWithEntries(List<int> ids)
    {
        return context.SwimEvents
            .AsNoTracking()
            .Where(swimEvent => ids.Contains(swimEvent.Id))
            .Include(swimEvent => swimEvent.AgeGroup)
            .Include(swimEvent => swimEvent.SwimStyle)
            .Include(swimEvent => swimEvent.Entries.OrderBy(entry => entry.EntryTime ?? int.MaxValue))
            .ThenInclude(entry => entry.Athlete)
            .ThenInclude(athlete => athlete.Club)
            .Include(swimEvent => swimEvent.Entries)
            .ThenInclude(entry => entry.Relay!)
            .ThenInclude(relay => relay.Club)
            .Include(swimEvent => swimEvent.Entries)
            .ThenInclude(entry => entry.Relay!)
            .ThenInclude(relay => relay.Positions)
            .ThenInclude(position => position.Athlete)
            .OrderBy(swimEvent => swimEvent.Order)
            .ToList();
    }

    public ICollection<SwimEvent> GetSwimEventsWithHeats(List<int> ids)
    {
        return context.SwimEvents
            .AsNoTracking()
            .Where(swimEvent => ids.Contains(swimEvent.Id))
            .Include(swimEvent => swimEvent.AgeGroup)
            .Include(swimEvent => swimEvent.SwimStyle)
            .Include(swimEvent => swimEvent.Heats.OrderBy(heat => heat.Order))
            .ThenInclude(heat => heat.Positions.OrderBy(position => position.Lane))
            .ThenInclude(position => position.Entry)
            .ThenInclude(entry => entry.Athlete)
            .ThenInclude(athlete => athlete.Club)
            .Include(swimEvent => swimEvent.Heats)
            .ThenInclude(heat => heat.Positions)
            .ThenInclude(position => position.Entry)
            .ThenInclude(entry => entry.Relay!)
            .ThenInclude(relay => relay.Club)
            .Include(swimEvent => swimEvent.Heats)
            .ThenInclude(heat => heat.Positions)
            .ThenInclude(position => position.Entry)
            .ThenInclude(entry => entry.Relay!)
            .ThenInclude(relay => relay.Positions)
            .ThenInclude(relayPosition => relayPosition.Athlete)
            .OrderBy(swimEvent => swimEvent.Order)
            .ToList();
    }

    public ICollection<SwimEvent> GetSwimEventsWithResults(List<int> ids)
    {
        return context.SwimEvents
            .AsNoTracking()
            .Where(swimEvent => ids.Contains(swimEvent.Id))
            .Include(swimEvent => swimEvent.AgeGroup)
            .Include(swimEvent => swimEvent.SwimStyle)
            .Include(swimEvent => swimEvent.Entries.OrderBy(entry => entry.FinishTime ?? int.MaxValue))
            .ThenInclude(entry => entry.Athlete)
            .ThenInclude(athlete => athlete.Club)
            .Include(swimEvent => swimEvent.Entries)
            .ThenInclude(entry => entry.Relay!)
            .ThenInclude(relay => relay.Club)
            .Include(swimEvent => swimEvent.Entries)
            .ThenInclude(entry => entry.Relay!)
            .ThenInclude(relay => relay.Positions)
            .ThenInclude(position => position.Athlete)
            .OrderBy(swimEvent => swimEvent.Order)
            .ToList();
    }
}