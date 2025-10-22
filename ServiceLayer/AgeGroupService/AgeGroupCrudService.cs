using DataLayer.EfClasses;

namespace ServiceLayer.AgeGroupService;

using Crud;

public class AgeGroupCrudService : EfCrudService<AgeGroup, int>
{
    public AgeGroupCrudService(DataLayer.EfCore.EfCoreContext db) : base(db)
    {
    }
}
