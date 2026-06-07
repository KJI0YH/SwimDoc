using BizLogic.GenericInterfaces;
using DataLayer.EfClasses;

namespace BizLogic.EntryDocumentReaderLogic;

public interface IEntryDocumentReadAction : IBizAction<string, IReadOnlyList<EntryDocument>>
{
}
