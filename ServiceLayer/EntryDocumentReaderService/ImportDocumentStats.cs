using BizLogic.EntryDocumentReaderLogic;
using DataLayer.EfClasses;

namespace ServiceLayer.EntryDocumentReaderService;

internal static class ImportDocumentStats
{
    public static void CountScanned(
        IReadOnlyList<EntryDocument> documents,
        out int clubsScanned,
        out int clubsWithErrors,
        out int athletesScanned,
        out int athletesWithErrors,
        out int entriesScanned,
        out int entriesWithErrors)
    {
        clubsScanned = 0;
        clubsWithErrors = 0;
        athletesScanned = 0;
        athletesWithErrors = 0;
        entriesScanned = 0;
        entriesWithErrors = 0;

        foreach (var document in documents)
        {
            var hasErrors = document.Errors.Count > 0;
            ICollection<Athlete>? athletes = document.Athletes;
            if (document.Club is not null)
            {
                clubsScanned++;
                if (hasErrors)
                    clubsWithErrors++;
                athletes ??= document.Club.Athletes;
            }

            if (athletes is null)
                continue;

            var athleteCount = athletes.Count;
            var entryCount = athletes.Sum(a => a.Entries?.Count ?? 0);
            athletesScanned += athleteCount;
            entriesScanned += entryCount;
            if (!hasErrors)
                continue;

            athletesWithErrors += athleteCount;
            entriesWithErrors += entryCount;
        }
    }
}
