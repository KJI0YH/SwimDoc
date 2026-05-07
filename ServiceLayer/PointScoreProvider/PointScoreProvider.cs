using DataLayer.EfClasses;
using ServiceLayer.BaseTimeRepository;

namespace ServiceLayer.PointScoreProvider;

public sealed class PointScoreProvider(IBaseTimeRepository baseTimeRepository) : IPointScoreProvider
{
    public int? CalculatePoints(
        Course course,
        int meters,
        Stroke stroke,
        int relayCount,
        Gender sex,
        int finishTimeHundredths,
        bool scoring)
    {
        if (!scoring)
            return null;

        if (finishTimeHundredths <= 0 || meters <= 0 || relayCount < 0)
            return null;

        var baseTimeHundredths = baseTimeRepository.GetBaseTimeHundredths(course, meters, stroke, relayCount, sex);
        if (baseTimeHundredths <= 0)
            return null;

        var ratio = (double)baseTimeHundredths / finishTimeHundredths;
        var points = 1000d * Math.Pow(ratio, 3);
        if (double.IsNaN(points) || double.IsInfinity(points))
            return null;

        var rounded = (int)Math.Round(points, MidpointRounding.AwayFromZero);
        return Math.Clamp(rounded, 0, 2000);
    }
}

