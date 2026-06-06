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
