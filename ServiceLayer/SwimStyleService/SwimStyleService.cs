using DataLayer.EfClasses;
using DataLayer.EfCore;
using ServiceLayer.Crud;
using ServiceLayer.Logging;

namespace ServiceLayer.SwimStyleService;

public class SwimStyleService(EfCoreContext dbContext, IAppLog log) : CrudService<SwimStyle, int?>(dbContext, log), ISwimStyleService;
