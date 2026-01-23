using DataLayer.EfClasses;
using ServiceLayer.Crud;

namespace ServiceLayer.ClubService;

public interface IClubService : ICrudService<Club, int?>
{
    
}