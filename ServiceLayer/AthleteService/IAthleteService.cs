using DataLayer.EfClasses;
using ServiceLayer.Crud;

namespace ServiceLayer.AthleteService;

public interface IAthleteService : ICrudService<Athlete, int>
{
    
}