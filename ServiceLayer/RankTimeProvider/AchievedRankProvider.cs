using BizLogic.ReportGenerator;
using DataLayer.Display;
using DataLayer.EfClasses;
using ServiceLayer.RankTimeRepository;

namespace ServiceLayer.RankTimeProvider;

public interface IAchievedRankProvider : IAchievedRankFormatter
{
    Category? Resolve(SwimEvent swimEvent, Entry entry);

    bool MeetsCategory(SwimEvent swimEvent, Entry entry, Category minimum);
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
        var norms = GetNorms(swimEvent, entry);
        return norms.Count == 0 ? null : CategoryClassifier.Classify(entry.FinishTime.Value, norms);
    }

    public bool MeetsCategory(SwimEvent swimEvent, Entry entry, Category minimum)
    {
        ArgumentNullException.ThrowIfNull(swimEvent);
        ArgumentNullException.ThrowIfNull(entry);
        if (minimum == Category.NoCategory)
            return true;
        if (entry.Status != EntryStatus.FINISH || entry.FinishTime is null or <= 0)
            return false;
        var norms = GetNorms(swimEvent, entry);
        if (norms.Count == 0)
            return true;
        var achieved = CategoryClassifier.Classify(entry.FinishTime.Value, norms);
        if (achieved is null)
            return false;
        return Array.IndexOf(CategoryDisplay.FromHighest, achieved.Value) <=
               Array.IndexOf(CategoryDisplay.FromHighest, minimum);
    }

    private IReadOnlyDictionary<Category, int> GetNorms(SwimEvent swimEvent, Entry entry)
    {
        var swimStyle = entry.SwimStyle ?? swimEvent.SwimStyle;
        if (swimStyle is null || swimStyle.IsRelay)
            return EmptyNorms;
        var gender = ResolveGender(swimEvent, entry);
        return rankTimeRepository.GetRankTimes(
            swimEvent.Course,
            swimStyle.Distance,
            swimStyle.Stroke,
            swimStyle.RelayCount,
            gender);
    }

    private static readonly IReadOnlyDictionary<Category, int> EmptyNorms = new Dictionary<Category, int>();

    private static Gender ResolveGender(SwimEvent swimEvent, Entry entry)
    {
        var gender = swimEvent.AgeGroup?.Gender ?? Gender.Mixed;
        if (gender != Gender.Mixed)
            return gender;
        return entry.Athlete?.Gender ?? Gender.Mixed;
    }
}
