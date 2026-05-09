using DataLayer.EfClasses;

namespace ServiceLayer.PointScoreProvider;

public interface IPointScoreProvider
{
    int CalculatePoints(
        Course course,
        int meters,
        Stroke stroke,
        int relayCount,
        Gender sex,
        int? finishTimeHundredths);
}

