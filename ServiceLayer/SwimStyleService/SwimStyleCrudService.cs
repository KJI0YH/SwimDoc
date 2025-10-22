using DataLayer.EfClasses;

namespace ServiceLayer.SwimStyleService;

using Crud;

public class SwimStyleCrudService : EfCrudService<SwimStyle, int>
{
    public SwimStyleCrudService(DataLayer.EfCore.EfCoreContext db) : base(db)
    {
    }
}
