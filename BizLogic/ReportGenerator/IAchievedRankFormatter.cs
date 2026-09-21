using DataLayer.EfClasses;

namespace BizLogic.ReportGenerator;

public interface IAchievedRankFormatter
{
    string Format(SwimEvent swimEvent, Entry entry);
    string FormatRequirements(SwimEvent swimEvent);
}
