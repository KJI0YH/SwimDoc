using DataLayer.EfClasses;

namespace ServiceLayer.EntryService;

using Crud;

public class EntryCrudService : EfCrudService<Entry, int>
{
    public EntryCrudService(DataLayer.EfCore.EfCoreContext db) : base(db)
    {
    }
}
