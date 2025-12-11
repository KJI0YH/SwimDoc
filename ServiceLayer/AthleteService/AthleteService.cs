using DataLayer.EfClasses;
using DataLayer.EfCore;
using ServiceLayer.Crud;

namespace ServiceLayer.AthleteService;

public class AthleteService(EfCoreContext dbContext) : CrudService<Athlete, int>(dbContext), IAthleteService;