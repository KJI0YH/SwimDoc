using DataLayer.EfClasses;

namespace BizDbAccess.EntryDocumentReader;

public interface IEntryDocumentReaderDbAccess
{
    Club GetOrAddClub(string name);
    Athlete GetOrAddAthlete(string firstName, string lastName, int yearOfBirth, Gender gender, Category category);
    Entry GetOrAddEntry(Athlete athlete, SwimStyle swimStyle, bool scoring, int? entryTime);
    SwimEvent? GetSwimEventByParameters(SwimStyle swimStyle, int birthYear, Gender gender);
    SwimStyle GetOrAddIndividualSwimStyleByParameters(int distance, Stroke stroke);
}
