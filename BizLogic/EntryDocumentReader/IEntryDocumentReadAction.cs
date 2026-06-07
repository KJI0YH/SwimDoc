using BizLogic.GenericInterfaces;

namespace BizLogic.EntryDocumentReader;

public interface IEntryDocumentReadAction : IBizAction<string, IReadOnlyList<EntryDocument>>
{
}
