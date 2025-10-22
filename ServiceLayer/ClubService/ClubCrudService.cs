using DataLayer.EfClasses;

namespace ServiceLayer.ClubService;

using Crud;

public class ClubCrudService : EfCrudService<Club, int>
{
    public ClubCrudService(DataLayer.EfCore.EfCoreContext db) : base(db)
    {
    }
}
