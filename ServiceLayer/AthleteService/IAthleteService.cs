using DataLayer.EfClasses;
using ServiceLayer.Crud;

namespace ServiceLayer.AthleteService;

public interface IAthleteService : ICrudService<Athlete, int?>
{
    Task<List<Athlete>> GetAthletesByClubIdAsync(int clubId);
}