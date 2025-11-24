using BizDbAccess.Helpers;
using DataLayer.EfClasses;
using DataLayer.EfCore;
using Microsoft.EntityFrameworkCore;

namespace BizDbAccess;

public interface IEntryDocumentReaderDbAccess
{
    void AddClub(Club club);
    void AddAthleteRange(ICollection<Athlete> athletes);
    SwimEvent? GetSwimEventByParameters(SwimStyle swimStyle, int birthYear, Gender gender);
    SwimStyle GetOrAddIndividualSwimStyleByParameters(int distance, Stroke stroke);
}

public class EntryDocumentReaderDbAccess(EfCoreContext context) : IEntryDocumentReaderDbAccess
{
    public void AddClub(Club club)
    {
        context.Clubs.Add(club);
    }

    public void AddAthleteRange(ICollection<Athlete> athletes)
    {
        context.Athletes.AddRange(athletes);
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