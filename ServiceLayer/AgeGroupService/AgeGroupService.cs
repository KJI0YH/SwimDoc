using DataLayer.EfClasses;
using DataLayer.EfCore;
using ServiceLayer.Crud;

namespace ServiceLayer.AgeGroupService;

public class AgeGroupService(EfCoreContext dbContext) : CrudService<AgeGroup, int>(dbContext);