using DataLayer.EfClasses;
using DataLayer.EfCore;
using ServiceLayer.Crud;
using ServiceLayer.Logging;

namespace ServiceLayer.AgeGroupService;

public class AgeGroupService(EfCoreContext dbContext, IAppLog log) : CrudService<AgeGroup, int?>(dbContext, log), IAgeGroupService;
