using DataLayer.EfClasses;
using ServiceLayer.Crud;

namespace ServiceLayer.EntryService;

public interface IEntryService : ICrudService<Entry, int?>
{
    Task<List<Entry>> GetEntriesByEventIdOrderByFinishTimeAsync(int eventId);
}