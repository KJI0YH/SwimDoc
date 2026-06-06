using BizDbAccess.Helpers;
using DataLayer.EfClasses;
using DataLayer.EfCore;

namespace BizDbAccess;

public class EntryDocumentReaderDbAccess(EfCoreContext context) : IEntryDocumentReaderDbAccess
{
    public Club GetOrAddClub(string name)
    {
        return context.GetOrAdd(club => club.Name == name, () => new Club { Name = name });
    }

    public Athlete GetOrAddAthlete(string firstName, string lastName, int yearOfBirth, Gender gender, Category category)
    {
        return context.GetOrAdd(athlete =>
                athlete.FirstName == firstName &&
                athlete.LastName == lastName &&
                athlete.YearOfBirth == yearOfBirth &&
                athlete.Gender == gender &&
                athlete.Category == category,
            () => new Athlete
            {
                FirstName = firstName, LastName = lastName, YearOfBirth = yearOfBirth, Gender = gender,
                Category = category
            });
    }

    public Entry GetOrAddEntry(Athlete athlete, SwimStyle swimStyle, bool scoring, int? entryTime)
    {
        var entry = context.GetOrAdd(entry =>
                entry.Athlete == athlete &&
                entry.SwimStyle == swimStyle,
            () => new Entry { Athlete = athlete, SwimStyle = swimStyle, Scoring = scoring, EntryTime = entryTime });
        var swimEvent = GetSwimEventByParameters(swimStyle, athlete.YearOfBirth, athlete.Gender);
        entry.SwimEvent = swimEvent;
        entry.Scoring = scoring;
        entry.EntryTime = entryTime;

        context.NormalizeEntry(entry);

        return entry;
    }

    public SwimEvent? GetSwimEventByParameters(SwimStyle swimStyle, int birthYear, Gender gender)
    {
        return context.SwimEvents.FirstOrDefault(swimEvent =>
            swimEvent.AgeGroup.Gender == gender &&
            (swimEvent.AgeGroup.BirthYearMin ?? 0) <= birthYear &&
            (swimEvent.AgeGroup.BirthYearMax ?? int.MaxValue) >= birthYear &&
            swimEvent.SwimStyle == swimStyle);
    }

    public SwimStyle GetOrAddIndividualSwimStyleByParameters(int distance, Stroke stroke)
    {
        return context.GetOrAdd(swimStyle =>
                swimStyle.Distance == distance &&
                swimStyle.Stroke == stroke &&
                swimStyle.RelayCount == 0,
            () => new SwimStyle { Distance = distance, Stroke = stroke }
        );
    }
}
