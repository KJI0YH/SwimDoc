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
