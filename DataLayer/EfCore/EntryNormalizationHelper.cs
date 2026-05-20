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
}
