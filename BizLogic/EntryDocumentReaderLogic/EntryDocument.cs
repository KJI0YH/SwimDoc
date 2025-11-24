using DataLayer.EfClasses;

namespace BizLogic.EntryDocumentReaderLogic;

public class EntryDocument
{
    public readonly Club? Club;
    public readonly ICollection<Athlete>? Athletes;
    public IReadOnlyList<string> Warnings;
    public IReadOnlyList<string> Errors;

    private EntryDocument(Club? club, ICollection<Athlete>? athletes, IReadOnlyList<string> warnings, IReadOnlyList<string> errors)
    {
        Club = club;
        Athletes = athletes;
        Warnings = warnings;
        Errors = errors;
    }

    public static EntryDocument OfError(IReadOnlyList<string> warnings, IReadOnlyList<string> errors)
    {
        return new EntryDocument(null, null, warnings, errors);
    }

    public static EntryDocument OfClub(Club club, IReadOnlyList<string> warnings, IReadOnlyList<string> errors)
    {
        return new EntryDocument(club, club.Athletes, warnings, errors);
    }

    public static EntryDocument OfAthletes(ICollection<Athlete> athletes, IReadOnlyList<string> warnings, IReadOnlyList<string> errors)
    {
        return new EntryDocument(null, athletes, warnings, errors);
    }
}