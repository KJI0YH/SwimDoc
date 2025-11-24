using DataLayer.EfClasses;
using DataLayer.EfCore;
using ServiceLayer.Crud;

namespace ServiceLayer.EventService;

public class EventService(EfCoreContext dbContext) : CrudService<SwimEvent, int>(dbContext);