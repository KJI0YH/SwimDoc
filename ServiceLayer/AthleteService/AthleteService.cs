using DataLayer.EfClasses;
using DataLayer.EfCore;
using Microsoft.EntityFrameworkCore;
using ServiceLayer.Crud;

namespace ServiceLayer.AthleteService;

public class AthleteService(EfCoreContext dbContext) : CrudService<Athlete, int?>(dbContext), IAthleteService
{
    public Task<List<Athlete>> GetAthletesByClubIdAsync(int clubId)
    {
        return dbContext.Athletes
            .AsNoTracking()
            .Where(a => a.ClubId == clubId)
            .ToListAsync();
    }
}
