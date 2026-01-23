using DataLayer.EfClasses;
using DataLayer.EfCore;
using ServiceLayer.Crud;

namespace ServiceLayer.ClubService;

public class ClubService(EfCoreContext dbContext) : CrudService<Club, int?>(dbContext), IClubService;