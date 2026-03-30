using BizLogic.EntryDocumentReaderLogic;

namespace ServiceLayer.EntryDocumentReaderService;

public interface IEntryDocumentReaderService
{
    public IReadOnlyList<EntryDocument> Read(string filePath);

    public (IReadOnlyList<EntryDocument> documents, EntryImportStats stats) ReadWithStats(string filePath);
}

public readonly record struct EntryImportStats(
    int ClubsAdded,
    int ClubsUpdated,
    int AthletesAdded,
    int AthletesUpdated,
    int EntriesAdded,
    int EntriesUpdated);