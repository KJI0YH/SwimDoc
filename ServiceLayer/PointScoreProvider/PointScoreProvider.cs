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
        var baseTime = baseTimeRepository.GetBaseTime(course, meters, stroke, relayCount, sex);
        return (int)Math.Truncate(1000 * Math.Pow(baseTime / (double)finishTime, 3));
    }
}

