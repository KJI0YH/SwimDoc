using DataLayer.EfClasses;
using ServiceLayer.BaseTimeRepository;

namespace ServiceLayer.PointScoreProvider;

public sealed class PointScoreProvider(IBaseTimeRepository baseTimeRepository) : IPointScoreProvider
{
    public int CalculatePoints(
        Course course,
        int meters,
        Stroke stroke,
        int relayCount,
        Gender sex,
        int? finishTime)
    {
        if (finishTime is null or <= 0) return 0;
        var baseTime = GetScoringBaseTime(course, meters, stroke, relayCount, sex);
        if (baseTime <= 0) return 0;
        return (int)Math.Truncate(1000 * Math.Pow(baseTime / (double)finishTime, 3));
    }

    private int GetScoringBaseTime(Course course, int meters, Stroke stroke, int relayCount, Gender sex)
    {
        var baseTime = baseTimeRepository.GetBaseTime(course, meters, stroke, relayCount, sex);
        if (baseTime > 0 || relayCount == 0)
            return baseTime;
        if (sex != Gender.Mixed)
            return 0;
        baseTime = baseTimeRepository.GetBaseTime(course, meters, stroke, relayCount, Gender.Male);
        if (baseTime > 0)
            return baseTime;
        return baseTimeRepository.GetBaseTime(course, meters, stroke, relayCount, Gender.Female);
    }
}
