using DataLayer.EfClasses;

namespace ServiceLayer.AthleteService;

using Crud;

public class AthleteCrudService : EfCrudService<Athlete, int>
{
    public AthleteCrudService(DataLayer.EfCore.EfCoreContext db) : base(db)
    {
    }
}
