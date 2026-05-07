using DataLayer.EfClasses;

namespace ServiceLayer.BaseTimeRepository;

public interface IBaseTimeRepository
{
    int GetBaseTimeHundredths(Course course, int meters, Stroke stroke, int relayCount, Gender sex);

    void SetBaseTimeHundredths(Course course, int meters, Stroke stroke, int relayCount, Gender sex, int baseTimeHundredths);

    void Save();
}

