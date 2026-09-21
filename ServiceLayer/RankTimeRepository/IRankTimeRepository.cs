using DataLayer.EfClasses;

namespace ServiceLayer.RankTimeRepository;

public interface IRankTimeRepository
{
    int GetRankTime(Course course, int meters, Stroke stroke, int relayCount, Gender sex, Category category);
    void SetRankTime(Course course, int meters, Stroke stroke, int relayCount, Gender sex, Category category, int timeHundredths);
    IReadOnlyDictionary<Category, int> GetRankTimes(Course course, int meters, Stroke stroke, int relayCount, Gender sex);
    void Save();
}
