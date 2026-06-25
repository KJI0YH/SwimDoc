using DataLayer.EfClasses;
using DataLayer.EfCore;
using Microsoft.EntityFrameworkCore;
using ServiceLayer.Crud;
using ServiceLayer.Logging;

namespace ServiceLayer.AthleteService;

public class AthleteService(EfCoreContext dbContext, IAppLog log) : CrudService<Athlete, int?>(dbContext, log), IAthleteService
{
    public async Task<List<Athlete>> GetAthletesByClubIdAsync(int clubId)
    {
        var athletes = await dbContext.Athletes
            .AsNoTracking()
            .Where(a => a.ClubId == clubId)
            .ToListAsync();
        log.Info($"Read Athletes by ClubId={clubId}: {athletes.Count} items");
        return athletes;
    }
}
