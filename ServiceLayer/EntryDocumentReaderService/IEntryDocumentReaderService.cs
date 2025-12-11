using BizLogic.EntryDocumentReaderLogic;

namespace ServiceLayer.EntryDocumentReaderService;

public interface IEntryDocumentReaderService
{
    public IReadOnlyList<EntryDocument> Read(string filePath);
}