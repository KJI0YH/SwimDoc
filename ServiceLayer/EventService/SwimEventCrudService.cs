using DataLayer.EfClasses;

namespace ServiceLayer.EventService;

using Crud;

public class SwimEventCrudService : EfCrudService<SwimEvent, int>
{
    public SwimEventCrudService(DataLayer.EfCore.EfCoreContext db) : base(db)
    {
    }
}
