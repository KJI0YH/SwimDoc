using BizLogic.ReportGenerator;
using DataLayer.Display;
using DataLayer.EfClasses;
using ServiceLayer.RankTimeRepository;

namespace ServiceLayer.RankTimeProvider;

public interface IAchievedRankProvider : IAchievedRankFormatter
{
    Category? Resolve(SwimEvent swimEvent, Entry entry);
}

public sealed class AchievedRankProvider(IRankTimeRepository rankTimeRepository) : IAchievedRankProvider
{
    public string Format(SwimEvent swimEvent, Entry entry) =>
        CategoryDisplay.Format(Resolve(swimEvent, entry));

    public string FormatRequirements(SwimEvent swimEvent)
    {
        ArgumentNullException.ThrowIfNull(swimEvent);
        var swimStyle = swimEvent.SwimStyle;
        if (swimStyle is null || swimStyle.IsRelay)
            return string.Empty;
        var gender = swimEvent.AgeGroup?.Gender ?? Gender.Mixed;
        if (gender == Gender.Mixed)
            return string.Empty;
        var norms = rankTimeRepository.GetRankTimes(
            swimEvent.Course,
            swimStyle.Distance,
            swimStyle.Stroke,
            swimStyle.RelayCount,
            gender);
        return CategoryRequirementsDisplay.FormatLine(norms);
    }

    public Category? Resolve(SwimEvent swimEvent, Entry entry)
    {
        ArgumentNullException.ThrowIfNull(swimEvent);
        ArgumentNullException.ThrowIfNull(entry);
        if (entry.Status != EntryStatus.FINISH || entry.FinishTime is null or <= 0)
            return null;
        var swimStyle = entry.SwimStyle ?? swimEvent.SwimStyle;
        if (swimStyle is null || swimStyle.IsRelay)
            return null;
        var gender = ResolveGender(swimEvent, entry);
        var norms = rankTimeRepository.GetRankTimes(
            swimEvent.Course,
            swimStyle.Distance,
            swimStyle.Stroke,
            swimStyle.RelayCount,
            gender);
        return CategoryClassifier.Classify(entry.FinishTime.Value, norms);
    }

    private static Gender ResolveGender(SwimEvent swimEvent, Entry entry)
    {
        var gender = swimEvent.AgeGroup?.Gender ?? Gender.Mixed;
        if (gender != Gender.Mixed)
            return gender;
        return entry.Athlete?.Gender ?? Gender.Mixed;
    }
}
