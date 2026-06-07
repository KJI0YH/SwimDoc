using DataLayer.EfClasses;

namespace BizDbAccess.ReportGenerator;

public interface IReportGeneratorDbAccess
{
    ICollection<SwimEvent> GetSwimEventsWithEntries(List<int> ids);

    ICollection<SwimEvent> GetSwimEventsWithHeats(List<int> ids);

    ICollection<SwimEvent> GetSwimEventsWithResults(List<int> ids);
}
