using DataLayer.EfClasses;
using ServiceLayer.Crud;

namespace ServiceLayer.AgeGroupService;

public interface IAgeGroupService : ICrudService<AgeGroup, int>
{
    
}