using DataLayer.EfClasses;

namespace BizLogic.ReportGenerator;

public sealed class NullAchievedRankFormatter : IAchievedRankFormatter
{
    public static NullAchievedRankFormatter Instance { get; } = new();

    public string Format(SwimEvent swimEvent, Entry entry) => string.Empty;

    public string FormatRequirements(SwimEvent swimEvent) => string.Empty;
}
