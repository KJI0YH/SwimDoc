using DataLayer.EfClasses;

namespace DataLayer.Display;

public static class AgeGroupParticipantCounter
{
    public static int Count(AgeGroup ageGroup, IEnumerable<Athlete> athletes) =>
        athletes.Count(athlete => ageGroup.Contains(athlete.YearOfBirth, athlete.Gender));

    public static int Count(
        int? birthYearMin,
        int? birthYearMax,
        Gender gender,
        IEnumerable<(int YearOfBirth, Gender Gender)> athletes) =>
        athletes.Count(athlete =>
            athlete.YearOfBirth >= (birthYearMin ?? 0) &&
            athlete.YearOfBirth <= (birthYearMax ?? int.MaxValue) &&
            (gender == Gender.Mixed || athlete.Gender == gender));
}
