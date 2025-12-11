using DataLayer.EfClasses;
using DataLayer.EfCore;
using ServiceLayer.Crud;

namespace ServiceLayer.SwimStyleService;

public class SwimStyleService(EfCoreContext dbContext) : CrudService<SwimStyle, int>(dbContext), ISwimStyleService;