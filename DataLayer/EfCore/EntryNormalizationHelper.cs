using DataLayer.EfClasses;
using Microsoft.EntityFrameworkCore;

namespace DataLayer.EfCore;

public static class EntryNormalizationHelper
{
    public static Entry NormalizeEntry(this EfCoreContext dbContext, Entry entry)
    {
        var swimEvent = entry.SwimEvent ??
                        dbContext.SwimEvents.AsNoTracking().FirstOrDefault(se => se.Id == entry.SwimEventId);

        if (swimEvent is not null)
            entry.SwimStyleId = swimEvent.SwimStyleId;

        var state = dbContext.Entry(entry).State;
        if (state == EntityState.Added || entry.Id == 0)
            entry.Status = swimEvent is null ? EntryStatus.ENTRY : EntryStatus.EVENT;

        return entry;
    }

    /// <summary>
    /// DSQ, DNS и DNF не имеют финишного времени и очков.
    /// </summary>
    public static Entry ApplyNonFinishResultRules(this Entry entry)
    {
        if (entry.Status is not (EntryStatus.DSQ or EntryStatus.DNS or EntryStatus.DNF))
            return entry;

        entry.FinishTime = null;
        entry.Points = 0;
        return entry;
    }

    public static void ClearHeatResultData(this Entry entry)
    {
        entry.FinishTime = null;
        entry.Points = null;
    }
}
