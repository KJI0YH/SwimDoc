using DataLayer.EfClasses;

namespace ServiceLayer.BaseTimeRepository;

public interface IBaseTimeRepository
{
    int GetBaseTime(Course course, int meters, Stroke stroke, int relayCount, Gender sex);

    void SetBaseTime(Course course, int meters, Stroke stroke, int relayCount, Gender sex, int baseTimeHundredths);

    void Save();
}

