using BizLogic.EntryDocumentReaderLogic;

namespace ServiceLayer.EntryDocumentReaderService;

public interface IEntryDocumentReaderService
{
    public IReadOnlyList<EntryDocument> Read(string filePath);

    public (IReadOnlyList<EntryDocument> documents, EntryImportStats stats) ReadWithStats(
        string filePath,
        CancellationToken cancellationToken = default,
        bool saveChanges = true);
}

public readonly record struct EntryImportStats(
    int ClubsScanned,
    int ClubsWithErrors,
    int ClubsAdded,
    int ClubsUpdated,
    int AthletesScanned,
    int AthletesWithErrors,
    int AthletesAdded,
    int AthletesUpdated,
    int EntriesScanned,
    int EntriesWithErrors,
    int EntriesAdded,
    int EntriesUpdated);