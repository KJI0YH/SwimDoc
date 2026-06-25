using DataLayer.EfClasses;
using DataLayer.EfCore;
using ServiceLayer.Crud;
using ServiceLayer.Logging;

namespace ServiceLayer.ClubService;

public class ClubService(EfCoreContext dbContext, IAppLog log) : CrudService<Club, int?>(dbContext, log), IClubService;
